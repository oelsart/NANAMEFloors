Shader "Custom/Terrain water polluted blend" {
    Properties {
        _MainTex ("Main texture", 2D) = "white" {}
        _RippleTex ("Ripple texture", 2D) = "white" {}
        _BurnTex ("Burn texture", 2D) = "white" {}
        _BurnColor ("BurnColor", Color) = (1,1,1,1)
        _Color ("Color", Color) = (1,1,1,1)
        _AlphaAddTex ("Alpha add texture", 2D) = "white" {}
        _MaskTex ("Mask texture", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-600" }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            #include "RotationUtil.cginc"
            
            struct v2f
            {
                float4 position : SV_POSITION0;
                float2 texcoord : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                float rotation : TEXCOORD2;
            };
            
            struct fout
            {
                float4 sv_target : SV_Target0;
            };

			float4x4 _MainCameraVP;
            float4 _MainCameraScreenParams, _Color, _WaterCastVectSun, _WaterCastVectMoon;
            float _LightsourceShineSizeReduction, _LightsourceShineIntensity, _GameSeconds, _DayPercent;
            float4 _BurnColor, _BurnScale;
            float2 _ScrollSpeed;
            
            uniform sampler2D _WaterOutputTex;
            sampler2D _MainTex, _AlphaAddTex, _NoiseTex, _WaterReflectionTex, _BurnTex, _MaskTex;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.texcoord1 = mul(_MainCameraVP, v.vertex);
                o.texcoord.xy = v.vertex.xz * 0.0625;
                o.rotation = v.color.r * 255.0;
                return o;
            }
            
            fout frag(v2f inp)
            {
                fout o;
			    float2 screenUV = inp.texcoord1.xy * 0.5 + 0.5;
			    
			    float slope = length(float2(ddx(inp.texcoord.x), ddy(inp.texcoord.y)));
			    
			    float2 screenStep = 0.5 / _MainCameraScreenParams.xy;
			    float2 baseUV = screenUV; 
			    
			    float hL = tex2D(_WaterOutputTex, baseUV + float2(-screenStep.x,  screenStep.y)).x - 0.5;
			    float hR = tex2D(_WaterOutputTex, baseUV + float2( screenStep.x,  screenStep.y)).x - 0.5;
			    float hU = tex2D(_WaterOutputTex, baseUV + float2(-screenStep.x, -screenStep.y)).x - 0.5;
			    float hD = tex2D(_WaterOutputTex, baseUV + float2( screenStep.x, -screenStep.y)).x - 0.5;

			    float3 normal = normalize(float3(hR + hD - hL - hU, hR + hL - hU - hD, slope * 500.0));
			    
                float3 moonDir = normalize(float3(-_WaterCastVectMoon.xz, -50.0));
                float3 sunDir = normalize(float3(-_WaterCastVectSun.xz, -50.0));
                
                float moonDot = dot(-moonDir, normal);
                float moonSpec;
                if (moonDot < -moonDir.z) {
                    moonSpec = 0.5 * pow(moonDot / -moonDir.z, 100.0);
                } else {
                    float moonSpecV = 1.0 - (moonDot + moonDir.z) / (moonDir.z + 1.0);
                    float moonSpecVPow = moonSpecV * moonSpecV;
                    moonSpecVPow *= moonSpecVPow;
                    moonSpec = (1.0 - moonSpecV * moonSpecVPow) * 0.5 + 0.5;
                }

                float sunDot = dot(-sunDir, normal);
                float sunSpec;
                if (sunDot < -sunDir.z) {
                    sunSpec = 0.5 * pow(sunDot / -sunDir.z, 100.0);
                } else {
                    float sunSpecV = 1.0 - (sunDot + sunDir.z) / (sunDir.z + 1.0);
                    float sunSpecVPow = sunSpecV * sunSpecV;
                    sunSpecVPow *= sunSpecVPow;
                    sunSpec = (1.0 - sunSpecV * sunSpecVPow) * 0.5 + 0.5;
                }

			    float maxDim = max(_MainCameraScreenParams.x, _MainCameraScreenParams.y);
                float3 viewDir = normalize(float3(-0.2 / maxDim * inp.texcoord1.xy * _MainCameraScreenParams.xy, -1.0));

			    float d = max(dot(reflect(-sunDir, normal), viewDir), 0);
                float sunShine = d > 0 ? pow(d, _LightsourceShineSizeReduction) * _LightsourceShineIntensity : 0;
			    float d2 = max(dot(reflect(-moonDir, normal), viewDir), 0);
                float moonShine = d2 > 0 ? pow(d2, _LightsourceShineSizeReduction) * _LightsourceShineIntensity : 0;

                float4 sunCol = tex2D(_MainTex, float2(sunSpec, sunShine));
                float4 moonCol = tex2D(_MainTex, float2(moonSpec, moonShine));
                float4 defaultCol = tex2D(_MainTex, float2(0, 0));

                float weight = _WaterCastVectMoon.w / (_WaterCastVectSun.w + _WaterCastVectMoon.w);
                float4 combinedLight = lerp(sunCol, moonCol, weight);
                float totalIntensity = length(float2(_WaterCastVectSun.w, _WaterCastVectMoon.w));
                float4 finalWaterCol = totalIntensity * (combinedLight - defaultCol) + defaultCol;
			    
                float aspectInv = _MainCameraScreenParams.y / _MainCameraScreenParams.x;
                float2 reflectionScreenUV = float2(screenUV.x, aspectInv * inp.texcoord1.y * 0.5 + 0.5);
                float2 reflectionUV = 0.009 * _GameSeconds + 0.6 * reflectionScreenUV + 0.15 * inp.texcoord.xy + 10.0 * normal.xz;
                float4 reflectionCol = tex2D(_WaterReflectionTex, reflectionUV);
                float reflectionStrength = _DayPercent <= 0.5
                    ? 0.02 + 0.04 * _DayPercent
                    : 0.06 - 0.04 * _DayPercent;
			    
                float3 litColor = finalWaterCol.rgb + reflectionStrength * (1.0 - 2.0 * reflectionCol.rgb);

			    float n1 = tex2D(_AlphaAddTex, inp.texcoord.xy * 2.0).r;
			    float n2 = tex2D(_AlphaAddTex, inp.texcoord.xy * 5.0).g;
			    float n3 = tex2D(_AlphaAddTex, inp.texcoord.xy * 10.0).b;
			    float avgNoise = (n1 + n2 + n3) * 0.333;

			    float vertexAlpha = finalWaterCol.a;
			    float combinedAlpha = (avgNoise - vertexAlpha) * 0.6 + vertexAlpha - 0.3;
			    float finalAlpha = clamp(combinedAlpha * 2.5, max(vertexAlpha * 1.5 - 0.5, 0.0), min(vertexAlpha * 1.5, 1.0));

			    float3 noise = tex2D(_NoiseTex, inp.texcoord.xy).rgb;
			    float3 detailRGB = (noise - 0.5) * 0.025 + litColor;
                
                float3 baseCol = detailRGB * _Color.rgb;
                float2 burnUV = inp.texcoord.xy * _BurnScale.xy + _ScrollSpeed * _GameSeconds;
                float4 burnTex = tex2D(_BurnTex, burnUV);
                float3 mBurn = lerp(0.5, burnTex.rgb, burnTex.a * _BurnColor.a);

                float3 softLightLow  = baseCol * baseCol + 2.0 * baseCol * mBurn * (1.0 - baseCol);
                float3 softLightHigh = 2.0 * baseCol * (1.0 - mBurn) + (2.0 * mBurn - 1.0) * sqrt(max(baseCol, 0.0));

                float3 finalRGB = mBurn < 0.5 ? softLightLow : softLightHigh;

                o.sv_target = float4(finalRGB * _Color.rgb, finalAlpha * _Color.a);
                o.sv_target.a *= tex2D(_MaskTex, RotateUV(frac(inp.texcoord.xy * 16.0), inp.rotation)).a;
                
                return o;
            }
            ENDCG
        }
    }
}
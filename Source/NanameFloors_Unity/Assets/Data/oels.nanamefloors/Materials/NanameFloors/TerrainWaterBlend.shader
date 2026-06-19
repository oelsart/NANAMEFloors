Shader "Custom/Terrain water blend" {
	Properties {
		_MainTex ("Main texture", 2D) = "white" {}
		_RippleTex ("Ripple texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_AlphaAddTex ("Alpha add texture", 2D) = "" {}
		_MaskTex ("Mask texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
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
			
			float4 _Color;
			float4 _WaterCastVectSun;
			float4 _WaterCastVectMoon;
			float _LightsourceShineSizeReduction;
			float _LightsourceShineIntensity;
			sampler2D _WaterOutputTex;
			sampler2D _MainTex;
			sampler2D _AlphaAddTex;
			sampler2D _NoiseTex;
			sampler2D _MaskTex;
			
			v2f vert(appdata_full v)
			{
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.texcoord1 = ComputeScreenPos(o.position);
                o.texcoord.xy = v.vertex.xz * 0.0625;
				o.rotation = v.color.r * 255.0;
                return o;
			}
			
			fout frag(v2f inp)
			{
				fout o;
			    float2 screenUV = inp.texcoord1.xy / inp.texcoord1.w;

			    float2 dUV = float2(ddx(inp.texcoord.x), ddy(inp.texcoord.y));
			    float slope = sqrt(dot(dUV, dUV));
			    
			    float2 screenStep = 0.5 / _ScreenParams.xy;
			    float2 baseUV = screenUV; 
			    
			    float hL = tex2D(_WaterOutputTex, baseUV + float2(-screenStep.x,  screenStep.y)).x - 0.5;
			    float hR = tex2D(_WaterOutputTex, baseUV + float2( screenStep.x,  screenStep.y)).x - 0.5;
			    float hU = tex2D(_WaterOutputTex, baseUV + float2(-screenStep.x, -screenStep.y)).x - 0.5;
			    float hD = tex2D(_WaterOutputTex, baseUV + float2( screenStep.x, -screenStep.y)).x - 0.5;

			    float3 normal = normalize(float3(hL + hU - (hR + hD), hL + hR - (hU + hD), slope * 500.0));

			    float3 moonDir = normalize(float3(-_WaterCastVectMoon.xz, -50.0));
			    float moonSpec = pow(1.0 - (dot(-moonDir, normal) + moonDir.z) / (moonDir.z + 1.0), 4.0) * 0.5 + 0.5;
			    
			    float3 sunDir = normalize(float3(-_WaterCastVectSun.xz, -50.0));
			    float sunSpec = pow(1.0 - (dot(-sunDir, normal) + sunDir.z) / (sunDir.z + 1.0), 4.0) * 0.5 + 0.5;

			    float2 centeredPos = (screenUV - 0.5) * 2.0;
			    float aspect = _ScreenParams.x / _ScreenParams.y;
			    float3 viewDir = normalize(float3(-0.2 * centeredPos * float2(aspect, 1.0), -1.0));
			    
			    float sunShine = exp(log(max(dot(sunDir, viewDir), 0.0)) * _LightsourceShineSizeReduction) * _LightsourceShineIntensity;
			    float moonShine = exp(log(max(dot(moonDir, viewDir), 0.0)) * _LightsourceShineSizeReduction) * _LightsourceShineIntensity;

			    float4 sunCol = tex2D(_MainTex, float2(sunSpec, sunShine));
			    float4 moonCol = tex2D(_MainTex, float2(moonSpec, moonShine));
			    float4 defaultCol = tex2D(_MainTex, float2(0, 0));

			    float weight = _WaterCastVectMoon.w / (_WaterCastVectSun.w + _WaterCastVectMoon.w);
			    float4 combinedLight = lerp(sunCol, moonCol, weight) - defaultCol;
			    float totalIntensity = length(_WaterCastVectSun.ww + _WaterCastVectMoon.ww);
			    float4 finalWaterCol = totalIntensity * combinedLight + defaultCol;

			    float n1 = tex2D(_AlphaAddTex, inp.texcoord.xy * 2.0).r;
			    float n2 = tex2D(_AlphaAddTex, inp.texcoord.xy * 5.0).g;
			    float n3 = tex2D(_AlphaAddTex, inp.texcoord.xy * 10.0).b;
			    float avgNoise = (n1 + n2 + n3) * 0.333;

			    float combinedAlpha = (avgNoise - finalWaterCol.a) * 0.6 + finalWaterCol.a - 0.3;
			    float finalAlpha = clamp(combinedAlpha * 2.5, max(finalWaterCol.a * 1.5 - 0.5, 0.0), min(finalWaterCol.a * 1.5, 1.0));

			    float3 noise = tex2D(_NoiseTex, inp.texcoord.xy).rgb;
			    float3 detailRGB = (noise - 0.5) * 0.025 + finalWaterCol.rgb;

			    o.sv_target = float4(detailRGB, finalAlpha) * _Color * _Color;
			    o.sv_target.a *= tex2D(_MaskTex, RotateUV(frac(inp.texcoord.xy * 16.0), inp.rotation)).a;
			    return o;
			}
			ENDCG
		}
	}
}
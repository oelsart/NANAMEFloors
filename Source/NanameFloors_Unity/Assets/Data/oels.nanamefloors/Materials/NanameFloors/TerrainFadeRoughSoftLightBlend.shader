Shader "Custom/Terrain fade rough Soft light blend" {
	Properties {
		_MainTex ("Main texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_PollutionTintColor ("PollutionTintColor", Color) = (1,1,1,1)
		_BurnTex ("Burn texture", 2D) = "white" {}
		_BurnColor ("BurnColor", Color) = (1,1,1,1)
		_AlphaAddTex ("Alpha add texture", 2D) = "" {}
		_MaskTex ("Mask texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType" = "Transparent"  "Queue" = "Transparent" }
		Pass {
			Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "RotationUtil.cginc"
			
			struct v2f
			{
				float4 position : SV_POSITION0;
				float2 texcoord : TEXCOORD0;
				float rotation : TEXCOORD1;
			};
			struct fout
			{
				float4 sv_target : SV_Target0;
			};
			
			float4 _Color;
			float4 _PollutionTintColor;
			float4 _BurnColor;
			float4 _BurnScale;
			float2 _ScrollSpeed;
			float _GameSeconds;
			sampler2D _MainTex;
			sampler2D _BurnTex;
			sampler2D _AlphaAddTex;
			sampler2D _MaskTex;
			
			v2f vert(appdata_full v)
			{
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.texcoord.xy = v.vertex.xz * 0.0625;
				o.rotation = v.color.r * 255.0;
                return o;
			}
			
			fout frag(v2f inp)
			{
				fout o;
			    float n1 = tex2D(_AlphaAddTex, inp.texcoord.xy * 2.0).r;
			    float n2 = tex2D(_AlphaAddTex, inp.texcoord.xy * 5.0).g;
			    float n3 = tex2D(_AlphaAddTex, inp.texcoord.xy * 10.0).b;
			    float avgNoise = (n1 + n2 + n3) * 0.333;

			    float4 mainTex = tex2D(_MainTex, inp.texcoord.xy);

			    float combinedAlpha = (avgNoise - mainTex.a) * 0.6 + mainTex.a - 0.3;
			    combinedAlpha *= 2.5;

			    float alphaLowerGate = max(mainTex.a * 1.5 - 0.5, 0.0);
			    float alphaUpperGate = min(mainTex.a * 1.5, 1.0);
			    float finalAlpha = min(alphaUpperGate, max(alphaLowerGate, combinedAlpha));

			    float2 burnUV = inp.texcoord.xy * _BurnScale.xy + (_ScrollSpeed * _GameSeconds);
			    float4 burnTex = tex2D(_BurnTex, burnUV);

			    float3 baseRGB = mainTex.rgb * _Color.rgb;
			    
			    float burnFactor = 1.0 - burnTex.a * _BurnColor.w;
			    float3 modifiedBurnRGB = lerp(burnTex.rgb, float3(0.5, 0.5, 0.5), burnFactor);

			    float3 doubleBase = baseRGB * 2.0;
			    float3 squareBase = baseRGB * baseRGB;
			    float3 sqrtBase = sqrt(baseRGB);

			    float3 colorIfDark = doubleBase * modifiedBurnRGB + squareBase * (1.0 - 2.0 * modifiedBurnRGB);
			    float3 colorIfLight = sqrtBase * (2.0 * modifiedBurnRGB - 1.0) + doubleBase * (1.0 - modifiedBurnRGB);

			    float3 finalRGB;
				
			    [unroll]
			    for(int i = 0; i < 3; i++) {
			        finalRGB[i] = modifiedBurnRGB[i] >= 0.5 ? colorIfLight[i] : colorIfDark[i];
			    }

			    float4 color;
			    color.rgb = finalRGB;
			    color.a = finalAlpha;
			    color *= _Color * _PollutionTintColor;
			    color.a *= tex2D(_MaskTex, RotateUV(frac(inp.texcoord.xy * 16.0), inp.rotation)).a;
			    o.sv_target = color;
			    return o;
			}
			ENDCG
		}
	}
}
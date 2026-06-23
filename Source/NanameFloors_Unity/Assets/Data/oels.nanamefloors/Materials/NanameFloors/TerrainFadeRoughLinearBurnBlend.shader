Shader "Custom/Terrain fade rough Linear burn blend" {
	Properties {
		_MainTex ("Main texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_PollutionTintColor ("PollutionTintColor", Color) = (1,1,1,1)
		_BurnTex ("Burn texture", 2D) = "white" {}
		_BurnColor ("BurnColor", Color) = (1,1,1,1)
		_BurnScale ("BurnScale", Vector) = (1,1,1,1)
		_AlphaAddTex ("Alpha add texture", 2D) = "" {}
		_MaskTex ("Mask texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType" = "Transparent"  "Queue" = "Transparent-600" }
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

			    float2 burnUV_raw = inp.texcoord.xy * _BurnScale.xy;
			    float2 tileID = floor(burnUV_raw);
			    float2 localUV = frac(burnUV_raw);

			    float2 centeredUV = localUV - 0.5;
			    float2 rotatedBurnUV = centeredUV;

			    if ((uint)tileID.y % 2 != 0) {
			        rotatedBurnUV = float2(-centeredUV.y, centeredUV.x);
			    }
			    
			    float2 finalBurnUV = rotatedBurnUV + 0.5;
			    float4 burnTex = tex2D(_BurnTex, finalBurnUV);
			    float3 burnColorRGB = burnTex.rgb * _BurnColor.rgb;
			    float burnAlpha = burnTex.a * _BurnColor.a;
			    
			    float3 combinedBurnRGB = lerp(float3(1.0, 1.0, 1.0), burnColorRGB, burnAlpha);

			    float4 color;
			    color.rgb = mainTex.rgb + combinedBurnRGB - 1.0;
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
Shader "Custom/Terrain fade rough blend" {
	Properties {
		_MainTex ("Main texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
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
			sampler2D _MainTex;
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
				
			    float noise1 = tex2D(_AlphaAddTex, inp.texcoord.xy * 2.0).r;
			    float noise2 = tex2D(_AlphaAddTex, inp.texcoord.xy * 5.0).g;
			    float noise3 = tex2D(_AlphaAddTex, inp.texcoord.xy * 10.0).b;
			    
			    float avgNoise = (noise1 + noise2 + noise3) * 0.333;

			    float4 mainTex = tex2D(_MainTex, inp.texcoord.xy);

			    float combinedAlpha = avgNoise - mainTex.a;
			    combinedAlpha = (combinedAlpha * 0.6 + mainTex.a - 0.3) * 2.5;

			    float clampHelper = max(mainTex.a * 1.5 - 0.5, 0.0);
			    
			    combinedAlpha = max(clampHelper, combinedAlpha);
			    
			    float finalAlpha = min(min(mainTex.a * 1.5, 1.0), combinedAlpha);

			    float4 color = mainTex * _Color;
			    color.a = finalAlpha;
			    color.a *= tex2D(_MaskTex, RotateUV(frac(inp.texcoord.xy * 16.0), inp.rotation)).a;
			    o.sv_target = color;
			    return o;
			}
			ENDCG
		}
	}
}
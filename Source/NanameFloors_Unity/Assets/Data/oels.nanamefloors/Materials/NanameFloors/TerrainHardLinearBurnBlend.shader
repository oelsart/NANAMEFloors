Shader "Custom/Terrain hard Linear burn blend" {
	Properties {
		_MainTex ("Main texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_PollutionTintColor ("PollutionTintColor", Color) = (1,1,1,1)
		_BurnTex ("Burn texture", 2D) = "white" {}
		_BurnColor ("BurnColor", Color) = (1,1,1,1)
		_MaskTex ("Mask texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
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
			sampler2D _BurnTex;
			sampler2D _MainTex;
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
			    float2 burnUV_raw = inp.texcoord.xy * 0.6666667;
			    float2 tileID = floor(burnUV_raw);
			    float2 localUV = frac(burnUV_raw);
				
			    float2 centeredUV = localUV - 0.5;
			    float2 rotatedUV = centeredUV;
				
			    if (((int)tileID.x & 0) == 0) {
			        rotatedUV = float2(-centeredUV.y, -centeredUV.x); 
			    }
			    
			    float2 secondCenteredUV = rotatedUV;
			    if ((int)tileID.y == 0) {
			        secondCenteredUV = float2(-rotatedUV.y, rotatedUV.x);
			    }

			    float2 finalBurnUV = secondCenteredUV + 0.5;

			    float4 burnTex = tex2D(_BurnTex, finalBurnUV);
			    float4 mainTex = tex2D(_MainTex, inp.texcoord.xy);

			    float3 burnRGB = burnTex.rgb * _BurnColor.rgb;
			    
			    float invBurnAlpha = 1.0 - _BurnColor.w;
			    float3 combinedBurnRGB = lerp(burnRGB, 1.0, invBurnAlpha);

			    float4 color;
			    color.rgb = combinedBurnRGB + mainTex.rgb - 1.0;
			    color.a = 1.0;
			    color *= _Color * _PollutionTintColor;
				color.a *= tex2D(_MaskTex, RotateUV(frac(inp.texcoord.xy * 16.0), inp.rotation)).a;

			    o.sv_target = color;
			    return o;
			}
			ENDCG
		}
	}
}	
Shader "Custom/Water depth blend" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_AlphaAddTex ("Alpha add texture", 2D) = "" {}
		_WaterDepthIntensity ("Water depth intensity", Float) = 1
		_WaterRippleDensity ("Water ripple density", Float) = 1
		_MaskTex ("Mask texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "QUEUE" = "Transparent" "Queue" = "Transparent" }
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
				float3 texcoord : TEXCOORD0;
				float4 texcoord2 : TEXCOORD2;
				float4 texcoord3 : TEXCOORD3;
				float rotation : TEXCOORD4;
			};
			struct fout
			{
				float4 sv_target : SV_Target0;
			};
			
			float4 _WaterOffsetTex_TexelSize;
			float _GameSeconds;
			float _WaterRippleDensity;
			int _UseWaterOffset;
			float _WaterDepthIntensity;
			sampler2D _WaterOffsetTex;
			sampler2D _RippleTex;
			sampler2D _MaskTex;
			
			v2f vert(appdata_full v)
			{
				v2f o;
			    o.position = UnityObjectToClipPos(v.vertex);

			    float2 rippleBaseUV;
			    if (_UseWaterOffset) {
			        float2 offsetUV = (v.vertex.xz + 2.5) * _WaterOffsetTex_TexelSize.xy;
			        float4 offsetData = tex2Dlod(_WaterOffsetTex, float4(offsetUV, 0, 0));
			        rippleBaseUV = offsetData.xy - float2(0, _GameSeconds);
			    } else {
			        rippleBaseUV = v.vertex.xz;
			    }
				
			    float2 ripplePos = rippleBaseUV * _WaterRippleDensity;
			    float2 scale1 = ripplePos * float2(0.0495, 0.10225);
				
			    o.texcoord2.x = dot(float2(0.9553365, -0.2955202), scale1);
			    o.texcoord2.y = dot(float2(0.2955202,  0.9553365), scale1) + (_GameSeconds * 0.025);

			    o.texcoord2.z = dot(float2(-0.1288445, -0.9916648), scale1);
			    o.texcoord2.w = dot(float2(0.9916648,  -0.1288445), scale1) + _GameSeconds * 0.016665;

			    float2 scale2 = ripplePos * float2(0.0705, 0.0775);
			    o.texcoord3.x = dot(float2( 0.6967067,  0.7173561), scale2) + (_GameSeconds * -0.0115);
			    o.texcoord3.y = dot(float2(-0.7173561,  0.6967067), scale2) + (_GameSeconds * 0.009385);

			    o.texcoord3.z = dot(float2(-0.1288445, -0.9916648), scale2) + (_GameSeconds * 0.01656);
			    o.texcoord3.w = dot(float2(0.9916648,  -0.1288445), scale2) + (_GameSeconds * 0.00222);

			    o.texcoord.xy = v.vertex.xz * 0.0625;
			    o.texcoord.z = v.color.w;
				o.rotation = v.color.r * 255.0;
			    return o;
			}
			
			fout frag(v2f inp)
			{
				fout o;
				
			    float ripple = tex2D(_RippleTex, inp.texcoord2.xy).r;
			    ripple *= tex2D(_RippleTex, inp.texcoord2.zw).r;
			    ripple *= tex2D(_RippleTex, inp.texcoord3.xy).r;
			    ripple *= tex2D(_RippleTex, inp.texcoord3.zw).r;

			    float finalRipple = (ripple - 0.0625) * _WaterDepthIntensity;
			    o.sv_target.x = finalRipple;
			    o.sv_target.yz = 0;
			    o.sv_target.w = inp.texcoord.z * inp.texcoord.z;

			    o.sv_target.a *= tex2D(_MaskTex, RotateUV(frac(inp.texcoord.xy * 16.0), inp.rotation)).a;

			    return o;
			}
			ENDCG
		}
	}
}
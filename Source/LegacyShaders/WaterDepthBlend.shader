Shader "Custom/Water depth blend" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_AlphaAddTex ("Alpha add texture", 2D) = "" {}
		_WaterDepthIntensity ("Water depth intensity", Float) = 1
		_WaterRippleDensity ("Water ripple density", Float) = 1
		_MaskTex ("Mask texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "QUEUE" = "Geometry" }
		Pass {
			Tags { "QUEUE" = "Geometry" }
			Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			GpuProgramID 43396
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			struct v2f
			{
				float4 position : SV_POSITION0;
				float3 texcoord : TEXCOORD0;
				float4 texcoord2 : TEXCOORD2;
				float4 texcoord3 : TEXCOORD3;
			};
			struct fout
			{
				float4 sv_target : SV_Target0;
			};
			// $Globals ConstantBuffers for Vertex Shader
			float4 _WaterOffsetTex_TexelSize;
			float _GameSeconds;
			float _WaterRippleDensity;
			int _UseWaterOffset;
			// $Globals ConstantBuffers for Fragment Shader
			float _WaterDepthIntensity;
			// Custom ConstantBuffers for Vertex Shader
			// Custom ConstantBuffers for Fragment Shader
			// Texture params for Vertex Shader
			sampler2D _WaterOffsetTex;
			// Texture params for Fragment Shader
			sampler2D _RippleTex;
			sampler2D _MaskTex;
			
			// Keywords: 
			v2f vert(appdata_full v)
			{
                v2f o;
                float4 tmp0;
                float4 tmp1;
                float4 tmp2;
                tmp0 = v.vertex.yyyy * unity_ObjectToWorld._m01_m11_m21_m31;
                tmp0 = unity_ObjectToWorld._m00_m10_m20_m30 * v.vertex.xxxx + tmp0;
                tmp0 = unity_ObjectToWorld._m02_m12_m22_m32 * v.vertex.zzzz + tmp0;
                tmp0 = tmp0 + unity_ObjectToWorld._m03_m13_m23_m33;
                tmp1 = tmp0.yyyy * unity_MatrixVP._m01_m11_m21_m31;
                tmp1 = unity_MatrixVP._m00_m10_m20_m30 * tmp0.xxxx + tmp1;
                tmp1 = unity_MatrixVP._m02_m12_m22_m32 * tmp0.zzzz + tmp1;
                o.position = unity_MatrixVP._m03_m13_m23_m33 * tmp0.wwww + tmp1;
                if (_UseWaterOffset) {
                    tmp0.xy = v.vertex.xz + float2(2.5, 2.5);
                    tmp0.xy = tmp0.xy * _WaterOffsetTex_TexelSize.xy;
                    tmp0 = tex2Dlod(_WaterOffsetTex, float4(tmp0.xy, 0, 0.0));
                    tmp1.x = 0.0;
                    tmp1.y = _GameSeconds;
                    tmp0.xy = tmp0.xy - tmp1.xy;
                } else {
                    tmp0.xy = v.vertex.xz;
                }
                tmp0 = tmp0.xyxy * _WaterRippleDensity.xxxx;
                tmp1 = tmp0.zwzw * float4(0.0495, 0.10225, 0.04, 0.06);
                tmp2.x = dot(float2(0.9553365, -0.2955202), tmp1.xy);
                tmp2.y = dot(float2(0.2955202, 0.9553365), tmp1.xy);
                o.texcoord2.xy = _GameSeconds.xx * float2(0.0, 0.025) + tmp2.xy;
                tmp2.z = dot(float2(-0.1288445, -0.9916648), tmp1.xy);
                tmp2.w = dot(float2(0.9916648, -0.1288445), tmp1.xy);
                o.texcoord2.zw = _GameSeconds.xx * float2(0.0105, 0.016665) + tmp2.zw;
                tmp0 = tmp0 * float4(0.0705, 0.0775, 0.07215, 0.025);
                tmp1.x = dot(float2(0.6967067, 0.7173561), tmp0.xy);
                tmp1.y = dot(float2(-0.7173561, 0.6967067), tmp0.xy);
                o.texcoord3.xy = _GameSeconds.xx * float2(-0.0115, 0.009385) + tmp1.xy;
                tmp1.z = dot(float2(-0.1288445, -0.9916648), tmp0.xy);
                tmp1.w = dot(float2(0.9916648, -0.1288445), tmp0.xy);
                o.texcoord3.zw = _GameSeconds.xx * float2(0.01656, 0.00222) + tmp1.zw;
                o.texcoord.xy = v.vertex.xz * float2(0.0625, 0.0625);
                o.texcoord.z = v.color.w;
                return o;
			}
			// Keywords: 
			fout frag(v2f inp)
			{
                fout o;
                float4 tmp0;
                float4 tmp1;
                tmp0 = tex2D(_RippleTex, inp.texcoord2.xy);
                tmp1 = tex2D(_RippleTex, inp.texcoord2.zw);
                tmp0.x = tmp0.x * tmp1.x;
                tmp1 = tex2D(_RippleTex, inp.texcoord3.xy);
                tmp0.x = tmp0.x * tmp1.x;
                tmp1 = tex2D(_RippleTex, inp.texcoord3.zw);
                tmp0.x = tmp0.x * tmp1.x + -0.0625;
                o.sv_target.x = tmp0.x * _WaterDepthIntensity;
                o.sv_target.w = inp.texcoord.z * inp.texcoord.z;
                o.sv_target.yz = float2(0.0, 0.0);
				o.sv_target.a *= tex2D(_MaskTex, inp.texcoord.xy * 16 % 1).a;
                return o;
			}
			ENDCG
		}
	}
}
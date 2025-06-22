Shader "Custom/Terrain hard Linear burn blend" {
	Properties {
		_MainTex ("Main texture", 2D) = "white" {}
		_MainTexTwo ("Main texture two", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_ColorTwo ("Color two", Color) = (1,1,1,1)
		_PollutionTintColor ("PollutionTintColor", Color) = (1,1,1,1)
		_PollutionTintColorTwo ("PollutionTintColor two", Color) = (1,1,1,1)
		_BurnTex ("Burn texture", 2D) = "white" {}
		_BurnTexTwo ("Burn texture two", 2D) = "white" {}
		_BurnColor ("BurnColor", Color) = (1,1,1,1)
		_BurnColorTwo ("BurnColor two", Color) = (1,1,1,1)
        _MaskTex ("Mask texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		Pass {
			Tags { "RenderType" = "Opaque" }
			ZWrite Off
			GpuProgramID 18286
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			struct v2f
			{
				float4 position : SV_POSITION0;
				float2 texcoord : TEXCOORD0;
			};
			struct fout
			{
				float4 sv_target : SV_Target0;
			};
			// $Globals ConstantBuffers for Vertex Shader
			// $Globals ConstantBuffers for Fragment Shader
			float4 _Color;
			float4 _ColorTwo;
			float4 _PollutionTintColor;
			float4 _PollutionTintColorTwo;
			float4 _BurnColor;
			float4 _BurnColorTwo;
			// Custom ConstantBuffers for Vertex Shader
			// Custom ConstantBuffers for Fragment Shader
			// Texture params for Vertex Shader
			// Texture params for Fragment Shader
			sampler2D _BurnTex;
			sampler2D _BurnTexTwo;
			sampler2D _MainTex;
			sampler2D _MainTexTwo;
            sampler2D _MaskTex;
			
			// Keywords: 
			v2f vert(appdata_full v)
			{
                v2f o;
                float4 tmp0;
                float4 tmp1;
                tmp0 = v.vertex.yyyy * unity_ObjectToWorld._m01_m11_m21_m31;
                tmp0 = unity_ObjectToWorld._m00_m10_m20_m30 * v.vertex.xxxx + tmp0;
                tmp0 = unity_ObjectToWorld._m02_m12_m22_m32 * v.vertex.zzzz + tmp0;
                tmp0 = tmp0 + unity_ObjectToWorld._m03_m13_m23_m33;
                tmp1 = tmp0.yyyy * unity_MatrixVP._m01_m11_m21_m31;
                tmp1 = unity_MatrixVP._m00_m10_m20_m30 * tmp0.xxxx + tmp1;
                tmp1 = unity_MatrixVP._m02_m12_m22_m32 * tmp0.zzzz + tmp1;
                o.position = unity_MatrixVP._m03_m13_m23_m33 * tmp0.wwww + tmp1;
                o.texcoord.xy = v.vertex.xz * float2(0.0625, 0.0625);
                return o;
			}
			// Keywords: 
			fout frag(v2f inp)
			{
                fout o;
                float4 tmp0;
                float4 tmp1;
                tmp0.xy = inp.texcoord.xy * float2(0.6666667, 0.6666667);
                tmp0.zw = trunc(tmp0.xy);
                tmp0.xy = frac(tmp0.xy);
                tmp0.zw = asint(tmp0.zw);
                tmp0.z = uint1(tmp0.z) & uint1(0.0);
                tmp1.xy = tmp0.yx - float2(0.5, 0.5);
                tmp1.z = tmp1.x * -0.0000001 + -tmp1.y;
                tmp1.x = -tmp1.y * -0.0000001 + -tmp1.x;
                tmp1.y = tmp1.x + 0.5;
                tmp1.x = tmp1.z + 0.5;
                tmp0.xy = tmp0.zz ? tmp0.xy : tmp1.xy;
                tmp1.xy = tmp0.xy - float2(0.5, 0.5);
                tmp0.z = tmp1.x * -0.0 + -tmp1.y;
                tmp1.x = tmp1.y * -0.0 + tmp1.x;
                tmp1.y = tmp1.x + 0.5;
                tmp1.x = tmp0.z + 0.5;
                tmp0.xy = tmp0.ww ? tmp0.xy : tmp1.xy;
                tmp0 = tex2D(_BurnTex, tmp0.xy);
                tmp1.xyz = tmp0.xyz * _BurnColor.xyz;
                tmp0.xyz = -tmp0.xyz * _BurnColor.xyz + float3(1.0, 1.0, 1.0);
                tmp0.w = 1.0 - _BurnColor.w;
                tmp0.xyz = tmp0.www * tmp0.xyz + tmp1.xyz;
                tmp1 = tex2D(_MainTex, inp.texcoord.xy);
                tmp0.xyz = tmp0.xyz + tmp1.xyz;
                tmp0.xyz = tmp0.xyz - float3(1.0, 1.0, 1.0);
                tmp0.w = 1.0;
                tmp0 = tmp0 * _Color;
                tmp0 = tmp0 * _PollutionTintColor;

				float4 tmp2;
                float4 tmp3;
                tmp2.xy = inp.texcoord.xy * float2(0.6666667, 0.6666667);
                tmp2.zw = trunc(tmp2.xy);
                tmp2.xy = frac(tmp2.xy);
                tmp2.zw = asint(tmp2.zw);
                tmp2.z = uint1(tmp2.z) & uint1(0.0);
                tmp3.xy = tmp2.yx - float2(0.5, 0.5);
                tmp3.z = tmp3.x * -0.0000001 + -tmp3.y;
                tmp3.x = -tmp3.y * -0.0000001 + -tmp3.x;
                tmp3.y = tmp3.x + 0.5;
                tmp3.x = tmp3.z + 0.5;
                tmp2.xy = tmp2.zz ? tmp2.xy : tmp3.xy;
                tmp3.xy = tmp2.xy - float2(0.5, 0.5);
                tmp2.z = tmp3.x * -0.0 + -tmp3.y;
                tmp3.x = tmp3.y * -0.0 + tmp3.x;
                tmp3.y = tmp3.x + 0.5;
                tmp3.x = tmp2.z + 0.5;
                tmp2.xy = tmp2.ww ? tmp2.xy : tmp3.xy;
                tmp2 = tex2D(_BurnTexTwo, tmp2.xy);
                tmp3.xyz = tmp2.xyz * _BurnColorTwo.xyz;
                tmp2.xyz = -tmp2.xyz * _BurnColorTwo.xyz + float3(1.0, 1.0, 1.0);
                tmp2.w = 1.0 - _BurnColorTwo.w;
                tmp2.xyz = tmp2.www * tmp2.xyz + tmp3.xyz;
                tmp3 = tex2D(_MainTexTwo, inp.texcoord.xy);
                tmp2.xyz = tmp2.xyz + tmp3.xyz;
                tmp2.xyz = tmp2.xyz - float3(1.0, 1.0, 1.0);
                tmp2.w = 1.0;
                tmp2 = tmp2 * _ColorTwo;
                tmp2 = tmp2 * _PollutionTintColorTwo;

                float4 tmp4;
                tmp4 = tex2D(_MaskTex, inp.texcoord.xy * 16);
                o.sv_target = tmp0 * (1 - tmp4.a) + tmp2 * tmp4.a;
                return o;
			}
			ENDCG
		}
	}
}
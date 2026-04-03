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
		Tags { "RenderType" = "Transparent" }
		Pass {
			Tags { "RenderType" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
			GpuProgramID 55576
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			struct v2f
			{
				float4 position : SV_POSITION0;
				float2 texcoord : TEXCOORD0;
				float4 color : COLOR0;
			};
			struct fout
			{
				float4 sv_target : SV_Target0;
			};
			// $Globals ConstantBuffers for Vertex Shader
			// $Globals ConstantBuffers for Fragment Shader
			float4 _Color;
			float4 _PollutionTintColor;
			float4 _BurnColor;
			float4 _BurnScale;
			float2 _ScrollSpeed;
			float _GameSeconds;
			// Custom ConstantBuffers for Vertex Shader
			// Custom ConstantBuffers for Fragment Shader
			// Texture params for Vertex Shader
			// Texture params for Fragment Shader
			sampler2D _MainTex;
			sampler2D _BurnTex;
			sampler2D _AlphaAddTex;
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
                o.color = v.color;
                return o;
			}
			// Keywords: 
			fout frag(v2f inp)
			{
                fout o;
                float4 tmp0;
                float4 tmp1;
                float4 tmp2;
                float4 tmp3;
                float4 tmp4;
                float4 tmp5;
                float4 tmp6;
                tmp0.xy = inp.texcoord.xy + inp.texcoord.xy;
                tmp0 = tex2D(_AlphaAddTex, tmp0.xy);
                tmp1 = inp.texcoord.xyxy * float4(5.0, 5.0, 10.0, 10.0);
                tmp2 = tex2D(_AlphaAddTex, tmp1.xy);
                tmp1 = tex2D(_AlphaAddTex, tmp1.zw);
                tmp0.y = tmp2.y * 0.333;
                tmp0.x = tmp0.x * 0.333 + tmp0.y;
                tmp0.x = tmp1.z * 0.333 + tmp0.x;
                tmp1 = tex2D(_MainTex, inp.texcoord.xy);
                tmp0.x = -tmp1.w * inp.color.w + tmp0.x;
                tmp1 = tmp1 * inp.color;
                tmp0.x = tmp0.x * 0.6 + tmp1.w;
                tmp0.x = tmp0.x - 0.3;
                tmp0.x = tmp0.x * 2.5;
                tmp0.y = tmp1.w * 1.5 + -1.5;
                tmp0.y = tmp0.y + 1.0;
                tmp0.y = max(tmp0.y, 0.0);
                tmp0.x = max(tmp0.y, tmp0.x);
                tmp0.y = tmp1.w * 1.5;
                tmp1.xyz = tmp1.xyz * _Color.xyz;
                tmp0.y = min(tmp0.y, 1.0);
                tmp0.w = min(tmp0.y, tmp0.x);
                tmp2.xyz = sqrt(tmp1.xyz);
                tmp3.xyz = tmp1.xyz + tmp1.xyz;
                tmp1.xyz = tmp1.xyz * tmp1.xyz;
                tmp4.xy = _ScrollSpeed * _GameSeconds.xx;
                tmp4.xy = inp.texcoord.xy * _BurnScale.xy + tmp4.xy;
                tmp4 = tex2D(_BurnTex, tmp4.xy);
                tmp1.w = -tmp4.w * _BurnColor.w + 1.0;
                tmp5.xyz = float3(0.5, 0.5, 0.5) - tmp4.xyz;
                tmp4.xyz = tmp1.www * tmp5.xyz + tmp4.xyz;
                tmp5.xyz = float3(1.0, 1.0, 1.0) - tmp4.xyz;
                tmp5.xyz = tmp3.xyz * tmp5.xyz;
                tmp6.xyz = tmp4.xyz * float3(2.0, 2.0, 2.0) + float3(-1.0, -1.0, -1.0);
                tmp2.xyz = tmp2.xyz * tmp6.xyz + tmp5.xyz;
                tmp5.xyz = -tmp4.xyz * float3(2.0, 2.0, 2.0) + float3(1.0, 1.0, 1.0);
                tmp1.xyz = tmp1.xyz * tmp5.xyz;
                tmp1.xyz = tmp3.xyz * tmp4.xyz + tmp1.xyz;
                tmp3.xyz = tmp4.xyz >= float3(0.5, 0.5, 0.5);
                tmp3.xyz = tmp3.xyz ? 1.0 : 0.0;
                tmp2.xyz = tmp2.xyz - tmp1.xyz;
                tmp0.xyz = tmp3.xyz * tmp2.xyz + tmp1.xyz;
                o.sv_target = tmp0 * _PollutionTintColor;
				o.sv_target.a *= tex2D(_MaskTex, inp.texcoord.xy * 16 % 1).a;
                return o;
			}
			ENDCG
		}
	}
}
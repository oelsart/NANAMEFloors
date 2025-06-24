Shader "Custom/Terrain water polluted blend" {
	Properties {
		_MainTex ("Main texture", 2D) = "white" {}
		_RippleTex ("Ripple texture", 2D) = "white" {}
		_BurnTex ("Burn texture", 2D) = "white" {}
		_BurnColor ("BurnColor", Color) = (1,1,1,1)
		_Color ("Color", Color) = (1,1,1,1)
		_AlphaAddTex ("Alpha add texture", 2D) = "" {}
		_MaskTex ("Mask texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType" = "Transparent" }
		Pass {
			Tags { "RenderType" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
            ZWrite Off
			Cull Off
			GpuProgramID 14119
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			struct v2f
			{
				float4 position : SV_POSITION0;
				float2 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 color : COLOR0;
			};
			struct fout
			{
				float4 sv_target : SV_Target0;
			};
			// $Globals ConstantBuffers for Vertex Shader
			// $Globals ConstantBuffers for Fragment Shader
			float4 _Color;
			float4 _WaterCastVectSun;
			float4 _WaterCastVectMoon;
			float _LightsourceShineSizeReduction;
			float _LightsourceShineIntensity;
			float4 _BurnColor;
			float4 _BurnScale;
			float2 _ScrollSpeed;
			float _GameSeconds;
			// Custom ConstantBuffers for Vertex Shader
			// Custom ConstantBuffers for Fragment Shader
			// Texture params for Vertex Shader
			// Texture params for Fragment Shader
			sampler2D _WaterOutputTex;
			sampler2D _MainTex;
			sampler2D _AlphaAddTex;
			sampler2D _NoiseTex;
			sampler2D _BurnTex;
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
                tmp0 = unity_MatrixVP._m03_m13_m23_m33 * tmp0.wwww + tmp1;
                o.position = tmp0;
                o.texcoord1 = tmp0 * float4(1.0, -1.0, 1.0, 1.0);
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
                tmp0.x = ddx(inp.texcoord.x);
                tmp0.y = ddy(inp.texcoord.y);
                tmp0.x = dot(tmp0.xy, tmp0.xy);
                tmp0.x = sqrt(tmp0.x);
                tmp0.x = tmp0.x / inp.color.w;
                tmp0.z = tmp0.x * 500.0;
                tmp1.xy = float2(1.0, 1.0) / _ScreenParams.xy;
                tmp1.zw = inp.texcoord1.xy * float2(0.5, 0.5) + float2(0.5, 0.5);
                tmp2.zw = -tmp1.xy * float2(0.5, 0.5) + tmp1.zw;
                tmp2.xy = tmp1.xy * float2(0.5, 0.5) + tmp1.zw;
                tmp1 = tex2D(_WaterOutputTex, tmp2.xw);
                tmp0.w = tmp1.x - 0.5;
                tmp1 = tex2D(_WaterOutputTex, tmp2.xy);
                tmp3 = tex2D(_WaterOutputTex, tmp2.zy);
                tmp2 = tex2D(_WaterOutputTex, tmp2.zw);
                tmp1.y = tmp2.x - 0.5;
                tmp1.z = tmp3.x - 0.5;
                tmp1.x = tmp1.x - 0.5;
                tmp1.w = tmp0.w + tmp1.x;
                tmp0.w = tmp0.w + tmp1.y;
                tmp1.xy = tmp1.xz + tmp1.zy;
                tmp0.y = tmp1.x - tmp0.w;
                tmp0.x = tmp1.w - tmp1.y;
                tmp0.w = dot(tmp0.xyz, tmp0.xyz);
                tmp0.w = rsqrt(tmp0.w);
                tmp0.xyz = tmp0.www * tmp0.xyz;
                tmp1.xy = -_WaterCastVectMoon.xz;
                tmp1.z = -50.0;
                tmp0.w = dot(tmp1.xyz, tmp1.xyz);
                tmp0.w = rsqrt(tmp0.w);
                tmp1.xyw = tmp0.www * tmp1.xyz;
                tmp2.x = dot(-tmp1.xyz, tmp0.xyz);
                tmp2.y = tmp1.z * tmp0.w + tmp2.x;
                tmp0.w = tmp1.z * tmp0.w + 1.0;
                tmp0.w = tmp2.y / tmp0.w;
                tmp0.w = 1.0 - tmp0.w;
                tmp1.z = tmp0.w * tmp0.w;
                tmp1.z = tmp1.z * tmp1.z;
                tmp0.w = -tmp0.w * tmp1.z + 1.0;
                tmp0.w = tmp0.w * 0.5 + 0.5;
                tmp1.z = tmp2.x / -tmp1.w;
                tmp1.z = log(tmp1.z);
                tmp1.z = tmp1.z * 100.0;
                tmp1.z = exp(tmp1.z);
                tmp1.z = tmp1.z * 0.5;
                tmp2.y = tmp2.x < -tmp1.w;
                tmp2.x = tmp2.x + tmp2.x;
                tmp1.xyw = tmp0.xyz * -tmp2.xxx + -tmp1.xyw;
                tmp2.x = tmp2.y ? tmp1.z : tmp0.w;
                tmp0.w = max(_ScreenParams.y, _ScreenParams.x);
                tmp0.w = 0.2 / tmp0.w;
                tmp2.zw = inp.texcoord1.xy * _ScreenParams.xy;
                tmp3.xy = -tmp0.ww * tmp2.zw;
                tmp3.z = -1.0;
                tmp0.w = dot(tmp3.xyz, tmp3.xyz);
                tmp0.w = rsqrt(tmp0.w);
                tmp3.xyz = tmp0.www * tmp3.xyz;
                tmp0.w = dot(tmp1.xyz, tmp3.xyz);
                tmp0.w = max(tmp0.w, 0.0);
                tmp0.w = log(tmp0.w);
                tmp0.w = tmp0.w * _LightsourceShineSizeReduction;
                tmp0.w = exp(tmp0.w);
                tmp2.y = tmp0.w * _LightsourceShineIntensity;
                tmp1 = tex2D(_MainTex, tmp2.xy);
                tmp2.xy = -_WaterCastVectSun.xz;
                tmp2.z = -50.0;
                tmp0.w = dot(tmp2.xyz, tmp2.xyz);
                tmp0.w = rsqrt(tmp0.w);
                tmp2.xyw = tmp0.www * tmp2.xyz;
                tmp3.w = dot(-tmp2.xyz, tmp0.xyz);
                tmp4.x = tmp2.z * tmp0.w + tmp3.w;
                tmp0.w = tmp2.z * tmp0.w + 1.0;
                tmp0.w = tmp4.x / tmp0.w;
                tmp0.w = 1.0 - tmp0.w;
                tmp2.z = tmp0.w * tmp0.w;
                tmp2.z = tmp2.z * tmp2.z;
                tmp0.w = -tmp0.w * tmp2.z + 1.0;
                tmp0.w = tmp0.w * 0.5 + 0.5;
                tmp2.z = tmp3.w / -tmp2.w;
                tmp2.z = log(tmp2.z);
                tmp2.z = tmp2.z * 100.0;
                tmp2.z = exp(tmp2.z);
                tmp2.z = tmp2.z * 0.5;
                tmp4.x = tmp3.w < -tmp2.w;
                tmp3.w = tmp3.w + tmp3.w;
                tmp0.xyz = tmp0.xyz * -tmp3.www + -tmp2.xyw;
                tmp0.x = dot(tmp0.xyz, tmp3.xyz);
                tmp0.x = max(tmp0.x, 0.0);
                tmp0.x = log(tmp0.x);
                tmp0.x = tmp0.x * _LightsourceShineSizeReduction;
                tmp0.x = exp(tmp0.x);
                tmp0.y = tmp0.x * _LightsourceShineIntensity;
                tmp0.x = tmp4.x ? tmp2.z : tmp0.w;
                tmp0 = tex2D(_MainTex, tmp0.xy);
                tmp1 = tmp1.wxyz - tmp0.wxyz;
                tmp2.x = _WaterCastVectSun.w + _WaterCastVectMoon.w;
                tmp2.x = _WaterCastVectMoon.w / tmp2.x;
                tmp0 = tmp2.xxxx * tmp1 + tmp0.wxyz;
                tmp1 = tex2D(_MainTex, float2(0.0, 0.0));
                tmp0 = tmp0 - tmp1.wxyz;
                tmp2.x = _WaterCastVectMoon.w;
                tmp2.y = _WaterCastVectSun.w;
                tmp2.x = dot(tmp2.xy, tmp2.xy);
                tmp2.x = sqrt(tmp2.x);
                tmp0 = tmp2.xxxx * tmp0 + tmp1.wxyz;
                tmp1.xy = inp.texcoord.xy + inp.texcoord.xy;
                tmp1 = tex2D(_AlphaAddTex, tmp1.xy);
                tmp2 = inp.texcoord.xyxy * float4(5.0, 5.0, 10.0, 10.0);
                tmp3 = tex2D(_AlphaAddTex, tmp2.xy);
                tmp2 = tex2D(_AlphaAddTex, tmp2.zw);
                tmp1.y = tmp3.y * 0.333;
                tmp1.x = tmp1.x * 0.333 + tmp1.y;
                tmp1.x = tmp2.z * 0.333 + tmp1.x;
                tmp1.x = -tmp0.x * inp.color.w + tmp1.x;
                tmp2 = tmp0 * inp.color.wxyz;
                tmp0.x = tmp1.x * 0.6 + tmp2.x;
                tmp0.x = tmp0.x - 0.3;
                tmp0.x = tmp0.x * 2.5;
                tmp1.x = tmp2.x * 1.5 + -0.5;
                tmp1.x = max(tmp1.x, 0.0);
                tmp0.x = max(tmp0.x, tmp1.x);
                tmp1.x = tmp2.x * 1.5;
                tmp1.x = min(tmp1.x, 1.0);
                tmp1.w = min(tmp0.x, tmp1.x);
                tmp3 = tex2D(_NoiseTex, inp.texcoord.xy);
                tmp3.xyz = tmp0.yzw * inp.color.xyz + tmp3.xyz;
                tmp3.xyz = tmp3.xyz - float3(0.5, 0.5, 0.5);
                tmp0.xyz = -tmp0.yzw * inp.color.xyz + tmp3.xyz;
                tmp1.xyz = tmp0.xyz * float3(0.025, 0.025, 0.025) + tmp2.yzw;
                tmp0 = tmp1 * _Color;
                tmp1.xyz = sqrt(tmp0.xyz);
                tmp2.xyz = tmp0.xyz + tmp0.xyz;
                tmp3.xy = _ScrollSpeed * _GameSeconds.xx;
                tmp3.xy = inp.texcoord.xy * _BurnScale.xy + tmp3.xy;
                tmp3 = tex2D(_BurnTex, tmp3.xy);
                tmp1.w = -tmp3.w * _BurnColor.w + 1.0;
                tmp4.xyz = float3(0.5, 0.5, 0.5) - tmp3.xyz;
                tmp3.xyz = tmp1.www * tmp4.xyz + tmp3.xyz;
                tmp4.xyz = float3(1.0, 1.0, 1.0) - tmp3.xyz;
                tmp4.xyz = tmp2.xyz * tmp4.xyz;
                tmp5.xyz = tmp3.xyz * float3(2.0, 2.0, 2.0) + float3(-1.0, -1.0, -1.0);
                tmp1.xyz = tmp1.xyz * tmp5.xyz + tmp4.xyz;
                tmp4.xyz = tmp0.xyz * tmp0.xyz;
                tmp5.xyz = -tmp3.xyz * float3(2.0, 2.0, 2.0) + float3(1.0, 1.0, 1.0);
                tmp4.xyz = tmp4.xyz * tmp5.xyz;
                tmp2.xyz = tmp2.xyz * tmp3.xyz + tmp4.xyz;
                tmp3.xyz = tmp3.xyz >= float3(0.5, 0.5, 0.5);
                tmp3.xyz = tmp3.xyz ? 1.0 : 0.0;
                tmp1.xyz = tmp1.xyz - tmp2.xyz;
                tmp0.xyz = tmp3.xyz * tmp1.xyz + tmp2.xyz;
                o.sv_target = tmp0 * _Color;
				o.sv_target.a *= tex2D(_MaskTex, inp.texcoord.xy * 16 % 1).a;
                return o;
			}
			ENDCG
		}
	}
}
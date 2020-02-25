Shader "Instanced/Tree" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_CutoutThreshold("Cutout Threshold", Range(0,1)) = 0.5
		[Header(Wind)]
		_WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
		_WindStrength("Wind Strength", Float) = 1
		_WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
	}

	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows addshadow vertex:vert
		// Use Shader model 3.0 target
		#pragma target 3.0

		sampler2D _MainTex;
		float _CutoutThreshold;

		sampler2D _WindDistortionMap;
		float4 _WindDistortionMap_ST;
		float _WindStrength;
		float2 _WindFrequency;

		struct Input {
			float2 uv_MainTex;
		};
		half _Glossiness;
		half _Metallic;

		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
		UNITY_INSTANCING_BUFFER_END(Props)

		void vert(inout appdata_full v) {

			float3 modelPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
			float2 uv = modelPos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
			float2 windSample = (tex2Dlod(_WindDistortionMap, float4(uv, 0, 0)).xy * 2 - 1) * _WindStrength;

			v.vertex.xz += windSample.xy * asin(v.vertex.y);
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			clip(c.a - _CutoutThreshold);
		}
		ENDCG
	}
	FallBack "Diffuse"
}

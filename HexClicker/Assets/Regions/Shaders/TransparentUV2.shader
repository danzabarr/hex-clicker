Shader "Custom/TransparentUV2"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_Intensity("Intensity", Float) = 1
	}
	SubShader
	{
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
		LOD 100

		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha
		Lighting Off
		Fog { Mode Off }

		CGPROGRAM
		#pragma surface surf NoLighting noforwardadd nofog alpha:fade

		sampler2D _MainTex;
		float4 _Color;
		float _Intensity;

		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) {
			return fixed4(s.Albedo, s.Alpha);
		}

		struct Input
		{
			float2 uv2_MainTex    : TEXCOORD0;
		};

		void surf(Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv2_MainTex);
			o.Albedo = c.rgb * _Color.rgb * _Intensity * c.a * _Color.a;
			o.Alpha = c.a * _Color.a;
		}
		ENDCG
	}

		Fallback "VertexLit"
}
Shader "Custom/Instanced"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Pass
		{



			Tags {"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			// compile shader into multiple variants, with and without shadows
			// (we don't care about any lightmaps yet, so skip these variants)
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma multi_compile_fog
			// shadow helper functions and macros
			#include "AutoLight.cginc"

			#ifdef SHADER_API_D3D11
			StructuredBuffer<float4> positionBuffer;
			#endif

			struct v2f
			{
				float2 uv : TEXCOORD0;
				SHADOW_COORDS(1) // put shadows data into TEXCOORD1
				UNITY_FOG_COORDS(2)
				fixed3 diff : COLOR0;
				fixed3 ambient : COLOR1;
				float4 pos : SV_POSITION;
			};
			v2f vert(appdata_base v, uint instanceID : SV_InstanceID)
			{
				v2f o;

				float4 data = positionBuffer[instanceID];

				float3 localPosition = v.vertex.xyz * data.w;
				float3 worldPosition = data.xyz + localPosition;

				o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
				o.uv = v.texcoord;
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0.rgb;
				o.ambient = ShadeSH9(half4(worldNormal,1));
				// compute shadows data
				TRANSFER_SHADOW(o)
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				// compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
				fixed shadow = SHADOW_ATTENUATION(i);
				// darken light's illumination with shadow, keep ambient intact
				fixed3 lighting = i.diff * shadow + i.ambient;
				col.rgb *= lighting;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}

	// shadow casting support
	UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}



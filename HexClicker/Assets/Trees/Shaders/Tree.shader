
Shader "HexClicker/Tree"
{
	Properties
	{
		_Cutoff("Cutoff", Range(0, 1)) = 0.5
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_Metallic("Metallic", 2D) = "black" {}
		_TranslucentGain("Translucent Gain", Range(0, 1)) = 0
		[Toggle(FOG_OF_WAR)]
		_FogOfWar("Fog of War", Float) = 1
		[Header(Wind)]
		_WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
		_WindStrength("Wind Strength", Float) = 1
		_WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
	}
	SubShader
	{
		Tags { "RenderType" = "Tree"}

		Pass
		{
			Lighting On
			Tags
			{
				"RenderQueue" = "Opaque"
				"LightMode" = "ForwardBase"
			}

			CGPROGRAM

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#include "Assets/GlobalShaders/FogOfWar.cginc"
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			#pragma shader_feature FOG_OF_WAR

			#pragma vertex vert addshadow
			#pragma fragment frag

			UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
			UNITY_INSTANCING_BUFFER_END(Props)

			uniform half4 _WaterColor;
			uniform half4 _SnowColor;
			uniform float _LatitudeScale;
			uniform float _WaterLevel;
			uniform float _WaterBlending;
			uniform float _AltitudeTemperature;
			uniform float _Temperature;
			uniform float _SnowBlending;
			uniform float _SnowShininess;
			uniform float _SnowIntensity;
			uniform float _SnowSpecular;
			uniform float _SnowSlopeMax;
			uniform float _OffsetAltitude;
			uniform float _OffsetLatitude;

			float _Cutoff;
			float _TranslucentGain;

			sampler2D _MainTex;
			sampler2D _Normal;
			sampler2D _Metallic;

			float4 _MainTex_ST;
			float4 _Normal_ST;
			float4 _Metallic_ST;

			sampler2D _WindDistortionMap;
			float4 _WindDistortionMap_ST;
			float _WindStrength;
			float2 _WindFrequency;

			struct appdata
			{
				float4 vertex    : POSITION;  // The vertex position in model space.
				float3 normal    : NORMAL;    // The vertex normal in model space.
				float4 texcoord  : TEXCOORD0; // The first UV coordinate.
				float4 texcoord1 : TEXCOORD1; // The second UV coordinate.
				float4 tangent   : TANGENT;   // The tangent vector in Model Space (used for normal mapping).
				float4 color     : COLOR;     // Per-vertex color
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct vertexOutput
			{
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				half3 worldNormal : TEXCOORD1;
				float2 uv : TEXCOORD2;
				float3 vertex : TEXCOORD3;

				half3 tspace0 : TEXCOORD4; // tangent.x, bitangent.x, normal.x
				half3 tspace1 : TEXCOORD5; // tangent.y, bitangent.y, normal.y
				half3 tspace2 : TEXCOORD6; // tangent.z, bitangent.z, normal.z

				half3 viewDir : TEXCOORD7;
				half3 normal : TEXCOORD8;

				SHADOW_COORDS(9)
				UNITY_FOG_COORDS(10)
				UNITY_VERTEX_INPUT_INSTANCE_ID

			};

			vertexOutput vert(appdata v)
			{
				vertexOutput output;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, output);

				float3 modelPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
				float2 uv = modelPos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
				float2 windSample = (tex2Dlod(_WindDistortionMap, float4(uv, 0, 0)).xy * 2 - 1) * _WindStrength;

				v.vertex.xz += windSample.xy * asin(v.vertex.y);

				output.pos = UnityObjectToClipPos(v.vertex);
				output.uv = v.texcoord;
				output.vertex = v.vertex;
				output.viewDir = normalize(ObjSpaceViewDir(v.vertex));
				output.normal = v.normal;

				half3 wNormal = UnityObjectToWorldNormal(v.normal);
				half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
				// compute bitangent from cross product of normal and tangent
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
				// output the tangent space matrix
				output.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
				output.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
				output.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);

				output.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				output.worldPos.x += _OffsetLatitude;
				output.worldPos.y += _OffsetAltitude;
				output.worldNormal = wNormal;

				TRANSFER_SHADOW(output);
				UNITY_TRANSFER_FOG(output, output.pos);

				return output;
			}

			half4 frag(vertexOutput input) : SV_Target
			{
				float4 col = tex2D(_MainTex, TRANSFORM_TEX(input.uv, _MainTex));

				if (col.a < _Cutoff)
					discard;
#if FOG_OF_WAR
				int2 tile = SampleTile(input.worldPos.x, input.worldPos.z);
				if (!Tile(tile.x, tile.y))
					return half4(0, 0, 0, 1);
#endif
				half3 lightDir = normalize(UnityWorldSpaceLightDir(input.worldPos.xyz));
				float atten = UNITY_SHADOW_ATTENUATION(input, input.worldPos);

				UNITY_SETUP_INSTANCE_ID(input);
				 

				col.rgb *= UNITY_ACCESS_INSTANCED_PROP(Props, _Color).rgb;
				float3 normal = UnpackNormal(tex2D(_Normal, TRANSFORM_TEX(input.uv, _Normal)));
				float3 worldNormal = half3(dot(input.tspace0, normal), dot(input.tspace1, normal), dot(input.tspace2, normal));
				float4 metallic = tex2D(_Metallic, TRANSFORM_TEX(input.uv, _Metallic));
				float diffuse = saturate(dot(lightDir.xyz, worldNormal));
				float specular = saturate(pow(max(0.0, dot(reflect(-lightDir, worldNormal), input.viewDir)), max(1, metallic.a * 50)) * metallic.r);
				diffuse = max(diffuse, _TranslucentGain);

				col.rgb *= max((diffuse + specular) * atten * _LightColor0, UNITY_LIGHTMODEL_AMBIENT);

				if (input.worldPos.y <= _WaterLevel)
				{
					float waterAmount = 1;
					if (input.worldPos.y <= _WaterLevel && input.worldPos.y > _WaterLevel - _WaterBlending)
						waterAmount *= 1 - (input.worldPos.y - _WaterLevel + _WaterBlending) / _WaterBlending;
					waterAmount *= _WaterColor.a;
					col = lerp(col, _WaterColor * _LightColor0, waterAmount);
				}

				//Keep hdr color within 0-5 range
				col = clamp(col, 0, 5);

				UNITY_APPLY_FOG(input.fogCoord, col);
#if FOG_OF_WAR
				ApplyFogOfWar(input.worldPos, col);
#endif
				return col;
			}

			ENDCG
		}

		
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			Fog {Mode Off}
			ZWrite On ZTest Less Cull Off

			CGPROGRAM

			#pragma multi_compile_instancing
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Cutoff;

			sampler2D _WindDistortionMap;
			float4 _WindDistortionMap_ST;
			float _WindStrength;
			float2 _WindFrequency;

			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 uv : TEXCOORD1;
			};

			v2f vert(appdata_full v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				float3 modelPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
				float2 uv = modelPos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
				float2 windSample = (tex2Dlod(_WindDistortionMap, float4(uv, 0, 0)).xy * 2 - 1) * _WindStrength;

				v.vertex.xz += windSample.xy * asin(v.vertex.y);
				o.uv = v.texcoord;
				TRANSFER_SHADOW_CASTER(o)
				return o;
			}

			float4 frag(v2f i) : COLOR
			{
				float4 col = tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
				clip(col.a - _Cutoff);
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}

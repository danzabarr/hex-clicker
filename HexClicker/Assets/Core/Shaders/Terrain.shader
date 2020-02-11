Shader "Custom/Terrain"
{
	Properties
	{
		//[Space] [Space]
		//_LatitudeScale("Latitude Scale (alters temperature according to X direction)", Range(1, 50)) = 5
		//_OffsetLatitude("Latitude Offset", Float) = 0
		//
		//[Space][Space]
		//_AltitudeTemperature("Altitude Scale (alters temperature according to Y direction)", Range(0.1,10)) = 1
		//_OffsetAltitude("Altitude Offset", Float) = 0
		//
		//[Space][Space]
		//_Temperature("Global Temperature", Range(-5,5)) = 0
		//
		//[Space][Space]
		//_WaterColor("Water Color", Color) = (.1,.2,.4,.5)
		//_WaterLevel("Water Level", Float) = 0
		//_WaterBlending("Water Blending", Range(0,1)) = .1

		[Space][Space]
		_Band0("Dirt Height", Float) = 0
		_Band1("Dirt/Grass Blending", Range(0,1)) = 1
		_Band2("Grass Height", Range(0,1)) = 1
		_Band3("Grass/Rock Blending", Range(0,1)) = 1

		[Space][Space]
		//_SlopeStart("Slope Start", Range(0, 1)) = .2
		_SlopeEnd("Terrain Slope Max"  , Range(0.001, 1)) = .7

		//[Space][Space]
		//_SnowColor("Snow Color", Color) = (1,1,1,1)
		//_SnowIntensity("Snow Intensity", Float) = 1
		//_SnowShininess("Snow Shininess", Range(0,1)) = .5
		//_SnowSpecular("Snow Specular", Range(0,1)) = .5
		//_SnowBlending("Snow Blending", Range(0,1)) = .5
		//_SnowSlopeMax("Snow Slope Max", Range(0,1)) = .5

		[Space][Space][Space][Space][Space][Space][Space][Space]
		_DirtAlbedo("Dirt Albedo", 2D) = "white" {}
		_DirtMetallic("Dirt Metallic", 2D) = "black" {}
		_DirtNormal("Dirt Normal", 2D) = "bump" {}

		[Space][Space][Space][Space][Space][Space][Space][Space]
		_GrassAlbedo("Grass Albedo", 2D) = "white" {}
		_GrassMetallic("Grass Metallic", 2D) = "black" {}
		_GrassNormal("Grass Normal", 2D) = "bump" {}

		[Space][Space][Space][Space][Space][Space][Space][Space]
		_RockAlbedo("Rock Albedo", 2D) = "white" {}
		_RockMetallic("Rock Metallic", 2D) = "black" {}
		_RockNormal("Rock Normal", 2D) = "bump" {}

		[Space][Space][Space][Space][Space][Space][Space][Space]
		_SandAlbedo("Sand Albedo", 2D) = "white" {}
		_SandMetallic("Sand Metallic", 2D) = "black" {}
		_SandNormal("Sand Normal", 2D) = "bump" {}
	}

	SubShader
	{
		Tags { "RenderType" = "Terrain"}

		Pass
		{
			Lighting On
			Tags
			{
				"RenderQueue" = "Opaque"
				"LightMode" = "ForwardBase"
			}

			CGPROGRAM

			#include "TerrainBase.cginc"
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex vert
			#pragma fragment frag

			half4 frag(vertexOutput input) : SV_Target
			{
				GroundType result = ground(input);

				float atten = UNITY_SHADOW_ATTENUATION(input, input.worldPos);
				float3 col = result.albedo;
				float diffuse = result.diffuse;
				float specular = result.specular;

				col *= max((diffuse + specular) * atten * _LightColor0, UNITY_LIGHTMODEL_AMBIENT);

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
				return half4(col, 1);
			}

			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ForwardAdd" }
			Blend One One
			ZWrite Off

			CGPROGRAM

			#include "UnityPBSLighting.cginc"
			#include "TerrainBase.cginc"

			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment pointLightFrag
			#pragma multi_compile_fwdadd_fullshadows

			#define POINT

			UnityLight CreateLight(float3 worldPos, float3 normal)
			{
				UnityLight light;
				light.dir = normalize(_WorldSpaceLightPos0.xyz - worldPos);

				float d = length(_WorldSpaceLightPos0.xyz - worldPos);
				float range = 50;
				float normalizedDist = d / range;
				float attenuation = saturate(1.0 / (1.0 + 25.0 * normalizedDist * normalizedDist) * saturate((1 - normalizedDist) * 5.0));

				light.color = _LightColor0.rgb * attenuation;
				return light;
			}

			float4 pointLightFrag(vertexOutput input) : SV_TARGET
			{
				GroundType result = ground(input);

				float3 specularTint;
				float oneMinusReflectivity;
				float albedo = DiffuseAndSpecularFromMetallic
				(
					result.albedo,
					result.metallic,
					specularTint,
					oneMinusReflectivity
				);

				UnityIndirect indirectLight;
				indirectLight.diffuse = 0;
				indirectLight.specular = 0;

				float3 cameraPos = _WorldSpaceCameraPos;
				cameraPos.x -= _OffsetLatitude;
				cameraPos.y -= _OffsetAltitude;

				float3 worldPos = input.worldPos;
				worldPos.x -= _OffsetLatitude;
				worldPos.y -= _OffsetAltitude;


				float3 viewDir = normalize(cameraPos - worldPos);
				float4 resultColor = UNITY_BRDF_PBS
				(
					albedo, specularTint,
					oneMinusReflectivity, result.shininess,
					result.worldNormal, viewDir,
					CreateLight(worldPos, result.worldNormal), indirectLight
				);

				resultColor = clamp(resultColor, 0, 3);

				resultColor *= UNITY_SHADOW_ATTENUATION(input, worldPos);

				return resultColor;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}


Shader "Custom/Terrain" {
	Properties{

		_WorldOffset("World Offset", Vector) = (0,0,0,0)

		_SlopeStart("Slope Start", Range(0, 1)) = .2
		_SlopeEnd("Slope End"  , Range(0, 1)) = .7


		/*
		_AmbientColor("Ambient Color", Color) = (1,1,1,1)
		_ShadowColor("Shadow Color", Color) = (0,0,0,1)
		_WaterColor("Water Color", Color) = (0,0,1,.5)
		_SnowColor("Snow Color", Color) = (1,1,1,1)
		_LatitudeScale("Latitude Scale", Float) = 5
		_WaterLevel("Water Level", Float) = 0
		_WaterBlending("Water Blending", Range(0,1)) = .1
		_AltitudeTemperature("Altitude Temperature", Range(0,1)) = 1
		_Temperature("Temperature", Range(-10,10)) = 0
		_Wetness("Wetness", Range(0, 1)) = 0
		_SnowAmount("Snow Amount", Range(0,1)) = 0
		_SnowBlending("Snow Blending", Range(0,1)) = .5
		_SnowShininess("Snow Shininess", Range(0,1)) = .5
		_SnowBrightness("Snow Brightness", Range(0,1)) = .5
		_SnowSpecular("Snow Specular", Range(0,1)) = .5
		_SnowSlopeMax("Snow Slope Max", Range(0,1)) = .5
		*/
		

		_Band0("Band 0", Float) = 1
		_Band1("Band 1", Float) = 1
		_Band2("Band 2", Float) = 1
		_Band3("Band 3", Float) = 1
		

		_MainTex ("Albedo", 2D) = "white" {}
		_BumpMap ("Normal", 2D) = "bump" {}
		_Metallic ("Metallic", 2D) = "black" {}

		_SandAlbedo("Sand Albedo", 2D) = "white" {}
        _SandNormal("Sand Normal", 2D) = "bump" {}
        _SandMetallic("Sand Metallic", 2D) = "black" {}

        _RockAlbedo("Rock Albedo", 2D) = "white" {}
        _RockNormal("Rock Normal", 2D) = "bump" {}
        _RockMetallic("Rock Metallic", 2D) = "black" {}

        _GrassAlbedo("Grass Albedo", 2D) = "white" {}
        _GrassNormal("Grass Normal", 2D) = "bump" {}
        _GrassMetallic("Grass Metallic", 2D) = "black" {}

        _DirtAlbedo("Dirt Albedo", 2D) = "white" {}
        _DirtNormal("Dirt Normal", 2D) = "bump" {}
        _DirtMetallic("Dirt Metallic", 2D) = "black" {}
   }

   SubShader {
		Tags { "RenderType" = "Terrain"}
        Pass {
            Lighting On
            Tags {
				"RenderQueue" = "Opaque"
				"LightMode" = "ForwardBase"
			}

            CGPROGRAM

			#include "TerrainBase.cginc"
            #pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex vert
            #pragma fragment frag
                         
            half4 frag(vertexOutput input) : SV_Target {
             
				GroundType result = ground(input);


				/*
				float3 specularTint;
				float oneMinusReflectivity;
				float3 resultColor = DiffuseAndSpecularFromMetallic(
					result.albedo, result.metallic, specularTint, oneMinusReflectivity
				);

				//resultColor *= max(result.light, _AmbientColor * _AmbientColor.a);

				if (input.worldPos.y <= _WaterLevel + _WaterBlending) {
					float waterAmount = _WaterColor.a;
					if (input.worldPos.y <= _WaterLevel + _WaterBlending && input.worldPos.y > _WaterLevel) waterAmount *= 1 - (input.worldPos.y - _WaterLevel) / _WaterBlending;
					resultColor = lerp(resultColor, _WaterColor, waterAmount);
				}
				
				*/

				//Get shadow attenuation
				float atten = SHADOW_ATTENUATION(input);

				float3 col = result.albedo;
				float diffuse = result.diffuse;
				float specular = result.specular;

				col *= max((diffuse + specular) * atten  * _LightColor0, UNITY_LIGHTMODEL_AMBIENT * .5) * 1.5;

				if (input.worldPos.y <= _WaterLevel) {
					float waterAmount = _WaterColor.a;
					if (input.worldPos.y <= _WaterLevel && input.worldPos.y > _WaterLevel - _WaterBlending) waterAmount *= 1 - (input.worldPos.y - _WaterLevel + _WaterBlending) / _WaterBlending;
					col = lerp(col, _WaterColor * _LightColor0, waterAmount);
				}



				//Keep hdr color within 0-5 range
				col = clamp(col, 0, 5);

				UNITY_APPLY_FOG(input.fogCoord, col);



				return half4(col, 1);
			}
             
            ENDCG
        }

		Pass {
			Tags {
				"LightMode" = "ForwardAdd"
			}
			Blend One One
			ZWrite Off

			CGPROGRAM

			#include "TerrainBase.cginc"
			#include "UnityPBSLighting.cginc"
			
			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment pointLightFrag

			#define POINT

			UnityLight CreateLight (float3 worldPos, float3 normal) {
				UnityLight light;
				light.dir = normalize(_WorldSpaceLightPos0.xyz - worldPos);

				float d = length(_WorldSpaceLightPos0.xyz - worldPos);
				float range = 50;
				float normalizedDist = d / range;
				float attenuation = saturate(1.0 / (1.0 + 25.0 * normalizedDist * normalizedDist) * saturate((1 - normalizedDist) * 5.0));

				light.color = _LightColor0.rgb * attenuation;
				return light;
			}

			float4 pointLightFrag(vertexOutput input) : SV_TARGET {

				GroundType result = ground(input);


				float3 specularTint;
				float oneMinusReflectivity;
				float albedo = DiffuseAndSpecularFromMetallic(
					result.albedo, result.metallic, specularTint, oneMinusReflectivity
				);

				UnityIndirect indirectLight;
				indirectLight.diffuse = 0;
				indirectLight.specular = 0;

				float3 viewDir = normalize(_WorldSpaceCameraPos - _WorldOffset - input.worldPos);
	
				float4 resultColor = UNITY_BRDF_PBS(
					albedo, specularTint,
					oneMinusReflectivity, result.shininess,
					result.worldNormal, viewDir,
					CreateLight(input.worldPos + _WorldOffset, result.worldNormal), indirectLight
				);



				if (input.worldPos.y <= _WaterLevel) {
					float waterAmount = _WaterColor.a;
					if (input.worldPos.y <= _WaterLevel && input.worldPos.y > _WaterLevel - _WaterBlending) waterAmount *= 1 - (input.worldPos.y - _WaterLevel + _WaterBlending) / _WaterBlending;
					resultColor.rgb = min(resultColor.rgb, lerp(_WaterColor.rgb, resultColor.rgb, waterAmount));
				}


				resultColor = clamp(resultColor, 0, 3);

				return resultColor;
			}

			ENDCG
		}
    }
    
    FallBack "Diffuse"
}


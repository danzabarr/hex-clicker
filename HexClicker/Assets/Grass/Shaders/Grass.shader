Shader "Roystan/Grass"
{
    Properties
    {
		[Header(Shading)]
        _TopColor("Top Color", Color) = (1,1,1,1)
		_BottomColor("Bottom Color", Color) = (1,1,1,1)
		_TranslucentGain("Translucent Gain", Range(0,1)) = 0.5
		_Smoothness("Smoothness", Range(0, 1)) = .5
		[Space]
		_TessellationUniform ("Tessellation Uniform", Range(1, 64)) = 1
		[Header(Blades)]
		_BladeWidth("Blade Width", Float) = 0.05
		_BladeWidthRandom("Blade Width Random", Float) = 0.02
		_BladeHeight("Blade Height", Float) = 0.5
		_BladeHeightRandom("Blade Height Random", Float) = 0.3
		_BladeForward("Blade Forward Amount", Float) = 0.38
		_BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2
		_BendRotationRandom("Bend Rotation Random", Range(0, 1)) = 0.2
		_ClipXZ("Clip XZ", Float) = 0.0001
		[Header(Wind)]
		_WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
		_WindStrength("Wind Strength", Float) = 1
		_WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
		[Header(Altitude Culling)]
		_MinAlt("Minimum Altitude", Float) = 10
		_MaxAlt("Maximum Altitude", Float) = 90
		_AltBlending("Altitude Blending", Range(0, 1)) = .2
		_MaxSlope("Maximum Slope", Range(0, 1)) = .03
		_SlopeBlending("Slope Blending", Range(0, 1)) = .2
		[Header(Temperature Culling)]
		_MinTemp("Minimum Temperature", Range(-10,10)) = 0.1
		_MaxTemp("Maximum Temperature", Range(-10,10)) = 0.9
		[Header(Masking)]
		_GrassMask("Mask", 2D) = "white" {}
		_MaskThreshold("Mask Threshold", Range(0, 1)) = 0.5
		_MaskBlending("Mask Blending", Range(0, 1)) = 0.5
		_MaskMinimum("Mask Minimum", Range(0, 1)) = 0.5
		_DistanceCulling("Distance Culling", Float) = 500

		_CameraMask("Camera Mask", 2D) = "black" {}
    }

    SubShader
    {
		Cull Off
        Pass
        {
			Tags
			{
				"RenderType" = "Opaque"
				"LightMode" = "ForwardBase"
			}

            CGPROGRAM
            #pragma vertex vert
			#pragma geometry geo
            #pragma fragment frag
			#pragma hull hull
			#pragma domain domain
			#pragma target 4.6
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#include "GrassBase.cginc"

			float3 _TopColor;
			float3 _BottomColor;
			float _TranslucentGain; 

			float4 frag (geometryOutput i, fixed facing : VFACE) : SV_Target
            {			
				half3 col = lerp(_BottomColor, _TopColor, i.uv.y);
				half3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos.xyz));

				half3 normal = facing > 0 ? i.normal : -i.normal;
				half diffuse = saturate(saturate(dot(normal, _WorldSpaceLightPos0)) + _TranslucentGain);
				//half3 ambient = ShadeSH9(float4(normal, 1)) + 0.5;
				half specular = pow(max(0.0, dot(reflect(-lightDir, normal), i.viewDir)), 1.5) * 1.25;
				//half3 lightIntensity = clamp(diffuse + ambient + specular * .5, 0, 1.25);
                half atten = SHADOW_ATTENUATION(i);

				col *= max((diffuse + specular) * atten  * _LightColor0, UNITY_LIGHTMODEL_AMBIENT);
				col = saturate(col);

				UNITY_APPLY_FOG(i.fogCoord, col);

				return float4(col, 1);
			}

            ENDCG
        }
		/*
		*/
		Pass
		{
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag
			#pragma hull hull
			#pragma domain domain
			#pragma target 4.6
			#pragma multi_compile_shadowcaster
			#include "GrassBase.cginc"

			float4 frag(geometryOutput i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
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

			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag
			#pragma hull hull
			#pragma domain domain
			#pragma target 4.6

			#define POINT

			#include "AutoLight.cginc"
			#include "GrassBase.cginc"
			#include "UnityPBSLighting.cginc"

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

			float4 frag (geometryOutput i, fixed facing : VFACE) : SV_Target
			{
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

				float3 albedo = 0;

				float metallic = 0;
				
				half3 normal = facing > 0 ? i.normal : -i.normal;

				float smoothness = _Smoothness;
				float3 specularTint;
				float oneMinusReflectivity;

				albedo = DiffuseAndSpecularFromMetallic(
					albedo, metallic, specularTint, oneMinusReflectivity
				);

				UnityIndirect indirectLight;
				indirectLight.diffuse = 0;
				indirectLight.specular = 0;

				float4 result = UNITY_BRDF_PBS(
					albedo, specularTint,
					oneMinusReflectivity, smoothness,
					normal, viewDir,
					CreateLight(i.worldPos + _WorldOffset, normal), indirectLight
				);

				result = clamp(result, 0, 1);

				

				return result;
			}

			ENDCG
		}
    }
}

#ifndef TERRAIN_BASE_INCLUDE
#define TERRAIN_BASE_INCLUDE

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

uniform half4 _WaterColor, _SnowColor;
uniform float _LatitudeScale, _WaterLevel, _WaterBlending, _AltitudeTemperature, _Temperature, _SnowBlending, _SnowShininess, _SnowIntensity, _SnowSpecular, _SnowSlopeMax;
uniform float _OffsetAltitude, _OffsetLatitude;

sampler2D  _SandAlbedo, _SandNormal, _SandMetallic;
sampler2D  _DirtAlbedo, _DirtNormal, _DirtMetallic;
sampler2D  _RockAlbedo, _RockNormal, _RockMetallic;
sampler2D _GrassAlbedo, _GrassNormal, _GrassMetallic;

float4  _SandAlbedo_ST, _SandNormal_ST, _SandMetallic_ST;
float4  _DirtAlbedo_ST, _DirtNormal_ST, _DirtMetallic_ST;
float4  _RockAlbedo_ST, _RockNormal_ST, _RockMetallic_ST;
float4 _GrassAlbedo_ST, _GrassNormal_ST, _GrassMetallic_ST;

float _SlopeStart, _SlopeEnd;

float _Band0, _Band1, _Band2, _Band3;

struct vertexOutput
{
	float4 pos : SV_POSITION;
	float3 worldPos : TEXCOORD0;
	half3 worldNormal : TEXCOORD1;
	float2 uv : TEXCOORD2;

	half3 tspace0 : TEXCOORD4; // tangent.x, bitangent.x, normal.x
	half3 tspace1 : TEXCOORD5; // tangent.y, bitangent.y, normal.y
	half3 tspace2 : TEXCOORD6; // tangent.z, bitangent.z, normal.z

	half3 viewDir : TEXCOORD7;
	half3 normal : TEXCOORD8;

	SHADOW_COORDS(9)
		UNITY_FOG_COORDS(10)
};

vertexOutput vert(appdata_full v)
{
	vertexOutput output;

	output.pos = UnityObjectToClipPos(v.vertex);
	output.uv = v.texcoord;
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

struct GroundType
{
	half3 albedo, normal, worldNormal;
	half diffuse, specular, metallic, shininess;
};

GroundType lerp(GroundType a, GroundType b, float t)
{
	GroundType c;
	c.albedo = lerp(a.albedo, b.albedo, t);
	c.normal = lerp(a.normal, b.normal, t);
	c.worldNormal = lerp(a.worldNormal, b.worldNormal, t);
	c.diffuse = lerp(a.diffuse, b.diffuse, t);
	c.specular = lerp(a.specular, b.specular, t);
	c.metallic = lerp(a.metallic, b.metallic, t);
	c.shininess = lerp(a.shininess, b.shininess, t);
	return c;
}

GroundType ground(vertexOutput input)
{
	half3 lightDir = normalize(UnityWorldSpaceLightDir(input.worldPos.xyz));
	float diffuse = dot(lightDir.xyz, input.normal);


	GroundType sand;
	sand.albedo = tex2D(_SandAlbedo, TRANSFORM_TEX(input.uv, _SandAlbedo)).rgb;
	sand.normal = UnpackNormal(tex2D(_SandNormal, TRANSFORM_TEX(input.uv, _SandNormal)));
	sand.worldNormal = half3(dot(input.tspace0, sand.normal), dot(input.tspace1, sand.normal), dot(input.tspace2, sand.normal));
	half4 sandMetallic = tex2D(_SandMetallic, TRANSFORM_TEX(input.uv, _SandMetallic));
	sand.metallic = sandMetallic.r;
	sand.shininess = sandMetallic.a;
	sand.diffuse = saturate(dot(lightDir.xyz, sand.worldNormal));
	sand.specular = saturate(pow(max(0.0, dot(reflect(-lightDir, sand.worldNormal), input.viewDir)), max(1, sand.shininess * 50)) * sand.metallic);


	GroundType rock;
	rock.albedo = tex2D(_RockAlbedo, TRANSFORM_TEX(input.uv, _RockAlbedo)).rgb;
	rock.normal = UnpackNormal(tex2D(_RockNormal, TRANSFORM_TEX(input.uv, _RockNormal)));
	rock.worldNormal = half3(dot(input.tspace0, rock.normal), dot(input.tspace1, rock.normal), dot(input.tspace2, rock.normal));
	half4 rockMetallic = tex2D(_RockMetallic, TRANSFORM_TEX(input.uv, _RockMetallic));
	rock.metallic = rockMetallic.r;
	rock.shininess = rockMetallic.a;
	rock.diffuse = saturate(dot(lightDir.xyz, rock.worldNormal));
	rock.specular = saturate(pow(max(0.0, dot(reflect(-lightDir, rock.worldNormal), input.viewDir)), max(1, rock.shininess * 50)) * rock.metallic);


	GroundType gras;
	gras.albedo = tex2D(_GrassAlbedo, TRANSFORM_TEX(input.uv, _GrassAlbedo)).rgb;
	gras.normal = UnpackNormal(tex2D(_GrassNormal, TRANSFORM_TEX(input.uv, _GrassNormal)));
	gras.worldNormal = half3(dot(input.tspace0, gras.normal), dot(input.tspace1, gras.normal), dot(input.tspace2, gras.normal));
	half4 grasMetallic = tex2D(_GrassMetallic, TRANSFORM_TEX(input.uv, _GrassMetallic));
	gras.metallic = grasMetallic.r;
	gras.shininess = grasMetallic.a;
	gras.diffuse = saturate(dot(lightDir.xyz, gras.worldNormal));
	gras.specular = saturate(pow(max(0.0, dot(reflect(-lightDir, gras.worldNormal), input.viewDir)), max(1, gras.shininess * 50)) * gras.metallic);


	GroundType dirt;
	dirt.albedo = tex2D(_DirtAlbedo, TRANSFORM_TEX(input.uv, _DirtAlbedo)).rgb;
	dirt.normal = UnpackNormal(tex2D(_DirtNormal, TRANSFORM_TEX(input.uv, _DirtNormal)));
	dirt.worldNormal = half3(dot(input.tspace0, dirt.normal), dot(input.tspace1, dirt.normal), dot(input.tspace2, dirt.normal));
	half4 dirtMetallic = tex2D(_DirtMetallic, TRANSFORM_TEX(input.uv, _DirtMetallic));
	dirt.metallic = dirtMetallic.r;
	dirt.shininess = dirtMetallic.a;
	dirt.diffuse = saturate(dot(lightDir.xyz, dirt.worldNormal));
	dirt.specular = saturate(pow(max(0.0, dot(reflect(-lightDir, dirt.worldNormal), input.viewDir)), max(1, dirt.shininess * 50)) * dirt.metallic);


	GroundType snow;
	snow.albedo = _SnowColor * _SnowIntensity;
	snow.normal = input.normal;
	snow.worldNormal = input.worldNormal;
	snow.metallic = _SnowSpecular;
	snow.shininess = _SnowShininess;
	snow.diffuse = saturate(dot(lightDir.xyz, snow.worldNormal));
	snow.specular = saturate(pow(max(0.0, dot(reflect(-lightDir, snow.worldNormal), input.viewDir)), max(1, _SnowShininess * 50)) * _SnowSpecular);

	GroundType result, slope, hi, mid, low, dirtSand;

	half y = input.worldPos.y;

	half s0 = _WaterLevel + _Band0;
	half s1 = s0 + _Band1;
	half s2 = s1 + _Band2;
	half s3 = s2 + _Band3;

	hi = rock;
	mid = gras;
	low = dirt;

	float temperature = _Temperature + 0.5;
	temperature += input.worldPos.x / _LatitudeScale;
	temperature -= max(0, input.worldPos.y * _AltitudeTemperature) - _AltitudeTemperature / 4;

	half t0 = -5;
	half t1 = 0;
	half t2 = 4;
	half t3 = 5;

	if (temperature < t0)
	{
		//Cold
		mid = dirt;
		dirtSand = dirt;
	}
	else if (temperature < t1)
	{
		//Cold - Temperate
		mid = lerp(dirt, gras, (temperature - t0) / (t1 - t0));
		dirtSand = dirt;
	}
	else if (temperature < t2)
	{
		//Temperate
		mid = gras;
		dirtSand = dirt;
		dirtSand = lerp(dirt, sand, (temperature - t1) / (t2 - t1));
	}
	else if (temperature < t3)
	{
		//Temperate - Warm
		mid = lerp(gras, sand, (temperature - t2) / (t3 - t2));
		dirtSand = sand;
	}
	else
	{
		//Warm
		mid = sand;
		dirtSand = sand;
	}

	if (y > s3)
	{
		//High band
		result = hi;
		slope = rock;
	}
	else if (y > s2)
	{
		//High - Mid
		result = lerp(mid, hi, (y - s2) / (s3 - s2));
		slope = rock;
	}
	else if (y > s1)
	{
		//Mid Band
		result = mid;
		slope = lerp(dirtSand, rock, (y - s1) / (s2 - s1));
	}
	else if (y > s0)
	{
		//Mid - Low
		result = lerp(low, mid, (y - s0) / (s1 - s0));
		slope = dirtSand;
	}
	else
	{
		//Low Band
		result = low;
		slope = dirtSand;
	}

	//Slope textures
	float slopeAmount = 1.0f - input.normal.y;
	if (slopeAmount > _SlopeEnd)
	{
		result = slope;
	}
	else if (slopeAmount >= _SlopeStart)
	{
		result = lerp(result, slope, (slopeAmount - _SlopeStart) * (1 / (_SlopeEnd - _SlopeStart)));
	}

	//Remove snow under/near water.
	float snowAmount = saturate((_SnowColor.a - .4) / .2) * saturate((-temperature + _SnowBlending) / _SnowBlending * .5);
	if (input.worldPos.y < _WaterLevel)
	{
		snowAmount *= 0;
	}
	else if (input.worldPos.y >= _WaterLevel && input.worldPos.y < _WaterLevel + _SnowBlending)
	{
		snowAmount *= 1 - ((_WaterLevel + _SnowBlending) - input.worldPos.y) / _SnowBlending;
	}

	//Remove snow on slopes
	if (slopeAmount >= _SnowSlopeMax && slopeAmount < _SnowSlopeMax + _SnowBlending * 5)
	{
		snowAmount *= 1 - (slopeAmount - _SnowSlopeMax) / (_SnowBlending * 5);
	}
	else if (slopeAmount > _SnowSlopeMax)
	{
		snowAmount *= 0;
	}

	result = lerp(result, snow, saturate(snowAmount));
	return result;
}

#endif
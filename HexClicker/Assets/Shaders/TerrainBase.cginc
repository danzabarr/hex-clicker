#ifndef TERRAIN_BASE_INCLUDE
#define TERRAIN_BASE_INCLUDE

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

uniform half4 _AmbientColor, _ShadowColor, _WaterColor, _SnowColor;
uniform float _LatitudeScale, _WaterLevel, _WaterBlending, _AltitudeTemperature, _Temperature, _Wetness, _SnowAmount, _SnowBlending, _SnowShininess, _SnowBrightness, _SnowSpecular, _SnowSlopeMax;
uniform float3 _WorldOffset;

sampler2D _MainTex, _BumpMap, _Metallic;
float4 _MainTex_ST, _BumpMap_ST, _Metallic_ST;


sampler2D  _SandAlbedo,  _SandNormal,  _SandMetallic;
sampler2D  _DirtAlbedo,  _DirtNormal,  _DirtMetallic;
sampler2D  _RockAlbedo,  _RockNormal,  _RockMetallic;
sampler2D _GrassAlbedo, _GrassNormal, _GrassMetallic;

float4  _SandAlbedo_ST,  _SandNormal_ST,  _SandMetallic_ST;
float4  _DirtAlbedo_ST,  _DirtNormal_ST,  _DirtMetallic_ST;
float4  _RockAlbedo_ST,  _RockNormal_ST,  _RockMetallic_ST;
float4 _GrassAlbedo_ST, _GrassNormal_ST, _GrassMetallic_ST;

float _SlopeStart, _SlopeEnd;

float _Band0, _Band1, _Band2, _Band3;

struct vertexOutput {
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

vertexOutput vert(appdata_full input) {
	vertexOutput output;

	output.pos = UnityObjectToClipPos(input.vertex);
	output.uv = input.texcoord;
	output.viewDir = normalize(ObjSpaceViewDir(input.vertex));
	output.normal = input.normal;

	half3 wNormal = UnityObjectToWorldNormal(input.normal);
	half3 wTangent = UnityObjectToWorldDir(input.tangent.xyz);
	// compute bitangent from cross product of normal and tangent
	half tangentSign = input.tangent.w * unity_WorldTransformParams.w;
	half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
	// output the tangent space matrix
	output.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
	output.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
	output.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
                
	output.worldPos = mul(unity_ObjectToWorld, input.vertex).xyz - _WorldOffset;
	output.worldNormal = wNormal;
                
	TRANSFER_SHADOW(output);
	UNITY_TRANSFER_FOG(output, output.pos);
                                 
	return output;
}

const int TEXTURE_SIZE = 512;
const int ATLAS_SIZE = 4;

float2 TransformAtlasedTex(float2 uv, int2 atlasPos, float2 tiling, float2 offset) {
	float atlasSize = 2;
	float2 atlasedUV = uv;
	atlasedUV += .5;
	atlasedUV *= tiling;
	atlasedUV += offset;

	/*
	atlasedUV /= 1024.0;
	atlasedUV *= 1024.0 - 16;
	atlasedUV -= 8.0 / 1024.0;
	*/
	atlasedUV %= 1.0;
	atlasedUV += 1.0;
	atlasedUV %= 1.0;

	atlasedUV /= atlasSize;
	atlasedUV += atlasPos * (1 / atlasSize);

	return atlasedUV;
}

struct GroundType {
	half3 albedo, worldNormal;
	half diffuse, specular, metallic, shininess;
};


GroundType lerp(GroundType a, GroundType b, float t) {
	GroundType c;
	c.albedo = lerp(a.albedo, b.albedo, t);
	c.worldNormal = lerp(a.worldNormal, b.worldNormal, t);
	c.diffuse = lerp(a.diffuse, b.diffuse, t);
	c.specular = lerp(a.specular, b.specular, t);
	c.metallic = lerp(a.metallic, b.metallic, t);
	c.shininess = lerp(a.shininess, b.shininess, t);
	return c;
}

float GetMipLevel(float2 iUV, float2 iTextureSize)
{
	float2 dx = ddx(iUV * iTextureSize.x);
	float2 dy = ddy(iUV * iTextureSize.y);
	float d = max(dot(dx, dx), dot(dy,dy));
	return 0.5 * log2(d);
}

GroundType ground(vertexOutput input) {
	half3 lightDir = normalize(UnityWorldSpaceLightDir(input.worldPos.xyz));
	float diffuse = dot(lightDir.xyz, input.normal);
				
	float slopeAmount = 1.0f - input.normal.y;

	float temperature = _Temperature / 50;
	temperature += (-input.worldPos.z + _LatitudeScale / 2) / _LatitudeScale;
	temperature -= input.worldPos.y / _AltitudeTemperature;
	//if (input.worldPos.y < (_WaterLevel + 20)) temperature -= (1 - input.worldPos.y / (_WaterLevel + 20)) * (temperature - .75) / .75 * (1 - slopeAmount * 2);
	//temperature += diffuse * .1;
	temperature -= .125;
	temperature = clamp(temperature, -1, 1);

	float mipLevel = GetMipLevel(input.uv, float2(2048, 2048));

	GroundType sand;
	//float4 sandAtlasedUV = float4(TransformAtlasedTex(input.uv, int2(0, 0), float2(1, 1), float2(0, 0)), 0, mipLevel);
	//sand.albedo = tex2Dlod(_MainTex, sandAtlasedUV).rgb;
	sand.albedo = tex2D(_SandAlbedo, TRANSFORM_TEX(input.uv, _SandAlbedo)).rgb;
	//half3 sandNormal = UnpackNormal(tex2Dlod(_BumpMap, sandAtlasedUV));
	half3 sandNormal = UnpackNormal(tex2D(_SandNormal, TRANSFORM_TEX(input.uv, _SandNormal)));
	sand.worldNormal =  half3(dot(input.tspace0, sandNormal), dot(input.tspace1, sandNormal), dot(input.tspace2, sandNormal));
	//half4 sandMetallic = tex2Dlod(_Metallic, sandAtlasedUV);
	half4 sandMetallic = tex2D(_SandMetallic, TRANSFORM_TEX(input.uv, _SandMetallic));
	sand.metallic = sandMetallic.r;
	sand.shininess = sandMetallic.a;
	sand.diffuse = saturate(dot(lightDir.xyz, sand.worldNormal)) * 1.25;
	sand.specular = saturate(pow(max(0.0, dot(reflect(-lightDir, sand.worldNormal), input.viewDir)), max(1, sand.shininess * 50)) * sand.metallic);

	GroundType rock;
	//float4 rockAtlasedUV = float4(TransformAtlasedTex(input.uv, int2(0, 1), float2(2, 2), float2(0, 0)), 0, mipLevel);
	//rock.albedo = tex2Dlod(_MainTex, rockAtlasedUV).rgb;
	rock.albedo = tex2D(_RockAlbedo, TRANSFORM_TEX(input.uv, _RockAlbedo)).rgb;
	//half3 rockNormal = UnpackNormal(tex2Dlod(_BumpMap, rockAtlasedUV));
	half3 rockNormal = UnpackNormal(tex2D(_RockNormal, TRANSFORM_TEX(input.uv, _RockNormal)));
	rock.worldNormal =  half3(dot(input.tspace0, rockNormal), dot(input.tspace1, rockNormal), dot(input.tspace2, rockNormal));
	//half4 rockMetallic = tex2Dlod(_Metallic, rockAtlasedUV);
	half4 rockMetallic = tex2D(_RockMetallic, TRANSFORM_TEX(input.uv, _RockMetallic));
	rock.metallic = rockMetallic.r;
	rock.shininess = rockMetallic.a;
	rock.diffuse = saturate(dot(lightDir.xyz, rock.worldNormal)) * 1.25;
	rock.specular = saturate(pow(max(0.0, dot(reflect(-lightDir, rock.worldNormal), input.viewDir)), max(1, rock.shininess * 50)) * rock.metallic);

	GroundType gras;
	//float4 grasAtlasedUV = float4(TransformAtlasedTex(input.uv, int2(1, 1), float2(4, 4), float2(0, 0)), 0, mipLevel);
	//gras.albedo = tex2Dlod(_MainTex, grasAtlasedUV).rgb;
	gras.albedo = tex2D(_GrassAlbedo, TRANSFORM_TEX(input.uv, _GrassAlbedo)).rgb;
	//half3 grasNormal = UnpackNormal(tex2Dlod(_BumpMap, grasAtlasedUV));
	half3 grasNormal = UnpackNormal(tex2D(_GrassNormal, TRANSFORM_TEX(input.uv, _GrassNormal)));
	gras.worldNormal =  half3(dot(input.tspace0, grasNormal), dot(input.tspace1, grasNormal), dot(input.tspace2, grasNormal));
	//half4 grasMetallic = tex2D(_Metallic, grasAtlasedUV);
	half4 grasMetallic = tex2D(_GrassMetallic, TRANSFORM_TEX(input.uv, _GrassMetallic));
	gras.metallic = grasMetallic.r;
	gras.shininess = grasMetallic.a;
	gras.diffuse = saturate(dot(lightDir.xyz, gras.worldNormal)) * 1.25;
	gras.specular = saturate(pow(max(0.0, dot(reflect(-lightDir, gras.worldNormal), input.viewDir)), max(1, gras.shininess * 50)) * gras.metallic);

	GroundType dirt;
	//float4 dirtAtlasedUV = float4(TransformAtlasedTex(input.uv, int2(1, 0), float2(10, 10), float2(0, 0)), 0, mipLevel);
	//dirt.albedo = tex2Dlod(_MainTex, dirtAtlasedUV).rgb;
	dirt.albedo = tex2D(_DirtAlbedo, TRANSFORM_TEX(input.uv, _DirtAlbedo)).rgb;
	//half3 dirtNormal = UnpackNormal(tex2Dlod(_BumpMap, dirtAtlasedUV));
	half3 dirtNormal = UnpackNormal(tex2D(_DirtNormal, TRANSFORM_TEX(input.uv, _DirtNormal)));
	dirt.worldNormal =  half3(dot(input.tspace0, dirtNormal), dot(input.tspace1, dirtNormal), dot(input.tspace2, dirtNormal));
	//half4 dirtMetallic = tex2D(_Metallic, dirtAtlasedUV);
	half4 dirtMetallic = tex2D(_DirtMetallic, TRANSFORM_TEX(input.uv, _DirtMetallic));
	dirt.metallic = dirtMetallic.r;
	dirt.shininess = dirtMetallic.a;
	dirt.diffuse = saturate(dot(lightDir.xyz, dirt.worldNormal)) * 1.25;
	dirt.specular = saturate(pow(max(0.0, dot(reflect(-lightDir, dirt.worldNormal), input.viewDir)), max(1, dirt.shininess * 50)) * dirt.metallic);

	GroundType snow;
	snow.albedo = _SnowColor * 1.5;
	snow.worldNormal = input.worldNormal;
	snow.metallic = _SnowSpecular;
	snow.shininess = _SnowShininess;
	snow.diffuse = saturate(dot(lightDir.xyz, snow.worldNormal));
	snow.specular =  saturate(pow(max(0.0, dot(reflect(-lightDir, snow.worldNormal), input.viewDir)), max(1, _SnowShininess * 50)) * _SnowSpecular);

	GroundType result, slope, hi, mid, low, dirtSand;


	half y = input.worldPos.y;

	half s0 = _WaterLevel + _Band0;
	half s1 = s0 + _Band1;
	half s2 = s1 + _Band2;// +(temperature + 1) * 50;
	half s3 = s2 + _Band3;

	hi = rock;
	mid = gras;
	low = dirt;

	half t0 = .7;
	half t1 = t0 + .3;
	half t2 = t1 + .3;

	if (temperature < t0) {
		mid = gras;
		dirtSand = dirt;
	}
	else if (temperature < t1) {
		mid = lerp(gras, sand, (temperature - t0) / (t1 - t0));
		dirtSand = lerp(dirt, sand, (temperature - t0) / (t1 - t0));
	}
	else {
		mid = sand;
		dirtSand = sand;
	}

	if (y > s3) {
		result = hi;
		slope = rock;
	}
	else if (y > s2) {
		result = lerp(mid, hi, (y - s2) / (s3 - s2));
		slope = rock;
	}
	else if (y > s1) {
		result = mid;
		slope = lerp(dirtSand, rock, (y - s1) / (s2 - s1));
	}
	else if (y > s0) {
		result = lerp(low, mid, (y - s0) / (s1 - s0));
		slope = dirtSand;
	}
	else {
		result = low;
		slope = dirtSand;
		//slope = dirt;
	}

	//slopeAmount = 1 - result.worldNormal.y;//(1.0f - result.worldNormal.y) * .25;
	//slopeAmount /= 1.25;

	float bumpedSlopeAmount = slopeAmount;//1 - result.worldNormal.y;

	if (bumpedSlopeAmount > _SlopeEnd) {
		result = slope;
	}
	else
	if (bumpedSlopeAmount >= _SlopeStart) {
		result = lerp(result, slope, (bumpedSlopeAmount - _SlopeStart) * (1 / (_SlopeEnd - _SlopeStart)));
	}

	float snowAmount =  saturate((_SnowAmount - .4) / .2) * saturate((-temperature * 5 + _SnowBlending) / _SnowBlending);
				
	//Remove snow under/near water.
	if (input.worldPos.y < _WaterLevel) snowAmount *= 0;
	//else if (input.worldPos.y >= _WaterLevel && input.worldPos.y < _WaterLevel + _SnowBlending) snowAmount *= 1 - ((_WaterLevel + _SnowBlending) - input.worldPos.y) / _SnowBlending;
	else {
		//Remove snow on slopes
		if (slopeAmount >= _SnowSlopeMax && slopeAmount < _SnowSlopeMax + _SnowBlending) {
			snowAmount *= 1 - (slopeAmount - _SnowSlopeMax) / _SnowBlending;
		} else if (slopeAmount > _SnowSlopeMax) snowAmount *= 0;
	}


	result = lerp(result, snow, saturate(snowAmount));


	

	

	return result;
}

#endif
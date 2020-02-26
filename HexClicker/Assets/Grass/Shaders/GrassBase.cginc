#ifndef GRASS_BASE_INCLUDE
#define GRASS_BASE_INCLUDE

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "Autolight.cginc"
#include "CustomTessellation.cginc"
#include "Assets/GlobalShaders/FogOfWar.cginc"

uniform float _LatitudeScale;
uniform float _WaterLevel;
uniform float _AltitudeTemperature;
uniform float _Temperature;
uniform float _Wetness;
uniform float3 _WorldOffset;
uniform float3 _CameraFocalPoint;

struct geometryOutput
{
	float4 pos : SV_POSITION;
	float3 worldPos : TEXCOORD3;
#if UNITY_PASS_FORWARDBASE		
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
	//unityShadowCoord4 is defined as a float4 in UnityShadowLibrary.cginc.
	unityShadowCoord4 _ShadowCoord : TEXCOORD1;
#elif UNITY_PASS_FORWARDADD
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
	//unityShadowCoord4 is defined as a float4 in UnityShadowLibrary.cginc.
	unityShadowCoord4 _ShadowCoord : TEXCOORD1;
#endif
	float3 viewDir : TEXCOORD4;
	UNITY_FOG_COORDS(2)
};

// Simple noise function, sourced from http://answers.unity.com/answers/624136/view.html
// Extended discussion on this function can be found at the following link:
// https://forum.unity.com/threads/am-i-over-complicating-this-random-function.454887/#post-2949326
// Returns a number in the 0...1 range.
float rand(float3 co)
{
	return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
}

// Construct a rotation matrix that rotates around the provided axis, sourced from:
// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
float3x3 AngleAxis3x3(float angle, float3 axis)
{
	float c, s;
	sincos(angle, s, c);

	float t = 1 - c;
	float x = axis.x;
	float y = axis.y;
	float z = axis.z;

	return float3x3(
		t * x * x + c, t * x * y - s * z, t * x * z + s * y,
		t * x * y + s * z, t * y * y + c, t * y * z - s * x,
		t * x * z - s * y, t * y * z + s * x, t * z * z + c
		);
}

geometryOutput VertexOutput(float3 pos, float3 normal, float2 uv)
{
	geometryOutput o;

	o.pos = UnityObjectToClipPos(pos);
		

#if UNITY_PASS_FORWARDBASE
	o.normal = UnityObjectToWorldNormal(normal);
	o.uv = uv;
	// Shadows are sampled from a screen-space shadow map texture.
	o._ShadowCoord = ComputeScreenPos(o.pos);
#elif UNITY_PASS_FORWARDADD
	o.normal = UnityObjectToWorldNormal(normal);
	o.uv = uv;
	// Shadows are sampled from a screen-space shadow map texture.
	o._ShadowCoord = ComputeScreenPos(o.pos);
		
#elif UNITY_PASS_SHADOWCASTER
	// Applying the bias prevents artifacts from appearing on the surface.
	o.pos = UnityApplyLinearShadowBias(o.pos);
		
#endif

	o.worldPos = mul(unity_ObjectToWorld, float4(pos.xyz, 1.0)).xyz - _WorldOffset;
	o.viewDir = normalize(ObjSpaceViewDir(float4(pos.xyz, 1.0))).xyz;
	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

geometryOutput GenerateGrassVertex(float3 vertexPosition, float width, float height, float forward, float2 uv, float3x3 transformMatrix)
{
	float3 tangentPoint = float3(width, forward, height);

	float3 tangentNormal = normalize(float3(0, -1, forward));

	float3 localPosition = vertexPosition + mul(transformMatrix, tangentPoint);
	float3 localNormal = mul(transformMatrix, tangentNormal);
		 

	return VertexOutput(localPosition, localNormal, uv);
}

float _BladeHeight;
float _BladeHeightRandom;

float _BladeWidthRandom;
float _BladeWidth;

float _BladeForward;
float _BladeCurve;

float _BendRotationRandom;

sampler2D _WindDistortionMap;
float4 _WindDistortionMap_ST;

float _WindStrength;
float2 _WindFrequency;

float _MaxAlt,_MinAlt;
float _AltBlending;

float _MaxSlope, _HighSlope, _HighSlopeBladeSize;
float _SlopeBlending;

float _MinTemp, _LowTemp, HighTemp, _MaxTemp;

float _MinTempBladeWidth, _MinTempBladeHeight;
float _LowTempBladeWidth, _LowTempBladeHeight;
float _HighTempBladeWidth, _HiTempBladeHeight;
float _MaxTempBladeWidth, _MaxTempBladeHeight;

float _ClipXZ, _DistanceCulling;

float _MaskThreshold;
float _MaskBlending;
float _MaskMinimum;

sampler2D _CameraMask;
float4 _CameraMask_ST;

float _Smoothness;

#define BLADE_SEGMENTS 1

// Geometry program that takes in a single triangle and outputs a blade
// of grass at that triangle first vertex position, aligned to the vertex's normal.
[maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
void geo(triangle vertexOutput IN[3], inout TriangleStream<geometryOutput> triStream)
{
	float3 pos = IN[0].vertex.xyz;
	float3 world = mul(unity_ObjectToWorld, float4(pos.xyz, 1.0)).xyz;

	if (dot(world - _WorldSpaceCameraPos, world - _WorldSpaceCameraPos) > _DistanceCulling * _DistanceCulling * 2)
		return;
	
	if (dot(world - _CameraFocalPoint, world - _CameraFocalPoint) > _DistanceCulling * _DistanceCulling)
		return;
	
	float3 viewDir = mul((float3x3)unity_CameraToWorld, float3(0, 0, 1));
	
	if (dot(viewDir, _WorldSpaceCameraPos - world) > 0)
		return;
	
	world -= _WorldOffset;
	
	float3 maskSample = tex2Dlod(_GrassMask, float4(IN[0].uv, 0, 0)).rgb;
	float mask = (maskSample.x + maskSample.y + maskSample.z) / 3;
	
	if (mask < _MaskThreshold)
		return;
	
	float3 cameraMaskSample = tex2Dlod(_CameraMask, float4(pos.xz / (_TileSize * 2) + .5, 0, 0)).rgb;
	float cameraMask = (cameraMaskSample.x + cameraMaskSample.y + cameraMaskSample.z) / 3;

	//Removes weird flickery grass in the very corner
	//if (pos.x < _ClipXZ && pos.z < _ClipXZ) return;

	if (world.y < _WaterLevel + _MinAlt)
		return;
	
	if (world.y > _MaxAlt)
		return;
	
	float slope = 1.0f - IN[0].normal.y;
	if (slope > _MaxSlope)
		return;

	float temperature = _Temperature + 0.5;
	temperature += world.x / _LatitudeScale;
	temperature -= max(0, world.y * _AltitudeTemperature) - _AltitudeTemperature / 4;

	float min = .2;
	float max = .5;

	float widthLo = .625;
	float heightLo = .625;

	float widthHi = .85;
	float heightHi = .925;
	
	if (temperature < _MinTemp || temperature > _MaxTemp)
		return;

	float bladeSize = 1;
	
	bladeSize *= saturate((mask - _MaskThreshold) / _MaskBlending);
	bladeSize *= 1 - cameraMask;
	if (bladeSize <= _MaskMinimum)
		return;

	if (slope + _SlopeBlending > _MaxSlope)
		bladeSize *= lerp(0.2, 1, 1 - (slope + _SlopeBlending - _MaxSlope) / _SlopeBlending);
	
	if (world.y + _AltBlending > _MaxAlt)
		bladeSize *= lerp(0.2, 1, 1 - (world.y + _AltBlending - _MaxAlt) / _AltBlending);
	
	if (world.y - _AltBlending < _MinAlt)
		bladeSize *= lerp(0.2, 1, (world.y - _MinAlt) / _AltBlending);
	
	if (bladeSize < 0)
		return;

	float t = (temperature + 1) / 2;

	float bladeWidth = 1;
	float bladeHeight = 1;

	if (temperature < widthLo) {
		float loT = saturate((temperature - min) / (widthLo - min));
		bladeWidth = lerp(0.25, 1, saturate((temperature - min) / (widthLo - min)));
	}
	
	if (temperature > widthHi) {
	
		bladeWidth = lerp(0.25, 1, 1 - saturate((temperature - widthHi) / (max - widthHi)));
	}
	
	if (temperature < heightLo) {
		float loT = saturate((temperature - min) / (heightLo - min));
		bladeHeight = lerp(0.5, 1, saturate((temperature - min) / (heightLo - min)));
	}
	
	if (temperature > heightHi) {
	
		bladeHeight = lerp(0.25, 1, 1 - saturate((temperature - heightHi) / (max - heightHi)));
	}


	// Each blade of grass is constructed in tangent space with respect
	// to the emitting vertex's normal and tangent vectors, where the width
	// lies along the X axis and the height along Z.

	// Construct random rotations to point the blade in a direction.
	float3x3 facingRotationMatrix = AngleAxis3x3(rand(pos) * UNITY_TWO_PI, float3(0, 0, 1));
	// Matrix to bend the blade in the direction it's facing.
	float3x3 bendRotationMatrix = AngleAxis3x3(rand(pos.zzx) * _BendRotationRandom * UNITY_PI * 0.5, float3(-1, 0, 0));

	// Sample the wind distortion map, and construct a normalized vector of its direction.
	float2 uv = world.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
	float2 windSample = (tex2Dlod(_WindDistortionMap, float4(uv, 0, 0)).xy * 2 - 1) * _WindStrength;
	float3 wind = normalize(float3(windSample.x, windSample.y, 0));

	float3x3 windRotation = AngleAxis3x3(UNITY_PI * windSample, wind);

	// Construct a matrix to transform our blade from tangent space
	// to local space; this is the same process used when sampling normal maps.
	float3 vNormal = float3(0, 1, 0);//IN[0].normal;
	float4 vTangent = IN[0].tangent;
	float3 vBinormal = cross(vNormal, vTangent) * vTangent.w;

	float3x3 tangentToLocal = float3x3(
		vTangent.x, vBinormal.x, vNormal.x,
		vTangent.y, vBinormal.y, vNormal.y,
		vTangent.z, vBinormal.z, vNormal.z
	);

	// Construct full tangent to local matrix, including our rotations.
	// Construct a second matrix with only the facing rotation; this will be used 
	// for the root of the blade, to ensure it always faces the correct direction.
	float3x3 transformationMatrix = mul(mul(mul(tangentToLocal, windRotation), facingRotationMatrix), bendRotationMatrix);
	float3x3 transformationMatrixFacing = mul(tangentToLocal, facingRotationMatrix);

	float height = ((rand(pos.zyx) * 2 - 1) * _BladeHeightRandom + _BladeHeight) * bladeHeight * bladeSize;
	float width = ((rand(pos.xzy) * 2 - 1) * _BladeWidthRandom + _BladeWidth) * bladeWidth * bladeSize;
	float forward = rand(pos.yyz) * _BladeForward;

	for (int i = 0; i < BLADE_SEGMENTS; i++)
	{
		float t = i / (float)BLADE_SEGMENTS;

		float segmentHeight = height * t;
		float segmentWidth = width * (1 - t);
		float segmentForward = pow(t, _BladeCurve) * forward;

		// Select the facing-only transformation matrix for the root of the blade.
		float3x3 transformMatrix = i == 0 ? transformationMatrixFacing : transformationMatrix;

		triStream.Append(GenerateGrassVertex(pos, segmentWidth, segmentHeight, segmentForward, float2(0, t), transformMatrix));
		triStream.Append(GenerateGrassVertex(pos, -segmentWidth, segmentHeight, segmentForward, float2(1, t), transformMatrix));
	}

	// Add the final vertex as the tip.
	triStream.Append(GenerateGrassVertex(pos, 0, height, forward, float2(0.5, 1), transformationMatrix));
}

#endif
#ifndef HEX_UTILS
#define HEX_UTILS

#define SQRT_3 1.73205080757

uniform float _Tiles[1024];
uniform float _TileSize;
uniform float _EdgeFactor;

bool Tile(int hexX, int hexY)
{
	hexX += 16;
	hexY += 16;
	return _Tiles[hexX + hexY * 32];
}

int3 CubeRound(float3 cube)
{
	int rx = round(cube.x);
	int ry = round(cube.y);
	int rz = round(cube.z);

	float x_diff = abs(rx - cube.x);
	float y_diff = abs(ry - cube.y);
	float z_diff = abs(rz - cube.z);

	if (x_diff > y_diff && x_diff > z_diff)
		rx = -ry - rz;
	else if (y_diff > z_diff)
		ry = -rx - rz;
	else
		rz = -rx - ry;

	return int3(rx, ry, rz);
}

float3 HexToCube(float2 hex)
{
	return float3(hex.x, hex.y, -hex.x - hex.y);
}

float2 CartesianToHex(float x, float z)
{
	return float2(2.0 / 3.0 * x, -1.0 / 3.0 * x + SQRT_3 / 3.0 * z) / _TileSize;
}

float2 CartesianToVertex(float x, float z)
{
	return float2(x - z * SQRT_3 / 3.0, 2.0 / 3.0 * z * SQRT_3) / _TileSize;
}

float2 HexToCartesian(float x, float y)
{
	return float2(3.0 / 2.0 * x, SQRT_3 / 2.0 * x + SQRT_3 * y) * _TileSize;
}

int2 HexRound(float2 hex)
{
	return CubeRound(HexToCube(hex)).xy;
}

int2 SampleTile(float x, float z)
{
	return HexRound(CartesianToHex(x, z));
}

float Fog(float3 worldPos)
{
	int2 tile = SampleTile(worldPos.x, worldPos.z);

	if (!Tile(tile.x, tile.y))
	{
		return 0;
	}

	float2 tileCart = HexToCartesian(tile.x, tile.y);

	float cartX = worldPos.x - tileCart.x;
	float cartZ = worldPos.z - tileCart.y;

	float2 vertex = CartesianToVertex(cartX, cartZ);
	float3 cube = HexToCube(vertex);


	float fog = 1;
	if (!Tile(tile.x + 1, tile.y - 1))
		fog *= saturate((1 - cube.x) * _EdgeFactor);

	if (!Tile(tile.x, tile.y - 1))
		fog *= saturate((1 + cube.y) * _EdgeFactor);

	if (!Tile(tile.x - 1, tile.y))
		fog *= saturate((1 - cube.z) * _EdgeFactor);

	if (!Tile(tile.x - 1, tile.y + 1))
		fog *= saturate((1 + cube.x) * _EdgeFactor);

	if (!Tile(tile.x, tile.y + 1))
		fog *= saturate((1 - cube.y) * _EdgeFactor);

	if (!Tile(tile.x + 1, tile.y))
		fog *= saturate((1 + cube.z) * _EdgeFactor);

	return fog;
}

void ApplyFogOfWar(float3 worldPos, inout float4 color)
{
	color.rgb *= Fog(worldPos);
}

void ApplyFogOfWar(float3 worldPos, inout float3 color)
{
	color *= Fog(worldPos);
}



#endif
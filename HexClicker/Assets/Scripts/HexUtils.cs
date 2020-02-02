using UnityEngine;

public class HexUtils
{
    public static readonly float SQRT_3 = Mathf.Sqrt(3);

    public static readonly int[] neighbourX = { 1, 0, -1, -1, 0, 1, };
    public static readonly int[] neighbourY = { 0, 1, 1, 0, -1, -1, };

    public static readonly float[] angles =
    {
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 0,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 1,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 2,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 3,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 4,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 5,
    };

    public static readonly float[] sinAngles =
    {
        Mathf.Sin(angles[0]),
        Mathf.Sin(angles[1]),
        Mathf.Sin(angles[2]),
        Mathf.Sin(angles[3]),
        Mathf.Sin(angles[4]),
        Mathf.Sin(angles[5]),
    };

    public static readonly float[] cosAngles =
    {
        Mathf.Cos(angles[0]),
        Mathf.Cos(angles[1]),
        Mathf.Cos(angles[2]),
        Mathf.Cos(angles[3]),
        Mathf.Cos(angles[4]),
        Mathf.Cos(angles[5]),
    };

    public static Vector2 HexToCartesian(float x, float y) => new Vector2(3f / 2f * x, SQRT_3 / 2f * x + SQRT_3 * y);
    public static Vector2 CartesianToHex(float x, float z) => new Vector2(2f / 3f * x, -1f / 3f * x + SQRT_3 / 3f * z);
    public static Vector2 CubeToAxial(Vector3 cube) => new Vector2(cube.x, cube.z);
    public static Vector3 AxialToCube(Vector2 axial) => new Vector3(axial.x, axial.y, -axial.x - axial.y);
    public static Vector3Int CubeRound(Vector3 cube)
    {
        int rx = Mathf.RoundToInt(cube.x);
        int ry = Mathf.RoundToInt(cube.y);
        int rz = Mathf.RoundToInt(cube.z);

        float x_diff = Mathf.Abs(rx - cube.x);
        float y_diff = Mathf.Abs(ry - cube.y);
        float z_diff = Mathf.Abs(rz - cube.z);

        if (x_diff > y_diff && x_diff > z_diff)
            rx = -ry - rz;
        else if (y_diff > z_diff)
            ry = -rx - rz;
        else
            rz = -rx - ry;

        return new Vector3Int(rx, ry, rz);
    }

    public static Vector2Int HexRound(Vector2 hex)
    {
        Vector3Int cube = CubeRound(AxialToCube(hex));
        return new Vector2Int(cube.x, cube.y);
    }
}

using UnityEngine;

public struct Bounds2
{
    public float minX, minY, maxX, maxY;

    public Bounds2(Vector2 center)
    {
        minX = center.x;
        maxX = center.x;
        minY = center.y;
        maxY = center.y;
    }
    public Bounds2(float x, float y)
    {
        minX = x;
        maxX = x;
        minY = y;
        maxY = y;
    }
    public Bounds2(float centerX, float centerY, float sizeX, float sizeY)
    {
        minX = centerX - sizeX / 2;
        maxX = centerX + sizeX / 2;
        minY = centerY - sizeY / 2;
        maxY = centerY + sizeY / 2;
    }

    public Vector2 Center => new Vector2(minX, minY) + Size / 2;
    public Vector2 Size => new Vector2(maxX - minX, maxY - minY);
}

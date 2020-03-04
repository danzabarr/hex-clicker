using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ColorExtensions 
{
    public static Vector3 HSV(this Color color) => ToHSV(color.r, color.g, color.b);

    public static Vector3 ToHSV(Vector3 rgb) => ToHSV(rgb.x, rgb.y, rgb.z);

    public static Vector3 ToHSV(float r, float g, float b)
    {
        float cmax = Mathf.Max(r, Mathf.Max(g, b));
        float cmin = Mathf.Min(r, Mathf.Min(g, b));
        float diff = cmax - cmin;

        float h = 0;
        float s;
        float v;

        if (cmax == cmin)
            h = 0;

        else if (cmax == r)
            h = (60f * ((g - b) / diff) + 360f) % 360f;

        else if (cmax == g)
            h = (60f * ((b - r) / diff) + 120f) % 360f;

        else if (cmax == b)
            h = (60f * ((r - g) / diff) + 240f) % 360f;


        if (cmax == 0)
            s = 0;
        else
            s = (diff / cmax) * 100f;

        v = cmax * 100f;

        return new Vector3(h, s, v);
    }

    public static Vector3 ToRGB(Vector3 hsv) => ToRGB(hsv.x, hsv.y, hsv.z);
    public static Vector3 ToRGB(float h, float s, float v)
    {
        s /= 100;
        v /= 100;

        float c = v * s;
        float h1 = h / 60;
        float x = c * (1 - Mathf.Abs(h1 % 2 - 1));

        Vector3 rgb = Vector3.zero;

        float f = Mathf.Floor(h1);
        if (f == 0)
            rgb = new Vector3(c, x, 0);
        else if (f == 1)
            rgb = new Vector3(x, c, 0);
        else if (f == 2)
            rgb = new Vector3(0, c, x);
        else if (f == 3)
            rgb = new Vector3(0, x, c);
        else if (f == 4)
            rgb = new Vector3(x, 0, c);
        else if (f == 5)
            rgb = new Vector3(c, 0, x);

        float m = v - c;

        return rgb + m * Vector3.one;
    }

    public static Color FromHSV(Vector3 hsv, float a = 1) => FromHSV(hsv.x, hsv.y, hsv.z, a);

    public static Color FromHSV(float h, float s, float v, float a = 1)
    {
        Vector3 rgb = ToRGB(h, s, v);
        return new Color(rgb.x, rgb.y, rgb.z, a);
    }
    public static Color RotateHue(this Color color, float amount)
    {
        Vector3 hsv = color.HSV();
        hsv.x += amount;
        hsv.x += 360;
        hsv.x %= 360;
        Vector3 rgb = ToRGB(hsv.x, hsv.y, hsv.z);
        return new Color(rgb.x, rgb.y, rgb.z, color.a);
    }

    public static Color AddSaturation(this Color color, float amount)
    {
        Vector3 hsv = color.HSV();
        hsv.y = Mathf.Clamp(hsv.y + amount, 0, 100);
        Vector3 rgb = ToRGB(hsv.x, hsv.y, hsv.z);
        return new Color(rgb.x, rgb.y, rgb.z, color.a);
    }

    public static Color AddValue(this Color color, float amount)
    {
        Vector3 hsv = color.HSV();
        hsv.z = Mathf.Clamp(hsv.z + amount, 0, 100);
        Vector3 rgb = ToRGB(hsv.x, hsv.y, hsv.z);
        return new Color(rgb.x, rgb.y, rgb.z, color.a);
    }

    public static Color SetHue(this Color color, float value)
    {
        Vector3 hsv = color.HSV();
        hsv.x = Mathf.Clamp(value, 0, 360);
        Vector3 rgb = ToRGB(hsv.x, hsv.y, hsv.z);
        return new Color(rgb.x, rgb.y, rgb.z, color.a);
    }

    public static Color SetSaturation(this Color color, float value)
    {
        Vector3 hsv = color.HSV();
        hsv.y = Mathf.Clamp(value, 0, 100);
        Vector3 rgb = ToRGB(hsv.x, hsv.y, hsv.z);
        return new Color(rgb.x, rgb.y, rgb.z, color.a);
    }

    public static Color SetValue(this Color color, float value)
    {
        Vector3 hsv = color.HSV();
        hsv.z = Mathf.Clamp(value, 0, 100);
        Vector3 rgb = ToRGB(hsv.x, hsv.y, hsv.z);
        return new Color(rgb.x, rgb.y, rgb.z, color.a);
    }
}

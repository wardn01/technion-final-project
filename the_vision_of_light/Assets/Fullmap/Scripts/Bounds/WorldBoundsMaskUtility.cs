using UnityEngine;

/// <summary>
/// Generates circle-mask textures for the world-bounds overlays:
/// transparent inside the playable circle, fog color outside, optional ring at the edge.
/// </summary>
public static class WorldBoundsMaskUtility
{
    /// <param name="texSize">Texture resolution (square).</param>
    /// <param name="areaSize">Covered area size in target units (UI or world).</param>
    /// <param name="circleCenterOffset">Circle center offset from the area center, same units.</param>
    /// <param name="circleRadius">Circle radius, same units.</param>
    /// <param name="outsideColor">Fog color outside the circle.</param>
    /// <param name="ringColor">Border ring color (ignored when ringWidth is 0).</param>
    /// <param name="ringWidth">Ring thickness in the same units; 0 = no ring.</param>
    public static Texture2D GenerateCircleMask(
        int texSize,
        Vector2 areaSize,
        Vector2 circleCenterOffset,
        float circleRadius,
        Color outsideColor,
        Color ringColor,
        float ringWidth)
    {
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[texSize * texSize];
        Color32 outside32 = outsideColor;
        Color32 ring32 = ringColor;
        Color32 clear = new Color32(0, 0, 0, 0);

        float halfRing = ringWidth * 0.5f;
        // Antialias band roughly one texel wide, in area units.
        float texel = Mathf.Max(areaSize.x, areaSize.y) / texSize;

        for (int y = 0; y < texSize; y++)
        {
            float v = (y + 0.5f) / texSize - 0.5f;
            float posY = v * areaSize.y - circleCenterOffset.y;

            for (int x = 0; x < texSize; x++)
            {
                float u = (x + 0.5f) / texSize - 0.5f;
                float posX = u * areaSize.x - circleCenterOffset.x;

                float dist = Mathf.Sqrt(posX * posX + posY * posY);
                int index = y * texSize + x;

                if (ringWidth > 0f && Mathf.Abs(dist - circleRadius) <= halfRing)
                {
                    pixels[index] = ring32;
                }
                else if (dist > circleRadius)
                {
                    // Soften the outer fog edge by one texel to avoid stair-stepping.
                    float blend = Mathf.Clamp01((dist - circleRadius - halfRing) / texel);
                    Color c = Color.Lerp(new Color(outsideColor.r, outsideColor.g, outsideColor.b, 0f), outsideColor, blend);
                    pixels[index] = c;
                }
                else
                {
                    pixels[index] = clear;
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }
}

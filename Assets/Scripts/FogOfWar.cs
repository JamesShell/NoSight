using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FogOfWar : MonoBehaviour
{
    [Header("Texture Settings")]
    public int textureWidth = 256;
    public int textureHeight = 256;

    [Tooltip("World width (in units) that the fog texture covers.")]
    public float worldWidth = 20f;

    [Header("Rays / Quality")]
    public int rayCount = 120;     // more = smoother, slower
    public float rayStep = 0.2f;   // world units between samples

    [Header("Origin / Camera Follow")]
    public bool useCameraAsOrigin = true;  // set true if you want fog to follow camera
    public Camera targetCamera;            // if null, will use Camera.main
    public Transform worldOrigin;          // optional fallback if not using camera

    [Header("Fade Settings")]
    public float fadeInTime = 0.1f;        // time from 0 -> 1 intensity
    public float fadeOutTime = 1.0f;       // time from 1 -> 0 intensity

    [Header("References")]
    public RawImage fogImage;              // full-screen RawImage
    public LayerMask wallLayer;            // walls that block the reveal

    private Texture2D fogTexture;
    private Color32[] pixels;

    private class EchoInstance
    {
        public Vector2 origin;
        public float radius;
        public float duration;   // "hold" time at full intensity
        public float startTime;
    }

    private readonly List<EchoInstance> activeEchos = new List<EchoInstance>();

    float PixelsPerUnit => textureWidth / worldWidth;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        fogTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        fogTexture.wrapMode = TextureWrapMode.Clamp;

        pixels = new Color32[textureWidth * textureHeight];

        // Start fully dark
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 255);

        fogTexture.SetPixels32(pixels);
        fogTexture.Apply();

        if (fogImage != null)
        {
            fogImage.texture = fogTexture;
            fogImage.color = Color.white; // don't tint darker
        }
    }

    void Update()
    {
        if (fogTexture == null || pixels == null)
            return;

        // If following camera, update worldWidth to match camera view
        if (useCameraAsOrigin && targetCamera != null)
        {
            worldWidth = targetCamera.orthographicSize * 2f * targetCamera.aspect;
        }

        // 1) Reset to full darkness (only alpha, keep RGB)
        for (int i = 0; i < pixels.Length; i++)
        {
            var c = pixels[i];
            c.a = 255;
            pixels[i] = c;
        }

        float ppu = PixelsPerUnit;

        // 2) Apply all active echoes (they fade in, hold, then fade out)
        for (int i = activeEchos.Count - 1; i >= 0; i--)
        {
            EchoInstance e = activeEchos[i];
            float age = Time.time - e.startTime;

            float totalLife = fadeInTime + e.duration + fadeOutTime;
            if (age >= totalLife)
            {
                // echo finished, remove it
                activeEchos.RemoveAt(i);
                continue;
            }

            // --- TIME-BASED INTENSITY ---
            float timeIntensity;

            if (age < fadeInTime)
            {
                // Fade in: 0 → 1
                timeIntensity = fadeInTime > 0f ? age / fadeInTime : 1f;
            }
            else if (age < fadeInTime + e.duration)
            {
                // Full bright
                timeIntensity = 1f;
            }
            else
            {
                // Fade out: 1 → 0
                float t = (age - fadeInTime - e.duration) / fadeOutTime;
                timeIntensity = 1f - Mathf.Clamp01(t);
            }

            RevealWithWalls(e.origin, e.radius, timeIntensity, ppu);
        }

        // 3) Upload to texture
        fogTexture.SetPixels32(pixels);
        fogTexture.Apply();
    }

    /// <summary>
    /// Called by gun/echo system.
    /// duration = time at full intensity (not including fadeIn/fadeOut).
    /// </summary>
    public void TriggerEcho(Vector2 origin, float radius, float duration)
    {
        EchoInstance e = new EchoInstance
        {
            origin = origin,
            radius = radius,
            duration = Mathf.Max(0f, duration),
            startTime = Time.time
        };
        activeEchos.Add(e);
    }

    /// <summary>
    /// Cast radial rays that stop at walls and reveal only along those rays.
    /// timeIntensity: 0..1 from fadeIn/hold/fadeOut
    /// </summary>
    private void RevealWithWalls(Vector2 origin, float radiusWorld, float timeIntensity, float ppu)
    {
        Vector2 originPos = GetOriginPosition();
        if (originPos == Vector2.negativeInfinity) return;

        int rays = Mathf.Max(8, rayCount);

        for (int i = 0; i < rays; i++)
        {
            float angleDeg = (360f / rays) * i;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            // Raycast to see where the wall blocks
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, radiusWorld, wallLayer);
            float maxDist = hit.collider != null ? hit.distance : radiusWorld;

            // Step along the ray until we hit a wall or max radius
            for (float dist = 0f; dist <= maxDist; dist += rayStep)
            {
                Vector2 worldPos = origin + dir * dist;

                // Optional radial gradient (keep or remove as you like):
                float normalizedDist = dist / radiusWorld;
                float gradientFade = 1f - normalizedDist;         // linear falloff
                // float gradientFade = 1f - normalizedDist * normalizedDist; // smoother falloff

                float finalIntensity = timeIntensity * Mathf.Clamp01(gradientFade);
                if (finalIntensity <= 0f) continue;

                RevealPoint(worldPos, finalIntensity, ppu, originPos);
            }
        }
    }

    /// <summary>
    /// Reveal a small dot around worldPos, with a certain intensity.
    /// </summary>
    private void RevealPoint(Vector2 worldPos, float intensity, float ppu, Vector2 originPos)
    {
        // originPos is the center of this fog window (camera or fixed origin)
        Vector2 localPos = worldPos - originPos;

        int centerX = Mathf.RoundToInt(localPos.x * ppu + textureWidth / 2f);
        int centerY = Mathf.RoundToInt(localPos.y * ppu + textureHeight / 2f);

        int radiusPixels = 3;
        int sqrRadius = radiusPixels * radiusPixels;

        // alpha we want: 0 = fully visible, 255 = fully dark
        byte targetAlpha = (byte)Mathf.RoundToInt(255f * (1f - intensity));

        for (int y = -radiusPixels; y <= radiusPixels; y++)
        {
            int py = centerY + y;
            if (py < 0 || py >= textureHeight) continue;

            for (int x = -radiusPixels; x <= radiusPixels; x++)
            {
                int px = centerX + x;
                if (px < 0 || px >= textureWidth) continue;

                if (x * x + y * y <= sqrRadius)
                {
                    int index = py * textureWidth + px;

                    // Use the most transparent value (so overlapping echoes blend nicely)
                    var c = pixels[index];
                    if (targetAlpha < c.a)
                    {
                        c.a = targetAlpha;
                        pixels[index] = c;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the current world origin used for mapping fog to the screen.
    /// If useCameraAsOrigin is true, this is the camera position.
    /// Otherwise, it uses worldOrigin (if set).
    /// </summary>
    private Vector2 GetOriginPosition()
    {
        if (useCameraAsOrigin && targetCamera != null)
        {
            return targetCamera.transform.position;
        }

        if (worldOrigin != null)
        {
            return worldOrigin.position;
        }

        // Fallback – avoids crashes if nothing is set
        return Vector2.negativeInfinity;
    }

    public bool IsWorldPositionVisible(Vector2 worldPos, byte visibleAlphaThreshold = 200)
    {
        if (fogTexture == null || pixels == null)
            return false;

        Vector2 originPos = GetOriginPosition();   // you already have this in your latest FogOfWar
        if (originPos == Vector2.negativeInfinity)
            return false;

        float ppu = PixelsPerUnit;
        Vector2 localPos = worldPos - originPos;

        int x = Mathf.RoundToInt(localPos.x * ppu + textureWidth * 0.5f);
        int y = Mathf.RoundToInt(localPos.y * ppu + textureHeight * 0.5f);

        if (x < 0 || x >= textureWidth || y < 0 || y >= textureHeight)
            return false;

        int index = y * textureWidth + x;
        byte alpha = pixels[index].a;   // 0 = fully visible, 255 = fully dark

        // visible if mostly transparent
        return alpha <= visibleAlphaThreshold;
    }
}

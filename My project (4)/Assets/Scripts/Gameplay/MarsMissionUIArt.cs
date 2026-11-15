using UnityEngine;

/// <summary>Shared ivory-and-color UI surfaces and icons for the Mars mission HUD.</summary>
public static class MarsMissionUIArt
{
    private const string ResourceRoot = "UI/MissionHUD/";
    private static Texture2D cardFrame;
    private static Texture2D[] vitalIcons;
    private static Texture2D pixel;
    private static Texture2D clearPixel;
    private static GUIStyle buttonStyle;

    private static readonly string[] IconNames =
    {
        "icon_energy", "icon_water", "icon_oxygen", "icon_thermal"
    };

    public static Texture2D GetVitalIcon(int index)
    {
        EnsureIcons();
        return vitalIcons[Mathf.Clamp(index, 0, vitalIcons.Length - 1)];
    }

    public static void DrawCard(Rect rect, Color accent, float pulse = 0f)
    {
        EnsureTextures();
        DrawNineSlice(rect, cardFrame, 18f);

        Color previous = GUI.color;
        float pulseAlpha = Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(pulse));
        GUI.color = new Color(accent.r, accent.g, accent.b, pulseAlpha);
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 9f, 3f, Mathf.Max(8f, rect.height - 18f)), pixel);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(rect.x + 14f, rect.y + 3f, Mathf.Max(1f, rect.width - 28f), 1f), pixel);
        GUI.color = previous;
    }

    public static void DrawIcon(Rect rect, int iconIndex, Color accent, float pulse = 0f)
    {
        var icon = GetVitalIcon(iconIndex);
        if (icon == null) return;

        Color previous = GUI.color;
        float pulseAlpha = Mathf.Lerp(0.84f, 1f, Mathf.Clamp01(pulse));
        GUI.color = new Color(1f, 1f, 1f, pulseAlpha);
        GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
        GUI.color = previous;
    }

    public static void DrawProgress(Rect rect, float value, Color accent, float phase)
    {
        EnsureTextures();
        DrawRect(rect, new Color(0.87f, 0.89f, 0.88f, 1f), pixel);
        float fillWidth = rect.width * Mathf.Clamp01(value);
        if (fillWidth <= 0f) return;

        DrawRect(new Rect(rect.x, rect.y, fillWidth, rect.height), accent, pixel);

        float sweepWidth = Mathf.Min(12f, fillWidth * 0.30f);
        float sweepX = rect.x + Mathf.Repeat(phase * (rect.width + sweepWidth), rect.width + sweepWidth) - sweepWidth;
        float visibleLeft = Mathf.Max(rect.x, sweepX);
        float visibleRight = Mathf.Min(rect.xMax, sweepX + sweepWidth);
        if (visibleRight > visibleLeft && visibleLeft < rect.x + fillWidth)
        {
            Color shine = new Color(1f, 1f, 1f, 0.38f);
            DrawRect(new Rect(visibleLeft, rect.y, Mathf.Min(visibleRight, rect.x + fillWidth) - visibleLeft, rect.height), shine, pixel);
        }
    }

    public static bool DrawButton(Rect rect, string label, Color accent, int fontSize = 13)
    {
        EnsureButtonStyle();
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f + rect.x * 0.01f);
        DrawCard(rect, accent, pulse * 0.35f);
        buttonStyle.fontSize = fontSize;
        return GUI.Button(rect, label, buttonStyle);
    }

    public static void DrawRect(Rect rect, Color color)
    {
        EnsureTextures();
        DrawRect(rect, color, pixel);
    }

    private static void EnsureIcons()
    {
        if (vitalIcons != null) return;
        vitalIcons = new Texture2D[IconNames.Length];
        for (int i = 0; i < IconNames.Length; i++)
            vitalIcons[i] = Resources.Load<Texture2D>(ResourceRoot + IconNames[i]);
    }

    private static void EnsureTextures()
    {
        if (cardFrame == null)
            cardFrame = Resources.Load<Texture2D>(ResourceRoot + "hud_card");

        if (pixel != null) return;
        pixel = Texture2D.whiteTexture;
    }

    private static void EnsureButtonStyle()
    {
        if (buttonStyle != null) return;
        clearPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        clearPixel.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
        clearPixel.Apply();
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { background = clearPixel, textColor = new Color(0.18f, 0.22f, 0.25f, 1f) },
            hover = { background = clearPixel, textColor = new Color(0.71f, 0.31f, 0.10f, 1f) },
            active = { background = clearPixel, textColor = new Color(0.55f, 0.24f, 0.08f, 1f) },
            focused = { background = clearPixel, textColor = new Color(0.18f, 0.22f, 0.25f, 1f) }
        };
    }

    private static void DrawNineSlice(Rect target, Texture2D texture, float sourceBorder)
    {
        if (texture == null)
        {
            DrawRect(target, new Color(1f, 0.99f, 0.96f, 1f), Texture2D.whiteTexture);
            return;
        }

        float sourceX = Mathf.Min(sourceBorder, texture.width * 0.25f);
        float sourceY = Mathf.Min(sourceBorder, texture.height * 0.25f);
        float targetX = Mathf.Min(sourceX, target.width * 0.25f);
        float targetY = Mathf.Min(sourceY, target.height * 0.30f);
        float[] dx = { target.x, target.x + targetX, target.xMax - targetX, target.xMax };
        float[] dy = { target.y, target.y + targetY, target.yMax - targetY, target.yMax };
        float[] sx = { 0f, sourceX, texture.width - sourceX, texture.width };
        float[] sy = { 0f, sourceY, texture.height - sourceY, texture.height };

        Color previous = GUI.color;
        GUI.color = Color.white;
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                Rect destination = Rect.MinMaxRect(dx[column], dy[row], dx[column + 1], dy[row + 1]);
                Rect uv = new Rect(
                    sx[column] / texture.width,
                    1f - sy[row + 1] / texture.height,
                    (sx[column + 1] - sx[column]) / texture.width,
                    (sy[row + 1] - sy[row]) / texture.height);
                GUI.DrawTextureWithTexCoords(destination, texture, uv, true);
            }
        }
        GUI.color = previous;
    }

    private static void DrawRect(Rect rect, Color color, Texture2D texture)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texture);
        GUI.color = previous;
    }
}

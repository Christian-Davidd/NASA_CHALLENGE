using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Full-screen in-camera inspection display for crop banks and station monitors.</summary>
public sealed class MarsMonitorScreen2D : MonoBehaviour
{
    private static MarsMonitorScreen2D instance;
    private static Texture2D whitePixel;

    private string title;
    private string detail;
    private MarsCropLifecycle2D crop;
    private GUIStyle headerStyle;
    private GUIStyle bodyStyle;
    private GUIStyle smallStyle;
    private GUIStyle valueStyle;
    private GUIStyle buttonStyle;
    private bool waitForERelease;

    public static bool IsOpen => instance != null && instance.isOpen;
    private bool isOpen;

    public static void OpenInformation(string screenTitle, string screenDetail)
    {
        var screen = EnsureInstance();
        screen.crop = null;
        screen.title = screenTitle;
        screen.detail = screenDetail;
        screen.isOpen = true;
        screen.waitForERelease = true;
    }

    public static void OpenCrop(MarsCropLifecycle2D cropBank)
    {
        if (cropBank == null) return;
        var screen = EnsureInstance();
        screen.crop = cropBank;
        screen.title = cropBank.CropName;
        screen.detail = cropBank.CropDescription;
        screen.isOpen = true;
        screen.waitForERelease = true;
    }

    public static void Close()
    {
        if (instance != null) instance.isOpen = false;
    }

    private static MarsMonitorScreen2D EnsureInstance()
    {
        if (instance != null) return instance;
        var screenObject = new GameObject("Agricultural_Inspection_Display");
        instance = screenObject.AddComponent<MarsMonitorScreen2D>();
        return instance;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (!isOpen) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (waitForERelease)
        {
            if (!keyboard.eKey.isPressed) waitForERelease = false;
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (!keyboard.eKey.wasPressedThisFrame) return;
        if (crop == null)
        {
            Close();
            return;
        }

        if (crop.CanHarvest) crop.Harvest();
    }

    private void OnGUI()
    {
        if (!isOpen) return;
        EnsureStyles();

        float vitalGutter = Mathf.Min(280f, Mathf.Max(196f, Screen.width * 0.23f));
        float availableWidth = Mathf.Max(240f, Screen.width - vitalGutter - 26f);
        float width = Mathf.Min(1120f, availableWidth * 0.94f);
        float height = Mathf.Min(Screen.height * 0.84f, 780f);
        var panel = new Rect(vitalGutter + (availableWidth - width) * 0.5f,
            (Screen.height - height) * 0.5f, width, height);

        GUI.color = new Color(0.025f, 0.026f, 0.045f, 0.76f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), WhitePixel);
        GUI.color = Color.white;

        DrawRect(panel, new Color(0.045f, 0.055f, 0.080f, 0.99f));
        DrawRect(new Rect(panel.x, panel.y, panel.width, 3f), new Color(1f, 0.59f, 0.30f, 1f));
        DrawRect(new Rect(panel.x + 10, panel.y + 10, panel.width - 20, 58), new Color(0.12f, 0.095f, 0.095f, 1f));
        GUI.Label(new Rect(panel.x + 30, panel.y + 17, panel.width - 170, 44), title, headerStyle);

        var close = new Rect(panel.xMax - 112, panel.y + 22, 80, 36);
        if (GUI.Button(close, "CERRAR", buttonStyle)) Close();

        if (crop != null)
            DrawCropPanel(panel);
        else
            DrawTelemetryPanel(panel);

        DrawRect(new Rect(panel.x + 10, panel.yMax - 52, panel.width - 20, 42), new Color(0.12f, 0.095f, 0.095f, 1f));
        string footer = crop == null
            ? "[E/ESC] Cerrar     [G] Más contexto"
            : crop.CanHarvest
                ? "[E] Cosechar     [G] Contexto     [ESC] Cerrar"
                : crop.StageTimeLabel + "     [G] Contexto     [ESC] Cerrar";
        GUI.Label(new Rect(panel.x + 28, panel.yMax - 48, panel.width - 56, 34), footer, smallStyle);
        MarsMissionHUD.DrawPersistentVitalsOnTopOfModal();
    }

    private void DrawCropPanel(Rect panel)
    {
        float margin = 28f;
        float imageSize = Mathf.Min(panel.height - 190f, panel.width * 0.31f);
        var imageRect = new Rect(panel.x + margin + 20, panel.y + 105, imageSize, imageSize);
        DrawRect(imageRect, new Color(0.13f, 0.10f, 0.09f, 1f));
        DrawRect(new Rect(imageRect.x, imageRect.y, imageRect.width, 3f), new Color(1f, 0.65f, 0.32f, 1f));
        if (crop.CurrentSprite != null)
            GUI.DrawTexture(new Rect(imageRect.x + 12, imageRect.y + 12, imageRect.width - 24, imageRect.height - 24), crop.CurrentSprite.texture, ScaleMode.ScaleToFit, true);

        float rightX = imageRect.xMax + 36f;
        float rightWidth = panel.xMax - rightX - margin - 20f;
        GUI.Label(new Rect(rightX, panel.y + 112, rightWidth, 52), crop.StageLabel, valueStyle);
        GUI.Label(new Rect(rightX, panel.y + 172, rightWidth, 100), MarsMissionHUD.GetContextText(detail, 180), bodyStyle);

        GUI.Label(new Rect(rightX, panel.y + 284, rightWidth, 32), crop.StageTimeLabel, smallStyle);
        DrawCropStageBar(new Rect(rightX, panel.y + 324, rightWidth, 82));
        GUI.Label(new Rect(rightX, panel.y + 424, rightWidth, 80),
            $"Cosechas: {crop.HarvestedCount:00}\nSemilla → crecimiento → cosecha", smallStyle);
        GUI.Label(new Rect(imageRect.x, imageRect.yMax + 18, imageRect.width, 52), "CULTIVO ACTUAL", smallStyle);
    }

    private void DrawCropStageBar(Rect rect)
    {
        var stages = new[] { "SEMILLA", "CRECIMIENTO", "MADURO" };
        var current = crop.StageLabel.StartsWith("SEMILLA") ? 0 : crop.StageLabel.StartsWith("CRECIMIENTO") ? 1 : 2;
        float segment = rect.width / 3f;
        for (int i = 0; i < 3; i++)
        {
            var box = new Rect(rect.x + i * segment + 5, rect.y, segment - 10, rect.height);
            DrawRect(box, i == current ? new Color(0.42f, 0.20f, 0.12f, 1f) : new Color(0.15f, 0.12f, 0.13f, 1f));
            if (i == current && i < 2)
                DrawRect(new Rect(box.x + 8, box.yMax - 12, (box.width - 16) * crop.StageProgress, 4), new Color(1f, 0.68f, 0.32f, 1f));
            GUI.Label(new Rect(box.x + 4, box.y + 20, box.width - 8, 40), stages[i], smallStyle);
        }
    }

    private void DrawTelemetryPanel(Rect panel)
    {
        float margin = 30f;
        float chartHeight = Mathf.Clamp(panel.height * 0.44f, 250f, 350f);
        var chartRect = new Rect(panel.x + margin, panel.y + 106f, panel.width - margin * 2f, chartHeight);
        DrawTelemetryChart(chartRect, title);

        float summaryTop = chartRect.yMax + 15f;
        var summaryRect = new Rect(panel.x + margin, summaryTop, panel.width - margin * 2f,
            panel.yMax - 62f - summaryTop);
        DrawRect(summaryRect, new Color(0.12f, 0.09f, 0.09f, 1f));
        GUI.Label(new Rect(summaryRect.x + 16f, summaryRect.y + 7f, summaryRect.width - 32f, 30f),
            "RESUMEN DEL SUBSISTEMA", valueStyle);

        float textWidth = summaryRect.width * 0.43f;
        GUI.Label(new Rect(summaryRect.x + 18f, summaryRect.y + 42f, textWidth - 28f, summaryRect.height - 50f),
            MarsMissionHUD.GetContextText(detail, 100), bodyStyle);

        var readings = GetReadings(title);
        float cardsX = summaryRect.x + textWidth;
        float cardsWidth = summaryRect.width - textWidth - 16f;
        float gap = 10f;
        float cardWidth = (cardsWidth - gap) * 0.5f;
        float cardHeight = Mathf.Max(57f, (summaryRect.height - 53f - gap) * 0.5f);
        for (int i = 0; i < readings.Length; i++)
        {
            var reading = readings[i];
            int column = i % 2;
            int row = i / 2;
            var card = new Rect(cardsX + column * (cardWidth + gap),
                summaryRect.y + 42f + row * (cardHeight + gap), cardWidth, cardHeight);
            DrawRect(card, new Color(0.075f, 0.065f, 0.075f, 1f));
            GUI.Label(new Rect(card.x + 10f, card.y + 4f, card.width - 20f, 22f), reading.Item1, smallStyle);
            GUI.Label(new Rect(card.x + 10f, card.y + 23f, card.width - 20f, 29f), reading.Item2, valueStyle);
            DrawRect(new Rect(card.x + 10f, card.yMax - 8f, card.width - 20f, 3f), new Color(0.31f, 0.22f, 0.20f, 1f));
            DrawRect(new Rect(card.x + 10f, card.yMax - 8f, (card.width - 20f) * reading.Item3, 3f), new Color(1f, 0.62f, 0.34f, 1f));
        }
    }

    private struct TrendSeries
    {
        public readonly string Label;
        public readonly Color Color;
        public readonly float[] Values;

        public TrendSeries(string label, Color color, float[] values)
        {
            Label = label;
            Color = color;
            Values = values;
        }
    }

    private void DrawTelemetryChart(Rect rect, string panelTitle)
    {
        DrawRect(rect, new Color(0.075f, 0.065f, 0.075f, 1f));
        bool waterMode = IsWaterMonitor(panelTitle);
        bool cropMode = !waterMode && IsCropMonitor(panelTitle);
        string chartTitle = waterMode ? "CIRCUITO DE AGUA Y NUTRIENTES"
            : cropMode ? "SENSORES DE LA ZONA RADICULAR" : "CONDICIONES AMBIENTALES DEL MÓDULO";
        GUI.Label(new Rect(rect.x + 18f, rect.y + 8f, rect.width - 150f, 32f), chartTitle, valueStyle);
        DrawRect(new Rect(rect.xMax - 128f, rect.y + 14f, 110f, 22f), new Color(0.35f, 0.19f, 0.12f, 1f));
        GUI.Label(new Rect(rect.xMax - 124f, rect.y + 12f, 104f, 24f), "SIMULADO", smallStyle);

        TrendSeries[] series = GetTrendSeries(waterMode, cropMode);
        float legendX = rect.x + 20f;
        for (int i = 0; i < series.Length; i++)
        {
            float x = legendX + i * Mathf.Min(230f, (rect.width - 100f) / series.Length);
            DrawRect(new Rect(x, rect.y + 48f, 12f, 4f), series[i].Color);
            GUI.Label(new Rect(x + 18f, rect.y + 38f, 190f, 24f), series[i].Label, smallStyle);
        }

        var plot = new Rect(rect.x + 64f, rect.y + 75f, rect.width - 84f, rect.height - 117f);
        Color grid = new Color(0.33f, 0.45f, 0.47f, 0.55f);
        for (int i = 0; i <= 4; i++)
        {
            float y = plot.y + plot.height * i / 4f;
            DrawRect(new Rect(plot.x, y, plot.width, 1f), grid);
            GUI.Label(new Rect(plot.x - 44f, y - 12f, 36f, 22f), (100 - i * 25).ToString(), smallStyle);
        }
        for (int i = 0; i <= 6; i++)
        {
            float x = plot.x + plot.width * i / 6f;
            DrawRect(new Rect(x, plot.y, 1f, plot.height), new Color(0.24f, 0.34f, 0.36f, 0.45f));
        }

        foreach (var line in series)
            DrawTrendLine(plot, line);

        string[] timeLabels = { "−30 min", "−20", "−10", "AHORA" };
        for (int i = 0; i < timeLabels.Length; i++)
        {
            float x = plot.x + plot.width * i / (timeLabels.Length - 1f);
            GUI.Label(new Rect(x - 36f, plot.yMax + 3f, 72f, 22f), timeLabels[i], smallStyle);
        }
        GUI.Label(new Rect(rect.xMax - 300f, rect.yMax - 24f, 280f, 20f),
            "Tendencia ilustrativa", smallStyle);
    }

    private static TrendSeries[] GetTrendSeries(bool waterMode, bool cropMode)
    {
        if (waterMode)
        {
            return new[]
            {
                new TrendSeries("ALIMENTACIÓN", new Color(0.34f, 0.78f, 0.96f), new[] { 43f, 48f, 46f, 53f, 55f, 51f, 58f, 56f, 62f, 59f, 64f, 61f }),
                new TrendSeries("RETORNO", new Color(1.00f, 0.59f, 0.32f), new[] { 34f, 38f, 36f, 41f, 39f, 46f, 43f, 47f, 45f, 51f, 48f, 52f }),
                new TrendSeries("RESERVORIO", new Color(0.55f, 0.88f, 0.48f), new[] { 79f, 78f, 77f, 77f, 76f, 75f, 76f, 75f, 74f, 74f, 73f, 74f })
            };
        }

        if (cropMode)
        {
            return new[]
            {
                new TrendSeries("HUMEDAD RAÍZ", new Color(0.34f, 0.78f, 0.96f), new[] { 58f, 61f, 63f, 62f, 65f, 66f, 64f, 68f, 67f, 69f, 67f, 68f }),
                new TrendSeries("O₂ RADICULAR", new Color(0.55f, 0.88f, 0.48f), new[] { 77f, 78f, 80f, 79f, 81f, 83f, 82f, 80f, 84f, 82f, 83f, 82f }),
                new TrendSeries("TEMP. FOLIAR", new Color(1.00f, 0.59f, 0.32f), new[] { 44f, 46f, 45f, 49f, 48f, 51f, 50f, 48f, 52f, 50f, 53f, 51f })
            };
        }

        return new[]
        {
            new TrendSeries("TEMPERATURA", new Color(1.00f, 0.59f, 0.32f), new[] { 54f, 56f, 55f, 58f, 57f, 61f, 60f, 59f, 63f, 61f, 64f, 62f }),
            new TrendSeries("HUMEDAD", new Color(0.34f, 0.78f, 0.96f), new[] { 70f, 68f, 66f, 67f, 65f, 64f, 66f, 63f, 62f, 64f, 61f, 63f }),
            new TrendSeries("CO₂", new Color(0.55f, 0.88f, 0.48f), new[] { 35f, 38f, 37f, 41f, 40f, 43f, 41f, 45f, 44f, 42f, 40f, 41f })
        };
    }

    private static void DrawTrendLine(Rect plot, TrendSeries series)
    {
        if (series.Values == null || series.Values.Length < 2) return;
        Vector2 previous = TrendPoint(plot, 0, series.Values[0], series.Values.Length);
        for (int i = 1; i < series.Values.Length; i++)
        {
            Vector2 current = TrendPoint(plot, i, series.Values[i], series.Values.Length);
            DrawSegment(previous, current, series.Color, 3f);
            DrawRect(new Rect(current.x - 3.5f, current.y - 3.5f, 7f, 7f), series.Color);
            previous = current;
        }
        Vector2 first = TrendPoint(plot, 0, series.Values[0], series.Values.Length);
        DrawRect(new Rect(first.x - 3.5f, first.y - 3.5f, 7f, 7f), series.Color);
    }

    private static Vector2 TrendPoint(Rect plot, int index, float value, int count)
    {
        float x = plot.x + plot.width * index / (count - 1f);
        float y = plot.yMax - plot.height * Mathf.Clamp01(value / 100f);
        return new Vector2(x, y);
    }

    private static void DrawSegment(Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
        DrawRect(new Rect(start.x, start.y - width * 0.5f, delta.magnitude, width), color);
        GUI.matrix = previousMatrix;
    }

    private static bool IsWaterMonitor(string panelTitle)
    {
        string name = panelTitle == null ? "" : panelTitle.ToLowerInvariant();
        return name.Contains("agua") || name.Contains("nutriente");
    }

    private static bool IsCropMonitor(string panelTitle)
    {
        string name = panelTitle == null ? "" : panelTitle.ToLowerInvariant();
        return name.Contains("raíz") || name.Contains("cultivo");
    }

    private static (string, string, float)[] GetReadings(string panelTitle)
    {
        string name = panelTitle == null ? "" : panelTitle.ToLowerInvariant();
        if (name.Contains("agua") || name.Contains("nutriente"))
            return new[] { ("RESERVORIO", "74 %", .74f), ("FLUJO DE RIEGO", "0.36 L/min", .58f), ("RETORNO", "0.28 L/min", .43f), ("pH / EC", "6.1 / 1.4", .62f) };
        if (name.Contains("raíz") || name.Contains("cultivo"))
            return new[] { ("HUMEDAD RADICULAR", "68 %", .68f), ("O₂ RADICULAR", "20.6 %", .82f), ("TEMP. FOLIAR", "22.1 °C", .57f), ("CÁMARAS", "SUPERIOR / LATERAL", .91f) };
        return new[] { ("TEMPERATURA", "21.8 °C", .69f), ("HUMEDAD RELATIVA", "63 %", .63f), ("CO₂", "612 ppm", .42f), ("O₂ / FILTRO", "20.9 % / NOMINAL", .88f) };
    }

    private void EnsureStyles()
    {
        if (headerStyle != null) return;
        headerStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 36, 22, 34), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = true };
        headerStyle.normal.textColor = new Color(1f, 0.94f, 0.82f);
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 62, 15, 22), alignment = TextAnchor.UpperLeft, wordWrap = true, padding = new RectOffset(4, 8, 4, 4) };
        bodyStyle.normal.textColor = new Color(0.91f, 0.88f, 0.83f);
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 76, 13, 18), alignment = TextAnchor.MiddleLeft, wordWrap = true };
        smallStyle.normal.textColor = new Color(0.79f, 0.81f, 0.81f);
        valueStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 56, 16, 23), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = true };
        valueStyle.normal.textColor = new Color(1f, 0.67f, 0.35f);
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.Clamp(Screen.height / 75, 13, 18), fontStyle = FontStyle.Bold };
    }

    private static Texture2D WhitePixel
    {
        get
        {
            if (whitePixel != null) return whitePixel;
            whitePixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whitePixel.SetPixel(0, 0, Color.white);
            whitePixel.Apply();
            return whitePixel;
        }
    }

    private static void DrawRect(Rect rect, Color color)
    {
        var previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, WhitePixel);
        GUI.color = previous;
    }
}

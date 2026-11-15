using UnityEngine;

// Interfaz del laboratorio. Los datos y gráficos de las pruebas son simulaciones didácticas.
internal sealed class MarsScienceLab
{
    private static readonly Color Ink = new(0.07f, 0.15f, 0.20f);
    private static readonly Color Light = new(0.96f, 0.97f, 0.92f);
    private static readonly string[] SignalClues = { "Cadena más corta", "Cadena intermedia", "Cadena más larga" };
    private static readonly string[] SamCompounds = { "Decano · 10 carbonos", "Undecano · 11 carbonos", "Dodecano · 12 carbonos" };
    private static readonly string[] GasClues = { "Vapor de agua", "Un carbono y dos oxígenos", "Dos oxígenos unidos" };
    private static readonly string[] Gases = { "H₂O", "CO₂", "O₂" };

    private readonly Texture2D[] art = new Texture2D[34];
    private readonly int[] signals = new int[3];
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    private GUIStyle sliderStyle;
    private GUIStyle thumbStyle;
    private MarsDiscovery specimen;
    private Texture2D specimenImage;
    private int variant;
    private float focus;
    private int pattern;
    private bool checkedAnswer;
    private bool success;

    public static string Hint(MarsDiscovery sample) => sample.instrument switch
    {
        "MAHLI" => "Ajusta el enfoque hasta la franja verde y toma una foto.",
        "SAM" => "Usa las pistas para relacionar tres señales con sus compuestos.",
        "CheMin" => "Busca la tarjeta cuyos picos coincidan con el patrón de referencia.",
        _ => ""
    };

    public void Open(MarsDiscovery sample)
    {
        specimen = sample;
        specimenImage = Resources.Load<Texture2D>("MarsDiscoveries/" + sample.id);
        variant = sample.id switch
        {
            "sedimentaria" or "suelo" or "minerales" => 1,
            "azufre" or "volatiles" or "arcillas" => 2,
            _ => 0
        };
        focus = 12f;
        pattern = -1;
        for (int i = 0; i < signals.Length; i++)
            signals[i] = -1;
        checkedAnswer = false;
        success = false;
    }

    // Devuelve true cuando se cierra la ventana.
    public bool Draw()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            Event.current.Use();
            return true;
        }
        if (Screen.width <= 24 || Screen.height <= 24) return false;

        EnsureStyles();
        const float width = 720f;
        const float height = 535f;
        float scale = Mathf.Min(1f, (Screen.width - 24f) / width, (Screen.height - 24f) / height);
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = -20;
        GUI.color = new Color(0f, 0.03f, 0.05f, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.matrix = previousMatrix * Matrix4x4.TRS(
            new Vector3((Screen.width - width * scale) * 0.5f, (Screen.height - height * scale) * 0.5f),
            Quaternion.identity, new Vector3(scale, scale, 1f));

        try
        {
            DrawArt(1, new Rect(0, 0, width, height));
            DrawArt(5, new Rect(16, 14, 688, 55));
            int instrumentIcon = specimen.instrument == "MAHLI" ? 31 : specimen.instrument == "SAM" ? 32 : 33;
            DrawArt(instrumentIcon, new Rect(29, 22, 37, 37), ScaleMode.ScaleToFit);
            GUI.Label(new Rect(77, 24, 557, 35), $"{specimen.instrument} · {specimen.title}", titleStyle);
            if (Button(new Rect(651, 25, 35, 30), "×", 12))
                return true;

            GUI.Label(new Rect(29, 79, 660, 32), Hint(specimen), textStyle);
            bool controlsEnabled = GUI.enabled;
            GUI.enabled = controlsEnabled && !success;
            switch (specimen.instrument)
            {
                case "MAHLI": DrawMahli(); break;
                case "SAM": DrawSam(); break;
                case "CheMin": DrawCheMin(); break;
            }
            GUI.enabled = controlsEnabled;

            if (checkedAnswer)
            {
                DrawArt(success ? 29 : 30, new Rect(31, 478, 26, 26), ScaleMode.ScaleToFit);
                string feedback = success ? "Análisis completado. Ya puedes consultar la ficha." : "Aún no coincide. Revisa la pista y vuelve a intentar.";
                GUI.Label(new Rect(65, 473, 432, 36), feedback, textStyle);
            }

            string action = success ? "Ver ficha" : specimen.instrument == "MAHLI" ? "Capturar foto" : "Comprobar";
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && (success || CanCheck());
            bool pressed = Button(new Rect(502, 470, 184, 45), action, success ? 11 : 8);
            GUI.enabled = previousEnabled;
            if (!pressed) return false;
            if (success) return true;

            checkedAnswer = true;
            success = specimen.instrument switch
            {
                "MAHLI" => Mathf.Abs(focus - FocusTarget) <= 8f,
                "SAM" => signals[0] == SignalAnswer(0) && signals[1] == SignalAnswer(1) && signals[2] == SignalAnswer(2),
                "CheMin" => pattern == variant,
                _ => false
            };
            return false;
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }

    private float FocusTarget => 46f + variant * 12f;

    private void DrawMahli()
    {
        var preview = new Rect(107, 112, 506, 240);
        DrawArt(3, preview);
        if (specimenImage != null)
        {
            GUI.color = Color.Lerp(new Color(0.60f, 0.70f, 0.75f), Color.white,
                1f - Mathf.Clamp01(Mathf.Abs(focus - FocusTarget) / 44f));
            GUI.DrawTexture(new Rect(241, 139, 238, 183), specimenImage, ScaleMode.ScaleToFit);
            GUI.color = Color.white;
        }
        DrawArt(19, new Rect(181, 130, 358, 196), ScaleMode.ScaleToFit);

        const float start = 111f;
        const float track = 498f;
        float left = start + track * (FocusTarget - 8f) / 100f;
        DrawArt(18, new Rect(left, 370, track * 0.16f, 11));
        focus = GUI.HorizontalSlider(new Rect(start, 385, track, 24), focus, 0f, 100f, sliderStyle, thumbStyle);
        GUI.Label(new Rect(start, 409, track, 27), $"Enfoque {Mathf.RoundToInt(focus)} %", textStyle);
    }

    private int SignalAnswer(int row) => (row + variant) % 3;

    private void DrawSam()
    {
        DrawArt(4, new Rect(30, 113, 660, 154));
        DrawArt(20, new Rect(52, 125, 616, 109));
        DrawArt(21, new Rect(53, 126, 613, 108));
        GUI.Label(new Rect(46, 236, 626, 25), "Tres señales · simulación educativa", textStyle);
        string[] names = variant == 0 ? SamCompounds : Gases;
        string[] clues = variant == 0 ? SignalClues : GasClues;
        for (int row = 0; row < 3; row++)
        {
            float y = 278f + row * 52f;
            GUI.Label(new Rect(39, y, 307, 40), $"Señal {row + 1} · {clues[SignalAnswer(row)]}", textStyle);
            string selected = signals[row] < 0 ? "Elegir compuesto" : names[signals[row]];
            if (Button(new Rect(356, y, 324, 39), selected, 26))
                signals[row] = (signals[row] + 1) % 3;
        }
        GUI.Label(new Rect(39, 432, 643, 25), "Pulsa el selector para cambiar de compuesto.", textStyle);
    }

    private void DrawCheMin()
    {
        DrawArt(4, new Rect(138, 109, 444, 153));
        GUI.Label(new Rect(156, 116, 410, 26), "Patrón de referencia · simulación educativa", textStyle);
        DrawArt(22, new Rect(166, 144, 388, 102));

        for (int i = 0; i < 3; i++)
        {
            float x = 30 + i * 224;
            var card = new Rect(x, 279, 212, 151);
            if (Button(card, "", pattern == i ? 14 : 13))
                pattern = i;
            int imageId = 23 + (i - variant + 3) % 3;
            DrawArt(imageId, new Rect(x + 18, 293, 176, 70));
            LabelInk(new Rect(x + 15, 396, 182, 27), $"Opción {(char)('A' + i)}");
        }
    }

    private bool CanCheck() => specimen.instrument switch
    {
        "SAM" => signals[0] >= 0 && signals[1] >= 0 && signals[2] >= 0,
        "CheMin" => pattern >= 0,
        _ => true
    };

    private bool Button(Rect rect, string text, int artId)
    {
        Texture2D background = GetArt(!GUI.enabled && artId == 8 ? 10 : artId);
        buttonStyle.normal.background = background;
        buttonStyle.hover.background = background;
        buttonStyle.active.background = GetArt(artId == 8 ? 9 : artId);
        return GUI.Button(rect, text, buttonStyle);
    }

    private void LabelInk(Rect rect, string label)
    {
        Color previous = textStyle.normal.textColor;
        textStyle.normal.textColor = Ink;
        GUI.Label(rect, label, textStyle);
        textStyle.normal.textColor = previous;
    }

    private Texture2D GetArt(int id)
    {
        if (art[id] == null)
            art[id] = Resources.Load<Texture2D>($"MarsLab/{id:00}");
        return art[id];
    }

    private void DrawArt(int id, Rect rect, ScaleMode mode = ScaleMode.StretchToFill)
    {
        var image = GetArt(id);
        if (image != null)
            GUI.DrawTexture(rect, image, mode);
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;

        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = Light } };
        titleStyle = new GUIStyle(textStyle) { fontSize = 23, fontStyle = FontStyle.Bold, wordWrap = false };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16, fontStyle = FontStyle.Bold, wordWrap = true,
            normal = { textColor = Ink }, hover = { textColor = Ink }, active = { textColor = Ink }
        };
        sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
        {
            normal = { background = GetArt(15) }
        };
        thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
        {
            fixedWidth = 19f, fixedHeight = 26f, normal = { background = GetArt(17) }
        };
    }
}

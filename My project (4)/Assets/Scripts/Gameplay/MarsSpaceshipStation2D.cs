using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Estación interactiva realista de la Nave Espacial (Módulo Lunar-Marciano Apollo LM).
/// Soporta:
///   - OxygenMoxie: Electrólisis de Óxido Sólido MOXIE (CO₂ -> O₂) con ventana didáctica interactiva.
///   - WaterUrineFilterUPA: Destilación Centrífuga por Compresión de Vapor (UPA / ECLSS) con ventana didáctica.
///   - SampleAnalyzerLab: Caja de Guantes de Bio-Aislamiento + Espectrómetro Láser UV Raman con ventana didáctica.
///   - ThermalControlBench: Mantenimiento del circuito LCVG + Sublimador Poroso y mejora Kapton (Nv.1->3).
///   - EvaAirlockHatch: Escotilla Frontal EVA del Apollo LM con proceso real de despresurización y salida a Marte.
///   - FlightCommandConsole: Consola de Control de Mando Apollo PGNCS / DSKY con telemetría en vivo.
///   - CommandChairProp: Sillas anatómicas de Comandante y Piloto con orden de profundidad y colisión física.
/// </summary>
[RequireComponent(typeof(MarsWorldPlaceholder2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MarsSpaceshipStation2D : MonoBehaviour
{
    public enum StationType
    {
        OxygenMoxie = 0,
        WaterUrineFilterUPA = 1,
        SampleAnalyzerLab = 2,
        ThermalControlBench = 3,
        EvaAirlockHatch = 4,
        FlightCommandConsole = 5,
        CommandChairProp = 6
    }

    private static MarsSpaceshipStation2D activeConsoleStation;
    private static bool arrivalSequencePlayedThisSceneLoad;

    public static bool IsMinigameActive =>
        activeConsoleStation != null &&
        (activeConsoleStation.consolaAbierta || activeConsoleStation.mostrarTarjetaEducativa);

    [SerializeField] private StationType stationType = StationType.OxygenMoxie;
    [SerializeField] private string labelTitle = "[ESTACIÓN NAVE]";
    [SerializeField] private Sprite activePulseSprite;
    [SerializeField] private Vector3 activePulseOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private GameObject fallbackAstronautPrefab;
    [SerializeField] private Vector2 solidObstacleSize = new Vector2(1.35f, 0.85f);
    [SerializeField] private Vector2 solidObstacleOffset = new Vector2(0f, -0.18f);

    [Header("Tamaño de Interfaz [E]")]
    [Range(1.0f, 1.8f)]
    [SerializeField] private float escalaPantallaConsola = 1.35f;

    private MarsWorldPlaceholder2D placeholder;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer activePulseRenderer;
    private bool usingCustomArtSprite;
    private float activePulseTimer;

    // Estado de la ventana interactiva didáctica
    private bool consolaAbierta;
    private bool mostrarTarjetaEducativa;
    private string tarjetaTitulo = "";
    private string tarjetaSubtitulo = "";
    private string tarjetaEstado = "";
    private readonly string[] tarjetaPuntos = new string[3];
    private int tarjetaPuntosCount;

    private float step1Value;        // 0..100
    private float step2Value;        // 0..100
    private bool step1Toggle;
    private int dskySelectedProgram = 1; // 1=PROG 64, 2=VERB 16/NOUN 36, 3=DONKI RADAR
    private string statusBanner = "";
    private Color statusBannerColor = new Color(0.35f, 0.92f, 1f, 1f);
    private float currentGuiScale = 1f;

    // Lienzo lógico de la consola. El GUI se escala uniformemente sobre este tamaño.
    private const float BasePanelW = 1000f;
    private const float BasePanelH = 630f;

    private static readonly Color TextMain = new Color(0.93f, 0.96f, 1f, 1f);
    private static readonly Color TextDim = new Color(0.62f, 0.72f, 0.83f, 1f);
    private static readonly Color Success = new Color(0.34f, 0.94f, 0.60f, 1f);
    private static readonly Color Warning = new Color(1f, 0.79f, 0.28f, 1f);
    private static readonly Color Danger = new Color(1f, 0.43f, 0.40f, 1f);

    private static Texture2D solidTex;
    private static Texture2D panelGradientTex;
    private static Texture2D headerGradientTex;
    private static Texture2D clearTex;
    private static Texture2D thumbTex;

    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle stepTitleStyle;
    private GUIStyle stepBodyStyle;
    private GUIStyle bodyStyle;
    private GUIStyle bodyTightStyle;
    private GUIStyle monoGreenStyle;
    private GUIStyle buttonLabelStyle;
    private GUIStyle primaryLabelStyle;
    private GUIStyle badgeStyle;
    private GUIStyle chipStyle;
    private GUIStyle statLabelStyle;
    private GUIStyle statValueStyle;
    private GUIStyle noteTitleStyle;
    private GUIStyle noteBodyStyle;
    private GUIStyle bulletStyle;
    private GUIStyle sliderStyle;
    private GUIStyle thumbStyle;

    public StationType Type => stationType;

    public bool SupportsHoldInteraction =>
        !IsMinigameActive &&
        (stationType == StationType.OxygenMoxie ||
         stationType == StationType.WaterUrineFilterUPA ||
         (stationType == StationType.ThermalControlBench &&
          DONKIFLRManager.Instance != null &&
          DONKIFLRManager.Instance.EstadoControlTermico < 98f));

    private void Awake()
    {
        placeholder = GetComponent<MarsWorldPlaceholder2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            usingCustomArtSprite = true;
            spriteRenderer.color = Color.white;
        }

        if (activePulseSprite != null && stationType != StationType.CommandChairProp)
        {
            var pulseGo = new GameObject("StationPulseFX");
            pulseGo.transform.SetParent(transform, false);
            pulseGo.transform.localPosition = activePulseOffset;
            activePulseRenderer = pulseGo.AddComponent<SpriteRenderer>();
            activePulseRenderer.sprite = activePulseSprite;
            activePulseRenderer.sortingOrder = 16;
            activePulseRenderer.enabled = false;
        }

        var triggerCol = GetComponent<BoxCollider2D>();
        if (triggerCol != null)
        {
            if (stationType == StationType.CommandChairProp)
            {
                triggerCol.isTrigger = false;
                triggerCol.size = new Vector2(0.95f, 0.75f);
                triggerCol.offset = new Vector2(0f, -0.12f);
            }
            else
            {
                triggerCol.isTrigger = true;
                if (triggerCol.size.sqrMagnitude < 0.05f)
                    triggerCol.size = new Vector2(2.0f, 1.65f);

                CreateSolidFootColliderIfNeeded();
            }
        }

        UpdateVisuals();
    }

    private void CreateSolidFootColliderIfNeeded()
    {
        if (solidObstacleSize.sqrMagnitude < 0.01f) return;

        // En la escotilla EVA inferior dejamos libre el paso central para que el astronauta camine encima de la rejilla
        if (stationType == StationType.EvaAirlockHatch) return;

        var solidGo = new GameObject("SolidFootprintCollider");
        solidGo.transform.SetParent(transform, false);
        solidGo.transform.localPosition = Vector3.zero;
        var box = solidGo.AddComponent<BoxCollider2D>();
        box.isTrigger = false;
        box.size = solidObstacleSize;
        box.offset = solidObstacleOffset;
    }

    private void Start()
    {
        // Si esta estación tiene el prefab de respaldo y no hay jugador en escena (ej. al dar Play directo en Spaceship_Interior)
        if (fallbackAstronautPrefab != null)
        {
            var existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer == null)
            {
                MarsScenePortal2D.SetDefaultExteriorReturnPosition(new Vector3(0f, -2.5f, 0f));
                var spawn = GameObject.Find("Spaceship_Interior_Spawn");
                Vector3 spawnPos = spawn != null ? spawn.transform.position : new Vector3(0f, -1.6f, 0f);
                var player = Instantiate(fallbackAstronautPrefab, spawnPos, Quaternion.identity);
                player.name = "Astronaut_Player";
                StartCoroutine(PlayCabinEntryStep(player));
            }
            else if (!arrivalSequencePlayedThisSceneLoad)
            {
                arrivalSequencePlayedThisSceneLoad = true;
                StartCoroutine(PlayCabinEntryStep(existingPlayer));
            }
        }
    }

    private void OnDestroy()
    {
        if (activeConsoleStation == this)
            activeConsoleStation = null;
        if (fallbackAstronautPrefab != null)
            arrivalSequencePlayedThisSceneLoad = false;
    }

    private static IEnumerator PlayCabinEntryStep(GameObject astronaut)
    {
        if (astronaut == null) yield break;
        var controller = astronaut.GetComponent<AstronautController2D>();
        var rb = astronaut.GetComponent<Rigidbody2D>();
        var sr = astronaut.GetComponent<SpriteRenderer>();

        Vector3 startPos = new Vector3(0f, -2.15f, 0f);
        Vector3 endPos = new Vector3(0f, -0.85f, 0f);

        controller?.SetTransitionLocked(true);
        astronaut.transform.position = startPos;

        float duration = 0.55f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            Vector3 p = Vector3.Lerp(startPos, endPos, t);
            if (rb != null) rb.position = p;
            else astronaut.transform.position = p;

            if (sr != null && controller != null)
                sr.sprite = controller.GetTransitionWalkSprite(true, (int)(elapsed / 0.16f) % 2);
            yield return null;
        }

        if (rb != null) rb.position = endPos;
        else astronaut.transform.position = endPos;

        controller?.FinishTransitionFacing(Vector2.up);
        controller?.SetTransitionLocked(false);
    }

    private void Update()
    {
        if (activePulseTimer > 0f)
        {
            activePulseTimer -= Time.deltaTime;
            if (activePulseRenderer != null)
            {
                activePulseRenderer.enabled = true;
                if (spriteRenderer != null)
                    activePulseRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
                float s = 0.94f + 0.10f * Mathf.Sin(Time.time * 12f);
                activePulseRenderer.transform.localScale = new Vector3(s, s, 1f);
                float a = 0.65f + 0.30f * Mathf.Sin(Time.time * 15f);
                activePulseRenderer.color = new Color(1f, 1f, 1f, a);
            }
        }
        else if (activePulseRenderer != null && activePulseRenderer.enabled)
        {
            activePulseRenderer.enabled = false;
        }

        if (consolaAbierta || mostrarTarjetaEducativa)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                CerrarConsola();
                return;
            }
        }

        UpdateVisuals();
    }

    private void LateUpdate()
    {
        if (spriteRenderer != null)
        {
            // Mismo criterio de profundidad por eje Y que AstronautController2D (10 + RoundToInt(-y * 3))
            spriteRenderer.sortingOrder = 10 + Mathf.RoundToInt(-transform.position.y * 3f);
        }
    }

    public string InteractTapFromPlayer()
    {
        if (stationType == StationType.CommandChairProp)
            return "Asiento anatómico de tripulación (Módulo Lunar-Marciano Apollo LM).";

        activePulseTimer = 0.35f;
        AbrirConsolaDidactica();

        return stationType switch
        {
            StationType.OxygenMoxie => "Abriendo Consola Didáctica MOXIE / ECS (Electrólisis CO₂ → O₂)...",
            StationType.WaterUrineFilterUPA => "Abriendo Consola Didáctica ECLSS UPA (Destilación Centrífuga de Agua)...",
            StationType.SampleAnalyzerLab => "Abriendo Laboratorio Glovebox y Espectrómetro Láser Raman...",
            StationType.ThermalControlBench => "Abriendo Banco de Mantenimiento Térmico PLSS / LCVG...",
            StationType.EvaAirlockHatch => "Abriendo Control de Presurización de la Escotilla Frontal EVA...",
            StationType.FlightCommandConsole => "Abriendo Computador de Guía Apollo (AGC / DSKY) y Control de Mando...",
            _ => labelTitle
        };
    }

    public string InteractHoldFromPlayer(float deltaTime)
    {
        if (stationType == StationType.CommandChairProp)
            return labelTitle;

        activePulseTimer = 0.18f;
        var flr = DONKIFLRManager.Instance;
        if (flr == null) return labelTitle;

        string result = stationType switch
        {
            StationType.OxygenMoxie => flr.RecargarOxigenoMoxieContinuo(deltaTime),
            StationType.WaterUrineFilterUPA => flr.FiltrarOrinaRecuperarAguaContinuo(deltaTime),
            StationType.ThermalControlBench => flr.RealizarMantenimientoTermicoContinuo(deltaTime),
            _ => labelTitle
        };

        UpdateVisuals();
        return result;
    }

    private void AbrirConsolaDidactica()
    {
        activeConsoleStation = this;
        consolaAbierta = true;
        mostrarTarjetaEducativa = false;
        step1Value = 0f;
        step2Value = 0f;
        step1Toggle = false;

        switch (stationType)
        {
            case StationType.OxygenMoxie:
                SetStatus("OBJETIVO 1 · Comprime el CO₂ marciano para alimentar el reactor.", StationAccent);
                break;
            case StationType.WaterUrineFilterUPA:
                SetStatus("OBJETIVO 1 · Abre la admisión y recupera el agua de la cabina.", StationAccent);
                break;
            case StationType.SampleAnalyzerLab:
                SetStatus(MarsSampleNode2D.HasHeldSample
                    ? $"Muestra en mano: {MarsSampleNode2D.HeldSampleLabel}. Sella el guante para analizarla."
                    : "Modo didáctico · Practica el análisis sin muestra real.", StationAccent);
                break;
            case StationType.ThermalControlBench:
                SetStatus("OBJETIVO 1 · Purga los 91 m de capilares del traje LCVG.", StationAccent);
                break;
            case StationType.EvaAirlockHatch:
                SetStatus("OBJETIVO 1 · Sella el traje a 4.3 psi antes de despresurizar.", StationAccent);
                break;
            case StationType.FlightCommandConsole:
                SetStatus("AGC en línea · Elige un programa para leer la telemetría.", StationAccent);
                break;
        }
    }

    private void SetStatus(string texto, Color color)
    {
        statusBanner = texto;
        statusBannerColor = color;
    }

    private void CerrarConsola()
    {
        consolaAbierta = false;
        mostrarTarjetaEducativa = false;
        if (activeConsoleStation == this)
            activeConsoleStation = null;
    }

    private void UpdateVisuals()
    {
        if (placeholder == null) return;

        if (stationType == StationType.CommandChairProp)
        {
            if (usingCustomArtSprite && spriteRenderer != null)
                spriteRenderer.color = Color.white;
            placeholder.SetVisualState(new Color(0.8f, 0.85f, 0.9f, 0.9f), labelTitle, "", !usingCustomArtSprite);
            return;
        }

        var flr = DONKIFLRManager.Instance;
        Color c;
        string sub;

        switch (stationType)
        {
            case StationType.OxygenMoxie:
                float o2 = flr != null ? flr.OxigenoActual : 100f;
                c = o2 < 35f
                    ? new Color(1.0f, 0.36f, 0.32f, 0.96f)
                    : new Color(0.26f, 0.86f, 1.0f, 0.94f);
                sub = $"O₂: {o2:F0}% — [E] Abrir Consola Didáctica MOXIE";
                break;

            case StationType.WaterUrineFilterUPA:
                float h2o = flr != null ? flr.AguaActual : 100f;
                float br = flr != null ? flr.ReservaOrinaHumedad : 40f;
                c = h2o < 35f
                    ? new Color(1.0f, 0.42f, 0.28f, 0.96f)
                    : new Color(0.28f, 0.65f, 1.0f, 0.94f);
                sub = $"Agua: {h2o:F0}% (Reserva: {br:F0}%) — [E] Consola UPA";
                break;

            case StationType.SampleAnalyzerLab:
                if (CommsTowerCMEEvent.HaySobrecargaActivaPorCME)
                {
                    c = new Color(1.0f, 0.30f, 0.42f, 0.96f);
                    sub = "¡BLOQUEADO POR CME! Apaga primero la Antena HGA";
                }
                else if (MarsSampleNode2D.HasHeldSample)
                {
                    c = new Color(0.28f, 0.98f, 0.54f, 0.96f);
                    sub = $"[E] Analizar {MarsSampleNode2D.HeldSampleLabel} ({MarsSampleNode2D.CollectedSamples}/{MarsSampleNode2D.TotalSamples})";
                }
                else
                {
                    c = new Color(0.42f, 0.72f, 0.56f, 0.90f);
                    sub = $"Analizadas: {MarsSampleNode2D.CollectedSamples}/{MarsSampleNode2D.TotalSamples} — [E] Abrir Espectrómetro";
                }
                break;

            case StationType.ThermalControlBench:
                if (flr == null)
                {
                    c = new Color(1.0f, 0.65f, 0.22f, 0.94f);
                    sub = "[E] Abrir Banco Térmico PLSS / LCVG";
                }
                else
                {
                    c = flr.EstadoControlTermico < 95f
                        ? new Color(1.0f, 0.48f, 0.16f, 0.96f)
                        : new Color(1.0f, 0.80f, 0.24f, 0.96f);
                    sub = $"Térmico {flr.EstadoControlTermico:F0}% (Nv.{flr.NivelControlTermico}) — [E] Abrir Banco PLSS";
                }
                break;

            case StationType.EvaAirlockHatch:
                c = new Color(0.95f, 0.52f, 0.26f, 0.95f);
                sub = "[E] Despresurizar Escotilla EVA y Salir a Marte";
                break;

            case StationType.FlightCommandConsole:
                c = new Color(0.98f, 0.82f, 0.24f, 0.96f);
                sub = "[E] Abrir Computador de Guía Apollo (DSKY / Telemetría)";
                break;

            default:
                c = Color.cyan;
                sub = "[E] Interactuar";
                break;
        }

        if (usingCustomArtSprite && spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        placeholder.SetVisualState(c, labelTitle, sub, !usingCustomArtSprite);
    }

    private static void EnsureTextures()
    {
        if (solidTex == null)
        {
            solidTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            solidTex.SetPixel(0, 0, Color.white);
            solidTex.Apply();
        }

        if (panelGradientTex == null)
        {
            panelGradientTex = MakeVerticalGradient(
                64,
                new Color(0.105f, 0.145f, 0.205f, 0.99f),
                new Color(0.045f, 0.070f, 0.105f, 0.99f));
        }

        if (headerGradientTex == null)
        {
            headerGradientTex = MakeVerticalGradient(
                48,
                new Color(0.135f, 0.185f, 0.255f, 1f),
                new Color(0.055f, 0.090f, 0.135f, 1f));
        }

        if (clearTex == null)
        {
            clearTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            clearTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
            clearTex.Apply();
        }

        if (thumbTex == null)
            thumbTex = MakeThumbTexture(16, 24);
    }

    private static Texture2D MakeVerticalGradient(int height, Color top, Color bottom)
    {
        height = Mathf.Max(2, height);
        var tex = new Texture2D(1, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D MakeThumbTexture(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool border = x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1;
                bool groove = y >= height / 2 - 1 && y <= height / 2 + 1 && x > 2 && x < width - 3;
                Color c = border
                    ? new Color(0.66f, 0.77f, 0.88f, 1f)
                    : groove
                        ? new Color(0.26f, 0.68f, 0.90f, 1f)
                        : new Color(0.86f, 0.92f, 0.98f, 1f);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private void EnsureStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            wordWrap = false,
            clipping = TextClipping.Clip,
            normal = { textColor = TextMain }
        };

        subHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            wordWrap = false,
            clipping = TextClipping.Clip,
            normal = { textColor = TextDim }
        };

        stepTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            wordWrap = false,
            clipping = TextClipping.Clip,
            normal = { textColor = TextMain }
        };

        stepBodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 16,
            wordWrap = true,
            richText = true,
            normal = { textColor = new Color(0.90f, 0.94f, 1f, 1f) }
        };

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 14,
            wordWrap = true,
            richText = true,
            normal = { textColor = new Color(0.84f, 0.90f, 0.97f, 1f) }
        };

        bodyTightStyle = new GUIStyle(bodyStyle)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft
        };

        monoGreenStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            richText = true,
            normal = { textColor = new Color(0.36f, 0.98f, 0.58f, 1f) }
        };

        buttonLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            clipping = TextClipping.Clip,
            normal = { textColor = TextMain }
        };

        primaryLabelStyle = new GUIStyle(buttonLabelStyle)
        {
            fontSize = 16,
            normal = { textColor = new Color(1f, 0.98f, 0.90f, 1f) }
        };

        badgeStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip,
            normal = { textColor = Color.white }
        };

        chipStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        statLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip,
            normal = { textColor = TextDim }
        };

        statValueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip,
            normal = { textColor = Color.white }
        };

        noteTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip,
            normal = { textColor = TextDim }
        };

        noteBodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 15,
            wordWrap = true,
            richText = true,
            normal = { textColor = new Color(0.74f, 0.82f, 0.90f, 1f) }
        };

        bulletStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 15,
            wordWrap = true,
            richText = true,
            normal = { textColor = TextMain }
        };

        sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
        {
            fixedHeight = 18f,
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            normal = { background = clearTex },
            hover = { background = clearTex },
            active = { background = clearTex },
            focused = { background = clearTex }
        };

        thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
        {
            fixedWidth = 16f,
            fixedHeight = 24f,
            border = new RectOffset(2, 2, 2, 2),
            normal = { background = thumbTex },
            hover = { background = thumbTex },
            active = { background = thumbTex }
        };
    }

    private Color StationAccent => stationType switch
    {
        StationType.OxygenMoxie => new Color(0.26f, 0.86f, 1f, 1f),
        StationType.WaterUrineFilterUPA => new Color(0.36f, 0.70f, 1f, 1f),
        StationType.SampleAnalyzerLab => new Color(0.38f, 0.95f, 0.62f, 1f),
        StationType.ThermalControlBench => new Color(1f, 0.64f, 0.24f, 1f),
        StationType.EvaAirlockHatch => new Color(1f, 0.58f, 0.28f, 1f),
        StationType.FlightCommandConsole => new Color(0.40f, 0.98f, 0.60f, 1f),
        _ => new Color(0.93f, 0.71f, 0.24f, 1f)
    };

    private string StationTag => stationType switch
    {
        StationType.OxygenMoxie => "O₂",
        StationType.WaterUrineFilterUPA => "H₂O",
        StationType.SampleAnalyzerLab => "LAB",
        StationType.ThermalControlBench => "PLSS",
        StationType.EvaAirlockHatch => "EVA",
        StationType.FlightCommandConsole => "AGC",
        _ => "NASA"
    };

    private string StationTitle => stationType switch
    {
        StationType.OxygenMoxie => "GENERADOR DE OXÍGENO · MOXIE",
        StationType.WaterUrineFilterUPA => "RECICLADO DE AGUA · ECLSS / UPA",
        StationType.SampleAnalyzerLab => "LABORATORIO DE MUESTRAS · GLOVEBOX",
        StationType.ThermalControlBench => "CONTROL TÉRMICO DEL TRAJE · PLSS",
        StationType.EvaAirlockHatch => "ESCOTILLA EVA · APOLLO LM",
        StationType.FlightCommandConsole => "PUENTE DE MANDO · AGC / DSKY",
        _ => labelTitle
    };

    private string StationSubtitle => stationType switch
    {
        StationType.OxygenMoxie => "Atmósfera de Marte → O₂ respirable  ·  Experimento NASA Perseverance",
        StationType.WaterUrineFilterUPA => "Ósmosis inversa y destilación centrífuga  ·  ISS / Artemis",
        StationType.SampleAnalyzerLab => "Protección planetaria y espectrometría  ·  Protocolo Apollo",
        StationType.ThermalControlBench => "LCVG + sublimador + Kapton MLI  ·  Traje EVA Apollo",
        StationType.EvaAirlockHatch => "Despresurización y salida al exterior marciano",
        StationType.FlightCommandConsole => "Computador de guía Apollo  ·  MIT Instrumentation Lab",
        _ => "Simulación didáctica basada en ingeniería real de la NASA"
    };

    private void OnGUI()
    {
        if (activeConsoleStation != this) return;
        if (!consolaAbierta && !mostrarTarjetaEducativa) return;
        if (Screen.width <= 40 || Screen.height <= 40) return;

        EnsureTextures();
        EnsureStyles();

        float maxEscalaAncho = (Screen.width - 20f) / BasePanelW;
        float maxEscalaAlto = (Screen.height - 20f) / BasePanelH;
        float escalaFinal = Mathf.Clamp(
            Mathf.Min(escalaPantallaConsola, Mathf.Min(maxEscalaAncho, maxEscalaAlto)),
            0.55f,
            1.9f);
        currentGuiScale = escalaFinal;

        Matrix4x4 prevMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escalaFinal, escalaFinal, 1f));

        float virtualW = Screen.width / escalaFinal;
        float virtualH = Screen.height / escalaFinal;

        Color prevColor = GUI.color;
        GUI.color = new Color(0.012f, 0.020f, 0.036f, 0.84f);
        GUI.DrawTexture(new Rect(0f, 0f, virtualW, virtualH), solidTex);
        GUI.color = prevColor;

        if (mostrarTarjetaEducativa)
        {
            DibujarTarjetaEducativaModal(virtualW, virtualH);
        }
        else
        {
            DibujarConsolaHardware(virtualW, virtualH);
        }

        GUI.matrix = prevMatrix;
    }

    private void DibujarConsolaHardware(float virtualW, float virtualH)
    {
        float w = Mathf.Min(BasePanelW, virtualW - 24f);
        float h = Mathf.Min(BasePanelH, virtualH - 24f);
        Rect panel = new Rect((virtualW - w) * 0.5f, (virtualH - h) * 0.5f, w, h);
        Color accent = StationAccent;

        // Chasis principal con bisel y fondo degradado.
        DrawBoxWithBorder(panel, new Color(0.035f, 0.050f, 0.075f, 0.99f), accent, 2.5f);
        GUI.DrawTexture(
            new Rect(panel.x + 3f, panel.y + 3f, panel.width - 6f, panel.height - 6f),
            panelGradientTex);

        Rect contentArea = DrawHeader(panel, accent);

        switch (stationType)
        {
            case StationType.OxygenMoxie:
                DibujarProcesoMoxie(contentArea);
                break;
            case StationType.WaterUrineFilterUPA:
                DibujarProcesoUPA(contentArea);
                break;
            case StationType.SampleAnalyzerLab:
                DibujarProcesoLaboratorio(contentArea);
                break;
            case StationType.ThermalControlBench:
                DibujarProcesoTermico(contentArea);
                break;
            case StationType.EvaAirlockHatch:
                DibujarProcesoEscotillaEVA(contentArea);
                break;
            case StationType.FlightCommandConsole:
                DibujarConsolaMandoApollo(contentArea);
                break;
        }

        DrawFooter(panel, accent);
    }

    private Rect DrawHeader(Rect panel, Color accent)
    {
        Rect header = new Rect(panel.x + 12f, panel.y + 10f, panel.width - 24f, 70f);
        DrawBoxWithBorder(header, new Color(0.055f, 0.080f, 0.115f, 1f), accent, 1.5f);
        GUI.DrawTexture(
            new Rect(header.x + 2f, header.y + 2f, header.width - 4f, header.height - 4f),
            headerGradientTex);

        Rect emblem = new Rect(header.x + 10f, header.y + 11f, 48f, 48f);
        DrawBoxWithBorder(emblem, new Color(0.035f, 0.065f, 0.095f, 1f), accent, 1.5f);

        Color prevText = chipStyle.normal.textColor;
        chipStyle.normal.textColor = accent;
        chipStyle.fontSize = 19;
        while (chipStyle.fontSize > 12 &&
               chipStyle.CalcSize(new GUIContent(StationTag)).x > emblem.width - 6f)
        {
            chipStyle.fontSize--;
        }
        GUI.Label(new Rect(emblem.x + 2f, emblem.y + 12f, emblem.width - 4f, 24f), StationTag, chipStyle);
        chipStyle.fontSize = 17;
        chipStyle.normal.textColor = prevText;

        GUI.Label(
            new Rect(emblem.xMax + 13f, header.y + 8f, header.width - emblem.width - 132f, 27f),
            StationTitle,
            headerStyle);
        GUI.Label(
            new Rect(emblem.xMax + 13f, header.y + 35f, header.width - emblem.width - 132f, 18f),
            StationSubtitle,
            subHeaderStyle);

        if (DrawButton(new Rect(header.xMax - 102f, header.y + 10f, 90f, 28f), "✕  ESC", new Color(0.90f, 0.46f, 0.36f, 1f), false, true))
        {
            CerrarConsola();
        }

        Rect missionPill = new Rect(header.xMax - 108f, header.y + 44f, 96f, 18f);
        DrawPill(missionPill, "APOLLO LM", new Color(0.58f, 0.70f, 0.82f, 1f), false);

        float contentY = header.yMax + 8f;
        float footerTop = panel.yMax - 58f;
        return new Rect(
            panel.x + 14f,
            contentY,
            panel.width - 28f,
            Mathf.Max(140f, footerTop - contentY - 8f));
    }

    private void DrawFooter(Rect panel, Color accent)
    {
        Rect footer = new Rect(panel.x + 12f, panel.yMax - 58f, panel.width - 24f, 46f);
        DrawBoxWithBorder(
            footer,
            new Color(0.045f, 0.070f, 0.100f, 1f),
            new Color(accent.r, accent.g, accent.b, 0.75f),
            1.5f);

        Color prev = GUI.color;
        GUI.color = accent;
        GUI.DrawTexture(new Rect(footer.x, footer.y, 4f, footer.height), solidTex);
        GUI.color = prev;

        Color prevTitle = noteTitleStyle.normal.textColor;
        noteTitleStyle.normal.textColor = accent;
        GUI.Label(new Rect(footer.x + 14f, footer.y + 3f, 180f, 16f), "OBJETIVO ACTUAL", noteTitleStyle);
        noteTitleStyle.normal.textColor = prevTitle;

        Color prevBody = bodyTightStyle.normal.textColor;
        bodyTightStyle.normal.textColor = new Color(0.96f, 0.98f, 1f, 1f);
        GUI.Label(new Rect(footer.x + 14f, footer.y + 18f, footer.width - 150f, 25f), statusBanner, bodyTightStyle);
        bodyTightStyle.normal.textColor = prevBody;

        DrawPill(
            new Rect(footer.xMax - 116f, footer.y + 9f, 104f, 28f),
            "ESC · CERRAR",
            new Color(0.60f, 0.70f, 0.80f, 1f),
            false);
    }

    private Rect[] SplitColumns(Rect area)
    {
        const float gap = 10f;
        float colW = (area.width - gap * 2f) / 3f;
        return new[]
        {
            new Rect(area.x, area.y, colW, area.height),
            new Rect(area.x + colW + gap, area.y, colW, area.height),
            new Rect(area.x + (colW + gap) * 2f, area.y, colW, area.height)
        };
    }

    private void DrawStepCard(Rect card, int step, string title, string objective, bool done, bool active, Color accent)
    {
        Color border = done
            ? Success
            : active
                ? accent
                : new Color(0.25f, 0.33f, 0.43f, 0.95f);
        Color background = done
            ? new Color(0.045f, 0.095f, 0.085f, 0.98f)
            : new Color(0.060f, 0.088f, 0.125f, 0.98f);

        DrawBoxWithBorder(card, background, border, done || active ? 2f : 1.2f);

        if (active && !done)
        {
            float pulse = 0.45f + 0.35f * Mathf.Sin(Time.time * 4.2f);
            Color prev = GUI.color;
            GUI.color = new Color(accent.r, accent.g, accent.b, pulse);
            GUI.DrawTexture(new Rect(card.x + 2f, card.y + 2f, card.width - 4f, 2f), solidTex);
            GUI.color = prev;
        }

        Rect chip = new Rect(card.x + 10f, card.y + 10f, 28f, 28f);
        DrawBoxWithBorder(
            chip,
            done ? new Color(0.06f, 0.18f, 0.13f, 1f) : new Color(0.035f, 0.075f, 0.110f, 1f),
            border,
            1f);

        Color prevChip = chipStyle.normal.textColor;
        chipStyle.normal.textColor = done ? Success : accent;
        GUI.Label(new Rect(chip.x, chip.y + 4f, chip.width, 20f), done ? "✓" : step.ToString(), chipStyle);
        chipStyle.normal.textColor = prevChip;

        string stateText = done ? "LISTO" : active ? "EN CURSO" : "BLOQUEADO";
        Color stateColor = done ? Success : active ? accent : new Color(0.55f, 0.63f, 0.73f, 1f);
        float stateWidth = badgeStyle.CalcSize(new GUIContent(stateText)).x;
        float pillW = Mathf.Max(72f, stateWidth + 12f);
        Rect statePill = new Rect(card.xMax - 10f - pillW, card.y + 10f, pillW, 24f);

        Color prevTitle = stepTitleStyle.normal.textColor;
        stepTitleStyle.normal.textColor = done ? Success : TextMain;
        float titleW = Mathf.Max(60f, statePill.x - 4f - (card.x + 42f));
        GUI.Label(new Rect(card.x + 42f, card.y + 7f, titleW, 30f), title, stepTitleStyle);
        stepTitleStyle.normal.textColor = prevTitle;

        DrawPill(statePill, stateText, stateColor, done || active);

        GUI.Label(
            new Rect(card.x + 12f, card.y + 41f, card.width - 24f, 76f),
            objective,
            stepBodyStyle);
    }

    private void DrawStatTile(Rect rect, string label, string value, Color accent)
    {
        DrawBoxWithBorder(
            rect,
            new Color(0.040f, 0.068f, 0.100f, 0.98f),
            new Color(accent.r, accent.g, accent.b, 0.42f),
            1f);

        GUI.Label(new Rect(rect.x + 5f, rect.y + 2f, rect.width - 10f, 14f), label, statLabelStyle);

        Color prev = statValueStyle.normal.textColor;
        statValueStyle.normal.textColor = accent;
        GUI.Label(new Rect(rect.x + 5f, rect.y + 16f, rect.width - 10f, rect.height - 18f), value, statValueStyle);
        statValueStyle.normal.textColor = prev;
    }

    private void DrawStatRow(
        Rect rect,
        string labelA, string valueA, Color colorA,
        string labelB, string valueB, Color colorB)
    {
        float half = (rect.width - 8f) * 0.5f;
        DrawStatTile(new Rect(rect.x, rect.y, half, rect.height), labelA, valueA, colorA);
        DrawStatTile(new Rect(rect.x + half + 8f, rect.y, half, rect.height), labelB, valueB, colorB);
    }

    private void DrawProgressBar(Rect rect, float t01, string label, Color barColor)
    {
        t01 = Mathf.Clamp01(t01);
        DrawBoxWithBorder(
            rect,
            new Color(0.030f, 0.052f, 0.078f, 1f),
            new Color(0.22f, 0.30f, 0.39f, 1f),
            1f);

        if (t01 > 0.005f)
        {
            Color prev = GUI.color;
            GUI.color = barColor;
            GUI.DrawTexture(
                new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * t01, rect.height - 4f),
                solidTex);

            GUI.color = new Color(1f, 1f, 1f, 0.22f);
            GUI.DrawTexture(
                new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * t01, 2f),
                solidTex);
            GUI.color = prev;
        }

        Color prevColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), label, badgeStyle);
        GUI.color = prevColor;
        GUI.Label(rect, label, badgeStyle);
    }

    private float DrawSlider(Rect rect, float value, bool enabled, Color accent)
    {
        bool prevEnabled = GUI.enabled;
        GUI.enabled = prevEnabled && enabled;

        Rect track = new Rect(rect.x, rect.y + (rect.height - 10f) * 0.5f, rect.width, 10f);
        DrawBoxWithBorder(
            track,
            enabled ? new Color(0.025f, 0.045f, 0.068f, 1f) : new Color(0.035f, 0.045f, 0.060f, 1f),
            enabled ? new Color(0.24f, 0.33f, 0.44f, 1f) : new Color(0.18f, 0.22f, 0.28f, 1f),
            1f);

        float t = Mathf.Clamp01(value / 100f);
        if (t > 0.005f)
        {
            Color prev = GUI.color;
            GUI.color = enabled ? accent : new Color(0.32f, 0.38f, 0.46f, 1f);
            GUI.DrawTexture(
                new Rect(track.x + 2f, track.y + 2f, (track.width - 4f) * t, track.height - 4f),
                solidTex);
            GUI.color = prev;
        }

        Color prevTick = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.24f);
        for (int i = 1; i <= 3; i++)
        {
            float x = Mathf.Lerp(track.x, track.xMax, i * 0.25f);
            GUI.DrawTexture(new Rect(x, track.y + 1f, 1f, track.height - 2f), solidTex);
        }
        GUI.color = prevTick;

        value = GUI.HorizontalSlider(rect, value, 0f, 100f, sliderStyle, thumbStyle);
        GUI.enabled = prevEnabled;
        return value;
    }

    private bool DrawButton(Rect rect, string label, Color accent, bool primary, bool enabled)
    {
        bool prevEnabled = GUI.enabled;
        GUI.enabled = prevEnabled && enabled;

        Vector2 mouseGuiPoint = Event.current != null
            ? Event.current.mousePosition / Mathf.Max(0.01f, currentGuiScale)
            : Vector2.zero;
        bool hover = enabled && prevEnabled && rect.Contains(mouseGuiPoint);

        Color background = !enabled
            ? new Color(0.085f, 0.105f, 0.135f, 0.92f)
            : primary
                ? Color.Lerp(new Color(0.075f, 0.115f, 0.165f, 1f), accent, hover ? 0.42f : 0.25f)
                : Color.Lerp(new Color(0.060f, 0.090f, 0.130f, 1f), accent, hover ? 0.22f : 0.06f);

        Color border = !enabled
            ? new Color(0.27f, 0.33f, 0.41f, 1f)
            : hover
                ? accent
                : new Color(
                    Mathf.Lerp(0.20f, accent.r, 0.55f),
                    Mathf.Lerp(0.27f, accent.g, 0.55f),
                    Mathf.Lerp(0.35f, accent.b, 0.55f),
                    1f);

        DrawBoxWithBorder(rect, background, border, primary ? 2f : 1.4f);

        if (primary && enabled)
        {
            Color prev = GUI.color;
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.9f);
            GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 2f), solidTex);
            GUI.color = prev;
        }

        GUIStyle style = primary ? primaryLabelStyle : buttonLabelStyle;
        Color prevText = style.normal.textColor;
        style.normal.textColor = enabled
            ? primary
                ? new Color(1f, 0.98f, 0.90f, 1f)
                : new Color(0.92f, 0.96f, 1f, 1f)
            : new Color(0.52f, 0.60f, 0.68f, 1f);

        GUI.Label(
            new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, rect.height - 4f),
            label,
            style);
        style.normal.textColor = prevText;

        bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        GUI.enabled = prevEnabled;
        return clicked;
    }

    private void DrawPill(Rect rect, string text, Color accent, bool filled)
    {
        Color background = filled
            ? Color.Lerp(new Color(0.040f, 0.070f, 0.100f, 1f), accent, 0.42f)
            : new Color(0.045f, 0.072f, 0.105f, 0.98f);

        DrawBoxWithBorder(rect, background, new Color(accent.r, accent.g, accent.b, 0.72f), 1f);

        Color prevText = badgeStyle.normal.textColor;
        badgeStyle.normal.textColor = filled ? Color.white : accent;
        GUI.Label(rect, text, badgeStyle);
        badgeStyle.normal.textColor = prevText;
    }

    private void DrawNasaNote(Rect rect, string title, string body, Color accent)
    {
        if (rect.width < 40f || rect.height < 24f) return;

        DrawBoxWithBorder(
            rect,
            new Color(0.035f, 0.060f, 0.090f, 0.96f),
            new Color(accent.r, accent.g, accent.b, 0.32f),
            1f);

        Color prev = GUI.color;
        GUI.color = new Color(accent.r, accent.g, accent.b, 0.9f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), solidTex);
        GUI.color = prev;

        Color prevTitle = noteTitleStyle.normal.textColor;
        noteTitleStyle.normal.textColor = accent;
        GUI.Label(new Rect(rect.x + 10f, rect.y + 4f, rect.width - 20f, 16f), title, noteTitleStyle);
        noteTitleStyle.normal.textColor = prevTitle;

        GUI.Label(
            new Rect(rect.x + 10f, rect.y + 22f, rect.width - 20f, rect.height - 28f),
            body,
            noteBodyStyle);
    }

    private void DrawHeaderStrip(Rect box, string title, string subtitle, Color accent)
    {
        Rect strip = new Rect(box.x + 7f, box.y + 7f, box.width - 14f, 38f);
        DrawBoxWithBorder(
            strip,
            new Color(0.040f, 0.070f, 0.100f, 0.98f),
            new Color(accent.r, accent.g, accent.b, 0.58f),
            1f);

        GUI.Label(new Rect(strip.x + 10f, strip.y + 2f, strip.width - 20f, 20f), title, stepTitleStyle);
        GUI.Label(new Rect(strip.x + 10f, strip.y + 22f, strip.width - 20f, 15f), subtitle, subHeaderStyle);
    }

    private void DrawInfoCard(Rect rect, string number, string title, string body, Color accent)
    {
        DrawBoxWithBorder(
            rect,
            new Color(0.048f, 0.076f, 0.110f, 0.98f),
            new Color(0.22f, 0.31f, 0.41f, 1f),
            1f);

        Rect chip = new Rect(rect.x + 9f, rect.y + 9f, 26f, 26f);
        DrawBoxWithBorder(chip, new Color(0.035f, 0.070f, 0.105f, 1f), accent, 1f);

        Color prevChip = chipStyle.normal.textColor;
        chipStyle.fontSize = 16;
        chipStyle.normal.textColor = accent;
        GUI.Label(new Rect(chip.x, chip.y + 5f, chip.width, 18f), number, chipStyle);
        chipStyle.normal.textColor = prevChip;
        chipStyle.fontSize = 17;

        GUI.Label(new Rect(rect.x + 44f, rect.y + 6f, rect.width - 54f, 26f), title, stepTitleStyle);
        GUI.Label(new Rect(rect.x + 11f, rect.y + 36f, rect.width - 22f, rect.height - 42f), body, bodyStyle);
    }

    private void PrepararTarjeta(string titulo, string subtitulo, string estado, params string[] puntos)
    {
        tarjetaTitulo = titulo;
        tarjetaSubtitulo = subtitulo;
        tarjetaEstado = estado;
        tarjetaPuntosCount = puntos != null ? Mathf.Min(tarjetaPuntos.Length, puntos.Length) : 0;
        for (int i = 0; i < tarjetaPuntos.Length; i++)
        {
            tarjetaPuntos[i] = i < tarjetaPuntosCount ? puntos[i] : "";
        }
        mostrarTarjetaEducativa = true;
    }

    // =========================================================================
    // 1. PROCESO REAL MOXIE: ELECTRÓLISIS DE ÓXIDO SÓLIDO (CO2 -> O2)
    // =========================================================================
    private void DibujarProcesoMoxie(Rect area)
    {
        var flr = DONKIFLRManager.Instance;
        float o2Actual = flr != null ? flr.OxigenoActual : 100f;
        float eActual = flr != null ? flr.EnergiaActual : 100f;
        Color accent = StationAccent;
        Rect[] col = SplitColumns(area);

        bool p1Done = step1Value >= 95f;
        bool p2Done = p1Done && step2Value >= 95f;

        // PASO 1: Captación y compresión del CO2 marciano.
        DrawStepCard(
            col[0], 1,
            "CAPTURA DE CO₂",
            "La atmósfera marciana es <b>95% CO₂</b> a 0.006 atm. El HEPA retiene polvo y el compresor la lleva a 1 atm.",
            p1Done, !p1Done, accent);

        float y1 = col[0].y + 118f;
        DrawStatRow(
            new Rect(col[0].x + 12f, y1, col[0].width - 24f, 42f),
            "ENTRADA", "95% CO₂", accent,
            "PRESIÓN", $"{Mathf.Lerp(0.01f, 1f, step1Value / 100f):F2} atm", Warning);

        DrawProgressBar(
            new Rect(col[0].x + 12f, y1 + 50f, col[0].width - 24f, 22f),
            step1Value / 100f,
            $"COMPRESOR HEPA · {step1Value:F0}%",
            accent);

        step1Value = DrawSlider(
            new Rect(col[0].x + 12f, y1 + 78f, col[0].width - 24f, 22f),
            step1Value, !p1Done, accent);

        if (DrawButton(
            new Rect(col[0].x + 12f, y1 + 104f, col[0].width - 24f, 36f),
            p1Done ? "✓ PRESIÓN LISTA · 1 ATM" : "COMPRIMIR CO₂ AL 100%",
            accent, !p1Done, !p1Done))
        {
            step1Value = 100f;
            SetStatus("OBJETIVO 2 · Calienta el reactor SOXE a 800 °C.", Warning);
        }

        DrawNasaNote(
            new Rect(col[0].x + 12f, y1 + 150f, col[0].width - 24f, col[0].yMax - y1 - 162f),
            "DATO NASA",
            "El aire de Marte es casi todo CO₂ y su presión es apenas el <b>0.6%</b> de la terrestre. El HEPA evita que el polvo de óxido de hierro dañe el reactor.",
            accent);

        // PASO 2: Reactor SOXE a 800 °C.
        DrawStepCard(
            col[1], 2,
            "HORNO SOXE",
            "Celdas cerámicas de circonio (YSZ) con aislamiento de <b>oro Kapton</b> separan el oxígeno a 800 °C.",
            p2Done, p1Done && !p2Done, accent);

        float y2 = col[1].y + 118f;
        float tempC = Mathf.Lerp(20f, 800f, step2Value / 100f);
        DrawStatRow(
            new Rect(col[1].x + 12f, y2, col[1].width - 24f, 42f),
            "REACTOR", "800 °C", Warning,
            "AISLANTE", "Kapton MLI", accent);

        DrawProgressBar(
            new Rect(col[1].x + 12f, y2 + 50f, col[1].width - 24f, 22f),
            step2Value / 100f,
            $"TEMPERATURA · {tempC:F0} / 800 °C",
            Warning);

        step2Value = DrawSlider(
            new Rect(col[1].x + 12f, y2 + 78f, col[1].width - 24f, 22f),
            step2Value, p1Done && !p2Done, Warning);

        if (DrawButton(
            new Rect(col[1].x + 12f, y2 + 104f, col[1].width - 24f, 36f),
            p2Done ? "✓ HORNO A 800 °C" : "CALENTAR HORNO A 800 °C",
            Warning, !p2Done, p1Done && !p2Done))
        {
            step2Value = 100f;
            SetStatus("OBJETIVO 3 · Ejecuta la electrólisis y llena el tanque de O₂.", Success);
        }

        DrawNasaNote(
            new Rect(col[1].x + 12f, y2 + 150f, col[1].width - 24f, col[1].yMax - y2 - 162f),
            "REACCIÓN REAL",
            "<b>2 CO₂ + electricidad → 2 CO + O₂</b>\nEl cátodo cerámico solo deja pasar iones de oxígeno (O²⁻).",
            Warning);

        // PASO 3: Electrólisis y recarga del tanque de O2.
        bool p3Done = o2Actual >= 99f;
        DrawStepCard(
            col[2], 3,
            "ELECTRÓLISIS O₂",
            "El oxígeno separado se recombina y pasa al tanque del traje PLSS listo para respirar.",
            p3Done, p2Done, accent);

        float y3 = col[2].y + 118f;
        DrawStatRow(
            new Rect(col[2].x + 12f, y3, col[2].width - 24f, 42f),
            "OXÍGENO", $"{o2Actual:F0}%", Success,
            "ENERGÍA", $"{eActual:F0}%", Warning);

        DrawProgressBar(
            new Rect(col[2].x + 12f, y3 + 50f, col[2].width - 24f, 22f),
            o2Actual / 100f,
            $"TANQUE O₂ · {o2Actual:F0}%",
            Success);

        DrawProgressBar(
            new Rect(col[2].x + 12f, y3 + 78f, col[2].width - 24f, 18f),
            eActual / 100f,
            $"ENERGÍA DISPONIBLE · {eActual:F0}%",
            Warning);

        if (DrawButton(
            new Rect(col[2].x + 12f, y3 + 108f, col[2].width - 24f, 44f),
            "⚡ EJECUTAR ELECTRÓLISIS\nRECARGAR OXÍGENO",
            Success, true, p2Done))
        {
            if (flr != null)
            {
                for (int i = 0; i < 30 && flr.OxigenoActual < 99.9f; i++)
                    flr.RecargarOxigenoMoxieContinuo(0.25f);
            }

            PrepararTarjeta(
                "OXÍGENO GENERADO DESDE EL AIRE DE MARTE",
                "Experimento MOXIE · NASA Perseverance",
                $"O₂ {(flr != null ? flr.OxigenoActual : 100f):F0}%  ·  ENERGÍA {(flr != null ? flr.EnergiaActual : 100f):F0}%",
                "CAPTACIÓN HEPA — Aire marciano (95% CO₂) filtrado y comprimido a 1 atm.",
                "HORNO SOXE — Celdas de circonio YSZ a 800 °C con aislamiento Kapton.",
                "ELECTRÓLISIS — 2CO₂ → 2CO + O₂ respirable almacenado en el PLSS.");
            SetStatus("PROCESO COMPLETO · El tanque de O₂ está listo para una nueva EVA.", Success);
        }

        DrawNasaNote(
            new Rect(col[2].x + 12f, y3 + 166f, col[2].width - 24f, col[2].yMax - y3 - 178f),
            "POR QUÉ IMPORTA",
            "MOXIE demuestra que una base marciana puede producir su propio oxígeno sin transportarlo desde la Tierra.",
            Success);
    }

    // =========================================================================
    // 2. PROCESO REAL UPA: DESTILACIÓN CENTRÍFUGA (ORINA/HUMEDAD -> AGUA)
    // =========================================================================
    private void DibujarProcesoUPA(Rect area)
    {
        var flr = DONKIFLRManager.Instance;
        float aguaActual = flr != null ? flr.AguaActual : 100f;
        float reservaBrine = flr != null ? flr.ReservaOrinaHumedad : 45f;
        float eActual = flr != null ? flr.EnergiaActual : 100f;
        Color accent = StationAccent;
        Rect[] col = SplitColumns(area);

        bool p1Done = step1Value >= 95f;
        bool p2Done = p1Done && step2Value >= 95f;

        // PASO 1: Admisión y pretratamiento.
        DrawStepCard(
            col[0], 1,
            "ADMISIÓN",
            "Se recolectan <b>orina, sudor y humedad</b> de la cabina y se estabilizan para evitar bacterias.",
            p1Done, !p1Done, accent);

        float y1 = col[0].y + 118f;
        DrawStatRow(
            new Rect(col[0].x + 12f, y1, col[0].width - 24f, 42f),
            "RESERVA", $"{reservaBrine:F0}%", Warning,
            "VÁLVULA", $"{step1Value:F0}%", accent);

        DrawProgressBar(
            new Rect(col[0].x + 12f, y1 + 50f, col[0].width - 24f, 22f),
            step1Value / 100f,
            $"VÁLVULA DE ADMISIÓN · {step1Value:F0}%",
            accent);

        step1Value = DrawSlider(
            new Rect(col[0].x + 12f, y1 + 78f, col[0].width - 24f, 22f),
            step1Value, !p1Done, accent);

        if (DrawButton(
            new Rect(col[0].x + 12f, y1 + 104f, col[0].width - 24f, 36f),
            p1Done ? "✓ ADMISIÓN ABIERTA" : "ABRIR VÁLVULA DE ENTRADA",
            accent, !p1Done, !p1Done))
        {
            step1Value = 100f;
            SetStatus("OBJETIVO 2 · Acelera la centrífuga al vacío para separar el agua.", accent);
        }

        DrawNasaNote(
            new Rect(col[0].x + 12f, y1 + 150f, col[0].width - 24f, col[0].yMax - y1 - 162f),
            "CIRCUITO CERRADO",
            "En la ISS se recupera hasta el <b>98%</b> del agua: cada gota de sudor, respiración u orina vuelve al sistema.",
            accent);

        // PASO 2: Destilación centrífuga al vacío.
        DrawStepCard(
            col[1], 2,
            "CENTRÍFUGA VCD",
            "En baja gravedad el vapor no sube solo: un tambor giratorio lo separa al vacío y retiene la salmuera.",
            p2Done, p1Done && !p2Done, accent);

        float y2 = col[1].y + 118f;
        DrawStatRow(
            new Rect(col[1].x + 12f, y2, col[1].width - 24f, 42f),
            "EBULLICIÓN", "~40 °C", accent,
            "GIRO", $"{step2Value * 2.4f:F0} RPM", Warning);

        DrawProgressBar(
            new Rect(col[1].x + 12f, y2 + 50f, col[1].width - 24f, 22f),
            step2Value / 100f,
            $"ROTACIÓN CENTRÍFUGA · {step2Value:F0}%",
            accent);

        step2Value = DrawSlider(
            new Rect(col[1].x + 12f, y2 + 78f, col[1].width - 24f, 22f),
            step2Value, p1Done && !p2Done, accent);

        if (DrawButton(
            new Rect(col[1].x + 12f, y2 + 104f, col[1].width - 24f, 36f),
            p2Done ? "✓ CENTRÍFUGA A 240 RPM" : "ACELERAR CENTRÍFUGA VCD",
            accent, !p2Done, p1Done && !p2Done))
        {
            step2Value = 100f;
            SetStatus("OBJETIVO 3 · Pule el destilado con filtros y luz UV.", Success);
        }

        DrawNasaNote(
            new Rect(col[1].x + 12f, y2 + 150f, col[1].width - 24f, col[1].yMax - y2 - 162f),
            "AHORRO ENERGÉTICO",
            "Al bajar la presión, el agua hierve cerca de <b>40 °C</b>. La centrífuga pega los residuos a la pared y deja vapor limpio en el centro.",
            accent);

        // PASO 3: Pulido WPA + UV.
        DrawStepCard(
            col[2], 3,
            "PULIDO WPA + UV",
            "Intercambio iónico, oxidación catalítica y luz ultravioleta eliminan cualquier contaminante.",
            aguaActual >= 99f, p2Done, accent);

        float y3 = col[2].y + 118f;
        DrawStatRow(
            new Rect(col[2].x + 12f, y3, col[2].width - 24f, 42f),
            "AGUA POTABLE", $"{aguaActual:F0}%", accent,
            "RESERVA", $"{reservaBrine:F0}%", Warning);

        DrawProgressBar(
            new Rect(col[2].x + 12f, y3 + 50f, col[2].width - 24f, 22f),
            aguaActual / 100f,
            $"DEPÓSITO DE AGUA · {aguaActual:F0}%",
            accent);

        DrawProgressBar(
            new Rect(col[2].x + 12f, y3 + 78f, col[2].width - 24f, 18f),
            reservaBrine / 100f,
            $"RESERVA FILTRABLE · {reservaBrine:F0}%",
            Warning);

        if (DrawButton(
            new Rect(col[2].x + 12f, y3 + 108f, col[2].width - 24f, 44f),
            "💧 PURIFICAR Y RECUPERAR AGUA\nECLSS UPA + WPA",
            accent, true, p2Done))
        {
            if (flr != null)
            {
                for (int i = 0; i < 30 && flr.ReservaOrinaHumedad > 1f && flr.AguaActual < 99.9f; i++)
                    flr.FiltrarOrinaRecuperarAguaContinuo(0.25f);
            }

            PrepararTarjeta(
                "AGUA RECICLADA EN CIRCUITO CERRADO",
                "ECLSS · UPA + WPA · ISS / Artemis",
                $"AGUA {(flr != null ? flr.AguaActual : 100f):F0}%  ·  RESERVA {(flr != null ? flr.ReservaOrinaHumedad : 0f):F0}%  ·  ENERGÍA {(flr != null ? flr.EnergiaActual : 100f):F0}%",
                "RECOLECCIÓN — Orina, sudor y condensado se estabilizan sin perder una gota.",
                "DESTILACIÓN VCD — Al vacío, el agua hierve a ~40 °C y deja atrás la salmuera.",
                "PULIDO WPA + UV — Intercambio iónico y luz ultravioleta entregan agua potable.");
            SetStatus("PROCESO COMPLETO · Agua disponible para beber, cultivos y refrigeración.", Success);
        }

        DrawNasaNote(
            new Rect(col[2].x + 12f, y3 + 166f, col[2].width - 24f, col[2].yMax - y3 - 178f),
            "CALIDAD DEL AGUA",
            "El agua reciclada del ECLSS supera los estándares potables terrestres. También enfría el traje y riega los cultivos.",
            Success);
    }

    // =========================================================================
    // 3. PROCESO REAL LABORATORIO: GLOVEBOX + ESPECTRÓMETRO RAMAN
    // =========================================================================
    private void DibujarProcesoLaboratorio(Rect area)
    {
        bool tieneMuestra = MarsSampleNode2D.HasHeldSample;
        var heldDiscovery = MarsSampleNode2D.HeldDiscovery;
        bool bloqueoCME = CommsTowerCMEEvent.HaySobrecargaActivaPorCME;
        Color accent = StationAccent;
        Rect[] col = SplitColumns(area);

        bool p1Done = step1Toggle;
        bool p2Done = p1Done && step2Value >= 95f;

        // PASO 1: Glovebox de bio-aislamiento.
        DrawStepCard(
            col[0], 1,
            "GLOVEBOX",
            "La muestra entra en una cámara sellada con guantes de neopreno para no contaminar la roca ni la nave.",
            p1Done, !p1Done, accent);

        float y1 = col[0].y + 118f;
        DrawStatRow(
            new Rect(col[0].x + 12f, y1, col[0].width - 24f, 42f),
            "CONTENEDOR EVA", tieneMuestra ? "1 / 1" : "0 / 1", tieneMuestra ? Success : Warning,
            "CÁMARA", p1Done ? "SELLADA" : "ABIERTA", p1Done ? Success : accent);

        DrawProgressBar(
            new Rect(col[0].x + 12f, y1 + 50f, col[0].width - 24f, 22f),
            p1Done ? 1f : 0f,
            tieneMuestra ? $"MUESTRA · {MarsSampleNode2D.HeldSampleLabel}" : "SIN MUESTRA EN MANO",
            tieneMuestra ? Success : new Color(0.55f, 0.63f, 0.72f, 1f));

        if (DrawButton(
            new Rect(col[0].x + 12f, y1 + 82f, col[0].width - 24f, 40f),
            p1Done ? "✓ CÁMARA SELLADA AL VACÍO" : "INSERTAR TUBO Y SELLAR CÁMARA",
            accent, !p1Done, !p1Done))
        {
            step1Toggle = true;
            SetStatus("OBJETIVO 2 · Ejecuta el instrumento científico asignado a la muestra.", accent);
        }

        DrawNasaNote(
            new Rect(col[0].x + 12f, y1 + 134f, col[0].width - 24f, col[0].yMax - y1 - 146f),
            "PROTECCIÓN PLANETARIA",
            $"Protocolo del Lunar Receiving Laboratory (Apollo).<br><b>{MarsSampleNode2D.CollectedSamples} / {MarsSampleNode2D.TotalSamples}</b> muestras catalogadas.",
            accent);

        // PASO 2: Instrumento NASA + minijuego.
        DrawStepCard(
            col[1], 2,
            "ESPECTRÓMETRO",
            "Curiosity analiza la muestra con cámara macro, cromatografía de gases o difracción de rayos X.",
            p2Done, p1Done && !p2Done, accent);

        float y2 = col[1].y + 118f;
        Rect photo = new Rect(col[1].x + 12f, y2, col[1].width - 24f, 86f);
        DrawBoxWithBorder(photo, new Color(0.028f, 0.048f, 0.072f, 1f), new Color(0.22f, 0.32f, 0.42f, 1f), 1f);

        Texture2D refPhoto = heldDiscovery != null ? heldDiscovery.LoadReference() : null;
        if (refPhoto != null)
        {
            GUI.DrawTexture(
                new Rect(photo.x + 4f, photo.y + 4f, photo.width - 8f, photo.height - 8f),
                refPhoto,
                ScaleMode.ScaleToFit);
        }
        else
        {
            GUI.Label(
                new Rect(photo.x + 10f, photo.y + 8f, photo.width - 20f, photo.height - 16f),
                heldDiscovery != null
                    ? $"<b>{heldDiscovery.title}</b>\nReferencia oficial: {heldDiscovery.credit}"
                    : "La fotografía oficial NASA aparece aquí cuando llevas una muestra al laboratorio.",
                bodyStyle);
        }

        bool puedeMinijuego = p1Done && heldDiscovery != null;
        if (DrawButton(
            new Rect(col[1].x + 12f, y2 + 94f, col[1].width - 24f, 34f),
            heldDiscovery != null
                ? $"🔬 ABRIR MINIJUEGO {heldDiscovery.instrument}"
                : "🔬 MINIJUEGO CON MUESTRA",
            accent, puedeMinijuego, puedeMinijuego))
        {
            step2Value = 100f;
            CerrarConsola();
            MarsMissionHUD.OpenExternalAnalysis(heldDiscovery);
            return;
        }

        step2Value = DrawSlider(
            new Rect(col[1].x + 12f, y2 + 136f, col[1].width - 24f, 20f),
            step2Value, p1Done && !p2Done, accent);

        if (DrawButton(
            new Rect(col[1].x + 12f, y2 + 160f, col[1].width - 24f, 34f),
            p2Done ? "✓ LECTURA CONFIRMADA" : "CONFIRMAR LECTURA DE DATOS",
            accent, !p2Done, p1Done && !p2Done))
        {
            step2Value = 100f;
            SetStatus("OBJETIVO 3 · Cataloga la muestra y libera el contenedor EVA.", Success);
        }

        DrawNasaNote(
            new Rect(col[1].x + 12f, y2 + 204f, col[1].width - 24f, col[1].yMax - y2 - 216f),
            heldDiscovery != null ? "HALLAZGO" : "CÓMO CONTINUAR",
            heldDiscovery != null
                ? $"<b>{heldDiscovery.title}</b><br>{heldDiscovery.description}"
                : "Recoge una muestra en Marte con [E], analízala y tráela de vuelta a la nave.",
            accent);

        // PASO 3: Catalogación y liberación del contenedor.
        DrawStepCard(
            col[2], 3,
            "CATALOGAR",
            "Guarda el núcleo sellado en la bóveda de retorno y libera el contenedor para la siguiente muestra.",
            false, p2Done, accent);

        float y3 = col[2].y + 118f;
        DrawStatRow(
            new Rect(col[2].x + 12f, y3, col[2].width - 24f, 42f),
            "CATALOGADAS", $"{MarsSampleNode2D.CollectedSamples} / {MarsSampleNode2D.TotalSamples}", Success,
            "ESTADO", bloqueoCME ? "BLOQUEADO" : tieneMuestra ? "LISTO" : "EN ESPERA",
            bloqueoCME ? Danger : tieneMuestra ? Success : new Color(0.60f, 0.68f, 0.76f, 1f));

        DrawProgressBar(
            new Rect(col[2].x + 12f, y3 + 50f, col[2].width - 24f, 22f),
            MarsSampleNode2D.TotalSamples > 0
                ? MarsSampleNode2D.CollectedSamples / (float)MarsSampleNode2D.TotalSamples
                : 0f,
            $"REGISTRO CIENTÍFICO · {MarsSampleNode2D.CollectedSamples} / {MarsSampleNode2D.TotalSamples}",
            Success);

        bool puedeCatalogar = p2Done && !bloqueoCME;
        if (DrawButton(
            new Rect(col[2].x + 12f, y3 + 84f, col[2].width - 24f, 52f),
            tieneMuestra
                ? "🔬 CATALOGAR Y GUARDAR\nLIBERAR CONTENEDOR EVA"
                : "🔬 COMPLETAR REGISTRO EN NAVE",
            Success, true, puedeCatalogar))
        {
            if (tieneMuestra)
            {
                MarsSampleNode2D.TryDeliverHeldSampleToShip(out string msg);
                PrepararTarjeta(
                    heldDiscovery != null
                        ? $"MUESTRA CATALOGADA · {heldDiscovery.title}"
                        : "MUESTRA MARCIANA CATALOGADA",
                    heldDiscovery != null
                        ? $"{heldDiscovery.instrument} · {heldDiscovery.credit}"
                        : "Bóveda de retorno · Módulo Apollo LM",
                    $"MUESTRAS {MarsSampleNode2D.CollectedSamples} / {MarsSampleNode2D.TotalSamples}  ·  CONTENEDOR LIBRE (0/1)",
                    heldDiscovery != null
                        ? $"INSTRUMENTO — {heldDiscovery.instrument}: {heldDiscovery.credit}."
                        : "REGISTRO — La muestra quedó guardada en la bóveda de retorno.",
                    heldDiscovery != null
                        ? $"HALLAZGO — {heldDiscovery.title}."
                        : "CONTENEDOR EVA — Libre para recoger otra muestra.",
                    "SIGUIENTE PASO — Sal a la superficie marciana y busca una nueva muestra.");
                SetStatus("MUESTRA GUARDADA · Contenedor EVA libre para otra recolección.", Success);
            }
            else
            {
                PrepararTarjeta(
                    "LABORATORIO LISTO PARA NUEVAS MUESTRAS",
                    "Glovebox + espectrómetro · Protección planetaria",
                    "CONTENEDOR EVA LIBRE (0/1)",
                    "RECOGE — Usa [E] sobre una muestra en la superficie de Marte.",
                    "ANALIZA — Juega el minijuego NASA (MAHLI / SAM / CheMin).",
                    "CATALOGA — Trae la muestra a la nave y regístrala aquí.");
            }
        }

        DrawNasaNote(
            new Rect(col[2].x + 12f, y3 + 148f, col[2].width - 24f, col[2].yMax - y3 - 160f),
            bloqueoCME ? "ALERTA CME" : "CURIOSITY",
            bloqueoCME
                ? "<b>Interferencia electromagnética activa.</b> Apaga la Antena HGA en la superficie antes de catalogar."
                : "Cada muestra desbloquea una ficha con fotografía, instrumento y explicación científica real.",
            bloqueoCME ? Danger : accent);
    }

    // =========================================================================
    // 4. PROCESO REAL CONTROL TÉRMICO: TRAJE PLSS / LCVG + SUBLIMADOR + KAPTON
    // =========================================================================
    private void DibujarProcesoTermico(Rect area)
    {
        var flr = DONKIFLRManager.Instance;
        float termicoActual = flr != null ? flr.EstadoControlTermico : 100f;
        int nivelActual = flr != null ? flr.NivelControlTermico : 1;
        float eActual = flr != null ? flr.EnergiaActual : 100f;
        float aActual = flr != null ? flr.AguaActual : 100f;
        Color accent = StationAccent;
        Rect[] col = SplitColumns(area);

        bool p1Done = step1Value >= 95f;
        bool p2Done = p1Done && step2Value >= 95f;

        // PASO 1: Purga de capilares LCVG.
        DrawStepCard(
            col[0], 1,
            "PURGA LCVG",
            "El traje interior lleva <b>91 m de capilares de agua</b> junto a la piel. Sin ellos, el cuerpo no puede ceder calor.",
            p1Done, !p1Done, accent);

        float y1 = col[0].y + 118f;
        DrawStatRow(
            new Rect(col[0].x + 12f, y1, col[0].width - 24f, 42f),
            "CIRCUITO", "91 m", accent,
            "BURBUJAS", $"{Mathf.Max(0f, 100f - step1Value):F0}%", Warning);

        DrawProgressBar(
            new Rect(col[0].x + 12f, y1 + 50f, col[0].width - 24f, 22f),
            step1Value / 100f,
            $"PURGA HIDRÁULICA · {step1Value:F0}%",
            accent);

        step1Value = DrawSlider(
            new Rect(col[0].x + 12f, y1 + 78f, col[0].width - 24f, 22f),
            step1Value, !p1Done, accent);

        if (DrawButton(
            new Rect(col[0].x + 12f, y1 + 104f, col[0].width - 24f, 36f),
            p1Done ? "✓ LCVG SIN BURBUJAS" : "PURGAR CIRCUITO HIDRÁULICO",
            accent, !p1Done, !p1Done))
        {
            step1Value = 100f;
            SetStatus("OBJETIVO 2 · Recarga el sublimador de la mochila PLSS.", Warning);
        }

        DrawNasaNote(
            new Rect(col[0].x + 12f, y1 + 150f, col[0].width - 24f, col[0].yMax - y1 - 162f),
            "SIN CONVECCIÓN",
            "En el espacio no hay aire que enfríe el cuerpo. El agua del LCVG es el único camino para evacuar el calor corporal y solar.",
            accent);

        // PASO 2: Sublimador de placa porosa.
        DrawStepCard(
            col[1], 2,
            "SUBLIMADOR PLSS",
            "El agua caliente se expone al vacío a través de una placa porosa: se congela y se sublima llevándose el calor.",
            p2Done, p1Done && !p2Done, accent);

        float y2 = col[1].y + 118f;
        DrawStatRow(
            new Rect(col[1].x + 12f, y2, col[1].width - 24f, 42f),
            "MOCHILA", "PLSS", Warning,
            "CARGA", $"{step2Value:F0}%", accent);

        DrawProgressBar(
            new Rect(col[1].x + 12f, y2 + 50f, col[1].width - 24f, 22f),
            step2Value / 100f,
            $"PLACA POROSA · {step2Value:F0}%",
            Warning);

        step2Value = DrawSlider(
            new Rect(col[1].x + 12f, y2 + 78f, col[1].width - 24f, 22f),
            step2Value, p1Done && !p2Done, Warning);

        if (DrawButton(
            new Rect(col[1].x + 12f, y2 + 104f, col[1].width - 24f, 36f),
            p2Done ? "✓ SUBLIMADOR RECARGADO" : "RECARGAR PLACA POROSA",
            Warning, !p2Done, p1Done && !p2Done))
        {
            step2Value = 100f;
            SetStatus("OBJETIVO 3 · Repara el sistema o instala capas Kapton MLI.", Success);
        }

        DrawNasaNote(
            new Rect(col[1].x + 12f, y2 + 150f, col[1].width - 24f, col[1].yMax - y2 - 162f),
            "TECNOLOGÍA APOLLO",
            "El sublimador poroso ya se usaba en el PLSS del Apollo y sigue siendo la base de los trajes EVA actuales.",
            Warning);

        // PASO 3: Reparación y mejora Kapton.
        bool p3Done = termicoActual >= 99f;
        DrawStepCard(
            col[2], 3,
            $"KAPTON MLI · NV.{nivelActual}",
            "Capas reflectantes de Kapton reducen el consumo de agua y energía del sistema térmico.",
            p3Done, p2Done, accent);

        float y3 = col[2].y + 118f;
        DrawStatRow(
            new Rect(col[2].x + 12f, y3, col[2].width - 24f, 42f),
            "CONTROL TÉRMICO", $"{termicoActual:F0}%", p3Done ? Success : Warning,
            "NIVEL", $"{nivelActual} / {DONKIFLRManager.MaxNivelControlTermico}", accent);

        DrawProgressBar(
            new Rect(col[2].x + 12f, y3 + 50f, col[2].width - 24f, 20f),
            termicoActual / 100f,
            $"SALUD TÉRMICA · {termicoActual:F0}%",
            p3Done ? Success : Warning);

        DrawProgressBar(
            new Rect(col[2].x + 12f, y3 + 74f, col[2].width - 24f, 18f),
            aActual / 100f,
            $"AGUA {aActual:F0}%  ·  ENERGÍA {eActual:F0}%",
            accent);

        if (DrawButton(
            new Rect(col[2].x + 12f, y3 + 102f, col[2].width - 24f, 38f),
            "🔧 MANTENIMIENTO AL 100%",
            Warning, false, p2Done && !p3Done))
        {
            if (flr != null)
            {
                for (int i = 0; i < 25 && (flr.EstadoControlTermico < 99.9f || flr.IntegridadTraje < 99.9f); i++)
                    flr.RealizarMantenimientoTermicoContinuo(0.25f);
            }
            SetStatus($"✓ Sistema térmico restaurado al {(flr != null ? flr.EstadoControlTermico : 100f):F0}%.", Success);
        }

        bool puedeMejorar = flr != null && flr.EstadoControlTermico >= 95f && flr.NivelControlTermico < DONKIFLRManager.MaxNivelControlTermico;
        string upgradeLabel = (flr != null && flr.NivelControlTermico >= DONKIFLRManager.MaxNivelControlTermico)
            ? "★ KAPTON EN NIVEL MÁXIMO · NV.3"
            : $"🛡️ INSTALAR KAPTON · NV.{nivelActual + 1}\n-16% ENERGÍA  ·  -14% AGUA";

        if (DrawButton(
            new Rect(col[2].x + 12f, y3 + 146f, col[2].width - 24f, 46f),
            upgradeLabel,
            Success, true, p2Done && puedeMejorar))
        {
            if (flr != null)
            {
                if (flr.IntentarMejorarControlTermico(out string upgMsg))
                {
                    PrepararTarjeta(
                        $"TRAJE BLINDADO A NIVEL TÉRMICO {flr.NivelControlTermico}",
                        "PLSS / LCVG · Aislamiento multicapa Kapton",
                        $"TÉRMICO {flr.EstadoControlTermico:F0}%  ·  AGUA {flr.AguaActual:F0}%  ·  ENERGÍA {flr.EnergiaActual:F0}%",
                        "LCVG — 91 m de capilares de agua enfrían el cuerpo dentro del traje.",
                        "SUBLIMADOR PLSS — El hielo pasa directo a vapor y expulsa el calor al vacío.",
                        "KAPTON MLI — Capas reflectantes reducen el consumo de agua y energía.");
                    SetStatus("MEJORA INSTALADA · El traje consume menos agua y energía.", Success);
                }
                else
                {
                    SetStatus(upgMsg, Danger);
                }
            }
        }

        DrawNasaNote(
            new Rect(col[2].x + 12f, y3 + 204f, col[2].width - 24f, col[2].yMax - y3 - 216f),
            "DATO NASA",
            "El Kapton soporta de <b>-269 °C a 400 °C</b>. Sus capas doradas protegen la nave y el traje de la radiación solar.",
            Success);
    }

    // =========================================================================
    // 5. PROCESO REAL ESCOTILLA EVA APOLLO LM: DESPRESURIZACIÓN Y SALIDA A MARTE
    // =========================================================================
    private void DibujarProcesoEscotillaEVA(Rect area)
    {
        Color accent = StationAccent;
        Rect[] col = SplitColumns(area);

        bool p1Done = step1Toggle;
        bool p2Done = p1Done && step2Value >= 95f;

        // PASO 1: Presurizar el traje.
        DrawStepCard(
            col[0], 1,
            "PRESURIZAR PLSS",
            "Sella el visor y presuriza el traje a <b>4.3 psi</b> con oxígeno puro antes de abrir la escotilla.",
            p1Done, !p1Done, accent);

        float y1 = col[0].y + 118f;
        DrawStatRow(
            new Rect(col[0].x + 12f, y1, col[0].width - 24f, 42f),
            "TRAJE", "4.3 psi", accent,
            "MARTE", "0.09 psi", Warning);

        if (DrawButton(
            new Rect(col[0].x + 12f, y1 + 58f, col[0].width - 24f, 44f),
            p1Done ? "✓ TRAJE SELLADO · 4.3 PSI" : "SELLAR VISOR Y PRESURIZAR",
            accent, !p1Done, !p1Done))
        {
            step1Toggle = true;
            SetStatus("OBJETIVO 2 · Abre la válvula de venteo para igualar la presión con Marte.", Warning);
        }

        DrawNasaNote(
            new Rect(col[0].x + 12f, y1 + 120f, col[0].width - 24f, col[0].yMax - y1 - 132f),
            "SEGURIDAD EVA",
            "La presurización evita la enfermedad por descompresión. El oxígeno puro reduce el nitrógeno disuelto en la sangre.",
            accent);

        // PASO 2: Válvula de venteo.
        DrawStepCard(
            col[1], 2,
            "VENTEO CABINA",
            "La escotilla Apollo abre hacia adentro. Mientras haya presión, miles de kilos la mantienen bloqueada.",
            p2Done, p1Done && !p2Done, accent);

        float y2 = col[1].y + 118f;
        float cabinPsi = Mathf.Lerp(4.8f, 0.09f, step2Value / 100f);
        DrawStatRow(
            new Rect(col[1].x + 12f, y2, col[1].width - 24f, 42f),
            "ESCOTILLA", $"{cabinPsi:F2} psi", Warning,
            "EXTERIOR", "0.09 psi", accent);

        DrawProgressBar(
            new Rect(col[1].x + 12f, y2 + 50f, col[1].width - 24f, 22f),
            step2Value / 100f,
            $"DESPRESURIZACIÓN · {step2Value:F0}%",
            Warning);

        step2Value = DrawSlider(
            new Rect(col[1].x + 12f, y2 + 78f, col[1].width - 24f, 22f),
            step2Value, p1Done && !p2Done, Warning);

        if (DrawButton(
            new Rect(col[1].x + 12f, y2 + 104f, col[1].width - 24f, 38f),
            p2Done ? "✓ PRESIÓN IGUALADA" : "ABRIR VÁLVULA DE DESCARGA",
            Warning, !p2Done, p1Done && !p2Done))
        {
            step2Value = 100f;
            SetStatus("OBJETIVO 3 · Destraba el volante y baja a la superficie marciana.", Success);
        }

        DrawNasaNote(
            new Rect(col[1].x + 12f, y2 + 152f, col[1].width - 24f, col[1].yMax - y2 - 164f),
            "APOLLO LM",
            "La cabina se despresurizaba antes de cada EVA. La válvula de descarga liberaba el aire al vacío de forma controlada.",
            Warning);

        // PASO 3: Abrir escotilla y salir.
        DrawStepCard(
            col[2], 3,
            "SALIDA A MARTE",
            "Al igualarse la presión, el volante libera los pestillos y la escalera de 9 peldaños queda disponible.",
            false, p2Done, accent);

        float y3 = col[2].y + 118f;
        DrawStatRow(
            new Rect(col[2].x + 12f, y3, col[2].width - 24f, 42f),
            "ESCOTILLA", p2Done ? "LIBRE" : "BLOQUEADA", p2Done ? Success : Danger,
            "DESTINO", "SUPERFICIE", accent);

        if (DrawButton(
            new Rect(col[2].x + 12f, y3 + 56f, col[2].width - 24f, 52f),
            "🚀 DESTRABAR ESCOTILLA\nSALIR A MARTE",
            Success, true, p2Done))
        {
            EjecutarSalidaAMarte();
        }

        if (DrawButton(
            new Rect(col[2].x + 12f, y3 + 116f, col[2].width - 24f, 34f),
            "⚡ SALIDA RÁPIDA DE EMERGENCIA",
            Danger, false, true))
        {
            EjecutarSalidaAMarte();
        }

        DrawNasaNote(
            new Rect(col[2].x + 12f, y3 + 162f, col[2].width - 24f, col[2].yMax - y3 - 174f),
            "CHECKLIST EVA",
            "Antes de salir: traje presurizado, PLSS cargado y presión de cabina igualada. La prisa es el peor enemigo.",
            Danger);
    }

    private void EjecutarSalidaAMarte()
    {
        CerrarConsola();
        var portal = GetComponent<MarsScenePortal2D>();
        var player = GameObject.FindGameObjectWithTag("Player");
        if (portal != null && player != null)
        {
            portal.Interact(player);
        }
    }

    // =========================================================================
    // 6. PUENTE DE MANDO APOLLO LM: COMPUTADOR DE GUÍA AGC / DSKY
    // =========================================================================
    private void DibujarConsolaMandoApollo(Rect area)
    {
        var flr = DONKIFLRManager.Instance;
        var cme = DONKICMEManager.Instance;
        Color accent = StationAccent;

        float leftW = area.width * 0.44f;
        Rect leftBox = new Rect(area.x, area.y, leftW, area.height);
        Rect rightBox = new Rect(leftBox.xMax + 12f, area.y, area.width - leftW - 12f, area.height);

        // ---- Panel izquierdo: AGC / DSKY ----
        DrawBoxWithBorder(leftBox, new Color(0.040f, 0.065f, 0.090f, 0.99f), accent, 1.5f);
        DrawHeaderStrip(leftBox, "COMPUTADOR AGC", "MIT · Apollo 1969", accent);

        Rect screen = new Rect(leftBox.x + 12f, leftBox.y + 50f, leftBox.width - 24f, 185f);
        DrawBoxWithBorder(screen, new Color(0.015f, 0.120f, 0.052f, 1f), new Color(0.22f, 0.88f, 0.42f, 1f), 1.5f);

        Color prevScreen = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.18f);
        for (int i = 1; i < 6; i++)
        {
            float ly = screen.y + screen.height * i / 6f;
            GUI.DrawTexture(new Rect(screen.x + 2f, ly, screen.width - 4f, 1f), solidTex);
        }
        GUI.color = prevScreen;

        string dskyText = dskySelectedProgram switch
        {
            1 => "PROG 64 · LANDING APPROACH\nVERB 16   NOUN 68\n\n" +
                 $"LAT  +18.38° N · JEZERO\nGRAV 0.38 G\nMUESTRAS  {MarsSampleNode2D.CollectedSamples} / {MarsSampleNode2D.TotalSamples}",
            2 => "PROG 00 · ECLSS LIFE SUPPORT\nVERB 16   NOUN 36\n\n" +
                 $"O₂   {(flr != null ? flr.OxigenoActual : 100f):F0}%      H₂O  {(flr != null ? flr.AguaActual : 100f):F0}%\n" +
                 $"⚡   {(flr != null ? flr.EnergiaActual : 100f):F0}%      TÉRM {(flr != null ? flr.EstadoControlTermico : 100f):F0}%\n" +
                 $"NIVEL TÉRM.  {(flr != null ? flr.NivelControlTermico : 1)} / {DONKIFLRManager.MaxNivelControlTermico}",
            _ => "PROG 37 · SPACE WEATHER\nVERB 06   NOUN 91\n\n" +
                 $"CME  {(cme != null && cme.HayTormentaCMEActiva ? $"ACTIVA · {cme.VelocidadActualKmS:F0} km/s" : "DESPEJADO")}\n" +
                 $"FLR  {(flr != null && flr.HayTormentaFLRActiva ? $"CLASE {flr.ClaseActual}" : $"PROB/DÍA {(flr != null ? flr.ProbabilidadEventoPorDia * 100f : 35f):F0}%")}\n" +
                 "ENLACE  ANTENA HGA"
        };

        GUI.Label(
            new Rect(screen.x + 14f, screen.y + 8f, screen.width - 28f, screen.height - 16f),
            dskyText,
            monoGreenStyle);

        GUI.Label(
            new Rect(leftBox.x + 12f, screen.yMax + 8f, leftBox.width - 24f, 34f),
            "VERBO = acción  ·  NOMBRE = dato",
            bodyStyle);

        float buttonY = screen.yMax + 48f;
        if (DrawButton(
            new Rect(leftBox.x + 12f, buttonY, leftBox.width - 24f, 32f),
            "64 · NAVEGACIÓN",
            accent, dskySelectedProgram == 1, true))
            dskySelectedProgram = 1;

        if (DrawButton(
            new Rect(leftBox.x + 12f, buttonY + 38f, leftBox.width - 24f, 32f),
            "00 · SOPORTE VITAL",
            accent, dskySelectedProgram == 2, true))
            dskySelectedProgram = 2;

        if (DrawButton(
            new Rect(leftBox.x + 12f, buttonY + 76f, leftBox.width - 24f, 32f),
            "37 · CLIMA DONKI",
            accent, dskySelectedProgram == 3, true))
            dskySelectedProgram = 3;

        DrawNasaNote(
            new Rect(leftBox.x + 12f, buttonY + 114f, leftBox.width - 24f, leftBox.yMax - buttonY - 126f),
            "TELEMETRÍA EN VIVO",
            "El DSKY mostraba cinco dígitos por registro. Aquí resume el estado real de la misión.",
            accent);

        // ---- Panel derecho: arquitectura del LM, sintetizada ----
        DrawBoxWithBorder(rightBox, new Color(0.040f, 0.065f, 0.090f, 0.99f), new Color(0.24f, 0.33f, 0.43f, 1f), 1.2f);
        DrawHeaderStrip(rightBox, "EL LM POR DENTRO", "Módulo Lunar Apollo", accent);

        float cardY = rightBox.y + 52f;
        DrawInfoCard(
            new Rect(rightBox.x + 12f, cardY, rightBox.width - 24f, 98f),
            "01",
            "VENTANAS LPD",
            "Triangulares e inclinadas hacia abajo, con retícula graduada para el descenso manual.",
            accent);

        DrawInfoCard(
            new Rect(rightBox.x + 12f, cardY + 106f, rightBox.width - 24f, 98f),
            "02",
            "PUESTOS DE MANDO",
            "Dos asientos anatómicos con arnés de 4 puntos frente al panel de guía PGNCS.",
            accent);

        DrawInfoCard(
            new Rect(rightBox.x + 12f, cardY + 212f, rightBox.width - 24f, 98f),
            "03",
            "CABINAS",
            "Babor: MOXIE y UPA. Estribor: glovebox científico y banco térmico PLSS.",
            accent);

        DrawNasaNote(
            new Rect(rightBox.x + 12f, cardY + 324f, rightBox.width - 24f, rightBox.yMax - cardY - 336f),
            "DATO CLAVE",
            "El AGC del Apollo tenía unos <b>2 KB de RAM</b> y aun así guió al hombre a la Luna en 1969.",
            Success);
    }

    private void DibujarTarjetaEducativaModal(float virtualW, float virtualH)
    {
        float w = Mathf.Min(790f, virtualW - 32f);
        float h = Mathf.Min(510f, virtualH - 32f);
        Rect card = new Rect((virtualW - w) * 0.5f, (virtualH - h) * 0.5f, w, h);
        Color accent = StationAccent;

        DrawBoxWithBorder(card, new Color(0.060f, 0.095f, 0.135f, 0.99f), Success, 2.5f);

        Color prev = GUI.color;
        GUI.color = Success;
        GUI.DrawTexture(new Rect(card.x + 2f, card.y + 2f, card.width - 4f, 3f), solidTex);
        GUI.color = prev;

        DrawPill(new Rect(card.x + 24f, card.y + 18f, 198f, 22f), "✓ PROCESO COMPLETADO", Success, true);
        DrawPill(new Rect(card.x + 230f, card.y + 18f, 118f, 22f), StationTag, accent, false);

        var titleStyle = new GUIStyle(headerStyle)
        {
            fontSize = 21,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        GUI.Label(new Rect(card.x + 24f, card.y + 50f, card.width - 48f, 50f), tarjetaTitulo, titleStyle);
        GUI.Label(new Rect(card.x + 24f, card.y + 101f, card.width - 48f, 20f), tarjetaSubtitulo, subHeaderStyle);

        float y = card.y + 128f;
        for (int i = 0; i < tarjetaPuntosCount; i++)
        {
            Rect row = new Rect(card.x + 24f, y, card.width - 48f, 66f);
            DrawBoxWithBorder(row, new Color(0.040f, 0.070f, 0.100f, 0.98f), new Color(0.20f, 0.31f, 0.41f, 1f), 1f);

            Rect chip = new Rect(row.x + 10f, row.y + 10f, 26f, 26f);
            DrawBoxWithBorder(chip, new Color(0.035f, 0.085f, 0.075f, 1f), Success, 1f);

            Color prevChip = chipStyle.normal.textColor;
            chipStyle.normal.textColor = Success;
            GUI.Label(new Rect(chip.x, chip.y + 4f, chip.width, 20f), (i + 1).ToString(), chipStyle);
            chipStyle.normal.textColor = prevChip;

            GUI.Label(new Rect(row.x + 46f, row.y + 7f, row.width - 58f, 52f), tarjetaPuntos[i], bulletStyle);
            y += 74f;
        }

        Rect stateBar = new Rect(card.x + 24f, card.yMax - 104f, card.width - 48f, 34f);
        DrawBoxWithBorder(stateBar, new Color(0.035f, 0.060f, 0.090f, 1f), new Color(Success.r, Success.g, Success.b, 0.55f), 1f);

        Color prevTitle = noteTitleStyle.normal.textColor;
        noteTitleStyle.normal.textColor = Success;
        GUI.Label(new Rect(stateBar.x + 12f, stateBar.y + 4f, 62f, 16f), "ESTADO", noteTitleStyle);
        noteTitleStyle.normal.textColor = prevTitle;

        Color prevBody = bodyStyle.normal.textColor;
        bodyStyle.normal.textColor = TextMain;
        GUI.Label(new Rect(stateBar.x + 78f, stateBar.y + 3f, stateBar.width - 90f, 24f), tarjetaEstado, bodyStyle);
        bodyStyle.normal.textColor = prevBody;

        if (DrawButton(
            new Rect(card.x + 24f, card.yMax - 58f, 256f, 38f),
            "VOLVER A LA CONSOLA",
            accent, false, true))
        {
            mostrarTarjetaEducativa = false;
            consolaAbierta = true;
            SetStatus("Proceso completado · Puedes repetirlo o revisar la telemetría.", Success);
        }

        if (DrawButton(
            new Rect(card.xMax - 288f, card.yMax - 58f, 264f, 38f),
            "✓ ENTENDIDO · CERRAR",
            Success, true, true))
        {
            CerrarConsola();
        }
    }

    private static void DrawBoxWithBorder(Rect rect, Color bg, Color border, float bw)
    {
        Color prev = GUI.color;
        GUI.color = bg;
        GUI.DrawTexture(rect, solidTex);
        GUI.color = border;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, bw), solidTex);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - bw, rect.width, bw), solidTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, bw, rect.height), solidTex);
        GUI.DrawTexture(new Rect(rect.xMax - bw, rect.y, bw, rect.height), solidTex);
        GUI.color = prev;
    }
}

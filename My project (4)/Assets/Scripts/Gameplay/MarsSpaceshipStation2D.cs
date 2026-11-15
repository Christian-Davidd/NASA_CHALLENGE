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

    private MarsWorldPlaceholder2D placeholder;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer activePulseRenderer;
    private bool usingCustomArtSprite;
    private float activePulseTimer;

    // Estado de la ventana interactiva didáctica
    private bool consolaAbierta;
    private bool mostrarTarjetaEducativa;
    private string tarjetaTitulo = "";
    private string tarjetaCuerpo = "";

    private float step1Value;        // 0..100
    private float step2Value;        // 0..100
    private bool step1Toggle;
    private int dskySelectedProgram = 1; // 1=PROG 64, 2=VERB 16/NOUN 36, 3=DONKI RADAR
    private string statusBanner = "";
    private Color statusBannerColor = new Color(0.35f, 0.92f, 1f, 1f);

    private static Texture2D solidTex;
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle stepTitleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle monoGreenStyle;
    private GUIStyle buttonStyle;
    private GUIStyle primaryActionBtnStyle;
    private GUIStyle badgeStyle;

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
                statusBanner = "PASO 1: Desliza el compresor HEPA al 100% para captar CO₂ de la atmósfera marciana.";
                statusBannerColor = new Color(0.28f, 0.88f, 1f, 1f);
                break;
            case StationType.WaterUrineFilterUPA:
                statusBanner = "PASO 1: Abre la válvula de admisión al 100% para bombear la reserva residual al destilador.";
                statusBannerColor = new Color(0.35f, 0.78f, 1f, 1f);
                break;
            case StationType.SampleAnalyzerLab:
                statusBanner = MarsSampleNode2D.HasHeldSample
                    ? $"Muestra lista: {MarsSampleNode2D.HeldSampleLabel}. PASO 1: Sella la cámara de bio-aislamiento."
                    : "Modo Didáctico activo (no llevas muestra en mano). Puedes practicar el proceso de espectrometría.";
                statusBannerColor = new Color(0.32f, 0.96f, 0.56f, 1f);
                break;
            case StationType.ThermalControlBench:
                statusBanner = "PASO 1: Purga al 100% los 91 metros de tubos capilares LCVG del traje interior.";
                statusBannerColor = new Color(1f, 0.68f, 0.24f, 1f);
                break;
            case StationType.EvaAirlockHatch:
                statusBanner = "PASO 1: Presuriza el traje PLSS a 4.3 psi con O₂ puro antes de ventilar la esclusa.";
                statusBannerColor = new Color(1f, 0.56f, 0.28f, 1f);
                break;
            case StationType.FlightCommandConsole:
                statusBanner = "COMPUTADOR DE GUÍA APOLLO (AGC / DSKY): Selecciona un programa de telemetría.";
                statusBannerColor = new Color(0.35f, 0.98f, 0.58f, 1f);
                break;
        }
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
            solidTex = new Texture2D(1, 1);
            solidTex.SetPixel(0, 0, Color.white);
            solidTex.Apply();
        }
    }

    private void EnsureStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.96f, 0.82f, 1f) }
        };

        subHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.68f, 0.86f, 1f, 1f) }
        };

        stepTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.38f, 0.92f, 1f, 1f) }
        };

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 13,
            wordWrap = true,
            richText = true,
            normal = { textColor = new Color(0.90f, 0.95f, 1f, 1f) }
        };

        monoGreenStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color(0.28f, 0.98f, 0.52f, 1f) }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        primaryActionBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.98f, 0.82f, 1f) }
        };

        badgeStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
    }

    private void OnGUI()
    {
        if (activeConsoleStation != this) return;
        if (!consolaAbierta && !mostrarTarjetaEducativa) return;

        EnsureTextures();
        EnsureStyles();

        // Fondo oscuro traslúcido estilo CommsTowerCMEEvent
        Color prev = GUI.color;
        GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.78f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), solidTex);
        GUI.color = prev;

        if (mostrarTarjetaEducativa)
        {
            DibujarTarjetaEducativaModal();
            return;
        }

        DibujarConsolaHardware();
    }

    private void DibujarConsolaHardware()
    {
        float w = Mathf.Min(900f, Screen.width - 36f);
        float h = Mathf.Min(560f, Screen.height - 36f);
        Rect panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

        // Chasis metálico Apollo LM
        DrawBoxWithBorder(panel, new Color(0.11f, 0.15f, 0.21f, 0.98f), new Color(0.88f, 0.65f, 0.18f, 1f), 3f);

        // Barra superior dorada/azul NASA
        Rect topBar = new Rect(panel.x + 10f, panel.y + 10f, panel.width - 20f, 52f);
        DrawBoxWithBorder(topBar, new Color(0.07f, 0.11f, 0.17f, 1f), new Color(0.28f, 0.62f, 0.88f, 1f), 2f);

        string headerText = stationType switch
        {
            StationType.OxygenMoxie => "ESTACIÓN NASA MOXIE + APOLLO ECS — GENERACIÓN DE OXÍGENO (CO₂ → O₂)",
            StationType.WaterUrineFilterUPA => "SISTEMA NASA ECLSS / UPA — DESTILACIÓN CENTRÍFUGA (ORINA Y HUMEDAD → AGUA)",
            StationType.SampleAnalyzerLab => "CAJA DE GUANTES LUNAR-MARCIANA Y ESPECTRÓMETRO LÁSER UV RAMAN",
            StationType.ThermalControlBench => "BANCO TÉRMICO DEL TRAJE PLSS / LCVG Y SUBLIMADOR DE PLACA POROSA",
            StationType.EvaAirlockHatch => "ESCOTILLA FRONTAL EVA APOLLO LM — DESPRESURIZACIÓN Y SALIDA A MARTE",
            StationType.FlightCommandConsole => "PUENTE DE MANDO APOLLO LM — COMPUTADOR DE GUÍA AGC / DSKY Y TELEMETRÍA",
            _ => labelTitle
        };
        GUI.Label(new Rect(topBar.x + 14f, topBar.y + 4f, topBar.width - 150f, 26f), headerText, headerStyle);
        GUI.Label(new Rect(topBar.x + 14f, topBar.y + 28f, topBar.width - 150f, 20f),
            "Módulo Lunar-Marciano Apollo LM  •  Entrenamiento Didáctico Basado en Ingeniería Real de la NASA", subHeaderStyle);

        if (GUI.Button(new Rect(topBar.xMax - 122f, topBar.y + 10f, 112f, 32f), "✕ CERRAR [ESC]", buttonStyle))
        {
            CerrarConsola();
            return;
        }

        // Contenido específico según estación
        Rect contentArea = new Rect(panel.x + 16f, topBar.yMax + 10f, panel.width - 32f, panel.height - 134f);

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

        // Barra inferior de estado e instrucciones paso a paso
        Rect bottomBar = new Rect(panel.x + 12f, panel.yMax - 52f, panel.width - 24f, 40f);
        DrawBoxWithBorder(bottomBar, new Color(0.06f, 0.09f, 0.14f, 1f), statusBannerColor, 2f);
        var bannerStyle = new GUIStyle(bodyStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            normal = { textColor = statusBannerColor }
        };
        GUI.Label(new Rect(bottomBar.x + 14f, bottomBar.y + 4f, bottomBar.width - 28f, 32f), statusBanner, bannerStyle);
    }

    // =========================================================================
    // 1. PROCESO REAL MOXIE: ELECTRÓLISIS DE ÓXIDO SÓLIDO (CO2 -> O2)
    // =========================================================================
    private void DibujarProcesoMoxie(Rect area)
    {
        var flr = DONKIFLRManager.Instance;
        float o2Actual = flr != null ? flr.OxigenoActual : 100f;
        float eActual = flr != null ? flr.EnergiaActual : 100f;

        float colW = (area.width - 24f) / 3f;
        Rect bay1 = new Rect(area.x, area.y, colW, area.height);
        Rect bay2 = new Rect(bay1.xMax + 12f, area.y, colW, area.height);
        Rect bay3 = new Rect(bay2.xMax + 12f, area.y, colW, area.height);

        bool p1Done = step1Value >= 95f;
        bool p2Done = p1Done && step2Value >= 95f;

        // PASO 1: Compresor HEPA
        DrawStepBay(bay1, "PASO 1: CAPTAR Y COMPRIMIR CO₂", p1Done, !p1Done);
        GUI.Label(new Rect(bay1.x + 12f, bay1.y + 38f, bay1.width - 24f, 76f),
            "La atmósfera de Marte tiene <b>95% de CO₂</b> a solo 0.006 atm. El filtro HEPA aspira el gas sin polvo y el compresor scroll lo sube a 1 atm.",
            bodyStyle);

        DrawProgressBar(new Rect(bay1.x + 14f, bay1.y + 122f, bay1.width - 28f, 24f), step1Value / 100f,
            $"Presión CO₂: {step1Value:F0}% ({Mathf.Lerp(0.01f, 1.0f, step1Value / 100f):F2} atm)", new Color(0.25f, 0.82f, 1f, 1f));

        step1Value = GUI.HorizontalSlider(new Rect(bay1.x + 16f, bay1.y + 158f, bay1.width - 32f, 24f), step1Value, 0f, 100f);
        if (GUI.Button(new Rect(bay1.x + 14f, bay1.y + 190f, bay1.width - 28f, 36f),
            p1Done ? "✓ COMPRESOR A 1.0 ATM" : "BOMBEAR CO₂ AL 100%", buttonStyle))
        {
            step1Value = 100f;
            statusBanner = "PASO 2: Calienta el reactor cerámico SOXE a 800 °C para separar los átomos de oxígeno.";
            statusBannerColor = new Color(1f, 0.82f, 0.25f, 1f);
        }

        // Mini-diagrama moléculas CO2
        Rect diag1 = new Rect(bay1.x + 14f, bay1.y + 238f, bay1.width - 28f, bay1.height - 252f);
        DrawBoxWithBorder(diag1, new Color(0.05f, 0.09f, 0.14f, 1f), new Color(0.2f, 0.45f, 0.65f, 1f), 1.5f);
        GUI.Label(new Rect(diag1.x + 8f, diag1.y + 8f, diag1.width - 16f, diag1.height - 16f),
            "<b>Fórmula Real NASA (Perseverance):</b>\n• Entrada: Aire marciano (CO₂)\n• Filtro HEPA retiene el polvo de óxido de hierro.\n• Estado: " + (p1Done ? "<color=#4ade80>COMPRIMIDO</color>" : "<color=#facc15>ESPERANDO COMPRESIÓN</color>"),
            bodyStyle);

        // PASO 2: Horno Cerámico SOXE 800 °C
        DrawStepBay(bay2, "PASO 2: HORNO SOXE A 800 °C", p2Done, p1Done && !p2Done);
        GUI.Label(new Rect(bay2.x + 12f, bay2.y + 38f, bay2.width - 24f, 76f),
            "El reactor tiene aislamiento de <b>oro Kapton</b> para no derretir la nave mientras calienta celdas cerámicas de circonio (YSZ) hasta <b>800 °C</b>.",
            bodyStyle);

        float tempC = Mathf.Lerp(20f, 800f, step2Value / 100f);
        DrawProgressBar(new Rect(bay2.x + 14f, bay2.y + 122f, bay2.width - 28f, 24f), step2Value / 100f,
            $"Temperatura SOXE: {tempC:F0} °C / 800 °C", new Color(1f, 0.62f, 0.18f, 1f));

        GUI.enabled = p1Done;
        step2Value = GUI.HorizontalSlider(new Rect(bay2.x + 16f, bay2.y + 158f, bay2.width - 32f, 24f), step2Value, 0f, 100f);
        if (GUI.Button(new Rect(bay2.x + 14f, bay2.y + 190f, bay2.width - 28f, 36f),
            p2Done ? "✓ HORNO CERÁMICO A 800 °C" : "CALENTAR HORNO A 800 °C", buttonStyle))
        {
            step2Value = 100f;
            statusBanner = "PASO 3: Activa la corriente de electrólisis para separar 2CO₂ → 2CO + O₂ respirable.";
            statusBannerColor = new Color(0.28f, 0.98f, 0.56f, 1f);
        }
        GUI.enabled = true;

        Rect diag2 = new Rect(bay2.x + 14f, bay2.y + 238f, bay2.width - 28f, bay2.height - 252f);
        DrawBoxWithBorder(diag2, new Color(0.05f, 0.09f, 0.14f, 1f), new Color(0.75f, 0.48f, 0.15f, 1f), 1.5f);
        GUI.Label(new Rect(diag2.x + 8f, diag2.y + 8f, diag2.width - 16f, diag2.height - 16f),
            "<b>Reacción Química a 800 °C:</b>\n2 CO₂ + electricidad → 2 CO + <b>O₂</b>\n• El cátodo cerámico solo deja pasar iones de oxígeno (O²⁻) puros.",
            bodyStyle);

        // PASO 3: Electrólisis y llenado de O2
        DrawStepBay(bay3, "PASO 3: ELECTRÓLISIS Y RECARGA O₂", o2Actual >= 99f, p2Done);
        GUI.Label(new Rect(bay3.x + 12f, bay3.y + 38f, bay3.width - 24f, 64f),
            "Aplica voltaje para recombinar iones O²⁻ en <b>Oxígeno respirable (O₂)</b> hacia los tanques esféricos del traje PLSS.",
            bodyStyle);

        DrawProgressBar(new Rect(bay3.x + 14f, bay3.y + 108f, bay3.width - 28f, 24f), o2Actual / 100f,
            $"Tanque de Oxígeno (O₂): {o2Actual:F0}%", new Color(0.22f, 0.92f, 0.52f, 1f));
        DrawProgressBar(new Rect(bay3.x + 14f, bay3.y + 138f, bay3.width - 28f, 22f), eActual / 100f,
            $"Energía de la Nave (⚡): {eActual:F0}%", new Color(0.98f, 0.80f, 0.22f, 1f));

        GUI.enabled = p2Done;
        if (GUI.Button(new Rect(bay3.x + 14f, bay3.y + 174f, bay3.width - 28f, 48f),
            "⚡ EJECUTAR ELECTRÓLISIS\n(RECARGAR OXÍGENO AL 100%)", primaryActionBtnStyle))
        {
            if (flr != null)
            {
                for (int i = 0; i < 30 && flr.OxigenoActual < 99.9f; i++)
                    flr.RecargarOxigenoMoxieContinuo(0.25f);
            }
            tarjetaTitulo = "¡OXÍGENO GENERADO DESDE EL AIRE DE MARTE (MOXIE)!";
            tarjetaCuerpo =
                "¡Completaste el proceso real del experimento MOXIE (Mars Oxygen ISRU Experiment) de la NASA!\n\n" +
                "• 1. Captación HEPA: Tomaste el aire marciano (95% CO₂) y lo comprimiste a 1 atmósfera.\n" +
                "• 2. Horno SOXE a 800 °C: Calentaste el electrolito cerámico de circonio recubierto en oro Kapton.\n" +
                "• 3. Separación Electroquímica (2CO₂ → 2CO + O₂): Extrajiste oxígeno puro respirable para el astronauta.\n\n" +
                $"Estado actual: Oxígeno { (flr != null ? flr.OxigenoActual : 100f):F0}%  |  Energía restante: {(flr != null ? flr.EnergiaActual : 100f):F0}%";
            mostrarTarjetaEducativa = true;
        }
        GUI.enabled = true;
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

        float colW = (area.width - 24f) / 3f;
        Rect bay1 = new Rect(area.x, area.y, colW, area.height);
        Rect bay2 = new Rect(bay1.xMax + 12f, area.y, colW, area.height);
        Rect bay3 = new Rect(bay2.xMax + 12f, area.y, colW, area.height);

        bool p1Done = step1Value >= 95f;
        bool p2Done = p1Done && step2Value >= 95f;

        // PASO 1: Admisión y pretratamiento
        DrawStepBay(bay1, "PASO 1: ADMISIÓN Y PRETRATAMIENTO", p1Done, !p1Done);
        GUI.Label(new Rect(bay1.x + 12f, bay1.y + 38f, bay1.width - 24f, 76f),
            "En el espacio y en Marte no se desperdicia ni una gota: se recolecta la <b>orina y el sudor/humedad condensada</b> de la cabina y se estabiliza químicamente.",
            bodyStyle);

        DrawProgressBar(new Rect(bay1.x + 14f, bay1.y + 122f, bay1.width - 28f, 24f), step1Value / 100f,
            $"Válvula de Admisión: {step1Value:F0}% (Reserva: {reservaBrine:F0}%)", new Color(0.95f, 0.75f, 0.20f, 1f));

        step1Value = GUI.HorizontalSlider(new Rect(bay1.x + 16f, bay1.y + 158f, bay1.width - 32f, 24f), step1Value, 0f, 100f);
        if (GUI.Button(new Rect(bay1.x + 14f, bay1.y + 190f, bay1.width - 28f, 36f),
            p1Done ? "✓ VÁLVULA ABIERTA AL 100%" : "ABRIR VÁLVULA DE ENTRADA", buttonStyle))
        {
            step1Value = 100f;
            statusBanner = "PASO 2: Acelera la centrífuga al vacío para separar el vapor de agua pura de las sales.";
            statusBannerColor = new Color(0.38f, 0.85f, 1f, 1f);
        }

        Rect diag1 = new Rect(bay1.x + 14f, bay1.y + 238f, bay1.width - 28f, bay1.height - 252f);
        DrawBoxWithBorder(diag1, new Color(0.05f, 0.09f, 0.14f, 1f), new Color(0.75f, 0.58f, 0.15f, 1f), 1.5f);
        GUI.Label(new Rect(diag1.x + 8f, diag1.y + 8f, diag1.width - 16f, diag1.height - 16f),
            $"<b>Reserva Reciclable en Cabina:</b> {reservaBrine:F0}%\n• Incluye condensado de respiración, sudor del traje EVA y orina pretratada.",
            bodyStyle);

        // PASO 2: Centrífuga de Destilación por Compresión de Vapor (VCD)
        DrawStepBay(bay2, "PASO 2: CENTRÍFUGA AL VACÍO (VCD)", p2Done, p1Done && !p2Done);
        GUI.Label(new Rect(bay2.x + 12f, bay2.y + 38f, bay2.width - 24f, 76f),
            "En baja gravedad el vapor no sube solo. La NASA usa un <b>tambor centrífugo rotatorio</b> al vacío que hierve el agua a baja temperatura y deja atrás la salmuera.",
            bodyStyle);

        DrawProgressBar(new Rect(bay2.x + 14f, bay2.y + 122f, bay2.width - 28f, 24f), step2Value / 100f,
            $"Rotación Centrífuga: {step2Value * 2.4f:F0} RPM", new Color(0.32f, 0.78f, 1f, 1f));

        GUI.enabled = p1Done;
        step2Value = GUI.HorizontalSlider(new Rect(bay2.x + 16f, bay2.y + 158f, bay2.width - 32f, 24f), step2Value, 0f, 100f);
        if (GUI.Button(new Rect(bay2.x + 14f, bay2.y + 190f, bay2.width - 28f, 36f),
            p2Done ? "✓ CENTRÍFUGA A 240 RPM" : "ACELERAR CENTRÍFUGA VCD", buttonStyle))
        {
            step2Value = 100f;
            statusBanner = "PASO 3: Activa el filtro catalítico y lámpara UV para obtener agua 100% potable.";
            statusBannerColor = new Color(0.28f, 0.98f, 0.56f, 1f);
        }
        GUI.enabled = true;

        Rect diag2 = new Rect(bay2.x + 14f, bay2.y + 238f, bay2.width - 28f, bay2.height - 252f);
        DrawBoxWithBorder(diag2, new Color(0.05f, 0.09f, 0.14f, 1f), new Color(0.25f, 0.58f, 0.85f, 1f), 1.5f);
        GUI.Label(new Rect(diag2.x + 8f, diag2.y + 8f, diag2.width - 16f, diag2.height - 16f),
            "<b>Destilación VCD (ISS / Artemis):</b>\n• Al bajar la presión, el agua hierve a ~40 °C ahorrando energía.\n• La fuerza centrífuga pega los residuos a la pared y extrae vapor limpio en el centro.",
            bodyStyle);

        // PASO 3: Filtro WPA y Mineralización
        DrawStepBay(bay3, "PASO 3: PURIFICACIÓN WPA Y UV", false, p2Done);
        GUI.Label(new Rect(bay3.x + 12f, bay3.y + 38f, bay3.width - 24f, 64f),
            "El destilado pasa por lechos de <b>intercambio iónico, oxidación catalítica y luz UV</b>, quedando más pura que el agua de la Tierra.",
            bodyStyle);

        DrawProgressBar(new Rect(bay3.x + 14f, bay3.y + 108f, bay3.width - 28f, 24f), aguaActual / 100f,
            $"Depósito Agua Potable (💧): {aguaActual:F0}%", new Color(0.25f, 0.82f, 1f, 1f));
        DrawProgressBar(new Rect(bay3.x + 14f, bay3.y + 138f, bay3.width - 28f, 22f), reservaBrine / 100f,
            $"Reserva Filtrable Disponible: {reservaBrine:F0}%", new Color(0.95f, 0.75f, 0.22f, 1f));

        GUI.enabled = p2Done;
        if (GUI.Button(new Rect(bay3.x + 14f, bay3.y + 174f, bay3.width - 28f, 48f),
            "💧 PURIFICAR Y RECUPERAR AGUA\n(SISTEMA ECLSS UPA + WPA)", primaryActionBtnStyle))
        {
            if (flr != null)
            {
                for (int i = 0; i < 30 && flr.ReservaOrinaHumedad > 1f && flr.AguaActual < 99.9f; i++)
                    flr.FiltrarOrinaRecuperarAguaContinuo(0.25f);
            }
            tarjetaTitulo = "¡RECICLAJE DE AGUA COMPLETADO (ECLSS UPA)!";
            tarjetaCuerpo =
                "¡Aprendiste cómo reciclan los astronautas hasta el 98% del agua en la Estación Espacial Internacional y misiones lunares/marcianas!\n\n" +
                "• 1. Recolección cerrada: Cada gota de humedad del aire, sudor del traje y orina se recupera.\n" +
                "• 2. Destilación Centrífuga al Vacío (VCD): Separa el vapor de agua en baja gravedad girando a alta velocidad.\n" +
                "• 3. Procesador WPA + UV: Elimina cualquier contaminante y entrega agua pura para beber, regar cultivos y enfriar el traje.\n\n" +
                $"Agua Potable actual: {(flr != null ? flr.AguaActual : 100f):F0}%  |  Energía: {(flr != null ? flr.EnergiaActual : 100f):F0}%";
            mostrarTarjetaEducativa = true;
        }
        GUI.enabled = true;
    }

    // =========================================================================
    // 3. PROCESO REAL LABORATORIO: GLOVEBOX + ESPECTRÓMETRO RAMAN
    // =========================================================================
    private void DibujarProcesoLaboratorio(Rect area)
    {
        bool tieneMuestra = MarsSampleNode2D.HasHeldSample;
        var heldDiscovery = MarsSampleNode2D.HeldDiscovery;
        bool bloqueoCME = CommsTowerCMEEvent.HaySobrecargaActivaPorCME;

        float colW = (area.width - 24f) / 3f;
        Rect bay1 = new Rect(area.x, area.y, colW, area.height);
        Rect bay2 = new Rect(bay1.xMax + 12f, area.y, colW, area.height);
        Rect bay3 = new Rect(bay2.xMax + 12f, area.y, colW, area.height);

        bool p1Done = step1Toggle;
        bool p2Done = p1Done && step2Value >= 95f;

        // PASO 1: Sellado de Caja de Guantes (Bio-Aislamiento)
        DrawStepBay(bay1, "PASO 1: ESCLUSA GLOVEBOX (1/1)", p1Done, !p1Done);
        GUI.Label(new Rect(bay1.x + 12f, bay1.y + 38f, bay1.width - 24f, 76f),
            "Igual que en el <b>Lunar Receiving Laboratory del programa Apollo</b>, la muestra entra en una cámara sellada con guantes de neopreno para no contaminar la roca ni la nave.",
            bodyStyle);

        string sampleStatus = tieneMuestra
            ? $"En contenedor EVA: {MarsSampleNode2D.HeldSampleLabel}"
            : "Sin muestra en mano (Recoge 1 afuera)";
        DrawProgressBar(new Rect(bay1.x + 14f, bay1.y + 122f, bay1.width - 28f, 24f), p1Done ? 1f : 0f,
            sampleStatus, new Color(0.28f, 0.92f, 0.56f, 1f));

        if (GUI.Button(new Rect(bay1.x + 14f, bay1.y + 162f, bay1.width - 28f, 42f),
            p1Done ? "✓ CÁMARA SELLADA AL VACÍO" : "INSERTAR TUBO Y SELLAR CÁMARA", buttonStyle))
        {
            step1Toggle = true;
            statusBanner = "PASO 2: Ejecuta el análisis científico con el instrumento real de la NASA.";
            statusBannerColor = new Color(0.35f, 0.96f, 0.62f, 1f);
        }

        Rect diag1 = new Rect(bay1.x + 14f, bay1.y + 218f, bay1.width - 28f, bay1.height - 232f);
        DrawBoxWithBorder(diag1, new Color(0.05f, 0.09f, 0.14f, 1f), new Color(0.22f, 0.68f, 0.45f, 1f), 1.5f);
        GUI.Label(new Rect(diag1.x + 8f, diag1.y + 8f, diag1.width - 16f, diag1.height - 16f),
            $"<b>Progreso Geológico:</b> {MarsSampleNode2D.CollectedSamples} / {MarsSampleNode2D.TotalSamples} muestras.\n• Protección Planetaria NASA: evita mezclar microorganismos terrestres con muestras marcianas.",
            bodyStyle);

        // PASO 2: Laboratorio Científico NASA (MAHLI / SAM / CheMin) + Foto real NASA
        string instrumentLabel = heldDiscovery != null ? heldDiscovery.instrument : "MAHLI / SAM / CheMin";
        DrawStepBay(bay2, $"PASO 2: INSTRUMENTO {instrumentLabel}", p2Done, p1Done && !p2Done);

        Texture2D refPhoto = heldDiscovery != null ? heldDiscovery.LoadReference() : null;
        if (refPhoto != null)
        {
            GUI.DrawTexture(new Rect(bay2.x + 14f, bay2.y + 38f, bay2.width - 28f, 96f), refPhoto, ScaleMode.ScaleToFit);
        }
        else
        {
            GUI.Label(new Rect(bay2.x + 12f, bay2.y + 38f, bay2.width - 24f, 96f),
                "Aquí puedes verificar los hallazgos obtenidos con los instrumentos de Curiosity: <b>MAHLI</b> (cámara macro), <b>SAM</b> (gases) y <b>CheMin</b> (rayos X).",
                bodyStyle);
        }

        GUI.enabled = p1Done;
        if (heldDiscovery != null)
        {
            if (GUI.Button(new Rect(bay2.x + 14f, bay2.y + 142f, bay2.width - 28f, 36f),
                $"🔬 ABRIR MINIJUEGO {heldDiscovery.instrument} (NASA)", buttonStyle))
            {
                step2Value = 100f;
                CerrarConsola();
                MarsMissionHUD.OpenExternalAnalysis(heldDiscovery);
                return;
            }
        }

        step2Value = GUI.HorizontalSlider(new Rect(bay2.x + 16f, bay2.y + 186f, bay2.width - 32f, 20f), step2Value, 0f, 100f);
        if (GUI.Button(new Rect(bay2.x + 14f, bay2.y + 210f, bay2.width - 28f, 34f),
            p2Done ? "✓ VERIFICACIÓN COMPLETADA" : "CONFIRMAR LECTURA DE DATOS", buttonStyle))
        {
            step2Value = 100f;
            statusBanner = "PASO 3: Cataloga la muestra para vaciar tu contenedor (0/1) y salir por otra.";
            statusBannerColor = new Color(0.28f, 0.98f, 0.56f, 1f);
        }
        GUI.enabled = true;

        Rect diag2 = new Rect(bay2.x + 14f, bay2.y + 252f, bay2.width - 28f, bay2.height - 266f);
        DrawBoxWithBorder(diag2, new Color(0.05f, 0.09f, 0.14f, 1f), new Color(0.22f, 0.68f, 0.45f, 1f), 1.5f);
        string infoTexto = heldDiscovery != null
            ? $"<b>{heldDiscovery.title}</b> ({heldDiscovery.credit})\n{heldDiscovery.description}"
            : "Recoge muestras reales en Marte [E] para ejecutar los minijuegos de <b>MAHLI</b>, <b>SAM</b> y <b>CheMin</b> y ver su fotografía oficial NASA.";
        GUI.Label(new Rect(diag2.x + 8f, diag2.y + 8f, diag2.width - 16f, diag2.height - 16f), infoTexto, bodyStyle);

        // PASO 3: Catalogación y Liberación del Contenedor (1/1 -> 0/1)
        DrawStepBay(bay3, "PASO 3: CATALOGAR MUESTRA", false, p2Done);
        GUI.Label(new Rect(bay3.x + 12f, bay3.y + 38f, bay3.width - 24f, 76f),
            bloqueoCME
                ? "<color=#ff6b6b>¡INTERFERENCIA CME ACTIVA!</color> La Antena HGA encendida induce ruido electromagnético. Apágala afuera primero."
                : "Guarda el núcleo sellado en la bóveda de retorno de la nave y libera tu contenedor EVA para recoger la siguiente muestra.",
            bodyStyle);

        GUI.enabled = p2Done && !bloqueoCME;
        if (GUI.Button(new Rect(bay3.x + 14f, bay3.y + 150f, bay3.width - 28f, 52f),
            tieneMuestra
                ? "🔬 CATALOGAR Y GUARDAR MUESTRA\n(LIBERAR CONTENEDOR EVA)"
                : "🔬 COMPLETAR REGISTRO EN NAVE", primaryActionBtnStyle))
        {
            if (tieneMuestra)
            {
                MarsSampleNode2D.TryDeliverHeldSampleToShip(out string msg);
                tarjetaTitulo = heldDiscovery != null
                    ? $"MUESTRA CATALOGADA: {heldDiscovery.title.ToUpperInvariant()}"
                    : "¡MUESTRA MARCIANA ANALIZADA Y CATALOGADA!";
                tarjetaCuerpo = heldDiscovery != null
                    ? $"{msg}\n\n• Instrumento: {heldDiscovery.instrument} ({heldDiscovery.credit})\n• Resumen NASA: {heldDiscovery.description}\n• Contenedor EVA libre (0/1): ya puedes salir a recoger otra muestra."
                    : $"{msg}\n\n• Contenedor EVA libre (0/1): ¡Ya puedes salir a la superficie marciana a recoger la siguiente muestra!";
            }
            else
            {
                tarjetaTitulo = "LABORATORIO LISTO PARA NUEVAS MUESTRAS";
                tarjetaCuerpo =
                    "Recoge una muestra en la superficie de Marte con [E] para jugar su minijuego NASA (MAHLI / SAM / CheMin), ver su fotografía oficial y luego traerla aquí para catalogarla.";
            }
            mostrarTarjetaEducativa = true;
        }
        GUI.enabled = true;
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

        float colW = (area.width - 24f) / 3f;
        Rect bay1 = new Rect(area.x, area.y, colW, area.height);
        Rect bay2 = new Rect(bay1.xMax + 12f, area.y, colW, area.height);
        Rect bay3 = new Rect(bay2.xMax + 12f, area.y, colW, area.height);

        bool p1Done = step1Value >= 95f;
        bool p2Done = p1Done && step2Value >= 95f;

        // PASO 1: Purga de tubos LCVG
        DrawStepBay(bay1, "PASO 1: PURGAR TUBOS LCVG", p1Done, !p1Done);
        GUI.Label(new Rect(bay1.x + 12f, bay1.y + 38f, bay1.width - 24f, 76f),
            "En el espacio el aire no enfría el cuerpo. El traje interior <b>LCVG</b> tiene <b>91 metros de tubitos de agua</b> pegados a la piel que absorben el calor corporal y solar.",
            bodyStyle);

        DrawProgressBar(new Rect(bay1.x + 14f, bay1.y + 122f, bay1.width - 28f, 24f), step1Value / 100f,
            $"Purga de Burbujas LCVG: {step1Value:F0}%", new Color(0.28f, 0.82f, 1f, 1f));

        step1Value = GUI.HorizontalSlider(new Rect(bay1.x + 16f, bay1.y + 158f, bay1.width - 32f, 24f), step1Value, 0f, 100f);
        if (GUI.Button(new Rect(bay1.x + 14f, bay1.y + 190f, bay1.width - 28f, 36f),
            p1Done ? "✓ CIRCUITO LCVG SIN BURBUJAS" : "PURGAR CIRCUITO HIDRÁULICO", buttonStyle))
        {
            step1Value = 100f;
            statusBanner = "PASO 2: Recarga de agua el sublimador de placa porosa de la mochila PLSS.";
            statusBannerColor = new Color(1f, 0.75f, 0.25f, 1f);
        }

        // PASO 2: Sublimador de Placa Porosa
        DrawStepBay(bay2, "PASO 2: SUBLIMADOR DE HIELO", p2Done, p1Done && !p2Done);
        GUI.Label(new Rect(bay2.x + 12f, bay2.y + 38f, bay2.width - 24f, 76f),
            "En la mochila PLSS del Apollo, el agua caliente pasa por una <b>placa metálica porosa</b> expuesta al vacío: se congela y se <b>sublima en vapor</b>, expulsando el calor al espacio.",
            bodyStyle);

        DrawProgressBar(new Rect(bay2.x + 14f, bay2.y + 122f, bay2.width - 28f, 24f), step2Value / 100f,
            $"Carga del Sublimador: {step2Value:F0}%", new Color(1f, 0.65f, 0.22f, 1f));

        GUI.enabled = p1Done;
        step2Value = GUI.HorizontalSlider(new Rect(bay2.x + 16f, bay2.y + 158f, bay2.width - 32f, 24f), step2Value, 0f, 100f);
        if (GUI.Button(new Rect(bay2.x + 14f, bay2.y + 190f, bay2.width - 28f, 36f),
            p2Done ? "✓ SUBLIMADOR RECARGADO" : "RECARGAR PLACA POROSA AL 100%", buttonStyle))
        {
            step2Value = 100f;
            statusBanner = "PASO 3: Completa el mantenimiento al 100% o instala capas Kapton para subir de nivel.";
            statusBannerColor = new Color(0.28f, 0.98f, 0.56f, 1f);
        }
        GUI.enabled = true;

        // PASO 3: Reparar al 100% y Mejorar Nivel Kapton (Nv.1 -> 3)
        DrawStepBay(bay3, $"PASO 3: CALIBRAR Y MEJORAR (NV.{nivelActual})", termicoActual >= 99f, p2Done);
        DrawProgressBar(new Rect(bay3.x + 14f, bay3.y + 42f, bay3.width - 28f, 24f), termicoActual / 100f,
            $"Salud Control Térmico: {termicoActual:F0}% (Nv.{nivelActual}/3)", new Color(1f, 0.58f, 0.20f, 1f));
        DrawProgressBar(new Rect(bay3.x + 14f, bay3.y + 72f, bay3.width - 28f, 20f), aActual / 100f,
            $"Agua disponible: {aActual:F0}%  |  Energía: {eActual:F0}%", new Color(0.28f, 0.82f, 1f, 1f));

        GUI.enabled = p2Done;
        if (GUI.Button(new Rect(bay3.x + 14f, bay3.y + 108f, bay3.width - 28f, 44f),
            "🔧 COMPLETAR MANTENIMIENTO (100%)\n(-Energía -Agua)", primaryActionBtnStyle))
        {
            if (flr != null)
            {
                for (int i = 0; i < 25 && (flr.EstadoControlTermico < 99.9f || flr.IntegridadTraje < 99.9f); i++)
                    flr.RealizarMantenimientoTermicoContinuo(0.25f);
            }
            statusBanner = $"✓ Control Térmico restaurado al {(flr != null ? flr.EstadoControlTermico : 100f):F0}%. Ya puedes mejorarlo de nivel si deseas.";
            statusBannerColor = new Color(0.28f, 0.98f, 0.56f, 1f);
        }

        bool puedeMejorar = flr != null && flr.EstadoControlTermico >= 95f && flr.NivelControlTermico < DONKIFLRManager.MaxNivelControlTermico;
        GUI.enabled = p2Done && puedeMejorar;
        string upgradeLabel = (flr != null && flr.NivelControlTermico >= DONKIFLRManager.MaxNivelControlTermico)
            ? "★ BLINDAJE KAPTON EN NIVEL MÁXIMO (NV.3)"
            : $"🛡️ MEJORAR BLINDAJE KAPTON A NV.{nivelActual + 1}\n(-16% ⚡  -14% 💧)";

        if (GUI.Button(new Rect(bay3.x + 14f, bay3.y + 162f, bay3.width - 28f, 48f), upgradeLabel, primaryActionBtnStyle))
        {
            if (flr != null)
            {
                if (flr.IntentarMejorarControlTermico(out string upgMsg))
                {
                    tarjetaTitulo = $"¡TRAJE MEJORADO A NIVEL TÉRMICO {flr.NivelControlTermico}!";
                    tarjetaCuerpo =
                        $"{upgMsg}\n\n" +
                        "• 1. Prenda de Enfriamiento Líquido (LCVG): Purgaste los 91 m de capilares internos.\n" +
                        "• 2. Sublimador Poroso PLSS: Renovaste las placas que evaporan hielo al vacío durante una tormenta solar (FLR).\n" +
                        "• 3. Aislamiento Multicapa Kapton (MLI): Añadiste láminas reflectantes doradas para reducir el gasto de agua y energía afuera.";
                    mostrarTarjetaEducativa = true;
                }
                else
                {
                    statusBanner = upgMsg;
                    statusBannerColor = new Color(1f, 0.45f, 0.35f, 1f);
                }
            }
        }
        GUI.enabled = true;
    }

    // =========================================================================
    // 5. PROCESO REAL ESCOTILLA EVA APOLLO LM: DESPRESURIZACIÓN Y SALIDA A MARTE
    // =========================================================================
    private void DibujarProcesoEscotillaEVA(Rect area)
    {
        float colW = (area.width - 24f) / 3f;
        Rect bay1 = new Rect(area.x, area.y, colW, area.height);
        Rect bay2 = new Rect(bay1.xMax + 12f, area.y, colW, area.height);
        Rect bay3 = new Rect(bay2.xMax + 12f, area.y, colW, area.height);

        bool p1Done = step1Toggle;
        bool p2Done = p1Done && step2Value >= 95f;

        // PASO 1: Presurizar traje a 4.3 psi
        DrawStepBay(bay1, "PASO 1: PRESURIZAR TRAJE PLSS", p1Done, !p1Done);
        GUI.Label(new Rect(bay1.x + 12f, bay1.y + 38f, bay1.width - 24f, 82f),
            "Antes de abrir la escotilla del <b>Módulo Lunar Apollo</b>, los astronautas sellan su casco y presurizan el traje a <b>4.3 psi (0.30 atm)</b> con oxígeno puro para evitar la descompresión.",
            bodyStyle);

        if (GUI.Button(new Rect(bay1.x + 14f, bay1.y + 142f, bay1.width - 28f, 44f),
            p1Done ? "✓ TRAJE SELLADO A 4.3 PSI" : "SELLAR VISOR Y PRESURIZAR TRAJE", buttonStyle))
        {
            step1Toggle = true;
            statusBanner = "PASO 2: Abre la válvula de venteo (Dump Valve) para igualar la presión con Marte.";
            statusBannerColor = new Color(1f, 0.78f, 0.25f, 1f);
        }

        // PASO 2: Válvula de Venteo (Forward Hatch Dump Valve)
        DrawStepBay(bay2, "PASO 2: VÁLVULA DE VENTEO", p2Done, p1Done && !p2Done);
        GUI.Label(new Rect(bay2.x + 12f, bay2.y + 38f, bay2.width - 24f, 82f),
            "En el Apollo LM la escotilla abre <b>hacia adentro</b>. Mientras haya presión dentro, miles de kilos de fuerza bloquean la puerta hasta abrir la <b>válvula de descarga</b>.",
            bodyStyle);

        float cabinPsi = Mathf.Lerp(4.8f, 0.09f, step2Value / 100f);
        DrawProgressBar(new Rect(bay2.x + 14f, bay2.y + 126f, bay2.width - 28f, 24f), step2Value / 100f,
            $"Presión Esclusa: {cabinPsi:F2} psi (Marte: 0.09 psi)", new Color(1f, 0.56f, 0.24f, 1f));

        GUI.enabled = p1Done;
        step2Value = GUI.HorizontalSlider(new Rect(bay2.x + 16f, bay2.y + 160f, bay2.width - 32f, 24f), step2Value, 0f, 100f);
        if (GUI.Button(new Rect(bay2.x + 14f, bay2.y + 192f, bay2.width - 28f, 38f),
            p2Done ? "✓ PRESIÓN IGUALADA CON MARTE" : "ABRIR VÁLVULA DE DESCARGA AL 100%", buttonStyle))
        {
            step2Value = 100f;
            statusBanner = "PASO 3: Destraba el volante de cierre y sal por la escalera frontal hacia Marte.";
            statusBannerColor = new Color(0.28f, 0.98f, 0.56f, 1f);
        }
        GUI.enabled = true;

        // PASO 3: Abrir escotilla y salir
        DrawStepBay(bay3, "PASO 3: ABRIR ESCOTILLA Y SALIR", false, p2Done);
        GUI.Label(new Rect(bay3.x + 12f, bay3.y + 38f, bay3.width - 24f, 76f),
            "Al igualarse la presión, el volante mecánico libera los pestillos y puedes bajar por la escalera de 9 peldaños hacia la superficie marciana.",
            bodyStyle);

        GUI.enabled = p2Done;
        if (GUI.Button(new Rect(bay3.x + 14f, bay3.y + 132f, bay3.width - 28f, 54f),
            "🚀 DESTRABAR ESCOTILLA EVA\nY SALIR A MARTE", primaryActionBtnStyle))
        {
            EjecutarSalidaAMarte();
        }
        GUI.enabled = true;

        // Botón de salida directa rápida por si hay emergencia CME afuera
        if (GUI.Button(new Rect(bay3.x + 14f, bay3.y + 204f, bay3.width - 28f, 36f),
            "⚡ Salida Rápida de Emergencia (1 clic)", buttonStyle))
        {
            EjecutarSalidaAMarte();
        }
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

        float leftW = area.width * 0.44f;
        Rect leftBox = new Rect(area.x, area.y, leftW, area.height);
        Rect rightBox = new Rect(leftBox.xMax + 14f, area.y, area.width - leftW - 14f, area.height);

        DrawStepBay(leftBox, "COMPUTADOR APOLLO AGC / DSKY (MIT)", true, false);
        Rect dskyScreen = new Rect(leftBox.x + 16f, leftBox.y + 42f, leftBox.width - 32f, 165f);
        DrawBoxWithBorder(dskyScreen, new Color(0.02f, 0.14f, 0.06f, 1f), new Color(0.22f, 0.88f, 0.42f, 1f), 2f);

        string dskyText = dskySelectedProgram switch
        {
            1 => "PROG 64  —  LANDING APPROACH\nVERB 16  NOUN 68 (MARS LANDER)\n" +
                 $"LAT: +18.38° N (JEZERO)   GRAV: 0.38 G\nMUESTRAS EVA: {MarsSampleNode2D.CollectedSamples} / {MarsSampleNode2D.TotalSamples}",
            2 => "PROG 00  —  ECLSS LIFE SUPPORT\nVERB 16  NOUN 36 (CABIN STATUS)\n" +
                 $"ENERGIA: {(flr != null ? flr.EnergiaActual : 100f):F0}%   OXIGENO: {(flr != null ? flr.OxigenoActual : 100f):F0}%\n" +
                 $"AGUA: {(flr != null ? flr.AguaActual : 100f):F0}%      TERMICO: {(flr != null ? flr.EstadoControlTermico : 100f):F0}% (NV.{(flr != null ? flr.NivelControlTermico : 1)})",
            _ => "PROG 37  —  NASA DONKI SPACE WEATHER\nVERB 06  NOUN 91 (SOLAR TELEMETRY)\n" +
                 $"CME: {(cme != null && cme.HayTormentaCMEActiva ? $"ACTIVA ({cme.VelocidadActualKmS:F0} km/s)" : "DESPEJADO")}\n" +
                 $"FLR: {(flr != null && flr.HayTormentaFLRActiva ? $"CLASE {flr.ClaseActual} ACTIVA" : $"PROB/DIA {(flr != null ? flr.ProbabilidadEventoPorDia * 100f : 35f):F0}%")}"
        };
        GUI.Label(new Rect(dskyScreen.x + 12f, dskyScreen.y + 10f, dskyScreen.width - 24f, dskyScreen.height - 20f), dskyText, monoGreenStyle);

        GUI.Label(new Rect(leftBox.x + 16f, dskyScreen.yMax + 10f, leftBox.width - 32f, 44f),
            "El <b>Apollo Guidance Computer (AGC)</b> fue el primer computador con circuitos integrados. Se operaba con códigos de <b>VERBO</b> (acción) y <b>NOMBRE</b> (dato):",
            bodyStyle);

        float btnY = dskyScreen.yMax + 60f;
        if (GUI.Button(new Rect(leftBox.x + 16f, btnY, leftBox.width - 32f, 34f), "PROG 64 — Navegación y Muestras EVA", buttonStyle))
            dskySelectedProgram = 1;
        if (GUI.Button(new Rect(leftBox.x + 16f, btnY + 40f, leftBox.width - 32f, 34f), "VERB 16 / NOUN 36 — Soporte Vital (O₂ / H₂O / ⚡)", buttonStyle))
            dskySelectedProgram = 2;
        if (GUI.Button(new Rect(leftBox.x + 16f, btnY + 80f, leftBox.width - 32f, 34f), "VERB 06 / NOUN 91 — Radar Clima Espacial DONKI", buttonStyle))
            dskySelectedProgram = 3;

        DrawStepBay(rightBox, "ARQUITECTURA INTERNA DEL MÓDULO LUNAR APOLLO (LM)", false, true);
        GUI.Label(new Rect(rightBox.x + 14f, rightBox.y + 42f, rightBox.width - 28f, rightBox.height - 54f),
            "<b>¿Cómo era por dentro el Módulo Lunar Apollo (LM-5 \"Eagle\")?</b>\n\n" +
            "• <b>1. Ventanas Triangulares Inclinadas:</b> Estaban inclinadas hacia abajo con una retícula graduada (LPD) para que el Comandante viera los cráteres durante el descenso.\n\n" +
            "• <b>2. Sillas y Puestos de Mando:</b> En la Luna (1/6 G) el LM original usaba cables y apoyabrazos para ahorrar peso, mientras que en el Módulo de Mando y aterrizadores marcianos (0.38 G) la tripulación cuenta con <b>2 asientos anatómicos con arnés de 4 puntos</b> frente al panel PGNCS.\n\n" +
            "• <b>3. Bahías Laterales Presurizadas:</b> A babor (izquierda) operan los sistemas de soporte vital (<b>MOXIE O₂</b> y <b>Filtro UPA de Agua</b>) y a estribor (derecha) el <b>Laboratorio Glovebox</b> y el <b>Banco Térmico PLSS</b>.",
            bodyStyle);
    }

    private void DibujarTarjetaEducativaModal()
    {
        float w = Mathf.Min(680f, Screen.width - 40f);
        float h = Mathf.Min(390f, Screen.height - 40f);
        Rect card = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

        DrawBoxWithBorder(card, new Color(0.08f, 0.13f, 0.20f, 0.98f), new Color(0.28f, 0.96f, 0.56f, 1f), 3f);
        GUI.Label(new Rect(card.x + 20f, card.y + 18f, card.width - 40f, 32f), tarjetaTitulo, headerStyle);
        GUI.Label(new Rect(card.x + 24f, card.y + 62f, card.width - 48f, card.height - 130f), tarjetaCuerpo, bodyStyle);

        if (GUI.Button(new Rect(card.x + (card.width - 240f) * 0.5f, card.yMax - 54f, 240f, 38f),
            "✓ ENTENDIDO — CONTINUAR", primaryActionBtnStyle))
        {
            CerrarConsola();
        }
    }

    private void DrawStepBay(Rect rect, string title, bool done, bool active)
    {
        Color border = done
            ? new Color(0.24f, 0.92f, 0.52f, 1f)
            : (active ? new Color(0.32f, 0.86f, 1f, 1f) : new Color(0.25f, 0.32f, 0.42f, 1f));
        DrawBoxWithBorder(rect, new Color(0.08f, 0.12f, 0.18f, 0.96f), border, done || active ? 2.5f : 1.5f);

        Rect head = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, 26f);
        DrawBoxWithBorder(head, new Color(0.05f, 0.08f, 0.13f, 1f), border, 1f);
        GUI.Label(new Rect(head.x + 8f, head.y + 2f, head.width - 16f, 22f), title, stepTitleStyle);
    }

    private void DrawProgressBar(Rect rect, float t01, string label, Color barColor)
    {
        t01 = Mathf.Clamp01(t01);
        DrawBoxWithBorder(rect, new Color(0.04f, 0.07f, 0.11f, 1f), new Color(0.28f, 0.36f, 0.46f, 1f), 1.5f);
        if (t01 > 0.005f)
        {
            Color prev = GUI.color;
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * t01, rect.height - 4f), solidTex);
            GUI.color = prev;
        }
        GUI.Label(rect, label, badgeStyle);
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

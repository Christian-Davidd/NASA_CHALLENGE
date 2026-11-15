using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// [PERSONA 1, 2 y 3] HUD Integrado de la Misión en Marte:
/// - Estado Actual del Clima Espacial: CME (/DONKI/CME) + Llamarada / Tormenta de Calor FLR (/DONKI/FLR)
/// - 4 Barras Vitales Realistas: Energía, Agua, Oxígeno y Control Térmico del Traje (Nv. 1..3)
/// - Contenedor EVA de 1 Muestra a la vez + Laboratorio Científico (MAHLI/SAM/CheMin + Tarjeta NASA)
/// - Pantalla de Derrota (Game Over) si Energía, Agua, Oxígeno o Control Térmico llegan a 0%
/// </summary>
public sealed class MarsMissionHUD : MonoBehaviour
{
    private static Texture2D solidTex;
    private GUIStyle panelTitleStyle;
    private GUIStyle bodyTextStyle;
    private GUIStyle smallHintStyle;
    private GUIStyle alertBannerStyle;
    private GUIStyle alertSubStyle;
    private GUIStyle sampleCounterStyle;
    private GUIStyle barLabelStyle;
    private GUIStyle heldSampleStyle;
    private GUIStyle gameOverTitleStyle;
    private GUIStyle gameOverBodyStyle;
    private GUIStyle retryBtnStyle;
    private GUIStyle discoveryTitleStyle;
    private GUIStyle discoveryTextStyle;

    private int lastDiscoveryVersion = -1;
    private const float DiscoveryDisplaySeconds = 15f;
    private double discoveryVisibleUntil;
    private Texture2D discoveryReference;
    private Vector2 discoveryScroll;
    private readonly Queue<MarsSampleNode2D> pendingAnalyses = new();
    private readonly MarsScienceLab scienceLab = new();
    private static MarsMissionHUD activeInstance;
    private static bool showExtendedContext;
    private static int lastContextToggleFrame = -1;
    private MarsSampleNode2D displayedDiscovery;
    private MarsDiscovery displayedDiscoveryData;
    private bool displayedByRover;
    private MarsSampleNode2D activeAnalysis;
    private MarsDiscovery activeAnalysisData;
    private bool activeAnalysisByRover;
    public static bool IsScienceLabOpen { get; private set; }
    public static bool ShowExtendedContext => showExtendedContext;

    public static string GetContextText(string fullText, int maxChars = 108)
    {
        if (string.IsNullOrWhiteSpace(fullText)) return string.Empty;

        string concise = fullText.Trim();
        if (!showExtendedContext)
        {
            for (int i = 0; i < concise.Length; i++)
            {
                if (concise[i] != '.' && concise[i] != '!' && concise[i] != '?') continue;
                if (i >= 20)
                {
                    concise = concise.Substring(0, i + 1);
                    break;
                }
            }
        }

        int limit = showExtendedContext ? maxChars * 2 : maxChars;
        if (concise.Length <= limit) return concise;
        return concise.Substring(0, Mathf.Max(1, limit - 1)).TrimEnd() + "…";
    }

    private Camera cachedCam;
    private Vector3 lastShakeOffset;
    private bool hasShakeOffset;

    private static bool IsGameplaySceneActive
    {
        get
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return sceneName == "Mars_Natural_Landscape_01" ||
                   sceneName == "Cultivation_Module_Interior" ||
                   sceneName == "Spaceship_Interior";
        }
    }

    private void Awake()
    {
        activeInstance = this;
    }

    private void OnEnable()
    {
        activeInstance = this;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (IsGameplaySceneActive && keyboard != null && keyboard.gKey.wasPressedThisFrame &&
            lastContextToggleFrame != Time.frameCount)
        {
            showExtendedContext = !showExtendedContext;
            lastContextToggleFrame = Time.frameCount;
        }

        ObserveDiscovery();
        if (activeAnalysisData != null ||
            CommsTowerCMEEvent.IsMinigameActive ||
            MarsSpaceshipStation2D.IsMinigameActive ||
            (DONKIFLRManager.Instance != null && DONKIFLRManager.Instance.MisionFallida))
            return;

        while (pendingAnalyses.Count > 0)
        {
            var node = pendingAnalyses.Dequeue();
            if (node == null || node.Discovery == null) continue;
            OpenAnalysis(node);
            break;
        }
    }

    private void LateUpdate()
    {
        RevertCameraShake();
        if (!IsGameplaySceneActive)
            return;

        var flr = DONKIFLRManager.Instance;
        if (flr == null || flr.MisionFallida || !flr.EnPicoRadiacion || !flr.EstaEnExteriorMarte)
            return;

        if (cachedCam == null)
            cachedCam = Camera.main;
        if (cachedCam == null)
            return;

        float amp = flr.ClaseActual == "X" ? 0.045f : (flr.ClaseActual == "M" ? 0.025f : 0.012f);
        if (flr.EscudoSobrecargadoSinControlTermico)
            amp *= 1.45f;

        Vector2 rnd = Random.insideUnitCircle * amp;
        lastShakeOffset = new Vector3(rnd.x, rnd.y, 0f);
        cachedCam.transform.localPosition += lastShakeOffset;
        hasShakeOffset = true;
    }

    private void OnDisable()
    {
        RevertCameraShake();
        if (activeInstance == this)
            activeInstance = null;
        if (activeAnalysisData != null || activeAnalysis != null)
        {
            activeAnalysis = null;
            activeAnalysisData = null;
            IsScienceLabOpen = false;
        }
    }

    private void RevertCameraShake()
    {
        if (!hasShakeOffset) return;
        if (cachedCam != null)
            cachedCam.transform.localPosition -= lastShakeOffset;
        lastShakeOffset = Vector3.zero;
        hasShakeOffset = false;
    }

    private void OnGUI()
    {
        if (!IsGameplaySceneActive)
            return;

        EnsureResources();

        var cme = DONKICMEManager.Instance;
        var flr = DONKIFLRManager.Instance;
        var tower = CommsTowerCMEEvent.Instance;

        bool hayCME = cme != null && cme.HayTormentaCMEActiva;
        bool hayInterferencia = CommsTowerCMEEvent.HaySobrecargaActivaPorCME;
        bool enModoSeguro = !CommsTowerCMEEvent.StaticTransmisorEncendido;
        bool hayFLR = flr != null && flr.HayTormentaFLRActiva;
        bool ventanaAbierta = activeAnalysisData != null || MarsMonitorScreen2D.IsOpen ||
                              MarsSpaceshipStation2D.IsMinigameActive || CommsTowerCMEEvent.IsMinigameActive;

        if (activeAnalysisData != null)
        {
            bool cerrar = scienceLab.Draw();
            DibujarBarrasVitales(flr);
            if (cerrar) CloseAnalysis();
            return;
        }

        if (!ventanaAbierta && hayFLR && flr.EstaEnExteriorMarte)
            DibujarOverlayCieloFLR(flr);

        // Las cuatro barras permanecen visibles; el resto del HUD se repliega detrás de las ventanas.
        DibujarBarrasVitales(flr);

        if (!ventanaAbierta)
        {
            DibujarPanelClimaYSupervivencia(cme, flr, tower, hayCME, hayInterferencia, hayFLR);
            DibujarPanelMuestrasYCultivos();
            DibujarAlertaCompacta(cme, flr, hayCME, hayInterferencia, enModoSeguro);
            DibujarHallazgo();

            if (flr != null && !flr.MisionFallida && Time.time < flr.MensajeEmergenciaHasta &&
                !string.IsNullOrEmpty(flr.UltimoMensajeEmergencia))
                DibujarAvisoEmergencia(flr.UltimoMensajeEmergencia);
        }

        if (flr != null && flr.MisionFallida)
            DibujarPantallaDerrota(flr);
    }

    public static void DrawPersistentVitalsOnTopOfModal()
    {
        if (activeInstance == null || !IsGameplaySceneActive) return;
        activeInstance.EnsureResources();
        activeInstance.DibujarBarrasVitales(DONKIFLRManager.Instance);
    }

    private void DibujarOverlayCieloFLR(DONKIFLRManager flr)
    {
        Color prev = GUI.color;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (flr.EnPreaviso ? 6.5f : 3.6f));

        Color baseSolarColor;
        float maxAlpha;
        switch (flr.ClaseActual)
        {
            case "X":
                baseSolarColor = new Color(1.0f, 0.36f, 0.08f, 1f);
                maxAlpha = 0.24f;
                break;
            case "M":
                baseSolarColor = new Color(1.0f, 0.58f, 0.12f, 1f);
                maxAlpha = 0.18f;
                break;
            default:
                baseSolarColor = new Color(1.0f, 0.76f, 0.18f, 1f);
                maxAlpha = 0.13f;
                break;
        }

        if (flr.EnPreaviso)
            maxAlpha *= 0.72f;
        else if (flr.EscudoSobrecargadoSinControlTermico)
            maxAlpha = Mathf.Min(0.34f, maxAlpha + 0.08f);

        float alphaFull = maxAlpha * Mathf.Lerp(0.55f, 1.0f, pulse);

        GUI.color = new Color(baseSolarColor.r, baseSolarColor.g, baseSolarColor.b, alphaFull * 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), solidTex);

        float skyBandH = Screen.height * 0.25f;
        GUI.color = new Color(1.0f, 0.72f, 0.18f, alphaFull * 0.88f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, skyBandH * 0.45f), solidTex);
        GUI.color = new Color(baseSolarColor.r, baseSolarColor.g, baseSolarColor.b, alphaFull * 0.45f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, skyBandH), solidTex);

        if (flr.EscudoSobrecargadoSinControlTermico)
        {
            GUI.color = new Color(1.0f, 0.22f, 0.10f, 0.36f * pulse);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, 10f), solidTex);
            GUI.DrawTexture(new Rect(0f, Screen.height - 10f, Screen.width, 10f), solidTex);
            GUI.DrawTexture(new Rect(0f, 0f, 10f, Screen.height), solidTex);
            GUI.DrawTexture(new Rect(Screen.width - 10f, 0f, 10f, Screen.height), solidTex);
        }

        GUI.color = prev;
    }

    private void DibujarBannerTormentaOProteccion(
        DONKICMEManager cme,
        bool hayCME,
        bool hayInterferencia,
        bool enModoSeguro,
        float topY)
    {
        Color prev = GUI.color;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4.2f);
        var plasma = PlasmaCloudOverlayController.Instance;

        float bannerW = Mathf.Min(680f, Screen.width - 720f);
        if (bannerW < 420f) bannerW = 460f;
        Rect bannerRect = new Rect((Screen.width - bannerW) * 0.5f, topY, bannerW, 64f);

        GUI.color = new Color(0.10f, 0.06f, 0.16f, 0.94f);
        GUI.DrawTexture(bannerRect, solidTex);

        Color barColor;
        string linea1;
        string linea2;
        float vel = cme != null && cme.VelocidadActualKmS > 0f ? cme.VelocidadActualKmS : 780f;
        float rest = cme != null ? cme.TiempoRestanteEvento : 0f;
        int cobPct = plasma != null ? Mathf.RoundToInt(plasma.CoberturaPorcentaje) : 0;
        int cargaPct = Mathf.RoundToInt(CommsTowerCMEEvent.StaticSobrecargaProgreso * 100f);

        if (hayInterferencia)
        {
            barColor = Color.Lerp(new Color(1f, 0.25f, 0.55f, 1f), new Color(0.3f, 0.9f, 1f, 1f), pulse);
            alertBannerStyle.normal.textColor = new Color(1f, 0.55f, 0.85f, 1f);
            linea1 = $"⚡ ALERTA CME ({vel:F0} km/s | Sobrecarga {cargaPct}% | Nube {cobPct}%) — ¡DRENANDO ENERGÍA!";
            linea2 = $"La antena activa daña la red y bloquea el Lab. Ve a la Torre HGA y apágala [E] ({rest:F0}s)";
        }
        else if (hayCME && enModoSeguro)
        {
            barColor = new Color(1f, 0.78f, 0.25f, 1f);
            alertBannerStyle.normal.textColor = new Color(1f, 0.86f, 0.40f, 1f);
            linea1 = $"🛡️ TORMENTA CME ({vel:F0} km/s) — ANTENA PROTEGIDA EN MODO SEGURO";
            linea2 = $"El transmisor está a salvo y sin cortocircuitos. Espera {rest:F0}s para reencender";
        }
        else
        {
            barColor = new Color(0.28f, 0.95f, 0.58f, 1f);
            alertBannerStyle.normal.textColor = new Color(0.45f, 1f, 0.72f, 1f);
            linea1 = "☀️ CIELO DESPEJADO — TRANSMISOR LISTO PARA REENCENDER";
            linea2 = "Ve a la Antena HGA y presiona [E] para encenderlo paso a paso";
        }

        GUI.color = barColor;
        GUI.DrawTexture(new Rect(bannerRect.x, bannerRect.y, bannerRect.width, 4f), solidTex);
        GUI.color = prev;

        GUI.Label(new Rect(bannerRect.x + 12f, bannerRect.y + 8f, bannerRect.width - 24f, 24f), linea1, alertBannerStyle);
        GUI.Label(new Rect(bannerRect.x + 12f, bannerRect.y + 34f, bannerRect.width - 24f, 22f), linea2, alertSubStyle);
    }

    private void DibujarBannerTormentaFLR(DONKIFLRManager flr, float topY)
    {
        Color prev = GUI.color;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5.2f);

        float bannerW = Mathf.Min(680f, Screen.width - 720f);
        if (bannerW < 420f) bannerW = 460f;
        Rect bannerRect = new Rect((Screen.width - bannerW) * 0.5f, topY, bannerW, 64f);

        GUI.color = new Color(0.16f, 0.09f, 0.03f, 0.95f);
        GUI.DrawTexture(bannerRect, solidTex);

        Color barColor;
        string linea1;
        string linea2;

        if (flr.EnPreaviso)
        {
            barColor = Color.Lerp(new Color(1.0f, 0.86f, 0.22f, 1f), new Color(1.0f, 0.52f, 0.12f, 1f), pulse);
            alertBannerStyle.normal.textColor = new Color(1.0f, 0.90f, 0.38f, 1f);
            linea1 = $"☀️ ¡PREAVISO LLAMARADA / CALOR (CLASE {flr.ClaseActual})! — PICO EN {flr.TiempoRestantePreaviso:F1}s";
            linea2 = "Refúgiate en el Módulo Agrícola o Nave [E]. Afuera consume Agua/Energía y desgasta Control Térmico";
        }
        else if (flr.EstaEnRefugioDomo)
        {
            barColor = new Color(0.32f, 0.95f, 0.55f, 1f);
            alertBannerStyle.normal.textColor = new Color(0.55f, 1.0f, 0.72f, 1f);
            string refugioNombre = flr.EstaEnNaveInterior ? "NAVE ESPACIAL" : "MÓDULO AGRÍCOLA";
            linea1 = $"🛡️ REFUGIO EN {refugioNombre} — CALOR Y RADIACIÓN BLOQUEADOS ({flr.TiempoRestanteEvento:F0}s)";
            linea2 = flr.EstaEnInvernaderoInterior
                ? "Protegido de la llamarada (el invernadero no recarga energía ni repara el traje)"
                : "Protegido de la llamarada (puedes mantener Control Térmico, O₂ o Agua aquí)";
        }
        else if (flr.EscudoSobrecargadoSinControlTermico)
        {
            barColor = Color.Lerp(new Color(1.0f, 0.18f, 0.12f, 1f), new Color(1.0f, 0.65f, 0.15f, 1f), pulse);
            alertBannerStyle.normal.textColor = new Color(1.0f, 0.48f, 0.35f, 1f);
            linea1 = $"🚨 ¡CONTROL TÉRMICO AGOTADO (0%)! — SOBRECALENTAMIENTO ({flr.IntegridadTraje:F0}% | {flr.TiempoRestanteEvento:F0}s)";
            linea2 = "¡Entra YA al Módulo Agrícola o Nave [E] antes que falle el traje!";
        }
        else
        {
            barColor = Color.Lerp(new Color(1.0f, 0.62f, 0.14f, 1f), new Color(1.0f, 0.84f, 0.28f, 1f), pulse);
            alertBannerStyle.normal.textColor = new Color(1.0f, 0.78f, 0.32f, 1f);
            linea1 = $"🌡️ {flr.NivelAlertaTexto} — SUBLIMADOR CONSUMIENDO AGUA ({flr.TiempoRestanteEvento:F0}s)";
            linea2 = $"Térmico Nv.{flr.NivelControlTermico}: {flr.EstadoControlTermico:F0}% | Refúgiate en Módulo Agrícola / Nave [E]";
        }

        GUI.color = barColor;
        GUI.DrawTexture(new Rect(bannerRect.x, bannerRect.y, bannerRect.width, 4f), solidTex);
        GUI.color = prev;

        GUI.Label(new Rect(bannerRect.x + 12f, bannerRect.y + 8f, bannerRect.width - 24f, 24f), linea1, alertBannerStyle);
        GUI.Label(new Rect(bannerRect.x + 12f, bannerRect.y + 34f, bannerRect.width - 24f, 22f), linea2, alertSubStyle);
    }

    private void DibujarPanelClimaYSupervivencia(
        DONKICMEManager cme,
        DONKIFLRManager flr,
        CommsTowerCMEEvent tower,
        bool hayCME,
        bool hayInterferencia,
        bool hayFLR)
    {
        float vitalWidth = Mathf.Min(258f, Mathf.Max(180f, Screen.width * 0.23f));
        float progressWidth = Mathf.Min(278f, Mathf.Max(196f, Screen.width * 0.23f));
        float x = 16f + vitalWidth + 12f;
        float rightX = Screen.width - progressWidth - 16f;
        float width = Mathf.Max(120f, rightX - x - 12f);
        bool expanded = ShowExtendedContext;
        float height = expanded ? 108f : 64f;
        Rect rect = new Rect(x, 16f, width, height);
        Color prev = GUI.color;

        Color accent = hayInterferencia ? new Color(1f, 0.34f, 0.45f, 1f)
            : hayFLR || hayCME ? new Color(1f, 0.66f, 0.23f, 1f)
            : new Color(0.39f, 0.83f, 0.91f, 1f);
        GUI.color = new Color(0.035f, 0.047f, 0.075f, 0.94f);
        GUI.DrawTexture(rect, solidTex);
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), solidTex);
        GUI.color = prev;

        string clima = hayCME && hayFLR ? "Tormenta solar doble"
            : hayInterferencia ? "Interferencia CME"
            : hayFLR ? (flr != null && flr.EnPreaviso ? "Alerta solar próxima" : "Llamarada solar")
            : hayCME ? "Tormenta CME"
            : "Cielo despejado";
        string antena = hayInterferencia ? "Antena: apagar [E]"
            : !CommsTowerCMEEvent.StaticTransmisorEncendido ? "Antena: protegida"
            : "Antena: en línea";
        GUI.Label(new Rect(rect.x + 11f, rect.y + 5f, rect.width - 22f, 20f), "CLIMA ESPACIAL", panelTitleStyle);
        GUI.Label(new Rect(rect.x + 11f, rect.y + 27f, rect.width - 22f, 22f), clima + "  ·  " + antena, bodyTextStyle);
        if (expanded)
        {
            string cmeInfo = cme != null && cme.HayTormentaCMEActiva
                ? $"CME: {cme.VelocidadActualKmS:F0} km/s · {cme.TiempoRestanteEvento:F0}s"
                : $"CME: próxima en {(cme != null ? cme.TiempoParaSiguienteCME : 0f):F0}s";
            string flrInfo = flr == null ? "FLR: cargando" : flr.HayTormentaFLRActiva
                ? $"FLR {flr.ClaseActual}: {(flr.EnPreaviso ? flr.TiempoRestantePreaviso : flr.TiempoRestanteEvento):F0}s"
                : $"FLR: {flr.TotalLlamaradasSemana}/sem · próxima en {flr.TiempoParaSiguienteFLR:F0}s";
            GUI.Label(new Rect(rect.x + 11f, rect.y + 50f, rect.width - 22f, 20f), cmeInfo, smallHintStyle);
            GUI.Label(new Rect(rect.x + 11f, rect.y + 72f, rect.width - 22f, 20f), flrInfo, smallHintStyle);
        }
        else
        {
            GUI.Label(new Rect(rect.xMax - 94f, rect.y + 5f, 84f, 18f), "[G] DETALLE", smallHintStyle);
        }

        GUI.color = prev;
    }

    private void DibujarBarrasVitales(DONKIFLRManager flr)
    {
        float scale = Mathf.Clamp(Screen.height / 900f, 0.72f, 1f);
        float width = Mathf.Min(258f, Mathf.Max(180f, Screen.width * 0.23f));
        float rowHeight = 31f * scale;
        float height = 12f + rowHeight * 4f + 18f;
        var panel = new Rect(16f, 16f, width, height);
        Color previous = GUI.color;
        GUI.color = new Color(0.035f, 0.047f, 0.075f, 0.96f);
        GUI.DrawTexture(panel, solidTex);
        GUI.color = new Color(1f, 0.58f, 0.28f, 1f);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3f), solidTex);
        GUI.color = previous;

        float[] values =
        {
            flr != null ? flr.EnergiaActual : 100f,
            flr != null ? flr.AguaActual : 100f,
            flr != null ? flr.OxigenoActual : 100f,
            flr != null ? flr.EstadoControlTermico : 100f
        };
        string[] labels = { "ENERGÍA", "AGUA", "OXÍGENO", "TÉRMICO" };
        Color[] colors =
        {
            new Color(1f, 0.72f, 0.27f, 1f),
            new Color(0.32f, 0.72f, 1f, 1f),
            new Color(0.29f, 0.88f, 0.69f, 1f),
            new Color(1f, 0.43f, 0.36f, 1f)
        };
        for (int i = 0; i < values.Length; i++)
        {
            float y = panel.y + 8f + i * rowHeight;
            Rect row = new Rect(panel.x + 7f, y, panel.width - 14f, rowHeight - 2f);
            GUI.color = new Color(0.07f, 0.09f, 0.13f, 0.92f);
            GUI.DrawTexture(row, solidTex);
            GUI.color = colors[i];
            GUI.DrawTexture(new Rect(row.x, row.y, 3f, row.height), solidTex);
            GUI.color = previous;

            string label = i == 3 && flr != null ? $"{labels[i]} · NV.{flr.NivelControlTermico}" : labels[i];
            barLabelStyle.normal.textColor = new Color(0.96f, 0.91f, 0.82f, 1f);
            GUI.Label(new Rect(row.x + 9f, row.y + 1f, row.width - 58f, 15f * scale), label, barLabelStyle);
            Color previousCounterColor = sampleCounterStyle.normal.textColor;
            sampleCounterStyle.normal.textColor = colors[i];
            sampleCounterStyle.fontSize = Mathf.RoundToInt(12f * scale);
            GUI.Label(new Rect(row.xMax - 48f, row.y + 1f, 44f, 15f * scale), $"{values[i]:F0}%", sampleCounterStyle);
            sampleCounterStyle.fontSize = 14;
            sampleCounterStyle.normal.textColor = previousCounterColor;

            Rect track = new Rect(row.x + 9f, row.y + row.height - 8f, row.width - 18f, 4f);
            GUI.color = new Color(0.16f, 0.19f, 0.24f, 1f);
            GUI.DrawTexture(track, solidTex);
            GUI.color = colors[i];
            GUI.DrawTexture(new Rect(track.x, track.y, track.width * Mathf.Clamp01(values[i] / 100f), track.height), solidTex);
            GUI.color = previous;
        }

        GUI.Label(new Rect(panel.x + 8f, panel.yMax - 18f, panel.width - 16f, 16f), "TRAJE EVA  ·  [G] MÁS CONTEXTO", smallHintStyle);
    }

    private void DibujarAlertaCompacta(
        DONKICMEManager cme,
        DONKIFLRManager flr,
        bool hayCME,
        bool hayInterferencia,
        bool enModoSeguro)
    {
        bool hayFLR = flr != null && flr.HayTormentaFLRActiva;
        if (!hayCME && !hayFLR) return;

        string mensaje;
        Color accent;
        if (hayInterferencia)
        {
            mensaje = "CME: apaga la antena HGA [E]";
            accent = new Color(1f, 0.36f, 0.50f, 1f);
        }
        else if (hayFLR && flr.EscudoSobrecargadoSinControlTermico)
        {
            mensaje = "¡Peligro térmico! Refúgiate dentro";
            accent = new Color(1f, 0.39f, 0.30f, 1f);
        }
        else if (hayFLR)
        {
            mensaje = flr.EstaEnRefugioDomo ? "Llamarada activa · estás a salvo" : "Llamarada activa · busca refugio";
            accent = new Color(1f, 0.68f, 0.25f, 1f);
        }
        else if (hayCME && enModoSeguro)
        {
            mensaje = $"CME activa · antena protegida ({(cme != null ? cme.TiempoRestanteEvento : 0f):F0}s)";
            accent = new Color(1f, 0.74f, 0.29f, 1f);
        }
        else
        {
            return;
        }

        float width = Mathf.Min(560f, Screen.width - 32f);
        float vitalScale = Mathf.Clamp(Screen.height / 900f, 0.72f, 1f);
        float vitalHeight = 12f + 31f * vitalScale * 4f + 18f;
        float topY = 16f + Mathf.Max(vitalHeight, 126f) + 10f;
        Rect rect = new Rect((Screen.width - width) * 0.5f, topY, width, 40f);
        Color previous = GUI.color;
        GUI.color = new Color(0.075f, 0.045f, 0.075f, 0.96f);
        GUI.DrawTexture(rect, solidTex);
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x, rect.y, 4f, rect.height), solidTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), solidTex);
        GUI.color = previous;
        alertBannerStyle.alignment = TextAnchor.MiddleLeft;
        alertBannerStyle.fontSize = 13;
        alertBannerStyle.normal.textColor = accent;
        GUI.Label(new Rect(rect.x + 13f, rect.y + 4f, rect.width - 26f, rect.height - 8f), "⚠  " + mensaje, alertBannerStyle);
    }

    private void DrawSurvivalBar(Rect area, string label, float ratio, Color fillColor)
    {
        Color prev = GUI.color;
        GUI.Label(new Rect(area.x, area.y, area.width, 18f), label, barLabelStyle);

        Rect bg = new Rect(area.x, area.y + 19f, area.width, 12f);
        GUI.color = new Color(0.14f, 0.18f, 0.25f, 1f);
        GUI.DrawTexture(bg, solidTex);

        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(bg.x, bg.y, bg.width * Mathf.Clamp01(ratio), bg.height), solidTex);
        GUI.color = prev;
    }

    private void DibujarPanelMuestrasYCultivos()
    {
        float panelW = Mathf.Min(278f, Mathf.Max(196f, Screen.width * 0.23f));
        const float panelH = 126f;
        Rect rect = new Rect(Screen.width - panelW - 16f, 16f, panelW, panelH);

        Color prev = GUI.color;
        GUI.color = new Color(0.05f, 0.08f, 0.13f, 0.92f);
        GUI.DrawTexture(rect, solidTex);

        GUI.color = new Color(1.0f, 0.66f, 0.29f, 1f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), solidTex);

        int collected = MarsSampleNode2D.CollectedSamples;
        int total = Mathf.Max(1, MarsSampleNode2D.TotalSamples);

        GUI.color = prev;
        GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f), "MISIÓN", panelTitleStyle);
        GUI.Label(
            new Rect(rect.x + 12f, rect.y + 32f, rect.width - 24f, 24f),
            $"Muestras en Nave: {collected} / {total}",
            sampleCounterStyle
        );

        Rect barBg = new Rect(rect.x + 12f, rect.y + 58f, rect.width - 24f, 12f);
        GUI.color = new Color(0.16f, 0.20f, 0.26f, 1f);
        GUI.DrawTexture(barBg, solidTex);

        float ratio = Mathf.Clamp01((float)collected / total);
        GUI.color = new Color(1.0f, 0.66f, 0.29f, 1f);
        GUI.DrawTexture(new Rect(barBg.x, barBg.y, barBg.width * ratio, barBg.height), solidTex);
        GUI.color = prev;

        string manoTexto = MarsSampleNode2D.HasHeldSample ? "Muestra: llena · vuelve a la nave" : "Muestra: contenedor libre";
        GUI.Label(new Rect(rect.x + 12f, rect.y + 76f, rect.width - 24f, 20f), manoTexto, heldSampleStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 98f, rect.width - 24f, 18f),
            $"Cosechas: {MarsCropPlot2D.TotalHarvestedCrops}   ·   [G] más", smallHintStyle);
        if (ShowExtendedContext)
        {
            GUI.Label(new Rect(rect.x + 12f, rect.y + 111f, rect.width - 24f, 14f),
                MarsSampleNode2D.HasHeldSample ? MarsSampleNode2D.HeldSampleLabel + " · WASD mover · E interactuar" : "WASD mover · E interactuar · T/F eventos", smallHintStyle);
        }
    }

    internal static bool OpenExternalAnalysis(MarsDiscovery discovery, bool byRover = false)
    {
        if (activeInstance == null || discovery == null) return false;
        if (discovery.isEquipment)
            activeInstance.ShowDiscovery(discovery, byRover, null);
        else
            activeInstance.OpenAnalysis(discovery, byRover, null);
        return true;
    }

    private void ObserveDiscovery()
    {
        if (MarsSampleNode2D.DiscoveryVersion < lastDiscoveryVersion)
        {
            lastDiscoveryVersion = MarsSampleNode2D.DiscoveryVersion;
            pendingAnalyses.Clear();
            activeAnalysis = null;
            activeAnalysisData = null;
            IsScienceLabOpen = false;
            displayedDiscovery = null;
            displayedDiscoveryData = null;
            discoveryVisibleUntil = 0;
        }

        var node = MarsSampleNode2D.LatestDiscovery;
        if (node == null || node.Discovery == null ||
            lastDiscoveryVersion == MarsSampleNode2D.DiscoveryVersion) return;

        lastDiscoveryVersion = MarsSampleNode2D.DiscoveryVersion;
        if (node.Discovery.isEquipment)
            ShowDiscovery(node);
        else
            pendingAnalyses.Enqueue(node);
    }

    private void ShowDiscovery(MarsSampleNode2D node)
    {
        if (node == null || node.Discovery == null) return;
        ShowDiscovery(node.Discovery, node.DeliveredByRover, node);
    }

    private void ShowDiscovery(MarsDiscovery discovery, bool byRover, MarsSampleNode2D node)
    {
        if (discovery == null) return;
        displayedDiscovery = node;
        displayedDiscoveryData = discovery;
        displayedByRover = byRover;
        discoveryVisibleUntil = Time.unscaledTimeAsDouble + DiscoveryDisplaySeconds;
        discoveryScroll = Vector2.zero;
        discoveryReference = discovery.LoadReference();
    }

    private void OpenAnalysis(MarsSampleNode2D node)
    {
        if (node == null || node.Discovery == null) return;
        OpenAnalysis(node.Discovery, node.DeliveredByRover, node);
    }

    private void OpenAnalysis(MarsDiscovery discovery, bool byRover, MarsSampleNode2D node)
    {
        activeAnalysis = node;
        activeAnalysisData = discovery;
        activeAnalysisByRover = byRover;
        scienceLab.Open(discovery);
        IsScienceLabOpen = true;
    }

    private void CloseAnalysis()
    {
        var node = activeAnalysis;
        var discovery = activeAnalysisData;
        bool byRover = activeAnalysisByRover;
        activeAnalysis = null;
        activeAnalysisData = null;
        IsScienceLabOpen = false;
        ShowDiscovery(discovery, byRover, node);
    }

    private void DibujarHallazgo()
    {
        var discovery = displayedDiscoveryData != null
            ? displayedDiscoveryData
            : (displayedDiscovery != null ? displayedDiscovery.Discovery : null);
        if (discovery == null || Time.unscaledTimeAsDouble >= discoveryVisibleUntil ||
            CommsTowerCMEEvent.IsMinigameActive ||
            MarsSpaceshipStation2D.IsMinigameActive ||
            (DONKIFLRManager.Instance != null && DONKIFLRManager.Instance.MisionFallida)) return;

        bool byRover = displayedDiscoveryData != null ? displayedByRover : displayedDiscovery.DeliveredByRover;
        float width = Mathf.Min(560f, Screen.width - 32f);
        float targetHeight = ShowExtendedContext ? 585f : 390f;
        float height = Mathf.Min(targetHeight, Screen.height - 32f);
        if (width < 180f || height < 80f) return;
        float left = (Screen.width - width) * 0.5f;
        float top = Screen.height - height - 16f;
        Rect panel = new Rect(left, top, width, height);
        Color previous = GUI.color;
        GUI.color = new Color(0.05f, 0.08f, 0.13f, 0.97f);
        GUI.DrawTexture(panel, solidTex);
        GUI.color = new Color(1f, 0.62f, 0.32f);
        GUI.DrawTexture(new Rect(left, top, width, 3f), solidTex);
        GUI.color = previous;

        GUI.Label(new Rect(left + 12f, top + 7f, width - 58f, 22f), "HALLAZGO CIENTÍFICO", panelTitleStyle);
        if (GUI.Button(new Rect(panel.xMax - 36f, top + 6f, 26f, 25f), "×"))
        {
            discoveryVisibleUntil = 0;
            return;
        }

        discoveryTitleStyle ??= new GUIStyle(sampleCounterStyle) { fontSize = 16, wordWrap = true };
        discoveryTextStyle ??= new GUIStyle(bodyTextStyle) { fontSize = 13, wordWrap = true, alignment = TextAnchor.UpperCenter };
        float contentWidth = width - 42f;
        float titleHeight = discoveryTitleStyle.CalcHeight(new GUIContent(discovery.title), contentWidth);
        string detail = discovery.isEquipment ? "Instrumento científico" : $"Instrumento: {discovery.instrument}";
        detail += byRover ? " · Rover" : " · Astronauta";
        float detailHeight = discoveryTextStyle.CalcHeight(new GUIContent(detail), contentWidth);
        string description = GetContextText(discovery.description, 160);
        float descriptionHeight = discoveryTextStyle.CalcHeight(new GUIContent(description), contentWidth);
        float imagesY = titleHeight + detailHeight + 12f;
        float imageHeight = Mathf.Min(ShowExtendedContext ? 225f : 155f, contentWidth * 0.56f);
        float bodyY = imagesY + (discoveryReference != null ? imageHeight + 14f : 0f);
        float scrollHeight = bodyY + descriptionHeight + 62f;
        Rect scrollArea = new Rect(left + 12f, top + 36f, width - 24f, height - 46f);
        discoveryScroll = GUI.BeginScrollView(scrollArea, discoveryScroll, new Rect(0, 0, contentWidth, scrollHeight));

        GUI.Label(new Rect(0, 0, contentWidth, titleHeight), discovery.title, discoveryTitleStyle);
        GUI.Label(new Rect(0, titleHeight + 2f, contentWidth, detailHeight), detail, discoveryTextStyle);

        if (discoveryReference != null)
            GUI.DrawTexture(new Rect(0, imagesY, contentWidth, imageHeight), discoveryReference, ScaleMode.ScaleToFit);

        GUI.Label(new Rect(0, bodyY, contentWidth, descriptionHeight), description, discoveryTextStyle);
        float footerY = bodyY + descriptionHeight + 5f;
        GUI.Label(new Rect(0, footerY, contentWidth, 20f), GetContextText(discovery.credit, 90), discoveryTextStyle);
        const float sourceWidth = 128f;
        GUI.Label(new Rect(0f, footerY + 20f, contentWidth, 20f), "[G] Más contexto", smallHintStyle);
        if (GUI.Button(new Rect((contentWidth - sourceWidth) * 0.5f, footerY + 42f, sourceWidth, 24f), "Fuente NASA"))
            Application.OpenURL(discovery.source);

        GUI.EndScrollView();
    }

    private void DibujarAvisoEmergencia(string texto)
    {
        Color prev = GUI.color;
        float w = Mathf.Min(780f, Screen.width - 40f);
        Rect rect = new Rect((Screen.width - w) * 0.5f, Screen.height - 134f, w, 44f);

        GUI.color = new Color(0.18f, 0.08f, 0.04f, 0.94f);
        GUI.DrawTexture(rect, solidTex);
        GUI.color = new Color(1.0f, 0.72f, 0.18f, 1f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), solidTex);
        GUI.color = prev;

        GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 28f), GetContextText(texto, 112), alertSubStyle);
    }

    private void DibujarPantallaDerrota(DONKIFLRManager flr)
    {
        Color prev = GUI.color;
        GUI.color = new Color(0.04f, 0.02f, 0.03f, 0.82f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), solidTex);

        float boxW = Mathf.Min(580f, Screen.width - 40f);
        float boxH = 260f;
        Rect box = new Rect((Screen.width - boxW) * 0.5f, (Screen.height - boxH) * 0.5f, boxW, boxH);

        GUI.color = new Color(0.11f, 0.05f, 0.07f, 0.97f);
        GUI.DrawTexture(box, solidTex);
        GUI.color = new Color(1.0f, 0.25f, 0.22f, 1f);
        GUI.DrawTexture(new Rect(box.x, box.y, box.width, 5f), solidTex);
        GUI.color = prev;

        GUI.Label(new Rect(box.x + 24f, box.y + 22f, box.width - 48f, 36f), "MISIÓN FALLIDA", gameOverTitleStyle);
        GUI.Label(new Rect(box.x + 24f, box.y + 66f, box.width - 48f, 28f), flr.MotivoDerrota, alertBannerStyle);
        GUI.Label(new Rect(box.x + 28f, box.y + 102f, box.width - 56f, 82f), GetContextText(flr.DetalleDerrota, 125), gameOverBodyStyle);
        GUI.Label(new Rect(box.x + 28f, box.y + 184f, box.width - 56f, 20f), "[G] Más contexto", smallHintStyle);

        Rect btnRect = new Rect(box.x + (box.width - 230f) * 0.5f, box.y + boxH - 64f, 230f, 44f);
        if (GUI.Button(btnRect, "Reintentar Misión", retryBtnStyle))
        {
            flr.ReiniciarPartidaCompleta();
        }
    }

    private void EnsureResources()
    {
        if (solidTex == null)
        {
            solidTex = new Texture2D(1, 1);
            solidTex.SetPixel(0, 0, Color.white);
            solidTex.Apply();
        }

        if (panelTitleStyle == null)
        {
            panelTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.70f, 0.38f, 1f) }
            };
        }

        if (bodyTextStyle == null)
        {
            bodyTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.96f, 0.92f, 0.85f, 0.96f) }
            };
        }

        if (smallHintStyle == null)
        {
            smallHintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.76f, 0.80f, 0.84f, 0.94f) }
            };
        }

        if (barLabelStyle == null)
        {
            barLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.91f, 0.75f, 1f) }
            };
        }

        if (heldSampleStyle == null)
        {
            heldSampleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.63f, 0.85f, 1f, 1f) }
            };
        }

        if (sampleCounterStyle == null)
        {
            sampleCounterStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.75f, 0.32f, 1f) }
            };
        }

        if (alertBannerStyle == null)
        {
            alertBannerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.55f, 0.85f, 1f) }
            };
        }

        if (alertSubStyle == null)
        {
            alertSubStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (gameOverTitleStyle == null)
        {
            gameOverTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.36f, 0.32f, 1f) }
            };
        }

        if (gameOverBodyStyle == null)
        {
            gameOverBodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.94f, 0.95f, 0.98f, 1f) }
            };
        }

        if (retryBtnStyle == null)
        {
            retryBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
        }
    }
}

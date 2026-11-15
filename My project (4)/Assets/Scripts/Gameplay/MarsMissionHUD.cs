using System.Collections.Generic;
using UnityEngine;

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
    private MarsSampleNode2D displayedDiscovery;
    private MarsDiscovery displayedDiscoveryData;
    private bool displayedByRover;
    private MarsSampleNode2D activeAnalysis;
    private MarsDiscovery activeAnalysisData;
    private bool activeAnalysisByRover;
    public static bool IsScienceLabOpen { get; private set; }

    private Camera cachedCam;
    private Vector3 lastShakeOffset;
    private bool hasShakeOffset;

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
        if (activeAnalysisData != null)
        {
            if (scienceLab.Draw())
                CloseAnalysis();
            return;
        }

        EnsureResources();

        var cme = DONKICMEManager.Instance;
        var flr = DONKIFLRManager.Instance;
        var tower = CommsTowerCMEEvent.Instance;

        bool hayCME = cme != null && cme.HayTormentaCMEActiva;
        bool hayInterferencia = CommsTowerCMEEvent.HaySobrecargaActivaPorCME;
        bool enModoSeguro = !CommsTowerCMEEvent.StaticTransmisorEncendido;
        bool hayFLR = flr != null && flr.HayTormentaFLRActiva;

        // 0. Overlay atmosférico dorado/naranja en el cielo cuando hay Llamarada / Tormenta de Calor en exterior
        if (hayFLR && flr.EstaEnExteriorMarte)
        {
            DibujarOverlayCieloFLR(flr);
        }

        // 1. Banner superior de CME / Modo Seguro
        float nextBannerY = 14f;
        if (hayCME || enModoSeguro)
        {
            DibujarBannerTormentaOProteccion(cme, hayCME, hayInterferencia, enModoSeguro, nextBannerY);
            nextBannerY += 70f;
        }

        // 2. Banner dorado/naranja de Llamarada Solar / Tormenta de Calor (FLR)
        if (hayFLR)
        {
            DibujarBannerTormentaFLR(flr, nextBannerY);
        }

        // 3. Panel Izquierdo: Clima + 4 Barras de Supervivencia
        DibujarPanelClimaYSupervivencia(cme, flr, tower, hayCME, hayInterferencia, hayFLR);

        // 4. Panel Derecho: Muestras (1/1 en mano) y Cultivos
        DibujarPanelMuestrasYCultivos();

        // 5. Tarjeta flotante de Hallazgo Científico NASA (del equipo)
        DibujarHallazgo();

        // 6. Aviso flotante de Energía / Emergencia / Térmico
        if (flr != null && !flr.MisionFallida && Time.time < flr.MensajeEmergenciaHasta && !string.IsNullOrEmpty(flr.UltimoMensajeEmergencia))
        {
            DibujarAvisoEmergencia(flr.UltimoMensajeEmergencia);
        }

        // 7. Pantalla de Derrota (Game Over)
        if (flr != null && flr.MisionFallida)
        {
            DibujarPantallaDerrota(flr);
        }
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
        Rect rect = new Rect(16f, 16f, 348f, 316f);
        Color prev = GUI.color;

        GUI.color = new Color(0.05f, 0.08f, 0.13f, 0.92f);
        GUI.DrawTexture(rect, solidTex);

        Color accent;
        if (hayInterferencia)
            accent = new Color(1f, 0.28f, 0.58f, 1f);
        else if (hayFLR)
            accent = new Color(1.0f, 0.68f, 0.18f, 1f);
        else if (hayCME)
            accent = new Color(1f, 0.78f, 0.25f, 1f);
        else
            accent = new Color(0.22f, 0.85f, 0.98f, 1f);

        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), solidTex);
        GUI.color = prev;

        string estadoClima;
        if (hayCME && hayFLR)
            estadoClima = $"DOBLE TORMENTA (CME + FLR {flr.ClaseActual})";
        else if (hayFLR)
            estadoClima = flr.EnPreaviso
                ? $"PREAVISO CALOR/FLR (Clase {flr.ClaseActual})"
                : $"LLAMARADA / CALOR FLR (Clase {flr.ClaseActual})";
        else if (hayCME)
            estadoClima = "TORMENTA CME (Nube de Plasma)";
        else
            estadoClima = "DESPEJADO (Viento Solar Estable)";

        string estadoTorre;
        if (hayInterferencia)
            estadoTorre = $"¡SOBRECARGA {Mathf.RoundToInt(CommsTowerCMEEvent.StaticSobrecargaProgreso * 100f)}%! (Apagar [E])";
        else if (!CommsTowerCMEEvent.StaticTransmisorEncendido)
            estadoTorre = CommsTowerCMEEvent.NecesitaReencendidoGlobal ? "MODO SEGURO (Reencender [E])" : "MODO SEGURO (Protegida)";
        else
            estadoTorre = "EN LÍNEA (Señal Estable)";

        string proxCMETexto = (cme != null && cme.HayTormentaCMEActiva)
            ? $"CME activa: {cme.TiempoRestanteEvento:F0}s | Vel: {cme.VelocidadActualKmS:F0} km/s"
            : $"Próxima CME: {(cme != null ? cme.TiempoParaSiguienteCME : 0f):F0}s ([T] probar)";

        string flrTexto;
        if (flr == null)
        {
            flrTexto = "FLR: Inicializando datos NASA...";
        }
        else if (flr.HayTormentaFLRActiva)
        {
            flrTexto = flr.EnPreaviso
                ? $"FLR {flr.ClaseActual}: Impacto en {flr.TiempoRestantePreaviso:F1}s"
                : $"FLR {flr.ClaseActual}: {flr.TiempoRestanteEvento:F0}s | {(flr.EstaEnRefugioDomo ? "EN REFUGIO" : "EXTERIOR")}";
        }
        else
        {
            flrTexto = $"FLR ({flr.TotalLlamaradasSemana}/sem | Prob {(flr.ProbabilidadEventoPorDia * 100f):F0}%): {flr.TiempoParaSiguienteFLR:F0}s ([F])";
        }

        GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 20f), "CLIMA ESPACIAL Y SUPERVIVENCIA", panelTitleStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 30f, rect.width - 24f, 20f), $"Clima: {estadoClima}", bodyTextStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 50f, rect.width - 24f, 20f), $"Antena HGA: {estadoTorre}", bodyTextStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 70f, rect.width - 24f, 20f), proxCMETexto, bodyTextStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 90f, rect.width - 24f, 20f), flrTexto, bodyTextStyle);

        GUI.color = new Color(0.22f, 0.32f, 0.44f, 0.75f);
        GUI.DrawTexture(new Rect(rect.x + 12f, rect.y + 114f, rect.width - 24f, 1f), solidTex);
        GUI.color = prev;

        float energia = flr != null ? flr.EnergiaActual : 100f;
        float agua = flr != null ? flr.AguaActual : 100f;
        float oxigeno = flr != null ? flr.OxigenoActual : 100f;
        float termico = flr != null ? flr.EstadoControlTermico : 100f;
        int nivelTerm = flr != null ? flr.NivelControlTermico : 1;

        // 1. ENERGÍA (baja siempre; mantén [E] en Paneles Solares)
        string hintE = hayInterferencia
            ? "¡CME drenando! (Paneles [E])"
            : (energia < 30f ? "¡CRÍTICA! Mantén [E] en Paneles" : "Baja siempre | Paneles [E]");
        DrawSurvivalBar(
            new Rect(rect.x + 12f, rect.y + 120f, rect.width - 24f, 34f),
            $"⚡ Energía: {energia:F0}% — {hintE}",
            energia / 100f,
            energia > 45f ? new Color(0.20f, 0.86f, 1.0f, 1f) : (energia > 22f ? new Color(1.0f, 0.76f, 0.18f, 1f) : new Color(1.0f, 0.24f, 0.20f, 1f))
        );

        // 2. AGUA (baja en uso/calor/cultivos; Filtro UPA Nave o Hielo ISRU)
        string hintW = agua < 30f ? "¡BAJA! Filtro Orina Nave / Hielo ISRU" : "Filtro Nave [E] / Hielo ISRU [E]";
        DrawSurvivalBar(
            new Rect(rect.x + 12f, rect.y + 158f, rect.width - 24f, 34f),
            $"💧 Agua: {agua:F0}% — {hintW}",
            agua / 100f,
            agua > 45f ? new Color(0.26f, 0.68f, 1.0f, 1f) : (agua > 22f ? new Color(1.0f, 0.72f, 0.20f, 1f) : new Color(1.0f, 0.24f, 0.20f, 1f))
        );

        // 3. OXÍGENO (baja en exterior; MOXIE en Nave)
        string hintO = (flr != null && !flr.EstaEnExteriorMarte)
            ? "Pausado en interior presurizado"
            : (oxigeno < 30f ? "¡BAJO! Recarga MOXIE en Nave [E]" : "MOXIE en Nave [E]");
        DrawSurvivalBar(
            new Rect(rect.x + 12f, rect.y + 196f, rect.width - 24f, 34f),
            $"🫧 Oxígeno: {oxigeno:F0}% — {hintO}",
            oxigeno / 100f,
            oxigeno > 45f ? new Color(0.36f, 0.96f, 0.76f, 1f) : (oxigeno > 22f ? new Color(1.0f, 0.72f, 0.20f, 1f) : new Color(1.0f, 0.24f, 0.20f, 1f))
        );

        // 4. CONTROL TÉRMICO DEL TRAJE (mantenimiento y mejora en la Nave)
        string hintT = termico < 65f
            ? "¡Requiere mantenimiento en Nave!"
            : (nivelTerm < DONKIFLRManager.MaxNivelControlTermico ? "Mejorable en Nave [E]" : "Nivel Máximo");
        DrawSurvivalBar(
            new Rect(rect.x + 12f, rect.y + 234f, rect.width - 24f, 34f),
            $"🌡️ Térmico (Nv.{nivelTerm}): {termico:F0}% — {hintT}",
            termico / 100f,
            termico > 55f ? new Color(1.0f, 0.68f, 0.22f, 1f) : new Color(1.0f, 0.30f, 0.18f, 1f)
        );

        string fuente = flr != null ? flr.FuenteDatosActual : "Inicializando...";
        GUI.Label(new Rect(rect.x + 12f, rect.y + 276f, rect.width - 24f, 18f), $"NASA: {fuente}", smallHintStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 293f, rect.width - 24f, 18f), "0% en Energía, Agua o Oxígeno = Misión fallida", smallHintStyle);
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
        const float panelW = 336f;
        const float panelH = 152f;
        Rect rect = new Rect(Screen.width - panelW - 16f, 16f, panelW, panelH);

        Color prev = GUI.color;
        GUI.color = new Color(0.05f, 0.08f, 0.13f, 0.92f);
        GUI.DrawTexture(rect, solidTex);

        GUI.color = new Color(1.0f, 0.78f, 0.22f, 1f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), solidTex);

        int collected = MarsSampleNode2D.CollectedSamples;
        int total = Mathf.Max(1, MarsSampleNode2D.TotalSamples);

        GUI.color = prev;
        GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f), "PROGRESO DE MISIÓN EN MARTE", panelTitleStyle);
        GUI.Label(
            new Rect(rect.x + 12f, rect.y + 32f, rect.width - 24f, 24f),
            $"Muestras en Nave: {collected} / {total}",
            sampleCounterStyle
        );

        Rect barBg = new Rect(rect.x + 12f, rect.y + 58f, rect.width - 24f, 12f);
        GUI.color = new Color(0.16f, 0.20f, 0.26f, 1f);
        GUI.DrawTexture(barBg, solidTex);

        float ratio = Mathf.Clamp01((float)collected / total);
        GUI.color = new Color(1.0f, 0.78f, 0.22f, 1f);
        GUI.DrawTexture(new Rect(barBg.x, barBg.y, barBg.width * ratio, barBg.height), solidTex);
        GUI.color = prev;

        string manoTexto = MarsSampleNode2D.HasHeldSample
            ? $"📦 En mano (1/1): {MarsSampleNode2D.HeldSampleLabel} → ¡Ve a la Nave!"
            : "📦 Contenedor EVA libre (0/1) — Recoge 1 muestra afuera";

        GUI.Label(new Rect(rect.x + 12f, rect.y + 76f, rect.width - 24f, 22f), manoTexto, heldSampleStyle);

        string cropInfo = $"🌱 Cultivos regados/cosechados: {MarsCropPlot2D.TotalHarvestedCrops}";
        GUI.Label(new Rect(rect.x + 12f, rect.y + 100f, rect.width - 24f, 20f), cropInfo, bodyTextStyle);
        GUI.Label(
            new Rect(rect.x + 12f, rect.y + 122f, rect.width - 24f, 20f),
            "WASD mover | [E] Usar/Mantener | [T] CME | [F] FLR",
            smallHintStyle
        );
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
        float height = Mathf.Min(585f, Screen.height - 32f);
        if (width < 180f || height < 80f) return;
        float left = (Screen.width - width) * 0.5f;
        float top = Screen.height - height - 16f;
        Rect panel = new Rect(left, top, width, height);
        Color previous = GUI.color;
        GUI.color = new Color(0.05f, 0.08f, 0.13f, 0.97f);
        GUI.DrawTexture(panel, solidTex);
        GUI.color = new Color(0.39f, 0.9f, 0.66f);
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
        string detail = discovery.isEquipment ? "Instrumento científico" : $"Análisis con {discovery.instrument}";
        detail += byRover ? " · Entregado por el rover" : " · Recogido por el astronauta";
        float detailHeight = discoveryTextStyle.CalcHeight(new GUIContent(detail), contentWidth);
        string description = discovery.description;
        float descriptionHeight = discoveryTextStyle.CalcHeight(new GUIContent(description), contentWidth);
        float imagesY = titleHeight + detailHeight + 12f;
        float imageHeight = Mathf.Min(225f, contentWidth * 0.65f);
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
        GUI.Label(new Rect(0, footerY, contentWidth, 20f), discovery.credit, discoveryTextStyle);
        const float sourceWidth = 128f;
        if (GUI.Button(new Rect((contentWidth - sourceWidth) * 0.5f, footerY + 24f, sourceWidth, 24f), "Ver fuente NASA"))
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

        GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 28f), texto, alertSubStyle);
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
        GUI.Label(new Rect(box.x + 28f, box.y + 102f, box.width - 56f, 82f), flr.DetalleDerrota, gameOverBodyStyle);

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
                normal = { textColor = new Color(0.45f, 0.92f, 1f, 1f) }
            };
        }

        if (bodyTextStyle == null)
        {
            bodyTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.92f, 0.95f, 1f, 0.96f) }
            };
        }

        if (smallHintStyle == null)
        {
            smallHintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.78f, 0.86f, 0.96f, 0.92f) }
            };
        }

        if (barLabelStyle == null)
        {
            barLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.96f, 0.86f, 1f) }
            };
        }

        if (heldSampleStyle == null)
        {
            heldSampleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 0.96f, 1f, 1f) }
            };
        }

        if (sampleCounterStyle == null)
        {
            sampleCounterStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.86f, 0.35f, 1f) }
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

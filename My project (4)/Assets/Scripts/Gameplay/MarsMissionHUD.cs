using UnityEngine;

/// <summary>
/// [PERSONA 1, 2 y 3] HUD Integrado de la Misión en Marte:
/// - Estado Actual del Clima Espacial (Persona 3 / Persona 1)
/// - Aviso visual en el cielo y HUD de Interferencia Electromagnética CME (Persona 1)
/// - Contador de Muestras Geológicas recolectadas y estado de Cultivos (Persona 2)
/// </summary>
public sealed class MarsMissionHUD : MonoBehaviour
{
    private static Texture2D solidTex;
    private GUIStyle panelTitleStyle;
    private GUIStyle bodyTextStyle;
    private GUIStyle alertBannerStyle;
    private GUIStyle alertSubStyle;
    private GUIStyle sampleCounterStyle;

    private void OnGUI()
    {
        EnsureResources();

        var cme = DONKICMEManager.Instance;
        var tower = CommsTowerCMEEvent.Instance;
        bool hayCME = cme != null && cme.HayTormentaCMEActiva;
        bool hayInterferencia = hayCME && (tower == null || tower.TieneInterferenciaCME);
        bool enModoSeguro = tower != null && !tower.TransmisorEncendido;

        // 1. [PERSONA 1] Banner superior de tormenta / Modo Seguro / Reencendido
        if (hayCME || enModoSeguro)
        {
            DibujarBannerTormentaOProteccion(cme, tower, hayCME, hayInterferencia, enModoSeguro);
        }

        // 2. [PERSONA 3 & 1] Panel de Estado Actual del Clima Espacial (Arriba Izquierda)
        DibujarPanelClimaEspacial(cme, tower, hayCME, hayInterferencia);

        // 3. [PERSONA 2] Panel de Contador de Muestras y Cultivos (Arriba Derecha)
        DibujarPanelMuestrasYCultivos();
    }

    private void DibujarBannerTormentaOProteccion(
        DONKICMEManager cme,
        CommsTowerCMEEvent tower,
        bool hayCME,
        bool hayInterferencia,
        bool enModoSeguro)
    {
        Color prev = GUI.color;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4.2f);
        var plasma = PlasmaCloudOverlayController.Instance;

        float bannerW = Mathf.Min(700f, Screen.width - 660f);
        if (bannerW < 440f) bannerW = 480f;
        Rect bannerRect = new Rect((Screen.width - bannerW) * 0.5f, 14f, bannerW, 66f);

        GUI.color = new Color(0.10f, 0.06f, 0.16f, 0.94f);
        GUI.DrawTexture(bannerRect, solidTex);

        Color barColor;
        string linea1;
        string linea2;
        float vel = cme != null && cme.VelocidadActualKmS > 0f ? cme.VelocidadActualKmS : 780f;
        float rest = cme != null ? cme.TiempoRestanteEvento : 0f;
        int cobPct = plasma != null ? Mathf.RoundToInt(plasma.CoberturaPorcentaje) : 0;

        if (hayInterferencia)
        {
            barColor = Color.Lerp(new Color(1f, 0.25f, 0.55f, 1f), new Color(0.3f, 0.9f, 1f, 1f), pulse);
            alertBannerStyle.normal.textColor = new Color(1f, 0.55f, 0.85f, 1f);
            linea1 = $"⚡ ALERTA CME ({vel:F0} km/s | Cobertura {cobPct}%) — ¡RIESGO ELÉCTRICO EN ANTENA!";
            linea2 = $"Ve a la Antena HGA y presiona [E] para Apagado Seguro ({rest:F0}s)";
        }
        else if (hayCME && enModoSeguro)
        {
            barColor = new Color(1f, 0.78f, 0.25f, 1f);
            alertBannerStyle.normal.textColor = new Color(1f, 0.86f, 0.40f, 1f);
            linea1 = $"🛡️ TORMENTA CME ({vel:F0} km/s) — ANTENA PROTEGIDA EN MODO SEGURO";
            linea2 = $"El transmisor está a salvo. Espera a que pase la nube ({rest:F0}s) para reencender";
        }
        else
        {
            barColor = new Color(0.28f, 0.95f, 0.58f, 1f);
            alertBannerStyle.normal.textColor = new Color(0.45f, 1f, 0.72f, 1f);
            linea1 = "☀️ CIELO DESPEJADO — TRANSMISOR LISTO PARA REENCENDER";
            linea2 = "Ve a la Antena HGA y presiona [E] para encenderlo paso a paso (sin límite de tiempo)";
        }

        GUI.color = barColor;
        GUI.DrawTexture(new Rect(bannerRect.x, bannerRect.y, bannerRect.width, 4f), solidTex);
        GUI.color = prev;

        GUI.Label(new Rect(bannerRect.x + 12f, bannerRect.y + 8f, bannerRect.width - 24f, 24f), linea1, alertBannerStyle);
        GUI.Label(new Rect(bannerRect.x + 12f, bannerRect.y + 34f, bannerRect.width - 24f, 22f), linea2, alertSubStyle);
    }

    private void DibujarPanelClimaEspacial(DONKICMEManager cme, CommsTowerCMEEvent tower, bool hayCME, bool hayInterferencia)
    {
        Rect rect = new Rect(16f, 16f, 330f, 134f);
        Color prev = GUI.color;

        GUI.color = new Color(0.05f, 0.08f, 0.13f, 0.90f);
        GUI.DrawTexture(rect, solidTex);

        Color accent = hayInterferencia
            ? new Color(1f, 0.28f, 0.58f, 1f)
            : (hayCME ? new Color(1f, 0.78f, 0.25f, 1f) : new Color(0.22f, 0.85f, 0.98f, 1f));
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), solidTex);
        GUI.color = prev;

        string estadoClima = hayCME
            ? "TORMENTA CME (Nube de Plasma)"
            : "DESPEJADO (Viento Solar Estable)";

        string estadoTorre;
        if (tower != null && tower.TieneInterferenciaCME)
            estadoTorre = "¡RIESGO EM! (Apagar con [E])";
        else if (tower != null && !tower.TransmisorEncendido)
            estadoTorre = tower.NecesitaReencendido ? "MODO SEGURO (Reencender con [E])" : "MODO SEGURO (Protegida)";
        else
            estadoTorre = "EN LÍNEA (Señal Estable)";

        string fuente = cme != null ? cme.FuenteDatosActual : "Inicializando...";
        string proxTexto = (cme != null && cme.HayTormentaCMEActiva)
            ? $"CME activa: {cme.TiempoRestanteEvento:F0}s | Vel: {cme.VelocidadActualKmS:F0} km/s"
            : $"Próxima CME en: {(cme != null ? cme.TiempoParaSiguienteCME : 0f):F0}s (Presiona [T] para probar)";

        GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f), "ESTADO ACTUAL DEL CLIMA ESPACIAL", panelTitleStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 32f, rect.width - 24f, 22f), $"Clima: {estadoClima}", bodyTextStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 54f, rect.width - 24f, 22f), $"Antena HGA: {estadoTorre}", bodyTextStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 76f, rect.width - 24f, 22f), proxTexto, bodyTextStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 98f, rect.width - 24f, 28f), $"NASA: {fuente}", bodyTextStyle);
    }

    private void DibujarPanelMuestrasYCultivos()
    {
        const float panelW = 310f;
        const float panelH = 122f;
        Rect rect = new Rect(Screen.width - panelW - 16f, 16f, panelW, panelH);

        Color prev = GUI.color;
        GUI.color = new Color(0.05f, 0.08f, 0.13f, 0.90f);
        GUI.DrawTexture(rect, solidTex);

        GUI.color = new Color(1.0f, 0.78f, 0.22f, 1f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), solidTex);

        int collected = MarsSampleNode2D.CollectedSamples;
        int total = Mathf.Max(1, MarsSampleNode2D.TotalSamples);

        GUI.color = prev;
        GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f), "PROGRESO DE MISIÓN EN MARTE", panelTitleStyle);
        GUI.Label(
            new Rect(rect.x + 12f, rect.y + 32f, rect.width - 24f, 24f),
            $"Muestras Recolectadas: {collected} / {MarsSampleNode2D.TotalSamples}",
            sampleCounterStyle
        );

        // Barra de progreso de muestras
        Rect barBg = new Rect(rect.x + 12f, rect.y + 60f, rect.width - 24f, 12f);
        GUI.color = new Color(0.16f, 0.20f, 0.26f, 1f);
        GUI.DrawTexture(barBg, solidTex);

        float ratio = Mathf.Clamp01((float)collected / total);
        GUI.color = new Color(1.0f, 0.78f, 0.22f, 1f);
        GUI.DrawTexture(new Rect(barBg.x, barBg.y, barBg.width * ratio, barBg.height), solidTex);
        GUI.color = prev;

        string cropInfo = MarsCropPlot2D.Instance != null
            ? $"Cultivos cosechados: {MarsCropPlot2D.Instance.HarvestedCropsCount}"
            : "Zona de Cultivos lista";

        GUI.Label(new Rect(rect.x + 12f, rect.y + 80f, rect.width - 24f, 20f), cropInfo, bodyTextStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 98f, rect.width - 24f, 20f), "Controles: WASD mover | [E] Interactuar | [T] CME", bodyTextStyle);
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
    }
}

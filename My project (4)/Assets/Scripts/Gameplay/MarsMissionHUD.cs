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

        // 1. [PERSONA 1] Efecto visual en el cielo + interferencia electromagnética en pantalla
        if (hayInterferencia)
        {
            DibujarEfectoCieloEInterferencia(cme);
        }

        // 2. [PERSONA 3 & 1] Panel de Estado Actual del Clima Espacial (Arriba Izquierda)
        DibujarPanelClimaEspacial(cme, tower, hayInterferencia);

        // 3. [PERSONA 2] Panel de Contador de Muestras y Cultivos (Arriba Derecha)
        DibujarPanelMuestrasYCultivos();
    }

    private void DibujarEfectoCieloEInterferencia(DONKICMEManager cme)
    {
        Color prev = GUI.color;

        // Tinte atmosférico de Nube de Plasma en el cielo / pantalla
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4.2f);
        GUI.color = new Color(0.58f, 0.14f, 0.78f, Mathf.Lerp(0.12f, 0.24f, pulse));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), solidTex);

        // Resplandor superior del cielo marciano ionizado
        GUI.color = new Color(0.85f, 0.20f, 0.65f, Mathf.Lerp(0.18f, 0.34f, pulse));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height * 0.22f), solidTex);

        // Líneas horizontales de interferencia electromagnética (scanlines dinámicas)
        GUI.color = new Color(0.95f, 0.45f, 1.0f, 0.14f);
        float offset = (Time.time * 90f) % 28f;
        for (float y = offset; y < Screen.height; y += 28f)
        {
            GUI.DrawTexture(new Rect(0, y, Screen.width, 2f), solidTex);
        }

        // Banner central superior de Alerta de Interferencia Electromagnética
        float bannerW = Mathf.Min(680f, Screen.width - 660f);
        if (bannerW < 420f) bannerW = 460f;
        Rect bannerRect = new Rect((Screen.width - bannerW) * 0.5f, 14f, bannerW, 64f);

        GUI.color = new Color(0.14f, 0.04f, 0.18f, 0.94f);
        GUI.DrawTexture(bannerRect, solidTex);

        GUI.color = Color.Lerp(new Color(1f, 0.25f, 0.55f, 1f), new Color(0.3f, 0.9f, 1f, 1f), pulse);
        GUI.DrawTexture(new Rect(bannerRect.x, bannerRect.y, bannerRect.width, 4f), solidTex);
        GUI.color = prev;

        float vel = cme != null ? cme.VelocidadActualKmS : 780f;
        float rest = cme != null ? cme.TiempoRestanteEvento : 0f;

        GUI.Label(
            new Rect(bannerRect.x + 12f, bannerRect.y + 8f, bannerRect.width - 24f, 24f),
            $"⚡ ALERTA CME: NUBE DE PLASMA ({vel:F0} km/s) — INTERFERENCIA ELECTROMAGNÉTICA",
            alertBannerStyle
        );
        GUI.Label(
            new Rect(bannerRect.x + 12f, bannerRect.y + 34f, bannerRect.width - 24f, 22f),
            $"Ve a [TORRE DE COMUNICACIONES] y presiona [E] para calibrar los círculos ({rest:F0}s restantes)",
            alertSubStyle
        );
    }

    private void DibujarPanelClimaEspacial(DONKICMEManager cme, CommsTowerCMEEvent tower, bool hayInterferencia)
    {
        Rect rect = new Rect(16f, 16f, 330f, 134f);
        Color prev = GUI.color;

        GUI.color = new Color(0.05f, 0.08f, 0.13f, 0.90f);
        GUI.DrawTexture(rect, solidTex);

        Color accent = hayInterferencia
            ? new Color(1f, 0.28f, 0.58f, 1f)
            : new Color(0.22f, 0.85f, 0.98f, 1f);
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), solidTex);
        GUI.color = prev;

        string estadoClima = hayInterferencia
            ? "TORMENTA CME (Nube de Plasma)"
            : "DESPEJADO (Viento Solar Estable)";

        string estadoTorre = (tower != null && tower.TieneInterferenciaCME)
            ? "¡INTERFERENCIA EM! (Calibrar con E)"
            : "EN LÍNEA (Señal Estable)";

        string fuente = cme != null ? cme.FuenteDatosActual : "Inicializando...";
        string proxTexto = (cme != null && cme.HayTormentaCMEActiva)
            ? $"CME activa: {cme.TiempoRestanteEvento:F0}s | Vel: {cme.VelocidadActualKmS:F0} km/s"
            : $"Próxima CME en: {(cme != null ? cme.TiempoParaSiguienteCME : 0f):F0}s (Presiona [T] para probar)";

        GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f), "ESTADO ACTUAL DEL CLIMA ESPACIAL", panelTitleStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 32f, rect.width - 24f, 22f), $"Clima: {estadoClima}", bodyTextStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 54f, rect.width - 24f, 22f), $"Torre Comms: {estadoTorre}", bodyTextStyle);
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

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PERSONA 1] Torre de Comunicaciones + Minijuego de Calibración/Reconexión CME.
/// Cuando llega una Nube de Plasma (CME), la torre sufre interferencia electromagnética.
/// Al acercarse y presionar [E], se abre el minijuego de aplastar con el mouse
/// diferentes círculos de señal que asoman en la pantalla.
/// </summary>
[RequireComponent(typeof(MarsWorldPlaceholder2D))]
public sealed class CommsTowerCMEEvent : MonoBehaviour
{
    public static CommsTowerCMEEvent Instance { get; private set; }
    public static bool IsMinigameActive => Instance != null && Instance.minijuegoAbierto;

    private struct SignalCircle
    {
        public Vector2 localPos;
        public float radius;
        public float spawnTime;
        public float lifeDuration;
        public Color color;
    }

    [Header("Configuración del Minijuego de Círculos")]
    [SerializeField] private int totalCirculosRequeridos = 6;
    [SerializeField] private int circulosSimultaneos = 3;
    [SerializeField] private float radioCirculoPx = 30f;
    [SerializeField] private float vidaCirculoSegundos = 2.8f;

    public bool TieneInterferenciaCME { get; private set; }
    public bool TorreCalibrada { get; private set; } = true;
    public int ReparacionesCompletadas { get; private set; }

    private MarsWorldPlaceholder2D placeholder;
    private bool minijuegoAbierto;
    private int circulosAplastados;
    private readonly List<SignalCircle> circulosActivos = new List<SignalCircle>();

    private bool mostrarTarjetaEducativa;
    private string tarjetaTitulo = "";
    private string tarjetaCuerpo = "";

    private static Texture2D circleTex;
    private static Texture2D solidTex;
    private GUIStyle headerStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;
    private GUIStyle circleLabelStyle;

    private static readonly Color ColorNormal = new Color(0.18f, 0.78f, 0.96f, 0.92f);
    private static readonly Color ColorAlerta = new Color(0.98f, 0.24f, 0.48f, 0.95f);
    private static readonly Color ColorCalibrada = new Color(0.22f, 0.92f, 0.52f, 0.95f);

    private void Awake()
    {
        Instance = this;
        placeholder = GetComponent<MarsWorldPlaceholder2D>();
        EnsureTextures();
    }

    private void Start()
    {
        if (DONKICMEManager.Instance != null)
        {
            DONKICMEManager.Instance.OnCMEIniciada += AlIniciarCME;
            DONKICMEManager.Instance.OnCMEFinalizada += AlFinalizarCME;
        }
        ActualizarVisualCuadrado();
    }

    private void OnDestroy()
    {
        if (DONKICMEManager.Instance != null)
        {
            DONKICMEManager.Instance.OnCMEIniciada -= AlIniciarCME;
            DONKICMEManager.Instance.OnCMEFinalizada -= AlFinalizarCME;
        }
    }

    private void Update()
    {
        if (TieneInterferenciaCME && placeholder != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 7f);
            Color c = Color.Lerp(ColorAlerta, new Color(0.75f, 0.2f, 1f, 0.95f), pulse);
            placeholder.SetVisualState(c, "[TORRE DE COMUNICACIONES]", "¡INTERFERENCIA EM! Presiona [E] para calibrar");
        }

        if (minijuegoAbierto)
        {
            ActualizarCirculosMinijuego();
        }
    }

    private void AlIniciarCME(DONKICMEManager.CMEEventData ev, float speedKmS)
    {
        TieneInterferenciaCME = true;
        TorreCalibrada = false;
        mostrarTarjetaEducativa = false;
        ActualizarVisualCuadrado();
    }

    private void AlFinalizarCME()
    {
        TieneInterferenciaCME = false;
        TorreCalibrada = true;
        minijuegoAbierto = false;
        circulosActivos.Clear();
        ActualizarVisualCuadrado();
    }

    public string InteractFromPlayer()
    {
        AbrirMinijuegoCalibracion();
        return TieneInterferenciaCME
            ? "Iniciando calibración de emergencia de la Torre de Comunicaciones..."
            : "Abriendo panel de calibración preventiva de la Torre de Comunicaciones...";
    }

    public void AbrirMinijuegoCalibracion()
    {
        minijuegoAbierto = true;
        mostrarTarjetaEducativa = false;
        circulosAplastados = 0;
        circulosActivos.Clear();

        for (int i = 0; i < circulosSimultaneos; i++)
        {
            GenerarNuevoCirculo();
        }
    }

    private void ActualizarCirculosMinijuego()
    {
        for (int i = circulosActivos.Count - 1; i >= 0; i--)
        {
            if (Time.time - circulosActivos[i].spawnTime >= circulosActivos[i].lifeDuration)
            {
                circulosActivos.RemoveAt(i);
            }
        }

        int faltantes = totalCirculosRequeridos - circulosAplastados;
        int objetivoEnPantalla = Mathf.Min(circulosSimultaneos, faltantes);
        while (circulosActivos.Count < objetivoEnPantalla)
        {
            GenerarNuevoCirculo();
        }
    }

    private void GenerarNuevoCirculo()
    {
        // Área interna del radar de calibración: 540 x 230 px
        const float areaW = 540f;
        const float areaH = 220f;
        float margin = radioCirculoPx + 14f;

        Vector2 candidate = Vector2.zero;
        for (int attempt = 0; attempt < 15; attempt++)
        {
            candidate = new Vector2(
                Random.Range(margin, areaW - margin),
                Random.Range(margin, areaH - margin)
            );

            bool overlaps = false;
            for (int i = 0; i < circulosActivos.Count; i++)
            {
                if (Vector2.Distance(candidate, circulosActivos[i].localPos) < radioCirculoPx * 2.2f)
                {
                    overlaps = true;
                    break;
                }
            }
            if (!overlaps) break;
        }

        Color[] palette =
        {
            new Color(0.2f, 0.95f, 1f, 1f),
            new Color(1f, 0.45f, 0.85f, 1f),
            new Color(0.35f, 1f, 0.55f, 1f),
            new Color(1f, 0.82f, 0.25f, 1f)
        };

        circulosActivos.Add(new SignalCircle
        {
            localPos = candidate,
            radius = radioCirculoPx,
            spawnTime = Time.time,
            lifeDuration = vidaCirculoSegundos + Random.Range(-0.35f, 0.45f),
            color = palette[Random.Range(0, palette.Length)]
        });
    }

    private void CompletarCalibracion()
    {
        minijuegoAbierto = false;
        circulosActivos.Clear();
        TieneInterferenciaCME = false;
        TorreCalibrada = true;
        ReparacionesCompletadas++;

        PrepararTarjetaEducativa();
        mostrarTarjetaEducativa = true;

        if (DONKICMEManager.Instance != null && DONKICMEManager.Instance.HayTormentaCMEActiva)
        {
            DONKICMEManager.Instance.FinalizarNubeDePlasma();
        }

        ActualizarVisualCuadrado();
    }

    private void PrepararTarjetaEducativa()
    {
        var manager = DONKICMEManager.Instance;
        var ev = manager != null ? manager.EventoActivo : null;
        float vel = manager != null && manager.VelocidadActualKmS > 0f ? manager.VelocidadActualKmS : 780f;
        string id = ev != null && !string.IsNullOrEmpty(ev.activityID) ? ev.activityID : "2026-11-12T08:12:00-CME-001";
        string origen = ev != null && !string.IsNullOrEmpty(ev.sourceLocation) ? ev.sourceLocation : "Región Solar Activa";

        tarjetaTitulo = "¡TORRE CALIBRADA Y COMUNICACIÓN RESTABLECIDA!";
        tarjetaCuerpo =
            $"¡Excelente trabajo, cadete! Aplastaste los nodos de interferencia y sincronizaste la antena.\n\n" +
            $"DATOS REALES DE LA NASA (/DONKI/CME):\n" +
            $"• Evento registrado: {id}\n" +
            $"• Velocidad de la nube de plasma: {vel:F0} km/s\n" +
            $"• Región solar de origen: {origen}\n\n" +
            $"¿Sabías qué? Una Eyección de Masa Coronal (CME) es una gigantesca nube de plasma " +
            $"y campo magnético lanzada por el Sol. Cuando llega a Marte o la Tierra, provoca " +
            $"interferencia electromagnética en las antenas y satélites, por lo que los astronautas " +
            $"recalibran las frecuencias para no perder contacto con la base.";
    }

    private void ActualizarVisualCuadrado()
    {
        if (placeholder == null) return;

        if (TieneInterferenciaCME)
        {
            placeholder.SetVisualState(
                ColorAlerta,
                "[TORRE DE COMUNICACIONES]",
                "¡INTERFERENCIA EM! Presiona [E] para calibrar"
            );
        }
        else if (ReparacionesCompletadas > 0)
        {
            placeholder.SetVisualState(
                ColorCalibrada,
                "[TORRE DE COMUNICACIONES]",
                $"En línea (Calibrada x{ReparacionesCompletadas}) — [E] Probar"
            );
        }
        else
        {
            placeholder.SetVisualState(
                ColorNormal,
                "[TORRE DE COMUNICACIONES]",
                "Señal en línea — [E] Calibrar antena"
            );
        }
    }

    private void OnGUI()
    {
        if (!minijuegoAbierto && !mostrarTarjetaEducativa) return;

        EnsureTextures();
        EnsureStyles();

        if (minijuegoAbierto)
        {
            DibujarVentanaMinijuegoCirculos();
        }
        else if (mostrarTarjetaEducativa)
        {
            DibujarTarjetaEducativa();
        }
    }

    private void DibujarVentanaMinijuegoCirculos()
    {
        const float winW = 580f;
        const float winH = 390f;
        Rect winRect = new Rect((Screen.width - winW) * 0.5f, (Screen.height - winH) * 0.5f, winW, winH);

        Color prev = GUI.color;
        // Fondo oscuro del panel
        GUI.color = new Color(0.05f, 0.08f, 0.14f, 0.96f);
        GUI.DrawTexture(winRect, solidTex);

        // Borde superior brillante
        GUI.color = new Color(0.25f, 0.88f, 1f, 1f);
        GUI.DrawTexture(new Rect(winRect.x, winRect.y, winRect.width, 4f), solidTex);
        GUI.color = prev;

        GUI.Label(
            new Rect(winRect.x + 16f, winRect.y + 12f, winRect.width - 32f, 28f),
            "CALIBRACIÓN DE TORRE — ¡APLASTA LOS CÍRCULOS CON EL MOUSE!",
            headerStyle
        );

        GUI.Label(
            new Rect(winRect.x + 16f, winRect.y + 40f, winRect.width - 32f, 24f),
            $"Haz clic sobre los nodos circulares de señal para eliminar la interferencia CME: {circulosAplastados} / {totalCirculosRequeridos}",
            bodyStyle
        );

        // Barra de progreso
        Rect barBg = new Rect(winRect.x + 20f, winRect.y + 68f, winRect.width - 40f, 14f);
        GUI.color = new Color(0.15f, 0.2f, 0.28f, 1f);
        GUI.DrawTexture(barBg, solidTex);
        float pct = Mathf.Clamp01((float)circulosAplastados / totalCirculosRequeridos);
        GUI.color = new Color(0.2f, 0.95f, 0.55f, 1f);
        GUI.DrawTexture(new Rect(barBg.x, barBg.y, barBg.width * pct, barBg.height), solidTex);
        GUI.color = prev;

        // Área del radar donde asoman los círculos
        Rect radarRect = new Rect(winRect.x + 20f, winRect.y + 92f, 540f, 230f);
        GUI.color = new Color(0.02f, 0.05f, 0.09f, 0.98f);
        GUI.DrawTexture(radarRect, solidTex);

        // Rejilla decorativa del osciloscopio
        GUI.color = new Color(0.15f, 0.45f, 0.55f, 0.22f);
        for (int gx = 1; gx < 6; gx++)
        {
            float lx = radarRect.x + (radarRect.width / 6f) * gx;
            GUI.DrawTexture(new Rect(lx, radarRect.y, 1f, radarRect.height), solidTex);
        }
        for (int gy = 1; gy < 4; gy++)
        {
            float ly = radarRect.y + (radarRect.height / 4f) * gy;
            GUI.DrawTexture(new Rect(radarRect.x, ly, radarRect.width, 1f), solidTex);
        }
        GUI.color = prev;

        // Dibujar y detectar clic en cada círculo
        Event ev = Event.current;
        for (int i = circulosActivos.Count - 1; i >= 0; i--)
        {
            SignalCircle sc = circulosActivos[i];
            float age = Time.time - sc.spawnTime;
            float lifeRatio = Mathf.Clamp01(1f - age / sc.lifeDuration);
            float pulseScale = 0.9f + 0.15f * Mathf.Sin(Time.time * 10f + i * 1.7f);
            float drawRadius = sc.radius * pulseScale;

            Vector2 screenCenter = new Vector2(radarRect.x + sc.localPos.x, radarRect.y + sc.localPos.y);
            Rect circleRect = new Rect(
                screenCenter.x - drawRadius,
                screenCenter.y - drawRadius,
                drawRadius * 2f,
                drawRadius * 2f
            );

            // Halo exterior
            GUI.color = new Color(sc.color.r, sc.color.g, sc.color.b, 0.35f * lifeRatio);
            GUI.DrawTexture(new Rect(
                screenCenter.x - drawRadius * 1.25f,
                screenCenter.y - drawRadius * 1.25f,
                drawRadius * 2.5f,
                drawRadius * 2.5f
            ), circleTex);

            // Círculo principal
            GUI.color = new Color(sc.color.r, sc.color.g, sc.color.b, 0.95f);
            GUI.DrawTexture(circleRect, circleTex);
            GUI.color = prev;

            GUI.Label(circleRect, "●", circleLabelStyle);

            // Detectar clic con el mouse dentro del radio del círculo
            if (ev.type == EventType.MouseDown && ev.button == 0)
            {
                if (Vector2.Distance(ev.mousePosition, screenCenter) <= drawRadius * 1.2f)
                {
                    circulosActivos.RemoveAt(i);
                    circulosAplastados++;
                    ev.Use();

                    if (circulosAplastados >= totalCirculosRequeridos)
                    {
                        CompletarCalibracion();
                        return;
                    }
                    else
                    {
                        GenerarNuevoCirculo();
                    }
                    break;
                }
            }
        }

        // Botón para cerrar si el jugador desea salir
        if (GUI.Button(new Rect(winRect.x + winRect.width - 130f, winRect.y + winRect.height - 48f, 110f, 34f), "Cerrar [X]", buttonStyle))
        {
            minijuegoAbierto = false;
            circulosActivos.Clear();
        }
    }

    private void DibujarTarjetaEducativa()
    {
        const float cardW = 560f;
        const float cardH = 310f;
        Rect cardRect = new Rect((Screen.width - cardW) * 0.5f, (Screen.height - cardH) * 0.5f, cardW, cardH);

        Color prev = GUI.color;
        GUI.color = new Color(0.05f, 0.1f, 0.16f, 0.96f);
        GUI.DrawTexture(cardRect, solidTex);

        GUI.color = new Color(0.22f, 0.92f, 0.55f, 1f);
        GUI.DrawTexture(new Rect(cardRect.x, cardRect.y, cardRect.width, 4f), solidTex);
        GUI.color = prev;

        GUI.Label(new Rect(cardRect.x + 20f, cardRect.y + 14f, cardRect.width - 40f, 28f), tarjetaTitulo, headerStyle);
        GUI.Label(new Rect(cardRect.x + 22f, cardRect.y + 50f, cardRect.width - 44f, 195f), tarjetaCuerpo, bodyStyle);

        if (GUI.Button(new Rect(cardRect.x + (cardRect.width - 180f) * 0.5f, cardRect.y + cardRect.height - 50f, 180f, 36f), "¡Entendido! Continuar", buttonStyle))
        {
            mostrarTarjetaEducativa = false;
        }
    }

    private static void EnsureTextures()
    {
        if (solidTex == null)
        {
            solidTex = new Texture2D(1, 1);
            solidTex.SetPixel(0, 0, Color.white);
            solidTex.Apply();
        }

        if (circleTex == null)
        {
            const int size = 64;
            circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float center = (size - 1) * 0.5f;
            float maxR = center;
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / maxR;
                    if (dist > 1f)
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                    else
                    {
                        // Borde circular definido + núcleo brillante
                        float ring = (dist > 0.78f && dist <= 0.96f) ? 1f : 0.72f * (1f - dist * 0.45f);
                        float edgeSmooth = Mathf.Clamp01((1f - dist) * 10f);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, ring * edgeSmooth);
                    }
                }
            }

            circleTex.SetPixels(pixels);
            circleTex.Apply();
        }
    }

    private void EnsureStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.95f, 1f, 1f) }
            };
        }

        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                fontSize = 13,
                normal = { textColor = new Color(0.92f, 0.96f, 1f, 1f) }
            };
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
        }

        if (circleLabelStyle == null)
        {
            circleLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}

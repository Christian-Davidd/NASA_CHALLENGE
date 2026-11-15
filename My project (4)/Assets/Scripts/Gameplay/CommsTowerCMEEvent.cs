using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [PERSONA 1 - DEIVIS] Antena de Alta Ganancia (High-Gain Antenna) marciana + Consola Táctil Didáctica.
/// Implementa los 4 controles físicos interactivos para niños basados en ingeniería espacial real:
///   1. Deslizador vertical tipo DJ con osciloscopio de onda RF en tiempo real.
///   2. Cable y enchufe arrastrable de Descarga a Tierra con mini-diagrama de rayos al suelo marciano.
///   3. Cruceta/Joystick para rotar físicamente el plato hexagonal en pantallita radar hasta ángulo paralelo seguro.
///   4. Tapa protectora amarilla/negra + botón rojo de mantener 2 segundos con anillo de carga 360°.
/// Con medidor analógico de sobrecarga guiado por la velocidad real de NASA (/DONKI/CME, km/s) al apagar,
/// y reencendido inverso sin límite de tiempo al despejarse la tormenta.
/// </summary>
public sealed class CommsTowerCMEEvent : MonoBehaviour
{
    public static CommsTowerCMEEvent Instance { get; private set; }
    public static bool IsMinigameActive => Instance != null && Instance.consolaAbierta;

    private enum ModoConsola
    {
        Apagar,
        EsperandoTormenta,
        Encender
    }

    private enum ActiveDragControl
    {
        None,
        Fader,
        GroundPlug,
        DishUp,
        DishDown,
        RedButtonHold
    }

    [Header("Referencias de Arte y Animación (Antena de Alta Ganancia)")]
    [SerializeField] private SpriteRenderer baseRenderer;
    [SerializeField] private Transform anemometerRotor;
    [SerializeField] private Transform dishPivot;
    [SerializeField] private SpriteRenderer dishRenderer;
    [SerializeField] private Transform feedHornEmitter;
    [SerializeField] private SpriteRenderer cmeSparksRenderer;
    [SerializeField] private Transform orbiterTransform;
    [SerializeField] private SpriteRenderer orbiterRenderer;
    [SerializeField] private SpriteRenderer[] signalPulses;

    [Header("Texturas Táctiles de la Consola")]
    [SerializeField] private Texture2D consoleChassisTex;
    [SerializeField] private Texture2D faderKnobTex;
    [SerializeField] private Texture2D groundPlugTex;
    [SerializeField] private Texture2D hazardCoverTex;
    [SerializeField] private Texture2D redButtonTex;
    [SerializeField] private Texture2D gaugeNeedleTex;
    [SerializeField] private Texture2D radarDishTex;

    [Header("Animación Enlace Orbital")]
    [SerializeField] private float rangoEtiquetaProximidad = 5.2f;

    public static bool StaticTransmisorEncendido { get; private set; } = true;
    public static float StaticSobrecargaProgreso { get; private set; }
    public static float StaticTiempoLimiteSobrecarga { get; private set; } = 24f;
    public static int StaticReparacionesCompletadas { get; private set; }

    public static bool HaySobrecargaActivaPorCME =>
        DONKICMEManager.Instance != null &&
        DONKICMEManager.Instance.HayTormentaCMEActiva &&
        StaticTransmisorEncendido;

    public static bool NecesitaReencendidoGlobal =>
        !StaticTransmisorEncendido &&
        (DONKICMEManager.Instance == null || !DONKICMEManager.Instance.HayTormentaCMEActiva);

    public static void ResetStaticState()
    {
        StaticTransmisorEncendido = true;
        StaticSobrecargaProgreso = 0f;
        StaticTiempoLimiteSobrecarga = 24f;
        StaticReparacionesCompletadas = 0;
        if (Instance != null)
        {
            Instance.TransmisorEncendido = true;
            Instance.TieneInterferenciaCME = false;
            Instance.ReparacionesCompletadas = 0;
            Instance.ReiniciarAPasoUnoApagado();
        }
    }

    public static void TickStaticOverloadDuringCME(float deltaTime)
    {
        var cme = DONKICMEManager.Instance;
        bool nubeActiva = cme != null && cme.HayTormentaCMEActiva;
        if (!nubeActiva || !StaticTransmisorEncendido)
        {
            StaticSobrecargaProgreso = 0f;
            if (Instance != null)
            {
                Instance.sobrecargaProgreso = 0f;
                Instance.TieneInterferenciaCME = false;
            }
            return;
        }

        if (Instance != null)
            Instance.TieneInterferenciaCME = true;

        float velKmS = cme.VelocidadActualKmS > 0f ? cme.VelocidadActualKmS : 680f;
        float speedNorm = Mathf.Clamp01(Mathf.InverseLerp(350f, 1300f, velKmS));
        StaticTiempoLimiteSobrecarga = velKmS >= 800f
            ? Mathf.Lerp(40f, 32f, Mathf.Clamp01(Mathf.InverseLerp(800f, 1400f, velKmS)))
            : Mathf.Lerp(58f, 44f, speedNorm);

        StaticSobrecargaProgreso += deltaTime / Mathf.Max(8f, StaticTiempoLimiteSobrecarga);

        if (StaticSobrecargaProgreso >= 1f)
        {
            StaticSobrecargaProgreso = 0f;
            DONKIFLRManager.Instance?.AplicarCortocircuitoAntenaCME(10f, velKmS);

            if (Instance != null)
            {
                if (Instance.consolaAbierta && Instance.modoConsola == ModoConsola.Apagar)
                    Instance.ReiniciarAPasoUnoApagado();

                Instance.FijarMensajeConsola(
                    $"¡CORTOCIRCUITO DE PLASMA ({velKmS:F0} km/s)! -10% Energía en la red. Apaga la antena desde el Paso 1.",
                    new Color(1f, 0.35f, 0.35f, 1f),
                    4.5f
                );
            }
        }

        if (Instance != null)
        {
            Instance.sobrecargaProgreso = StaticSobrecargaProgreso;
            Instance.tiempoLimiteSobrecarga = StaticTiempoLimiteSobrecarga;
        }
    }

    public bool TransmisorEncendido { get; private set; } = true;
    public bool TieneInterferenciaCME { get; private set; }
    public bool TorreCalibrada => TransmisorEncendido && !TieneInterferenciaCME;
    public bool NecesitaReencendido
    {
        get
        {
            if (TransmisorEncendido) return false;
            var cme = DONKICMEManager.Instance;
            return cme == null || !cme.HayTormentaCMEActiva;
        }
    }
    public int ReparacionesCompletadas { get; private set; }

    private bool consolaAbierta;
    private ModoConsola modoConsola = ModoConsola.Apagar;
    private ActiveDragControl activeDrag = ActiveDragControl.None;

    // Estados físicos interactivos
    private float rfPowerPercent = 100f;      // 100% -> 0%
    private bool pararrayosTierraActivo;     // Enchufe conectado al puerto ⏚
    private bool draggingPlug;
    private Vector2 plugOffsetPx;             // Posición local actual del enchufe arrastrado
    private float dishAngleDeg;               // 0° = apuntando al cielo/orbitador, 90° = paralelo al suelo (reposo)
    private bool platoEstacionado;
    private bool tapaSeguridadAbierta;
    private float tapaOpenAnim;               // 0 = cerrada, 1 = levantada
    private float holdRedButtonSeconds;       // 0 .. 2.0s manteniendo clic en el botón rojo

    // Medidor de sobrecarga CME
    private float sobrecargaProgreso;
    private float tiempoLimiteSobrecarga = 24f;

    private string mensajeConsola = "";
    private Color colorMensajeConsola = new Color(0.75f, 0.92f, 1f, 1f);
    private float mensajeConsolaHasta;

    private bool mostrarTarjetaEducativa;
    private string tarjetaTitulo = "";
    private string tarjetaCuerpo = "";

    private Transform playerTransform;
    private Vector3 orbiterHomeLocalPos = new Vector3(2.55f, 3.05f, 0f);
    private float reLockFlashUntil;

    private static Texture2D solidTex;
    private static Texture2D discTex;
    private SpriteRenderer auraGlowRenderer;
    private SpriteRenderer auraRingRenderer;
    private static Sprite auraGlowSprite;
    private static Sprite auraRingSprite;
    private GUIStyle headerStyle;
    private GUIStyle panelTitleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle tinyLabelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle badgeStyle;
    private GUIStyle worldTitleStyle;
    private GUIStyle worldSubStyle;

    private static readonly Color WaveNormalColor = new Color(0.28f, 0.88f, 1.0f, 1f);
    private static readonly Color WaveRelockColor = new Color(0.25f, 1.0f, 0.58f, 1f);
    private static readonly Color WaveInterferenceColor = new Color(1.0f, 0.28f, 0.62f, 1f);
    private static readonly Color SafeModeColor = new Color(1.0f, 0.78f, 0.25f, 1f);

    // Coordenadas base exactas dentro de hga_panel_chassis.png (920x580)
    private const float BaseW = 920f;
    private const float BaseH = 580f;
    private static readonly Vector2 SocketCenter = new Vector2(110f, 398f);
    private static readonly Vector2 PlugRestCenter = new Vector2(110f, 490f);
    private static readonly Vector2 CableAnchor = new Vector2(72f, 526f);
    private static readonly Vector2 RadarCenter = new Vector2(609f, 212f);
    private static readonly Vector2 RedBtnCenter = new Vector2(476f, 438f);
    private static readonly Vector2 GaugePivot = new Vector2(746f, 486f);

    private void Awake()
    {
        Instance = this;
        if (baseRenderer == null)
            baseRenderer = GetComponent<SpriteRenderer>();
        if (orbiterTransform != null)
            orbiterHomeLocalPos = orbiterTransform.localPosition;

        // Restaurar estado persistente al regresar desde Nave o Invernadero
        TransmisorEncendido = StaticTransmisorEncendido;
        ReparacionesCompletadas = StaticReparacionesCompletadas;
        sobrecargaProgreso = StaticSobrecargaProgreso;
        tiempoLimiteSobrecarga = StaticTiempoLimiteSobrecarga;
        if (!TransmisorEncendido)
        {
            rfPowerPercent = 0f;
            pararrayosTierraActivo = true;
            plugOffsetPx = SocketCenter;
            dishAngleDeg = 90f;
            platoEstacionado = true;
        }
        else
        {
            plugOffsetPx = PlugRestCenter;
        }

        if (DONKICMEManager.Instance != null && DONKICMEManager.Instance.HayTormentaCMEActiva && TransmisorEncendido)
            TieneInterferenciaCME = true;

        EnsureTextures();
        EnsureAuraRenderers();
    }

    private void Start()
    {
        if (DONKICMEManager.Instance != null)
        {
            DONKICMEManager.Instance.OnCMEIniciada += AlIniciarCME;
            DONKICMEManager.Instance.OnCMEFinalizada += AlFinalizarCME;
        }

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;

        ActualizarSortingOrders();
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
        ActualizarSortingOrders();
        AnimarAnemometro();
        AnimarOrbitadorYPlato();
        AnimarPulsosSenal();
        AnimarChispasCME();
        AnimarAuraEstado();

        if (!consolaAbierta) return;

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            CerrarConsola();
            return;
        }

        tapaOpenAnim = Mathf.MoveTowards(tapaOpenAnim, tapaSeguridadAbierta ? 1f : 0f, Time.deltaTime * 6.5f);

        ActualizarControlesMantenidos(kb);
        ActualizarSobrecargaDuranteApagado();
    }

    private void ActualizarControlesMantenidos(Keyboard kb)
    {
        bool leftHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (!leftHeld && (activeDrag == ActiveDragControl.Fader ||
                          activeDrag == ActiveDragControl.GroundPlug ||
                          activeDrag == ActiveDragControl.DishUp ||
                          activeDrag == ActiveDragControl.DishDown ||
                          activeDrag == ActiveDragControl.RedButtonHold))
        {
            FinalizarArrastreOButton();
        }

        // Soporte cruceta en pantalla O flechas del teclado para mover el plato físico
        bool upArrow = kb != null && (kb.upArrowKey.isPressed || kb.wKey.isPressed);
        bool downArrow = kb != null && (kb.downArrowKey.isPressed || kb.sKey.isPressed);

        if (modoConsola == ModoConsola.Apagar)
        {
            if ((activeDrag == ActiveDragControl.DishDown || downArrow) && !platoEstacionado)
            {
                if (rfPowerPercent > 5f || !pararrayosTierraActivo)
                {
                    FijarMensajeConsola("¡Completa primero el Paso 1 (Potencia 0%) y Paso 2 (Enchufe a Tierra)!", SafeModeColor, 2.5f);
                }
                else
                {
                    dishAngleDeg = Mathf.MoveTowards(dishAngleDeg, 90f, 54f * Time.deltaTime);
                    if (dishAngleDeg >= 87f)
                    {
                        dishAngleDeg = 90f;
                        platoEstacionado = true;
                        FijarMensajeConsola("¡CLIC! Plato paralelo al suelo con seguros verdes bloqueados. Sigue al Paso 4.", WaveRelockColor, 3.0f);
                    }
                }
            }
            else if (activeDrag == ActiveDragControl.DishUp || upArrow)
            {
                if (!platoEstacionado)
                    dishAngleDeg = Mathf.MoveTowards(dishAngleDeg, 0f, 45f * Time.deltaTime);
            }
        }
        else if (modoConsola == ModoConsola.Encender)
        {
            if ((activeDrag == ActiveDragControl.DishUp || upArrow) && platoEstacionado)
            {
                if (!TransmisorEncendido)
                {
                    FijarMensajeConsola("¡Primero enciende la Energía Central en el Paso 1 para activar el motor!", SafeModeColor, 2.5f);
                }
                else
                {
                    dishAngleDeg = Mathf.MoveTowards(dishAngleDeg, 0f, 54f * Time.deltaTime);
                    if (dishAngleDeg <= 3f)
                    {
                        dishAngleDeg = 0f;
                        platoEstacionado = false;
                        FijarMensajeConsola("¡CLIC! Plato apuntando al cielo hacia el orbitador. Desconecta Tierra (Paso 3).", WaveRelockColor, 3.0f);
                    }
                }
            }
        }

        // Mantener botón rojo por 2.0 segundos
        if (activeDrag == ActiveDragControl.RedButtonHold && leftHeld && tapaSeguridadAbierta)
        {
            holdRedButtonSeconds = Mathf.MoveTowards(holdRedButtonSeconds, 2.0f, Time.deltaTime);
            if (holdRedButtonSeconds >= 2.0f)
            {
                activeDrag = ActiveDragControl.None;
                holdRedButtonSeconds = 0f;
                if (modoConsola == ModoConsola.Apagar)
                {
                    CompletarApagadoSeguro();
                }
                else if (modoConsola == ModoConsola.Encender)
                {
                    TransmisorEncendido = true;
                    StaticTransmisorEncendido = true;
                    tapaSeguridadAbierta = false;
                    FijarMensajeConsola("¡Energía principal ON! Ahora sube el plato con la flecha ▲ (Paso 2).", WaveRelockColor, 3.0f);
                }
            }
        }
        else if (activeDrag != ActiveDragControl.RedButtonHold)
        {
            holdRedButtonSeconds = Mathf.MoveTowards(holdRedButtonSeconds, 0f, Time.deltaTime * 3.5f);
        }
    }

    private void FinalizarArrastreOButton()
    {
        if (activeDrag == ActiveDragControl.GroundPlug)
        {
            draggingPlug = false;
            if (modoConsola == ModoConsola.Apagar)
            {
                if (!pararrayosTierraActivo && Vector2.Distance(plugOffsetPx, SocketCenter) <= 58f)
                {
                    if (rfPowerPercent > 5f)
                    {
                        plugOffsetPx = PlugRestCenter;
                        FijarMensajeConsola("¡Primero baja el deslizador de Potencia RF al 0% (Paso 1)!", SafeModeColor, 3.0f);
                    }
                    else
                    {
                        pararrayosTierraActivo = true;
                        plugOffsetPx = SocketCenter;
                        FijarMensajeConsola("¡Enchufe conectado! Los rayos viajan por el cable directo al suelo marciano.", WaveRelockColor, 3.2f);
                    }
                }
                else
                {
                    plugOffsetPx = pararrayosTierraActivo ? SocketCenter : PlugRestCenter;
                }
            }
            else if (modoConsola == ModoConsola.Encender)
            {
                if (pararrayosTierraActivo && Vector2.Distance(plugOffsetPx, SocketCenter) > 56f)
                {
                    if (!TransmisorEncendido || platoEstacionado)
                    {
                        plugOffsetPx = SocketCenter;
                        FijarMensajeConsola("¡Primero enciende energía (Paso 1) y apunta el plato al cielo (Paso 2)!", SafeModeColor, 3.0f);
                    }
                    else
                    {
                        pararrayosTierraActivo = false;
                        plugOffsetPx = PlugRestCenter;
                        FijarMensajeConsola("¡Tierra desconectada! Sube el deslizador RF al 100% en el Paso 4.", WaveRelockColor, 3.0f);
                    }
                }
                else
                {
                    plugOffsetPx = pararrayosTierraActivo ? SocketCenter : PlugRestCenter;
                }
            }
        }

        activeDrag = ActiveDragControl.None;
    }

    private void ActualizarSobrecargaDuranteApagado()
    {
        sobrecargaProgreso = StaticSobrecargaProgreso;
        tiempoLimiteSobrecarga = StaticTiempoLimiteSobrecarga;
    }

    private void ReiniciarAPasoUnoApagado()
    {
        rfPowerPercent = 100f;
        pararrayosTierraActivo = false;
        draggingPlug = false;
        plugOffsetPx = PlugRestCenter;
        dishAngleDeg = 0f;
        platoEstacionado = false;
        tapaSeguridadAbierta = false;
        tapaOpenAnim = 0f;
        holdRedButtonSeconds = 0f;
        TransmisorEncendido = true;
        activeDrag = ActiveDragControl.None;
    }

    private void ActualizarSortingOrders()
    {
        int baseOrder = 10 + Mathf.RoundToInt(-transform.position.y * 3f);
        int layerId = baseRenderer != null ? baseRenderer.sortingLayerID : 0;
        if (auraGlowRenderer != null)
        {
            auraGlowRenderer.sortingLayerID = layerId;
            auraGlowRenderer.sortingOrder = baseOrder - 1;
        }
        if (auraRingRenderer != null)
        {
            auraRingRenderer.sortingLayerID = layerId;
            auraRingRenderer.sortingOrder = baseOrder - 1;
        }
        if (baseRenderer != null)
            baseRenderer.sortingOrder = baseOrder;
        if (dishRenderer != null)
            dishRenderer.sortingOrder = baseOrder + 2;
        if (cmeSparksRenderer != null)
            cmeSparksRenderer.sortingOrder = baseOrder + 3;
    }

    private void AnimarAnemometro()
    {
        if (anemometerRotor == null) return;
        float spinSpeed = TieneInterferenciaCME ? 360f : 155f;
        anemometerRotor.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
    }

    private void AnimarOrbitadorYPlato()
    {
        if (orbiterTransform != null)
        {
            float driftX = Mathf.Sin(Time.time * 0.55f) * 0.52f;
            float driftY = Mathf.Cos(Time.time * 0.85f) * 0.16f;
            orbiterTransform.localPosition = orbiterHomeLocalPos + new Vector3(driftX, driftY, 0f);

            float bank = Mathf.Sin(Time.time * 0.65f) * 4.5f;
            orbiterTransform.localRotation = Quaternion.Euler(0f, 0f, bank);

            if (orbiterRenderer != null)
            {
                if (Time.time < reLockFlashUntil)
                    orbiterRenderer.color = Color.Lerp(Color.white, WaveRelockColor, 0.45f);
                else if (TieneInterferenciaCME)
                    orbiterRenderer.color = Color.Lerp(Color.white, WaveInterferenceColor, 0.25f + 0.2f * Mathf.Sin(Time.time * 8f));
                else
                    orbiterRenderer.color = Color.white;
            }
        }

        if (dishPivot != null)
        {
            float targetAngle;
            if (platoEstacionado || !TransmisorEncendido)
            {
                targetAngle = -68f;
                dishPivot.localPosition = new Vector3(0f, 0.50f, 0f);
            }
            else if (consolaAbierta)
            {
                // Refleja en vivo en el escenario el movimiento del joystick en consola
                targetAngle = Mathf.Lerp(0f, -68f, dishAngleDeg / 90f);
                dishPivot.localPosition = new Vector3(0f, 0.50f, 0f);
            }
            else if (TieneInterferenciaCME)
            {
                targetAngle = Mathf.Sin(Time.time * 2.6f) * 14f + Mathf.Sin(Time.time * 16f) * 3.5f;
                dishPivot.localPosition = new Vector3(
                    Mathf.Sin(Time.time * 24f) * 0.015f,
                    0.50f + Mathf.Cos(Time.time * 19f) * 0.012f,
                    0f
                );
            }
            else
            {
                targetAngle = Mathf.Sin(Time.time * 0.55f) * 6.5f;
                dishPivot.localPosition = new Vector3(0f, 0.50f, 0f);
            }

            float currentZ = dishPivot.localEulerAngles.z;
            float nextZ = Mathf.LerpAngle(currentZ, targetAngle, 1f - Mathf.Exp(-8f * Time.deltaTime));
            dishPivot.localRotation = Quaternion.Euler(0f, 0f, nextZ);
        }
    }

    private void AnimarPulsosSenal()
    {
        if (signalPulses == null || signalPulses.Length == 0) return;

        bool emitirSenal = TransmisorEncendido && !platoEstacionado && rfPowerPercent > 10f;
        if (!emitirSenal)
        {
            for (int i = 0; i < signalPulses.Length; i++)
            {
                if (signalPulses[i] != null)
                    signalPulses[i].color = new Color(1f, 1f, 1f, 0f);
            }
            return;
        }

        Vector3 startWorld = feedHornEmitter != null
            ? feedHornEmitter.position
            : (dishPivot != null ? dishPivot.position + new Vector3(0.08f, 0.14f, 0f) : transform.position + new Vector3(0.08f, 0.65f, 0f));

        Vector3 endWorld = orbiterTransform != null
            ? orbiterTransform.position + new Vector3(0f, -0.18f, 0f)
            : startWorld + new Vector3(2.4f, 2.5f, 0f);

        Vector3 dir = endWorld - startWorld;
        float beamAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        bool isRelock = Time.time < reLockFlashUntil;
        float cycleDuration = TieneInterferenciaCME ? 1.05f : (isRelock ? 0.9f : 1.75f);
        float powerMul = Mathf.Clamp01(rfPowerPercent / 100f);

        for (int i = 0; i < signalPulses.Length; i++)
        {
            var sr = signalPulses[i];
            if (sr == null) continue;

            float phase = Mathf.Repeat(Time.time / cycleDuration + (float)i / signalPulses.Length, 1f);

            if (TieneInterferenciaCME)
            {
                float maxProgress = 0.42f;
                float t = phase * maxProgress;
                sr.transform.position = Vector3.Lerp(startWorld, endWorld, t);
                sr.transform.rotation = Quaternion.Euler(0f, 0f, beamAngle + Mathf.Sin(Time.time * 18f + i) * 14f);
                float scale = Mathf.Lerp(0.32f, 0.72f, phase);
                sr.transform.localScale = new Vector3(scale, scale, 1f);

                float flicker = (Mathf.Sin(Time.time * 26f + i * 2.1f) > -0.25f) ? 1f : 0.2f;
                float alpha = (1f - phase) * 0.85f * flicker * powerMul;
                sr.color = new Color(WaveInterferenceColor.r, WaveInterferenceColor.g, WaveInterferenceColor.b, alpha);
            }
            else
            {
                float t = Mathf.Lerp(0.08f, 0.92f, phase);
                sr.transform.position = Vector3.Lerp(startWorld, endWorld, t);
                sr.transform.rotation = Quaternion.Euler(0f, 0f, beamAngle);
                float scale = Mathf.Lerp(0.35f, isRelock ? 1.25f : 0.98f, phase);
                sr.transform.localScale = new Vector3(scale, scale, 1f);

                float fadeInOut = Mathf.Sin(phase * Mathf.PI);
                Color baseC = isRelock ? WaveRelockColor : WaveNormalColor;
                sr.color = new Color(baseC.r, baseC.g, baseC.b, fadeInOut * (isRelock ? 1f : 0.86f) * powerMul);
            }
        }
    }

    private void AnimarChispasCME()
    {
        if (cmeSparksRenderer == null) return;

        cmeSparksRenderer.enabled = TieneInterferenciaCME;
        if (!TieneInterferenciaCME) return;

        float pulse = 0.75f + 0.35f * Mathf.Sin(Time.time * 14f);
        cmeSparksRenderer.transform.localScale = new Vector3(pulse, pulse, 1f);
        cmeSparksRenderer.transform.Rotate(0f, 0f, 95f * Time.deltaTime);
        float alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 11f));
        cmeSparksRenderer.color = new Color(1f, 0.65f, 1f, alpha);
    }

    private void EnsureAuraRenderers()
    {
        if (auraGlowSprite == null)
        {
            const int sz = 128;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float half = (sz - 1) * 0.5f;
            var px = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                    float falloff = Mathf.Clamp01(1f - d);
                    px[y * sz + x] = new Color(1f, 1f, 1f, falloff * falloff * 0.85f);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            auraGlowSprite = Sprite.Create(tex, new Rect(0f, 0f, sz, sz), new Vector2(0.5f, 0.5f), 40f);
        }

        if (auraRingSprite == null)
        {
            const int sz = 128;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float half = (sz - 1) * 0.5f;
            var px = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                    float band = Mathf.Exp(-Mathf.Pow((d - 0.78f) / 0.11f, 2f));
                    px[y * sz + x] = new Color(1f, 1f, 1f, band * 0.9f);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            auraRingSprite = Sprite.Create(tex, new Rect(0f, 0f, sz, sz), new Vector2(0.5f, 0.5f), 40f);
        }

        if (auraGlowRenderer == null)
        {
            var glowGo = new GameObject("HGA_AuraGlow");
            glowGo.transform.SetParent(transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            auraGlowRenderer = glowGo.AddComponent<SpriteRenderer>();
            auraGlowRenderer.sprite = auraGlowSprite;
            auraGlowRenderer.enabled = false;
        }

        if (auraRingRenderer == null)
        {
            var ringGo = new GameObject("HGA_AuraRing");
            ringGo.transform.SetParent(transform, false);
            ringGo.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            auraRingRenderer = ringGo.AddComponent<SpriteRenderer>();
            auraRingRenderer.sprite = auraRingSprite;
            auraRingRenderer.enabled = false;
        }
    }

    private void AnimarAuraEstado()
    {
        EnsureAuraRenderers();

        var cme = DONKICMEManager.Instance;
        bool tormentaActiva = cme != null && cme.HayTormentaCMEActiva;

        bool alertaRojaApagar = tormentaActiva && TransmisorEncendido;
        bool alertaVerdeEncender = !tormentaActiva && !TransmisorEncendido;

        if (!alertaRojaApagar && !alertaVerdeEncender)
        {
            if (auraGlowRenderer != null) auraGlowRenderer.enabled = false;
            if (auraRingRenderer != null) auraRingRenderer.enabled = false;
            if (baseRenderer != null) baseRenderer.color = Color.white;
            if (dishRenderer != null) dishRenderer.color = Color.white;
            return;
        }

        Color auraBase = alertaRojaApagar
            ? new Color(1f, 0.14f, 0.16f, 1f)
            : new Color(0.18f, 0.98f, 0.48f, 1f);

        float pulseFreq = alertaRojaApagar ? 5.0f : 3.0f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseFreq);

        if (auraGlowRenderer != null)
        {
            auraGlowRenderer.enabled = true;
            float s = Mathf.Lerp(1.05f, 1.32f, pulse);
            auraGlowRenderer.transform.localScale = new Vector3(s, s, 1f);
            auraGlowRenderer.color = new Color(auraBase.r, auraBase.g, auraBase.b, Mathf.Lerp(0.42f, 0.78f, pulse));
        }

        if (auraRingRenderer != null)
        {
            auraRingRenderer.enabled = true;
            float expand = Mathf.Repeat(Time.time * (alertaRojaApagar ? 1.15f : 0.75f), 1f);
            float rs = Mathf.Lerp(0.72f, 1.55f, expand);
            auraRingRenderer.transform.localScale = new Vector3(rs, rs, 1f);
            float ringAlpha = (1f - expand) * (alertaRojaApagar ? 0.85f : 0.72f);
            auraRingRenderer.color = new Color(auraBase.r, auraBase.g, auraBase.b, ringAlpha);
        }

        Color rimTint = Color.Lerp(Color.white, auraBase, 0.18f + 0.22f * pulse);
        if (baseRenderer != null) baseRenderer.color = rimTint;
        if (dishRenderer != null) dishRenderer.color = rimTint;
    }

    private void AlIniciarCME(DONKICMEManager.CMEEventData ev, float speedKmS)
    {
        TieneInterferenciaCME = TransmisorEncendido;
        mostrarTarjetaEducativa = false;
        sobrecargaProgreso = 0f;

        if (consolaAbierta && modoConsola == ModoConsola.Encender)
        {
            modoConsola = ModoConsola.Apagar;
            FijarMensajeConsola("¡Llegó otra nube de plasma! Sigue los 4 pasos de Apagado Seguro.", WaveInterferenceColor, 3.8f);
        }
    }

    private void AlFinalizarCME()
    {
        TieneInterferenciaCME = false;
        sobrecargaProgreso = 0f;

        if (consolaAbierta && modoConsola == ModoConsola.EsperandoTormenta)
        {
            PrepararEstadoParaEncender();
            FijarMensajeConsola("¡La nube pasó! Sigue los 4 pasos inversos para reencender (sin límite de tiempo).", WaveRelockColor, 4.0f);
        }
    }

    public string InteractFromPlayer()
    {
        AbrirMinijuegoCalibracion();
        if (!TransmisorEncendido)
            return NecesitaReencendido
                ? "Abriendo tablero HGA para reencender el transmisor..."
                : "Antena HGA en Modo Seguro esperando que pase la tormenta CME...";

        return TieneInterferenciaCME
            ? "Abriendo tablero de Apagado Seguro de la Antena HGA..."
            : "Abriendo consola de ingeniería de la Antena HGA...";
    }

    public void AbrirMinijuegoCalibracion()
    {
        consolaAbierta = true;
        mostrarTarjetaEducativa = false;
        sobrecargaProgreso = StaticSobrecargaProgreso;
        activeDrag = ActiveDragControl.None;

        var cme = DONKICMEManager.Instance;
        bool cmeActiva = cme != null && cme.HayTormentaCMEActiva;

        if (TransmisorEncendido)
        {
            modoConsola = ModoConsola.Apagar;
            ReiniciarAPasoUnoApagado();
            if (cmeActiva)
            {
                float vel = cme.VelocidadActualKmS > 0f ? cme.VelocidadActualKmS : 680f;
                FijarMensajeConsola($"Tormenta CME ({vel:F0} km/s): sigue los pasos 1 → 2 → 3 → 4 antes de la sobrecarga.", WaveInterferenceColor, 3.8f);
            }
            else
            {
                FijarMensajeConsola("Modo práctica: prueba bajar potencia (1), enchufe a tierra (2), plato (3) y botón rojo (4).", WaveNormalColor, 3.8f);
            }
        }
        else if (cmeActiva)
        {
            modoConsola = ModoConsola.EsperandoTormenta;
            FijarMensajeConsola("Transmisor protegido en Modo Seguro. Espera unos segundos a que pase la nube.", SafeModeColor, 3.8f);
        }
        else
        {
            PrepararEstadoParaEncender();
            FijarMensajeConsola("Cielo despejado: sigue el orden inverso sin límite de tiempo para reencender.", WaveRelockColor, 3.8f);
        }
    }

    private void PrepararEstadoParaEncender()
    {
        modoConsola = ModoConsola.Encender;
        TransmisorEncendido = false;
        platoEstacionado = true;
        dishAngleDeg = 90f;
        pararrayosTierraActivo = true;
        plugOffsetPx = SocketCenter;
        rfPowerPercent = 0f;
        tapaSeguridadAbierta = false;
        tapaOpenAnim = 0f;
        holdRedButtonSeconds = 0f;
        activeDrag = ActiveDragControl.None;
    }

    private void CerrarConsola()
    {
        consolaAbierta = false;
        activeDrag = ActiveDragControl.None;
        draggingPlug = false;

        if (TransmisorEncendido)
        {
            ReiniciarAPasoUnoApagado();
        }
    }

    private void CompletarApagadoSeguro()
    {
        TransmisorEncendido = false;
        StaticTransmisorEncendido = false;
        TieneInterferenciaCME = false;
        platoEstacionado = true;
        dishAngleDeg = 90f;
        pararrayosTierraActivo = true;
        plugOffsetPx = SocketCenter;
        rfPowerPercent = 0f;
        tapaSeguridadAbierta = false;
        sobrecargaProgreso = 0f;
        StaticSobrecargaProgreso = 0f;
        consolaAbierta = false;

        if (DONKICMEManager.Instance != null && DONKICMEManager.Instance.HayTormentaCMEActiva)
        {
            DONKICMEManager.Instance.AcelerarFinDeTormenta(7.5f);
        }

        PrepararTarjetaEducativa(true);
        mostrarTarjetaEducativa = true;
    }

    private void CompletarReencendidoSeguro()
    {
        TransmisorEncendido = true;
        StaticTransmisorEncendido = true;
        TieneInterferenciaCME = DONKICMEManager.Instance != null && DONKICMEManager.Instance.HayTormentaCMEActiva;
        platoEstacionado = false;
        dishAngleDeg = 0f;
        pararrayosTierraActivo = false;
        plugOffsetPx = PlugRestCenter;
        rfPowerPercent = 100f;
        tapaSeguridadAbierta = false;
        consolaAbierta = false;
        ReparacionesCompletadas++;
        StaticReparacionesCompletadas = ReparacionesCompletadas;
        reLockFlashUntil = Time.time + 2.2f;

        PrepararTarjetaEducativa(false);
        mostrarTarjetaEducativa = true;
    }

    private void PrepararTarjetaEducativa(bool fueApagado)
    {
        var manager = DONKICMEManager.Instance;
        var ev = manager != null ? manager.EventoActivo : null;
        float vel = manager != null && manager.VelocidadActualKmS > 0f ? manager.VelocidadActualKmS : 780f;
        string id = ev != null && !string.IsNullOrEmpty(ev.activityID) ? ev.activityID : "2026-11-12T08:12:00-CME-001";

        if (fueApagado)
        {
            tarjetaTitulo = "¡ANTENA EN MODO SEGURO!";
            tarjetaCuerpo =
                "¡Aprendiste cómo protegen los ingenieros espaciales una antena real durante una CME!\n\n" +
                "• 1. Onda RF a 0%: redujiste la amplitud de energía antes de desconectar.\n" +
                "• 2. Cable a Tierra: desviaste los rayos solares al suelo rojo marciano.\n" +
                "• 3. Plato paralelo al suelo: evitaste que el viento solar dañe sus motores.\n" +
                "• 4. Tapa y botón 2s: confirmaste un apagado seguro sin errores.\n\n" +
                (manager != null && manager.HayTormentaCMEActiva
                    ? $"Espera unos segundos a que se disipe la tormenta ({vel:F0} km/s) y presiona [E] para encenderla sin límite de tiempo."
                    : "Cuando quieras reactivar las comunicaciones hacia el orbitador, presiona [E] en la antena.");
        }
        else
        {
            tarjetaTitulo = "¡TRANSMISOR EN LÍNEA CON EL ORBITADOR!";
            tarjetaCuerpo =
                "¡Excelente trabajo! Volviste a encender el transmisor paso a paso sin sobrecargas.\n\n" +
                "• Encendiste la energía central, levantaste el plato hacia el satélite orbitador, " +
                "desconectaste la tierra protectora y devolviste la amplitud RF al 100%.\n\n" +
                $"DATOS REALES NASA (/DONKI/CME): Evento {id}  |  Velocidad: {vel:F0} km/s";
        }
    }

    private void FijarMensajeConsola(string texto, Color color, float duracionSeg)
    {
        mensajeConsola = texto;
        colorMensajeConsola = color;
        mensajeConsolaHasta = Time.time + duracionSeg;
    }

    private void OnGUI()
    {
        EnsureTextures();
        EnsureStyles();

        DibujarEtiquetasEnlaceMundo();

        if (consolaAbierta)
        {
            EnsureTextures();
            DibujarTableroHardware();
        }
        else if (mostrarTarjetaEducativa)
        {
            DibujarTarjetaEducativa();
        }
    }

    private void DibujarEtiquetasEnlaceMundo()
    {
        var cam = Camera.main;
        if (cam == null) return;

        bool jugadorCerca = playerTransform != null &&
            Vector2.Distance(playerTransform.position, transform.position) <= rangoEtiquetaProximidad;

        bool alertaActiva = TieneInterferenciaCME || NecesitaReencendido;

        if ((jugadorCerca || alertaActiva) && orbiterTransform != null)
        {
            string satTitle;
            string satSub;
            Color satAccent;

            if (!TransmisorEncendido)
            {
                satTitle = "ORBITADOR MARCIANO (EN ESPERA)";
                satSub = NecesitaReencendido ? "Listo para señal — Reenciende la antena" : "Antena en Modo Seguro por tormenta CME";
                satAccent = SafeModeColor;
            }
            else if (TieneInterferenciaCME)
            {
                satTitle = "ORBITADOR MARCIANO (INTERFERENCIA)";
                satSub = "¡Peligro eléctrico en transmisor!";
                satAccent = WaveInterferenceColor;
            }
            else
            {
                satTitle = "ORBITADOR MARCIANO";
                satSub = "Recibe señal de la antena → Tierra";
                satAccent = WaveNormalColor;
            }

            DibujarEtiquetaFlotante(cam, orbiterTransform.position + new Vector3(0f, 0.55f, 0f), satTitle, satSub, satAccent);
        }

        if (jugadorCerca || alertaActiva)
        {
            string antTitle = "ANTENA DE ALTA GANANCIA";
            string antSub;
            Color antAccent;

            if (TieneInterferenciaCME)
            {
                antSub = "¡TORMENTA CME! Presiona [E] para Apagado Seguro";
                antAccent = WaveInterferenceColor;
            }
            else if (!TransmisorEncendido)
            {
                antSub = NecesitaReencendido
                    ? "¡CIELO DESPEJADO! [E] Reencender Transmisor"
                    : "MODO SEGURO ACTIVO — Esperando fin de tormenta";
                antAccent = NecesitaReencendido ? WaveRelockColor : SafeModeColor;
            }
            else
            {
                antSub = "Apuntando al orbitador — [E] Abrir Tablero";
                antAccent = new Color(0.28f, 0.92f, 0.58f, 1f);
            }

            DibujarEtiquetaFlotante(cam, transform.position + new Vector3(0f, -1.25f, 0f), antTitle, antSub, antAccent);
        }
    }

    private void DibujarEtiquetaFlotante(Camera cam, Vector3 worldPos, string title, string subtitle, Color accent)
    {
        Vector3 sp = cam.WorldToScreenPoint(worldPos);
        if (sp.z <= 0f) return;
        if (sp.x < -100f || sp.x > Screen.width + 100f || sp.y < -80f || sp.y > Screen.height + 80f) return;

        float gx = sp.x;
        float gy = Screen.height - sp.y;
        Vector2 titleSize = worldTitleStyle.CalcSize(new GUIContent(title));
        Vector2 subtitleSize = worldSubStyle.CalcSize(new GUIContent(subtitle));

        const float paddingX = 12f;
        const float paddingY = 6f;
        float w = Mathf.Max(235f, Mathf.Max(titleSize.x, subtitleSize.x) + paddingX * 2f);
        float titleLineHeight = Mathf.Ceil(titleSize.y);
        float subtitleLineHeight = Mathf.Ceil(subtitleSize.y);
        float h = paddingY * 2f + titleLineHeight + subtitleLineHeight + 3f;
        Rect rect = new Rect(gx - w * 0.5f, gy - h * 0.5f, w, h);

        Color prev = GUI.color;
        GUI.color = new Color(0.05f, 0.08f, 0.13f, 0.88f);
        GUI.DrawTexture(rect, solidTex);
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 4f), solidTex);
        GUI.color = prev;

        worldTitleStyle.normal.textColor = accent;
        GUI.Label(
            new Rect(rect.x + paddingX, rect.y + paddingY, rect.width - paddingX * 2f, titleLineHeight),
            title,
            worldTitleStyle);
        GUI.Label(
            new Rect(
                rect.x + paddingX,
                rect.y + paddingY + titleLineHeight + 3f,
                rect.width - paddingX * 2f,
                subtitleLineHeight),
            subtitle,
            worldSubStyle);
    }

    private void DibujarMarcoPaso(Rect rect, bool completado, bool esActual, float scale)
    {
        if (!completado && !esActual) return;
        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * 5.2f);
        Color c = completado
            ? new Color(0.24f, 0.96f, 0.56f, 0.92f)
            : new Color(0.25f, 0.92f, 1.0f, 0.45f + 0.45f * pulse);
        float t = (completado ? 2.5f : 3.2f) * scale;
        Color prev = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, t), solidTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - t, rect.width, t), solidTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, t, rect.height), solidTex);
        GUI.DrawTexture(new Rect(rect.x + rect.width - t, rect.y, t, rect.height), solidTex);
        GUI.color = prev;
    }

    private void DibujarTableroHardware()
    {
        // Fondo oscuro semitransparente detrás de la consola
        Color prev = GUI.color;
        GUI.color = new Color(0.02f, 0.03f, 0.06f, 0.72f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), solidTex);
        GUI.color = prev;

        // Escalado uniforme limpio incluyendo la barra de mensajes inferior (58px extra)
        float scale = Mathf.Min(1.0f, Mathf.Min((Screen.width - 36f) / BaseW, (Screen.height - 88f) / BaseH));
        float ox = (Screen.width - BaseW * scale) * 0.5f;
        float oy = (Screen.height - (BaseH + 58f) * scale) * 0.5f;

        Rect ToScreen(float x, float y, float w, float h) => new Rect(ox + x * scale, oy + y * scale, w * scale, h * scale);
        Vector2 ToScreenPt(Vector2 p) => new Vector2(ox + p.x * scale, oy + p.y * scale);
        Vector2 ToPanelPt(Vector2 screenPt) => new Vector2((screenPt.x - ox) / scale, (screenPt.y - oy) / scale);

        // Chasis industrial marciano de fondo
        Rect chassisRect = ToScreen(0f, 0f, BaseW, BaseH);
        if (consoleChassisTex != null)
        {
            GUI.DrawTexture(chassisRect, consoleChassisTex);
        }
        else
        {
            GUI.color = new Color(0.24f, 0.30f, 0.37f, 1f);
            GUI.DrawTexture(chassisRect, solidTex);
            GUI.color = prev;
        }

        // Título superior en la placa (1 sola línea limpia)
        string modeBanner = modoConsola switch
        {
            ModoConsola.Apagar => "TORRE COMMS — APAGADO SEGURO",
            ModoConsola.EsperandoTormenta => "TORRE COMMS — MODO SEGURO ACTIVO",
            _ => "TORRE COMMS — REENCENDIDO SEGURO"
        };
        headerStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(13 * scale));
        headerStyle.normal.textColor = modoConsola == ModoConsola.Encender ? WaveRelockColor : new Color(0.98f, 0.92f, 0.72f, 1f);
        GUI.Label(ToScreen(256f, 8f, 408f, 29f), modeBanner, headerStyle);

        tinyLabelStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(11 * scale));
        panelTitleStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(11 * scale));
        badgeStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(10 * scale));
        bodyStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(11 * scale));

        bool p1Done = modoConsola == ModoConsola.Apagar ? (rfPowerPercent <= 5f) : (rfPowerPercent >= 95f);
        bool p2Done = modoConsola == ModoConsola.Apagar ? pararrayosTierraActivo : !pararrayosTierraActivo;
        bool p3Done = modoConsola == ModoConsola.Apagar ? platoEstacionado : !platoEstacionado;
        bool p4Done = modoConsola == ModoConsola.Apagar ? !TransmisorEncendido : TransmisorEncendido;

        bool b1Actual = modoConsola == ModoConsola.Apagar ? !p1Done : (p4Done && p3Done && p2Done && !p1Done);
        bool b2Actual = modoConsola == ModoConsola.Apagar ? (p1Done && !p2Done) : (p4Done && p3Done && !p2Done);
        bool b3Actual = modoConsola == ModoConsola.Apagar ? (p1Done && p2Done && !p3Done) : (p4Done && !p3Done);
        bool b4Actual = modoConsola == ModoConsola.Apagar ? (p1Done && p2Done && p3Done && !p4Done) : !p4Done;

        // Resaltado visual automático del módulo actual (cyan pulsante) y completados (verde)
        if (modoConsola != ModoConsola.EsperandoTormenta)
        {
            DibujarMarcoPaso(ToScreen(44f, 58f, 428f, 218f), p1Done, b1Actual, scale);
            DibujarMarcoPaso(ToScreen(44f, 300f, 292f, 242f), p2Done, b2Actual, scale);
            DibujarMarcoPaso(ToScreen(488f, 58f, 388f, 256f), p3Done, b3Actual, scale);
            DibujarMarcoPaso(ToScreen(350f, 300f, 252f, 242f), p4Done, b4Actual, scale);
        }

        Event ev = Event.current;
        Vector2 mousePanel = ToPanelPt(ev.mousePosition);

        // =========================================================================
        // PASO 1: DESLIZADOR VERTICAL DJ + PANTALLA OSCILOSCOPIO DE ONDA RF
        // =========================================================================
        panelTitleStyle.normal.textColor = p1Done ? WaveRelockColor : (b1Actual ? WaveNormalColor : new Color(0.82f, 0.88f, 0.94f, 1f));
        GUI.Label(ToScreen(60f, 64f, 396f, 24f),
            modoConsola == ModoConsola.Encender
                ? (p1Done ? "✓ 4. POTENCIA RF EN 100%" : "4. SUBE LA POTENCIA AL 100%")
                : (p1Done ? "✓ 1. POTENCIA RF EN 0%" : "1. BAJA LA POTENCIA AL 0%"),
            panelTitleStyle);

        // Track vertical: 100% en y=124, 0% en y=230
        float faderMinY = 124f;
        float faderMaxY = 230f;
        float knobY = Mathf.Lerp(faderMaxY, faderMinY, rfPowerPercent / 100f);

        Rect faderHit = ToScreen(60f, 96f, 86f, 166f);
        if (ev.type == EventType.MouseDown && ev.button == 0 && faderHit.Contains(ev.mousePosition))
        {
            if (modoConsola == ModoConsola.EsperandoTormenta)
            {
                FijarMensajeConsola("Espera a que pase la tormenta para tocar los controles.", SafeModeColor, 2.5f);
            }
            else if (modoConsola == ModoConsola.Encender && (!TransmisorEncendido || platoEstacionado || pararrayosTierraActivo))
            {
                FijarMensajeConsola("Completa primero 1 (Botón), 2 (Plato) y 3 (Quitar Tierra).", SafeModeColor, 3.0f);
            }
            else
            {
                activeDrag = ActiveDragControl.Fader;
            }
            ev.Use();
        }

        if (activeDrag == ActiveDragControl.Fader)
        {
            float clampedY = Mathf.Clamp(mousePanel.y, faderMinY, faderMaxY);
            float pct = (1f - (clampedY - faderMinY) / (faderMaxY - faderMinY)) * 100f;
            if (modoConsola == ModoConsola.Apagar && pct <= 7f) pct = 0f;
            if (modoConsola == ModoConsola.Encender && pct >= 93f) pct = 100f;
            rfPowerPercent = pct;

            if (modoConsola == ModoConsola.Apagar && rfPowerPercent <= 5f)
            {
                rfPowerPercent = 0f;
                FijarMensajeConsola("Paso 1 listo. Ahora arrastra el enchufe naranja arriba al conector.", WaveRelockColor, 3.0f);
            }
            else if (modoConsola == ModoConsola.Encender && rfPowerPercent >= 98f)
            {
                rfPowerPercent = 100f;
                activeDrag = ActiveDragControl.None;
                CompletarReencendidoSeguro();
                return;
            }
        }

        // Barra LED vertical dentro del riel (x=98..106)
        float fillFrac = rfPowerPercent / 100f;
        float barH = (faderMaxY - faderMinY) * fillFrac;
        GUI.color = rfPowerPercent <= 5f ? WaveRelockColor : WaveNormalColor;
        GUI.DrawTexture(ToScreen(98f, faderMaxY - barH, 8f, barH), solidTex);
        GUI.color = prev;

        // Perilla metálica DJ centrada en x=102
        Rect knobRect = ToScreen(102f - 34f, knobY - 21f, 68f, 42f);
        if (faderKnobTex != null) GUI.DrawTexture(knobRect, faderKnobTex);
        else { GUI.color = Color.gray; GUI.DrawTexture(knobRect, solidTex); GUI.color = prev; }

        // Osciloscopio en tiempo real dentro de (156, 98, 302, 162)
        DibujarOsciloscopioOnda(ToScreenPt, scale);

        // =========================================================================
        // PASO 2: CABLE Y ENCHUFE ARRASTRABLE DE TOMA DE TIERRA + MINI-DIAGRAMA
        // =========================================================================
        panelTitleStyle.normal.textColor = p2Done ? WaveRelockColor : (b2Actual ? WaveNormalColor : new Color(0.82f, 0.88f, 0.94f, 1f));
        GUI.Label(ToScreen(60f, 306f, 260f, 24f),
            modoConsola == ModoConsola.Encender
                ? (p2Done ? "✓ 3. TIERRA DESCONECTADA" : "3. DESCONECTA EL ENCHUFE")
                : (p2Done ? "✓ 2. TIERRA CONECTADA" : "2. CONECTA EL ENCHUFE"),
            panelTitleStyle);

        // Mini-diagrama animado de rayos al suelo marciano en (168, 340, 154, 190)
        DibujarMiniDiagramaTierra(ToScreenPt, scale);

        // Indicador visual claro en el conector cuando toca el Paso 2
        if (b2Actual)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
            if (modoConsola == ModoConsola.Apagar)
            {
                Rect targetOutline = ToScreen(64f, 340f, 92f, 92f);
                DibujarMarcoPaso(targetOutline, false, true, scale);
                float lift = Mathf.Repeat(Time.time * 1.4f, 1f);
                Vector2 arrowPt = ToScreenPt(new Vector2(110f, Mathf.Lerp(452f, 434f, lift)));
                badgeStyle.normal.textColor = new Color(0.3f, 1f, 0.7f, 0.65f + 0.35f * pulse);
                GUI.Label(new Rect(arrowPt.x - 16f * scale, arrowPt.y - 10f * scale, 32f * scale, 20f * scale), "▲", badgeStyle);
            }
            else
            {
                float drop = Mathf.Repeat(Time.time * 1.4f, 1f);
                Vector2 arrowPt = ToScreenPt(new Vector2(110f, Mathf.Lerp(438f, 466f, drop)));
                badgeStyle.normal.textColor = new Color(0.3f, 1f, 0.7f, 0.65f + 0.35f * pulse);
                GUI.Label(new Rect(arrowPt.x - 16f * scale, arrowPt.y - 10f * scale, 32f * scale, 20f * scale), "▼", badgeStyle);
            }
        }

        // Si está arrastrando el enchufe, sigue al mouse y hace auto-snap suave al entrar en el puerto
        if (activeDrag == ActiveDragControl.GroundPlug)
        {
            plugOffsetPx = new Vector2(
                Mathf.Clamp(mousePanel.x, 62f, 166f),
                Mathf.Clamp(mousePanel.y, 344f, 526f)
            );

            if (modoConsola == ModoConsola.Apagar && !pararrayosTierraActivo && Vector2.Distance(plugOffsetPx, SocketCenter) <= 44f)
            {
                if (rfPowerPercent > 5f)
                {
                    plugOffsetPx = PlugRestCenter;
                    activeDrag = ActiveDragControl.None;
                    draggingPlug = false;
                    FijarMensajeConsola("Primero baja la Potencia RF al 0% (Paso 1).", SafeModeColor, 3.0f);
                }
                else
                {
                    pararrayosTierraActivo = true;
                    plugOffsetPx = SocketCenter;
                    activeDrag = ActiveDragControl.None;
                    draggingPlug = false;
                    FijarMensajeConsola("Enchufe conectado. Ahora mantén ▼ para estacionar el plato (Paso 3).", WaveRelockColor, 3.2f);
                }
            }
            else if (modoConsola == ModoConsola.Encender && pararrayosTierraActivo && Vector2.Distance(plugOffsetPx, SocketCenter) >= 54f)
            {
                if (!TransmisorEncendido || platoEstacionado)
                {
                    plugOffsetPx = SocketCenter;
                    activeDrag = ActiveDragControl.None;
                    draggingPlug = false;
                    FijarMensajeConsola("Primero enciende energía (1) y sube el plato (2).", SafeModeColor, 3.0f);
                }
                else
                {
                    pararrayosTierraActivo = false;
                    plugOffsetPx = PlugRestCenter;
                    activeDrag = ActiveDragControl.None;
                    draggingPlug = false;
                    FijarMensajeConsola("Tierra desconectada. Sube la Potencia RF al 100% (Paso 4).", WaveRelockColor, 3.0f);
                }
            }
        }
        else if (!draggingPlug)
        {
            plugOffsetPx = pararrayosTierraActivo ? SocketCenter : PlugRestCenter;
        }

        // Cable naranja flexible conectando el dock inferior con la base del enchufe
        DibujarCableTierra(ToScreenPt(CableAnchor), ToScreenPt(plugOffsetPx + new Vector2(0f, 34f)), scale, pararrayosTierraActivo);

        // Enchufe naranja arrastrable
        Rect plugScreenRect = ToScreen(plugOffsetPx.x - 36f, plugOffsetPx.y - 41f, 72f, 82f);
        if (groundPlugTex != null) GUI.DrawTexture(plugScreenRect, groundPlugTex);
        else { GUI.color = new Color(0.95f, 0.45f, 0.1f, 1f); GUI.DrawTexture(plugScreenRect, solidTex); GUI.color = prev; }

        if (ev.type == EventType.MouseDown && ev.button == 0 && plugScreenRect.Contains(ev.mousePosition))
        {
            if (modoConsola == ModoConsola.EsperandoTormenta)
            {
                FijarMensajeConsola("Antena protegida. Espera a que termine la tormenta.", SafeModeColor, 2.5f);
            }
            else if (modoConsola == ModoConsola.Apagar && pararrayosTierraActivo)
            {
                FijarMensajeConsola("Tierra ya conectada. Sigue al Paso 3: Estacionar Plato.", WaveRelockColor, 2.5f);
            }
            else if (modoConsola == ModoConsola.Encender && !pararrayosTierraActivo)
            {
                FijarMensajeConsola("Tierra ya desconectada. Sigue al Paso 4: Subir RF al 100%.", WaveRelockColor, 2.5f);
            }
            else
            {
                activeDrag = ActiveDragControl.GroundPlug;
                draggingPlug = true;
            }
            ev.Use();
        }

        // =========================================================================
        // PASO 3: CRUCETA / JOYSTICK ORIENTACIÓN + RADAR PLATO Y SEGUROS
        // =========================================================================
        panelTitleStyle.normal.textColor = p3Done ? WaveRelockColor : (b3Actual ? WaveNormalColor : new Color(0.82f, 0.88f, 0.94f, 1f));
        GUI.Label(ToScreen(504f, 64f, 356f, 24f),
            modoConsola == ModoConsola.Encender
                ? (p3Done ? "✓ 2. PLATO AL CIELO" : "2. MANTÉN ▲ PARA SUBIR EL PLATO")
                : (p3Done ? "✓ 3. PLATO ESTACIONADO" : "3. MANTÉN ▼ PARA ESTACIONAR EL PLATO"),
            panelTitleStyle);

        DibujarRadarPlato(ToScreenPt, scale);

        // Columna de Joystick y botones mantener ▲ / ▼ en (736..856, 104..298)
        Rect upBtnRect = ToScreen(736f, 104f, 120f, 44f);
        Rect joyBaseRect = ToScreen(768f, 158f, 56f, 56f);
        Rect downBtnRect = ToScreen(736f, 224f, 120f, 44f);

        if (ev.type == EventType.MouseDown && ev.button == 0)
        {
            if (upBtnRect.Contains(ev.mousePosition))
            {
                activeDrag = ActiveDragControl.DishUp;
                ev.Use();
            }
            else if (downBtnRect.Contains(ev.mousePosition))
            {
                activeDrag = ActiveDragControl.DishDown;
                ev.Use();
            }
        }

        // Botones táctiles con borde limpio
        GUI.color = (activeDrag == ActiveDragControl.DishUp) ? WaveNormalColor : new Color(0.14f, 0.24f, 0.34f, 1f);
        GUI.DrawTexture(upBtnRect, solidTex);
        GUI.color = (activeDrag == ActiveDragControl.DishDown) ? WaveNormalColor : new Color(0.14f, 0.24f, 0.34f, 1f);
        GUI.DrawTexture(downBtnRect, solidTex);

        // Base circular del joystick con palanca móvil
        GUI.color = new Color(0.08f, 0.12f, 0.18f, 1f);
        GUI.DrawTexture(joyBaseRect, discTex);
        float stickYShift = (activeDrag == ActiveDragControl.DishUp) ? -12f : ((activeDrag == ActiveDragControl.DishDown) ? 12f : 0f);
        GUI.color = new Color(0.88f, 0.48f, 0.24f, 1f);
        GUI.DrawTexture(ToScreen(780f, 170f + stickYShift, 32f, 32f), discTex);
        GUI.color = prev;

        badgeStyle.normal.textColor = Color.white;
        GUI.Label(upBtnRect, "▲ SUBIR\n(AL CIELO)", badgeStyle);
        GUI.Label(downBtnRect, "▼ BAJAR\n(REPOSO)", badgeStyle);

        tinyLabelStyle.normal.textColor = platoEstacionado ? WaveRelockColor : new Color(0.75f, 0.9f, 1f, 1f);
        GUI.Label(ToScreen(732f, 276f, 128f, 22f), platoEstacionado ? "🔒 PARALELO 90°" : $"Ángulo: {dishAngleDeg:F0}°", tinyLabelStyle);

        // =========================================================================
        // PASO 4: TAPA PROTECTORA TRASLÚCIDA + BOTÓN ROJO MANTENER 2 SEGUNDOS
        // =========================================================================
        panelTitleStyle.normal.textColor = p4Done ? WaveRelockColor : (b4Actual ? WaveNormalColor : new Color(0.82f, 0.88f, 0.94f, 1f));
        GUI.Label(ToScreen(364f, 306f, 224f, 24f),
            modoConsola == ModoConsola.Encender
                ? (p4Done ? "✓ 1. ENERGÍA ENCENDIDA" : "1. TAPA Y BOTÓN (2s ON)")
                : (p4Done ? "✓ 4. MODO SEGURO ACTIVO" : "4. TAPA Y BOTÓN (2s OFF)"),
            panelTitleStyle);

        // Botón rojo grande en (RedBtnCenter)
        Rect redBtnRect = ToScreen(RedBtnCenter.x - 63f, RedBtnCenter.y - 63f, 126f, 126f);
        float pressShrink = activeDrag == ActiveDragControl.RedButtonHold ? 0.94f : 1.0f;
        Rect redDrawRect = ToScreen(RedBtnCenter.x - 63f * pressShrink, RedBtnCenter.y - 63f * pressShrink, 126f * pressShrink, 126f * pressShrink);

        if (redButtonTex != null) GUI.DrawTexture(redDrawRect, redButtonTex);
        else { GUI.color = Color.red; GUI.DrawTexture(redDrawRect, discTex); GUI.color = prev; }

        if (holdRedButtonSeconds > 0.01f)
        {
            float chargeFrac = Mathf.Clamp01(holdRedButtonSeconds / 2.0f);
            DibujarAnilloProgreso(ToScreenPt(RedBtnCenter), 67f * scale, chargeFrac, 7f * scale, WaveRelockColor);
        }

        badgeStyle.normal.textColor = Color.white;
        string redTxt = holdRedButtonSeconds > 0.05f
            ? $"MANTÉN...\n{Mathf.Clamp01(holdRedButtonSeconds / 2.0f) * 100f:F0}%"
            : (modoConsola == ModoConsola.Encender ? "ENCENDER\n(2s)" : "APAGAR\n(2s)");
        GUI.Label(redDrawRect, redTxt, badgeStyle);

        // Tapa protectora traslúcida con bisagra superior: al abrirse sube a y=336 sin tapar el botón
        float closedY = RedBtnCenter.y - 58f;
        float openY = 336f;
        float coverY = Mathf.Lerp(closedY, openY, tapaOpenAnim);
        float coverH = Mathf.Lerp(114f, 26f, tapaOpenAnim);
        Rect coverRect = ToScreen(RedBtnCenter.x - 84f, coverY, 168f, coverH);

        if (hazardCoverTex != null) GUI.DrawTexture(coverRect, hazardCoverTex);
        else { GUI.color = SafeModeColor; GUI.DrawTexture(coverRect, solidTex); GUI.color = prev; }

        if (ev.type == EventType.MouseDown && ev.button == 0)
        {
            if (!tapaSeguridadAbierta && coverRect.Contains(ev.mousePosition))
            {
                bool pasosPreviosOk = modoConsola == ModoConsola.Apagar
                    ? (rfPowerPercent <= 5f && pararrayosTierraActivo && platoEstacionado)
                    : (modoConsola == ModoConsola.Encender);

                if (modoConsola == ModoConsola.EsperandoTormenta)
                {
                    FijarMensajeConsola("Tormenta CME en curso. Espera unos segundos a que se despeje.", SafeModeColor, 2.5f);
                }
                else if (!pasosPreviosOk)
                {
                    FijarMensajeConsola("Completa primero los pasos anteriores antes de levantar la tapa.", SafeModeColor, 3.2f);
                }
                else
                {
                    tapaSeguridadAbierta = true;
                    FijarMensajeConsola("Tapa levantada. Ahora mantén presionado el botón rojo por 2 segundos.", WaveNormalColor, 3.0f);
                }
                ev.Use();
            }
            else if (tapaSeguridadAbierta && redBtnRect.Contains(ev.mousePosition))
            {
                if (modoConsola == ModoConsola.Apagar || (modoConsola == ModoConsola.Encender && !TransmisorEncendido))
                {
                    activeDrag = ActiveDragControl.RedButtonHold;
                }
                ev.Use();
            }
        }

        // =========================================================================
        // MEDIDOR ANALÓGICO DE SOBRECARGA / PELIGRO (ABAJO DERECHA)
        // =========================================================================
        DibujarMedidorSobrecarga(ToScreenPt, ToScreen, scale);

        // Barra de estado inferior amplia, legible y con borde luminoso + botón cerrar
        Rect footerBg = ToScreen(0f, BaseH + 10f, BaseW - 150f, 48f);
        bool tieneAlertaReciente = !string.IsNullOrEmpty(mensajeConsola) && Time.time < mensajeConsolaHasta;
        string footerMsg = tieneAlertaReciente
            ? mensajeConsola
            : (modoConsola == ModoConsola.Encender
                ? "Sigue los recuadros iluminados para encender paso a paso (sin límite de tiempo)."
                : (modoConsola == ModoConsola.EsperandoTormenta
                    ? "Transmisor protegido en Modo Seguro. Espera unos segundos a que pase la nube."
                    : "Sigue los recuadros iluminados en orden: 1 → 2 → 3 → 4."));
        Color msgColor = tieneAlertaReciente
            ? colorMensajeConsola
            : (modoConsola == ModoConsola.EsperandoTormenta ? SafeModeColor : new Color(0.92f, 0.97f, 1f, 1f));

        GUI.color = new Color(0.05f, 0.08f, 0.14f, 0.96f);
        GUI.DrawTexture(footerBg, solidTex);
        GUI.color = new Color(msgColor.r, msgColor.g, msgColor.b, 0.85f);
        GUI.DrawTexture(new Rect(footerBg.x, footerBg.y, 5f * scale, footerBg.height), solidTex);
        GUI.DrawTexture(new Rect(footerBg.x, footerBg.y, footerBg.width, 2f * scale), solidTex);
        GUI.color = prev;

        bodyStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(17 * scale));
        bodyStyle.fontStyle = FontStyle.Bold;
        bodyStyle.alignment = TextAnchor.MiddleCenter;
        bodyStyle.normal.textColor = msgColor;
        GUI.Label(new Rect(footerBg.x + 14f, footerBg.y + 4f, footerBg.width - 28f, footerBg.height - 8f), footerMsg, bodyStyle);

        buttonStyle.fontSize = Mathf.Max(13, Mathf.RoundToInt(15 * scale));
        if (GUI.Button(ToScreen(BaseW - 140f, BaseH + 10f, 140f, 48f), "Cerrar [Esc]", buttonStyle))
        {
            CerrarConsola();
        }
    }

    private void DibujarOsciloscopioOnda(System.Func<Vector2, Vector2> toScreenPt, float scale)
    {
        float left = 172f;
        float right = 442f;
        float centerY = 192f;
        float frac = rfPowerPercent / 100f;

        Color prev = GUI.color;
        Vector2 gL = toScreenPt(new Vector2(left, centerY));
        Vector2 gR = toScreenPt(new Vector2(right, centerY));
        DibujarLinea(gL, gR, 1.5f * scale, new Color(0.25f, 0.55f, 0.65f, 0.35f));

        // Onda de radio viva: alta amplitud y rapidez al 100% -> línea plana verde al 0%
        int segments = 34;
        float amp = Mathf.Lerp(0f, 36f, frac);
        float freq = Mathf.Lerp(1.5f, 6.0f, frac);
        float speed = Mathf.Lerp(1.0f, 11.5f, frac);
        Color waveColor = frac <= 0.05f
            ? WaveRelockColor
            : Color.Lerp(WaveNormalColor, new Color(0.95f, 0.38f, 0.92f, 1f), 0.5f + 0.5f * Mathf.Sin(Time.time * 4f));

        Vector2 prevPt = Vector2.zero;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float x = Mathf.Lerp(left, right, t);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float y = centerY + Mathf.Sin(t * freq * Mathf.PI * 2f - Time.time * speed) * amp * envelope;
            Vector2 pt = toScreenPt(new Vector2(x, y));

            if (i > 0)
            {
                DibujarLinea(prevPt, pt, 3.2f * scale, waveColor);
            }
            prevPt = pt;
        }

        Vector2 lblPos = toScreenPt(new Vector2(166f, 104f));
        badgeStyle.normal.textColor = frac <= 0.05f ? WaveRelockColor : waveColor;
        GUI.Label(
            new Rect(lblPos.x, lblPos.y, 282f * scale, 22f * scale),
            frac <= 0.05f ? "✓ ONDA PLANA (0% SEGURO)" : $"ONDA DE RADIO: {rfPowerPercent:F0}%",
            badgeStyle
        );
        GUI.color = prev;
    }

    private void DibujarMiniDiagramaTierra(System.Func<Vector2, Vector2> toScreenPt, float scale)
    {
        // Suelo marciano rojo dentro de (174 .. 316, y = 492 .. 514)
        Vector2 groundL = toScreenPt(new Vector2(178f, 492f));
        Vector2 groundR = toScreenPt(new Vector2(312f, 492f));
        GUI.color = new Color(0.72f, 0.25f, 0.14f, 1f);
        GUI.DrawTexture(new Rect(groundL.x, groundL.y, groundR.x - groundL.x, 20f * scale), solidTex);

        // Mini antena estrictamente debajo de la barra de estado (y >= 384)
        Vector2 dishPt = toScreenPt(new Vector2(245f, 412f));
        Vector2 mastBase = toScreenPt(new Vector2(245f, 474f));
        Vector2 soilSink = toScreenPt(new Vector2(245f, 500f));
        DibujarLinea(dishPt, mastBase, 3.5f * scale, new Color(0.75f, 0.82f, 0.90f, 1f));
        GUI.color = new Color(0.88f, 0.90f, 0.94f, 1f);
        GUI.DrawTexture(new Rect(dishPt.x - 16f * scale, dishPt.y - 5f * scale, 32f * scale, 9f * scale), solidTex);

        // Rayos de plasma entrando desde y = 378 hasta el plato
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 12f);
        Vector2 boltTop = toScreenPt(new Vector2(232f, 378f));
        Vector2 boltMid = toScreenPt(new Vector2(254f, 393f));
        Color boltCol = pararrayosTierraActivo
            ? Color.Lerp(new Color(1f, 0.85f, 0.25f, 1f), WaveRelockColor, 0.4f)
            : Color.Lerp(WaveInterferenceColor, Color.white, pulse);
        DibujarLinea(boltTop, boltMid, 2.5f * scale, boltCol);
        DibujarLinea(boltMid, dishPt, 2.5f * scale, boltCol);

        if (pararrayosTierraActivo)
        {
            float travel = Mathf.Repeat(Time.time * 2.2f, 1f);
            Vector2 sparkPos = Vector2.Lerp(dishPt, soilSink, travel);
            DibujarLinea(mastBase, soilSink, 4.0f * scale, new Color(1f, 0.82f, 0.22f, 1f));
            GUI.color = Color.yellow;
            GUI.DrawTexture(new Rect(sparkPos.x - 5f * scale, sparkPos.y - 5f * scale, 10f * scale, 10f * scale), discTex);
            GUI.color = WaveRelockColor;
            GUI.DrawTexture(new Rect(soilSink.x - (14f + pulse * 10f) * scale, soilSink.y - 2f * scale, (28f + pulse * 20f) * scale, 4f * scale), solidTex);

            badgeStyle.normal.textColor = WaveRelockColor;
            Vector2 lbl = toScreenPt(new Vector2(175f, 346f));
            GUI.Label(new Rect(lbl.x, lbl.y, 140f * scale, 22f * scale), "✓ DESCARGA SEGURA", badgeStyle);
        }
        else
        {
            GUI.color = new Color(1f, 0.25f, 0.45f, 0.5f + 0.5f * pulse);
            GUI.DrawTexture(new Rect(mastBase.x - 13f * scale, mastBase.y - 10f * scale, 26f * scale, 18f * scale), discTex);
            badgeStyle.normal.textColor = WaveInterferenceColor;
            Vector2 lbl = toScreenPt(new Vector2(175f, 346f));
            GUI.Label(new Rect(lbl.x, lbl.y, 140f * scale, 22f * scale), "¡SIN TIERRA!", badgeStyle);
        }
        GUI.color = Color.white;
    }

    private void DibujarCableTierra(Vector2 startScreen, Vector2 endScreen, float scale, bool energizado)
    {
        int segs = 16;
        Vector2 mid = (startScreen + endScreen) * 0.5f + new Vector2(-24f * scale, 22f * scale);
        Color outer = new Color(0.18f, 0.09f, 0.04f, 1f);
        Color inner = energizado ? new Color(1f, 0.68f, 0.20f, 1f) : new Color(0.94f, 0.44f, 0.14f, 1f);

        Vector2 prev = startScreen;
        for (int i = 1; i <= segs; i++)
        {
            float t = i / (float)segs;
            float omt = 1f - t;
            Vector2 p = omt * omt * startScreen + 2f * omt * t * mid + t * t * endScreen;
            DibujarLinea(prev, p, 11f * scale, outer);
            DibujarLinea(prev, p, 6.5f * scale, inner);
            prev = p;
        }
    }

    private void DibujarRadarPlato(System.Func<Vector2, Vector2> toScreenPt, float scale)
    {
        Vector2 groundRefL = toScreenPt(new Vector2(534f, 276f));
        Vector2 groundRefR = toScreenPt(new Vector2(684f, 276f));
        DibujarLinea(groundRefL, groundRefR, 3f * scale, new Color(0.75f, 0.35f, 0.22f, 0.9f));

        Vector2 pivot = toScreenPt(new Vector2(609f, 238f));
        DibujarLinea(pivot, toScreenPt(new Vector2(593f, 276f)), 4f * scale, new Color(0.55f, 0.65f, 0.75f, 1f));
        DibujarLinea(pivot, toScreenPt(new Vector2(625f, 276f)), 4f * scale, new Color(0.55f, 0.65f, 0.75f, 1f));

        Matrix4x4 prevMat = GUI.matrix;
        GUIUtility.RotateAroundPivot(dishAngleDeg, pivot);

        if (radarDishTex != null)
        {
            Rect dishRect = new Rect(pivot.x - 56f * scale, pivot.y - 72f * scale, 112f * scale, 84f * scale);
            GUI.color = platoEstacionado ? WaveRelockColor : Color.white;
            GUI.DrawTexture(dishRect, radarDishTex);
        }
        else
        {
            Rect dishBar = new Rect(pivot.x - 46f * scale, pivot.y - 28f * scale, 92f * scale, 14f * scale);
            GUI.color = platoEstacionado ? WaveRelockColor : new Color(0.88f, 0.92f, 0.96f, 1f);
            GUI.DrawTexture(dishBar, solidTex);
        }
        GUI.matrix = prevMat;

        if (platoEstacionado)
        {
            GUI.color = WaveRelockColor;
            GUI.DrawTexture(new Rect(pivot.x - 58f * scale, pivot.y - 34f * scale, 10f * scale, 22f * scale), solidTex);
            GUI.DrawTexture(new Rect(pivot.x + 48f * scale, pivot.y - 34f * scale, 10f * scale, 22f * scale), solidTex);
            badgeStyle.normal.textColor = WaveRelockColor;
            Vector2 lbl = toScreenPt(new Vector2(510f, 102f));
            GUI.Label(new Rect(lbl.x, lbl.y, 198f * scale, 22f * scale), "✓ ¡CLIC! SEGURO 90°", badgeStyle);
        }
        else if (modoConsola == ModoConsola.Apagar)
        {
            badgeStyle.normal.textColor = WaveNormalColor;
            Vector2 lbl = toScreenPt(new Vector2(510f, 102f));
            GUI.Label(new Rect(lbl.x, lbl.y, 198f * scale, 22f * scale), "MANTÉN ▼ HASTA 90°", badgeStyle);
        }
        else
        {
            badgeStyle.normal.textColor = WaveRelockColor;
            Vector2 lbl = toScreenPt(new Vector2(510f, 102f));
            GUI.Label(new Rect(lbl.x, lbl.y, 198f * scale, 22f * scale), "APUNTANDO AL SATÉLITE", badgeStyle);
        }
        GUI.color = Color.white;
    }

    private void DibujarMedidorSobrecarga(
        System.Func<Vector2, Vector2> toScreenPt,
        System.Func<float, float, float, float, Rect> toScreen,
        float scale)
    {
        var cme = DONKICMEManager.Instance;
        bool cmeActiva = cme != null && cme.HayTormentaCMEActiva;
        float velKmS = (cme != null && cme.VelocidadActualKmS > 0f) ? cme.VelocidadActualKmS : 680f;

        panelTitleStyle.normal.textColor = (modoConsola == ModoConsola.Apagar && cmeActiva && sobrecargaProgreso > 0.7f)
            ? new Color(1f, 0.35f, 0.35f, 1f)
            : new Color(0.95f, 0.90f, 0.70f, 1f);
        GUI.Label(toScreen(630f, 336f, 232f, 22f),
            modoConsola == ModoConsola.Apagar && cmeActiva
                ? $"SOBRECARGA ({velKmS:F0} km/s)"
                : "MEDIDOR DE SOBRECARGA",
            panelTitleStyle);

        if (modoConsola == ModoConsola.Apagar && cmeActiva && sobrecargaProgreso >= 0.70f)
        {
            float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * 14f);
            GUI.color = new Color(1f, 0.18f, 0.22f, blink);
            GUI.DrawTexture(toScreen(836f, 362f, 22f, 22f), discTex);
            GUI.color = Color.white;
        }

        float pctDisplay = (modoConsola == ModoConsola.Apagar && cmeActiva) ? sobrecargaProgreso : 0f;
        float needleAngle = Mathf.Lerp(-165f, -15f, pctDisplay);
        Vector2 pivotScreen = toScreenPt(GaugePivot);

        Matrix4x4 prevMat = GUI.matrix;
        GUIUtility.RotateAroundPivot(needleAngle, pivotScreen);
        Rect needleRect = new Rect(pivotScreen.x - 14f * scale, pivotScreen.y - 14f * scale, 108f * scale, 28f * scale);
        if (gaugeNeedleTex != null) GUI.DrawTexture(needleRect, gaugeNeedleTex);
        else { GUI.color = new Color(1f, 0.5f, 0.1f, 1f); GUI.DrawTexture(needleRect, solidTex); GUI.color = Color.white; }
        GUI.matrix = prevMat;

        badgeStyle.normal.textColor = pctDisplay > 0.7f ? new Color(1f, 0.4f, 0.4f, 1f) : new Color(0.85f, 0.95f, 0.9f, 1f);
        string statusGauge = (modoConsola == ModoConsola.Encender)
            ? "SIN TIEMPO (SEGURO)"
            : (cmeActiva ? $"{pctDisplay * 100f:F0}% (100% = REINICIA)" : "PRÁCTICA (0%)");
        GUI.Label(toScreen(626f, 506f, 240f, 22f), statusGauge, badgeStyle);
    }

    private void DibujarAnilloProgreso(Vector2 center, float radius, float fraction, float thickness, Color color)
    {
        int totalSegs = 48;
        int activeSegs = Mathf.RoundToInt(totalSegs * Mathf.Clamp01(fraction));
        for (int i = 0; i < activeSegs; i++)
        {
            float a0 = (-90f + (i / (float)totalSegs) * 360f) * Mathf.Deg2Rad;
            float a1 = (-90f + ((i + 1) / (float)totalSegs) * 360f) * Mathf.Deg2Rad;
            Vector2 p0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            Vector2 p1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            DibujarLinea(p0, p1, thickness, color);
        }
    }

    private void DibujarLinea(Vector2 p0, Vector2 p1, float thickness, Color color)
    {
        float len = Vector2.Distance(p0, p1);
        if (len < 0.01f) return;
        float angle = Mathf.Atan2(p1.y - p0.y, p1.x - p0.x) * Mathf.Rad2Deg;

        Color prevC = GUI.color;
        Matrix4x4 prevM = GUI.matrix;
        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, p0);
        GUI.DrawTexture(new Rect(p0.x, p0.y - thickness * 0.5f, len, thickness), solidTex);
        GUI.matrix = prevM;
        GUI.color = prevC;
    }

    private void DibujarTarjetaEducativa()
    {
        const float cardW = 800f;
        const float cardH = 545f;
        Rect cardRect = new Rect((Screen.width - cardW) * 0.5f, (Screen.height - cardH) * 0.5f, cardW, cardH);

        Color prev = GUI.color;
        GUI.color = new Color(0.05f, 0.1f, 0.16f, 0.96f);
        GUI.DrawTexture(cardRect, solidTex);

        GUI.color = new Color(0.22f, 0.92f, 0.55f, 1f);
        GUI.DrawTexture(new Rect(cardRect.x, cardRect.y, cardRect.width, 4f), solidTex);
        GUI.color = prev;

        headerStyle.fontSize = 35;
        headerStyle.normal.textColor = new Color(0.45f, 0.95f, 1f, 1f);
        GUI.Label(new Rect(cardRect.x + 20f, cardRect.y + 14f, cardRect.width - 40f, 28f), tarjetaTitulo, headerStyle);
        bodyStyle.alignment = TextAnchor.MiddleLeft;
        bodyStyle.fontStyle = FontStyle.Normal;
        bodyStyle.fontSize = 25;
        bodyStyle.normal.textColor = new Color(0.92f, 0.96f, 1f, 1f);
        GUI.Label(new Rect(cardRect.x + 22f, cardRect.y + 50f, cardRect.width - 44f, 230f), tarjetaCuerpo, bodyStyle);

        buttonStyle.fontSize = 16;
        if (GUI.Button(new Rect(cardRect.x + (cardRect.width - 180f) * 0.5f, cardRect.y + cardRect.height - 50f, 180f, 36f), "¡Entendido! Continuar", buttonStyle))
        {
            mostrarTarjetaEducativa = false;
        }
    }

    private void EnsureTextures()
    {
        if (consoleChassisTex == null) consoleChassisTex = Resources.Load<Texture2D>("HighGainAntenna/hga_panel_chassis");
        if (faderKnobTex == null) faderKnobTex = Resources.Load<Texture2D>("HighGainAntenna/hga_fader_knob");
        if (groundPlugTex == null) groundPlugTex = Resources.Load<Texture2D>("HighGainAntenna/hga_ground_plug");
        if (hazardCoverTex == null) hazardCoverTex = Resources.Load<Texture2D>("HighGainAntenna/hga_hazard_cover");
        if (redButtonTex == null) redButtonTex = Resources.Load<Texture2D>("HighGainAntenna/hga_red_button");
        if (gaugeNeedleTex == null) gaugeNeedleTex = Resources.Load<Texture2D>("HighGainAntenna/hga_gauge_needle");
        if (radarDishTex == null) radarDishTex = Resources.Load<Texture2D>("HighGainAntenna/hga_radar_dish");

        if (solidTex == null)
        {
            solidTex = new Texture2D(1, 1);
            solidTex.SetPixel(0, 0, Color.white);
            solidTex.Apply();
        }

        if (discTex == null)
        {
            const int sz = 48;
            discTex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float half = (sz - 1) * 0.5f;
            var px = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                    float a = Mathf.Clamp01((1f - d) * 8f);
                    px[y * sz + x] = new Color(1f, 1f, 1f, a);
                }
            }
            discTex.SetPixels(px);
            discTex.Apply();
        }
    }

    private void EnsureStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (panelTitleStyle == null)
        {
            panelTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }

        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                fontSize = 12,
                normal = { textColor = new Color(0.92f, 0.96f, 1f, 1f) }
            };
        }

        if (tinyLabelStyle == null)
        {
            tinyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }

        if (badgeStyle == null)
        {
            badgeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };
        }

        if (worldTitleStyle == null)
        {
            worldTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Overflow
            };
        }

        if (worldSubStyle == null)
        {
            worldSubStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                normal = { textColor = new Color(0.9f, 0.95f, 1f, 0.95f) }
            };
        }
    }
}

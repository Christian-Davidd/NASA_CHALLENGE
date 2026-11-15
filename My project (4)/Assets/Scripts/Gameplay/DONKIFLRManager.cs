using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using MarteSim.CicloDiaNoche;

/// <summary>
/// Gestor central de Clima Espacial FLR (/DONKI/FLR) y Supervivencia Realista en Marte:
/// - Llamaradas solares / Tormentas de Calor (FLR) desde nasa-data/estadisticas_tormenta_marte.json y NASA CCMC.
/// - Preaviso dorado/naranja de 3 segundos antes del pico de calor y radiación.
/// - Energía: baja SIEMPRE en cualquier escena (traje/calefacción/soporte) y sube progresivamente
///   al mantener aplastado [E] en los Paneles Solares. Llegar a 0% = derrota.
/// - Agua: baja continuamente (hidratación), al regar cultivos y fuertemente durante tormentas de calor/FLR
///   (sublimador del traje LCVG). Se consigue filtrando orina/humedad en la Nave (UPA) o extrayendo
///   hielo subsuperficial (ISRU) en los nodos del mapa. Llegar a 0% = derrota.
/// - Oxígeno: baja en el exterior (95% CO2 marciano); se recarga en la Nave con el generador MOXIE. Llegar a 0% = derrota.
/// - Control Térmico del Traje: se degrada en tormentas de calor/FLR; requiere mantenimiento y mejora en la Nave.
/// </summary>
public sealed class DONKIFLRManager : MonoBehaviour
{
    public static DONKIFLRManager Instance { get; private set; }

    [Header("Configuración de Red NASA FLR")]
    [SerializeField] private string apiKey = "DEMO_KEY";
    [SerializeField] private string githubRawJsonUrl =
        "https://raw.githubusercontent.com/Christian-Davidd/NASA_CHALLENGE/main/nasa-data/estadisticas_tormenta_marte.json";
    [SerializeField] private float cacheVigenciaHoras = 6f;

    [Header("Temporización de Llamaradas / Tormentas de Calor (FLR)")]
    [SerializeField] private float preavisoSegundos = 8f;
    [SerializeField] private float primerEventoFLRSegundos = 84f;
    [SerializeField] private float duracionPartidaSeg = 420f;
    [SerializeField] private int totalDiasJuego = 5;

    [Header("Consumos Continuos Realistas (%/seg)")]
    [SerializeField] private float consumoBaseEnergiaPorSeg = 0.09f;
    [SerializeField] private float consumoExtraInvernaderoPorSeg = 0.04f;
    [SerializeField] private float consumoExtraAntenaCMEPorSeg = 0.18f;
    [SerializeField] private float consumoBaseAguaExteriorPorSeg = 0.08f;
    [SerializeField] private float consumoBaseAguaInteriorPorSeg = 0.035f;
    [SerializeField] private float consumoOxigenoExteriorPorSeg = 0.16f;

    [Header("Recargas Progresivas al Mantener [E] (%/seg)")]
    [SerializeField] private float recargaSolarEnergiaPorSeg = 8f;
    [SerializeField] private float recargaMoxieOxigenoPorSeg = 16f;
    [SerializeField] private float costoEnergiaMoxiePorSeg = 2f;
    [SerializeField] private float recargaFiltroOrinaAguaPorSeg = 12f;
    [SerializeField] private float costoEnergiaFiltroOrinaPorSeg = 2.2f;
    [SerializeField] private float extraccionHieloISRUPorSeg = 9f;
    [SerializeField] private float costoEnergiaISRUPorSeg = 2.5f;

    private const string PREF_FLR_CACHE_JSON = "DONKI_FLR_CACHE_JSON_V1";
    private const string PREF_FLR_CACHE_TIME = "DONKI_FLR_CACHE_TIME_V1";

    // Eventos públicos solicitados
    public event Action<string, float> OnFLRIniciada;
    public event Action OnFLRFinalizada;
    public event Action<int> OnNuevoDia;
    public event Action<string> OnAvisoSistemaEnergia;

    // Estadísticas NASA
    public EstadisticasFLRNasa StatsActuales { get; private set; }
    public string FuenteDatosActual { get; private set; } = "Cargando FLR...";
    public int TotalLlamaradasSemana => StatsActuales != null ? StatsActuales.total_llamaradas_semana : 7;
    public float ProbabilidadEventoPorDia => StatsActuales != null ? StatsActuales.probabilidad_evento_por_dia : 0.6421f;
    public float DuracionMediaTormentaSeg => StatsActuales != null ? StatsActuales.duracion_media_tormenta_seg : 16.8f;

    // Estado FLR (Llamarada Solar / Tormenta de Calor)
    public bool HayTormentaFLRActiva { get; private set; }
    public bool EnPreaviso { get; private set; }
    public bool EnPicoRadiacion => HayTormentaFLRActiva && !EnPreaviso;
    public string ClaseActual { get; private set; } = "M";
    public string NivelAlertaTexto { get; private set; } = "ALERTA MODERADA (Clase M)";
    public float DuracionTotalActual { get; private set; } = 16.8f;
    public float TiempoRestantePreaviso { get; private set; }
    public float TiempoRestanteEvento { get; private set; }
    public float TiempoParaSiguienteFLR { get; private set; } = 84f;
    public float DrenajeBateriaPorSegActual { get; private set; }
    public float ConsumoAguaTermicoPorSegActual { get; private set; }
    public float DesgasteTermicoPorSegActual { get; private set; }

    // Reloj de misión
    public int DiaActual { get; private set; } = 1;
    public float TiempoTranscurridoPartida { get; private set; }

    // Recursos vitales (0..100%)
    public float EnergiaActual { get; private set; } = 100f;
    public float AguaActual { get; private set; } = 100f;
    public float OxigenoActual { get; private set; } = 100f;
    public float ReservaOrinaHumedad { get; private set; } = 85f;

    // Control Térmico del Traje (LCVG + Sublimador)
    public float EstadoControlTermico { get; private set; } = 100f;
    public int NivelControlTermico { get; private set; } = 1; // 1 a 3
    public const int MaxNivelControlTermico = 3;
    public bool RequiereMantenimientoTermico => EstadoControlTermico < 65f;
    public float IntegridadTraje { get; private set; } = 100f;

    // Estado de derrota
    public bool MisionFallida { get; private set; }
    public string MotivoDerrota { get; private set; } = "";
    public string DetalleDerrota { get; private set; } = "";

    public string UltimoMensajeEmergencia { get; private set; } = "";
    public float MensajeEmergenciaHasta { get; private set; }

    private int indiceClasePrueba;
    private readonly string[] ordenClasesPrueba = { "M", "X", "C" };

    public bool EstaEnInvernaderoInterior =>
        SceneManager.GetActiveScene().name == "Cultivation_Module_Interior";

    public bool EstaEnNaveInterior =>
        SceneManager.GetActiveScene().name == "Spaceship_Interior";

    public bool EstaEnExteriorMarte =>
        SceneManager.GetActiveScene().name == "Mars_Natural_Landscape_01" &&
        !MarsScenePortal2D.IsTransitioningToAnyInterior;

    public bool EstaEnRefugioDomo =>
        EstaEnInvernaderoInterior ||
        EstaEnNaveInterior ||
        MarsScenePortal2D.IsTransitioningToAnyInterior;

    public bool EscudoSobrecargadoSinControlTermico =>
        EnPicoRadiacion && !EstaEnRefugioDomo && EstadoControlTermico <= 5f;

    public float MultiplicadorEficienciaTermica
    {
        get
        {
            float baseNivel = NivelControlTermico switch
            {
                3 => 0.48f,
                2 => 0.70f,
                _ => 1.00f
            };
            float factorDesgaste = Mathf.Lerp(1.75f, 1.0f, Mathf.Clamp01(EstadoControlTermico / 100f));
            return baseNivel * factorDesgaste;
        }
    }

    [Serializable]
    public class ConfiguracionTiempoFLR
    {
        public float duracion_max_partida_seg = 420f;
        public int total_dias_juego = 5;
        public float segundos_por_dia = 84f;
    }

    [Serializable]
    public class DistribucionSeveridadFLR
    {
        public float C = 0.20f;
        public float M = 0.65f;
        public float X = 0.15f;
    }

    [Serializable]
    public class EstadisticasFLRNasa
    {
        public string ultima_actualizacion_utc;
        public int total_llamaradas_semana;
        public float probabilidad_evento_por_dia;
        public float duracion_media_tormenta_seg;
        public ConfiguracionTiempoFLR configuracion_tiempo_juego;
        public DistribucionSeveridadFLR distribucion_severidad;
    }

    [Serializable]
    public class RawFLREvent
    {
        public string flrID = "";
        public string beginTime = "";
        public string peakTime = "";
        public string endTime = "";
        public string classType = "";
    }

    [Serializable]
    public class RawFLRListWrapper
    {
        public RawFLREvent[] items = Array.Empty<RawFLREvent>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureManagerExists()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        if (activeScene != "Mars_Natural_Landscape_01" &&
            activeScene != "Cultivation_Module_Interior" &&
            activeScene != "Spaceship_Interior")
            return;

        if (Instance != null)
        {
            if (Instance.GetComponent<CicloDiaNocheManager>() == null)
                Instance.gameObject.AddComponent<CicloDiaNocheManager>();
            return;
        }

        var existingHud = FindFirstObjectByType<MarsMissionHUD>();
        if (existingHud != null)
        {
            if (existingHud.GetComponent<DONKIFLRManager>() == null)
                existingHud.gameObject.AddComponent<DONKIFLRManager>();
            if (existingHud.GetComponent<CicloDiaNocheManager>() == null)
                existingHud.gameObject.AddComponent<CicloDiaNocheManager>();
            return;
        }

        var go = new GameObject("SpaceWeather_And_HUD_Manager_Runtime");
        go.AddComponent<DONKICMEManager>();
        go.AddComponent<DONKIFLRManager>();
        go.AddComponent<CicloDiaNocheManager>();
        go.AddComponent<MarsMissionHUD>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        CargarEstadisticasPorDefecto();
        TiempoParaSiguienteFLR = primerEventoFLRSegundos;
    }

    private void Start()
    {
        StartCoroutine(FlujoCargaEstadisticasFLR());
    }

    private void Update()
    {
        if (MisionFallida) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            string claseManual = ordenClasesPrueba[indiceClasePrueba % ordenClasesPrueba.Length];
            indiceClasePrueba++;
            DispararLlamaradaFLR(claseManual);
        }

        ActualizarRelojPartida();
        ActualizarTormentaFLR();
        ActualizarRecursosVitales();
    }

    private void ActualizarRelojPartida()
    {
        TiempoTranscurridoPartida += Time.deltaTime;
        float segPorDia = Mathf.Max(12f, duracionPartidaSeg / Mathf.Max(1, totalDiasJuego));
        int diaCalculado = Mathf.Clamp(Mathf.FloorToInt(TiempoTranscurridoPartida / segPorDia) + 1, 1, totalDiasJuego);

        if (diaCalculado != DiaActual)
        {
            DiaActual = diaCalculado;
            OnNuevoDia?.Invoke(DiaActual);
        }
    }

    private void ActualizarTormentaFLR()
    {
        if (HayTormentaFLRActiva)
        {
            if (EnPreaviso)
            {
                TiempoRestantePreaviso -= Time.deltaTime;
                if (TiempoRestantePreaviso <= 0f)
                {
                    TiempoRestantePreaviso = 0f;
                    EnPreaviso = false;
                }
            }
            else
            {
                TiempoRestanteEvento -= Time.deltaTime;
                if (TiempoRestanteEvento <= 0f)
                {
                    FinalizarLlamaradaFLR();
                }
            }
            return;
        }

        TiempoParaSiguienteFLR -= Time.deltaTime;
        if (TiempoParaSiguienteFLR <= 0f)
        {
            // Se conserva la estadística DONKI y se escala su frecuencia para la partida.
            float prob = Mathf.Clamp(ProbabilidadEventoPorDia * 0.70f, 0.10f, 0.65f);
            if (UnityEngine.Random.value <= prob)
            {
                string clase = SortearSeveridad(StatsActuales?.distribucion_severidad);
                DispararLlamaradaFLR(clase);
            }
            else
            {
                TiempoParaSiguienteFLR = CalcularIntervaloEntreVentanas();
            }
        }
    }

    private void ActualizarRecursosVitales()
    {
        float dt = Time.deltaTime;

        // 1. ENERGÍA: siempre baja en toda escena (calefacción marciana -60 °C, presión, luces, electrónica).
        float drenajeEnergia = consumoBaseEnergiaPorSeg;
        if (EstaEnInvernaderoInterior)
            drenajeEnergia += consumoExtraInvernaderoPorSeg;

        // Si las nubes CME están golpeando la Antena HGA encendida, induce sobrecarga eléctrica en la red.
        if (CommsTowerCMEEvent.HaySobrecargaActivaPorCME)
            drenajeEnergia += consumoExtraAntenaCMEPorSeg;

        // 2. OXÍGENO: baja siempre en la superficie marciana (95% CO2); en interiores presurizados se pausa.
        if (EstaEnExteriorMarte)
        {
            OxigenoActual = Mathf.Max(0f, OxigenoActual - consumoOxigenoExteriorPorSeg * dt);
        }

        // 3. AGUA BASE: hidratación continua del astronauta (el vapor/orina recuperable aumenta lentamente).
        float aguaBase = EstaEnExteriorMarte ? consumoBaseAguaExteriorPorSeg : consumoBaseAguaInteriorPorSeg;
        GastarAguaConRetornoOrina(aguaBase * dt);
        ReservaOrinaHumedad = Mathf.Clamp(ReservaOrinaHumedad + 0.22f * dt, 0f, 100f);

        // 4. TORMENTA DE CALOR / LLAMARADA SOLAR (FLR) EN EL EXTERIOR (tras el preaviso):
        //    El traje activa el Control Térmico (LCVG + sublimador de agua) + escudo activo,
        //    lo cual consume AGUA y ENERGÍA y desgasta el Control Térmico (requiere mantenimiento en la Nave).
        if (EnPicoRadiacion && !EstaEnRefugioDomo)
        {
            float ef = MultiplicadorEficienciaTermica;
            drenajeEnergia += DrenajeBateriaPorSegActual * Mathf.Lerp(1f, ef, 0.55f);

            float gastoAguaCalor = ConsumoAguaTermicoPorSegActual * ef * dt;
            GastarAguaConRetornoOrina(gastoAguaCalor);

            float desgaste = DesgasteTermicoPorSegActual * (NivelControlTermico switch { 3 => 0.50f, 2 => 0.72f, _ => 1.0f });
            EstadoControlTermico = Mathf.Max(0f, EstadoControlTermico - desgaste * dt);

            if (EstadoControlTermico <= 0.01f)
            {
                // Sobrecalentamiento crítico del traje al quedarse sin control térmico en plena tormenta
                IntegridadTraje = Mathf.Max(0f, IntegridadTraje - 16f * dt);
            }
        }

        EnergiaActual = Mathf.Max(0f, EnergiaActual - drenajeEnergia * dt);

        // Verificar condiciones reales de derrota
        VerificarCondicionesDerrota();
    }

    private void GastarAguaConRetornoOrina(float cantidad)
    {
        if (cantidad <= 0f) return;
        AguaActual = Mathf.Max(0f, AguaActual - cantidad);
        // En ECLSS/ISS se recupera ~93-98% entre sudor, humedad y orina; aquí acumulamos 85% para que UPA pueda reciclar.
        ReservaOrinaHumedad = Mathf.Clamp(ReservaOrinaHumedad + cantidad * 0.85f, 0f, 100f);
    }

    private void VerificarCondicionesDerrota()
    {
        if (EnergiaActual <= 0.01f)
        {
            EnergiaActual = 0f;
            DispararDerrota(
                "AGOTAMIENTO ELÉCTRICO (ENERGÍA 0%)",
                "Se apagaron la calefacción (-60 °C), la presurización y los sistemas de soporte vital. Recuerda recargar manteniendo [E] en los Paneles Solares y apagar la Antena HGA durante una CME."
            );
            return;
        }

        if (AguaActual <= 0.01f)
        {
            AguaActual = 0f;
            DispararDerrota(
                "DESHIDRATACIÓN CRÍTICA (AGUA 0%)",
                "Se agotó el agua del traje y de reserva térmica. Filtra orina en la estación UPA de la Nave, extrae hielo ISRU afuera y mejora el Control Térmico del traje."
            );
            return;
        }

        if (OxigenoActual <= 0.01f)
        {
            OxigenoActual = 0f;
            DispararDerrota(
                "HIPOXIA EN SUPERFICIE (OXÍGENO 0%)",
                "La atmósfera de Marte tiene 95% de CO₂. Recuerda volver a la Nave a recargar el tanque con el generador MOXIE antes de agotar el oxígeno."
            );
            return;
        }

        if (IntegridadTraje <= 0.01f)
        {
            IntegridadTraje = 0f;
            DispararDerrota(
                "FALLO TÉRMICO DEL TRAJE (0%)",
                "El Control Térmico llegó a 0% durante una tormenta solar. Refúgiate en el Módulo Agrícola/Nave durante el pico y realiza mantenimiento térmico en la Nave."
            );
        }
    }

    private void DispararDerrota(string motivo, string detalle)
    {
        if (MisionFallida) return;
        MisionFallida = true;
        MotivoDerrota = motivo;
        DetalleDerrota = detalle;
    }

    public void AplicarCortocircuitoAntenaCME(float porcentajeEnergiaPerdida, float velKmS)
    {
        if (MisionFallida) return;
        EnergiaActual = Mathf.Max(0f, EnergiaActual - porcentajeEnergiaPerdida);
        MostrarAvisoEmergencia(
            $"⚡ ¡CORTOCIRCUITO CME ({velKmS:F0} km/s) EN LA ANTENA HGA! -{porcentajeEnergiaPerdida:F0}% Energía. ¡Ve a la torre y apágala [E]!",
            5.0f
        );
        VerificarCondicionesDerrota();
    }

    public void DispararLlamaradaFLR(string claseForzada = null)
    {
        if (MisionFallida) return;
        if (StatsActuales == null)
            CargarEstadisticasPorDefecto();

        string clase = string.IsNullOrEmpty(claseForzada)
            ? SortearSeveridad(StatsActuales.distribucion_severidad)
            : claseForzada.ToUpperInvariant();

        if (clase != "C" && clase != "M" && clase != "X")
            clase = "M";

        float duracion = Mathf.Clamp(StatsActuales.duracion_media_tormenta_seg, 12f, 35f);
        float bateriaTotalPct;
        float aguaTermicaTotalPct;
        float desgasteTermicoTotalPct;
        string nivelTexto;

        switch (clase)
        {
            case "X":
                nivelTexto = "TORMENTA DE CALOR / FLR CRÍTICA (Clase X)";
                bateriaTotalPct = 28f;
                aguaTermicaTotalPct = 24f;
                desgasteTermicoTotalPct = 30f;
                break;
            case "M":
                nivelTexto = "TORMENTA DE CALOR / FLR MODERADA (Clase M)";
                bateriaTotalPct = 15f;
                aguaTermicaTotalPct = 12f;
                desgasteTermicoTotalPct = 16f;
                break;
            default:
                nivelTexto = "TORMENTA DE CALOR / FLR MENOR (Clase C)";
                bateriaTotalPct = 7f;
                aguaTermicaTotalPct = 6f;
                desgasteTermicoTotalPct = 8f;
                break;
        }

        ClaseActual = clase;
        NivelAlertaTexto = nivelTexto;
        DuracionTotalActual = duracion;
        DrenajeBateriaPorSegActual = bateriaTotalPct / Mathf.Max(1f, duracion);
        ConsumoAguaTermicoPorSegActual = aguaTermicaTotalPct / Mathf.Max(1f, duracion);
        DesgasteTermicoPorSegActual = desgasteTermicoTotalPct / Mathf.Max(1f, duracion);

        HayTormentaFLRActiva = true;
        EnPreaviso = true;
        TiempoRestantePreaviso = Mathf.Max(1f, preavisoSegundos);
        TiempoRestanteEvento = duracion;

        OnFLRIniciada?.Invoke(ClaseActual, DuracionTotalActual);
    }

    public void FinalizarLlamaradaFLR()
    {
        if (!HayTormentaFLRActiva) return;

        HayTormentaFLRActiva = false;
        EnPreaviso = false;
        TiempoRestantePreaviso = 0f;
        TiempoRestanteEvento = 0f;
        TiempoParaSiguienteFLR = CalcularIntervaloEntreVentanas();

        if (RequiereMantenimientoTermico)
        {
            MostrarAvisoEmergencia(
                $"🌡️ Control Térmico del traje al {EstadoControlTermico:F0}%. Pasa por la Nave para darle mantenimiento (y mejorarlo).",
                4.8f
            );
        }

        OnFLRFinalizada?.Invoke();
    }

    /// <summary>
    /// Recarga progresiva de Energía mientras se mantiene aplastado [E] en los Paneles Solares.
    /// Nunca sube al 100% de golpe en un solo clic.
    /// </summary>
    public string RecargarEnPanelSolarContinuo(string nombrePanel, float deltaTime)
    {
        if (EnergiaActual >= 99.9f)
        {
            EnergiaActual = 100f;
            return $"{nombrePanel}: Batería completa al 100%.";
        }

        float incremento = recargaSolarEnergiaPorSeg * Mathf.Max(0.01f, deltaTime);
        EnergiaActual = Mathf.Clamp(EnergiaActual + incremento, 0f, 100f);

        if (EnergiaActual >= 99.9f)
        {
            EnergiaActual = 100f;
            return $"{nombrePanel}: ¡Energía recargada al 100%!";
        }

        return $"{nombrePanel}: Cargando energía solar... {EnergiaActual:F0}% (mantén [E])";
    }

    /// <summary>
    /// Recarga progresiva de Oxígeno en la Nave (Estación MOXIE: CO₂ -> O₂ usando Energía).
    /// </summary>
    public string RecargarOxigenoMoxieContinuo(float deltaTime)
    {
        if (OxigenoActual >= 99.9f)
        {
            OxigenoActual = 100f;
            return "Generador MOXIE: Tanque de Oxígeno al 100%.";
        }

        float dt = Mathf.Max(0.01f, deltaTime);
        float costoEnergia = costoEnergiaMoxiePorSeg * dt;
        if (EnergiaActual <= costoEnergia + 1f)
        {
            return "⚡ Energía demasiado baja para operar MOXIE. Sal a recargar en los Paneles Solares [E].";
        }

        EnergiaActual = Mathf.Max(1f, EnergiaActual - costoEnergia);
        OxigenoActual = Mathf.Clamp(OxigenoActual + recargaMoxieOxigenoPorSeg * dt, 0f, 100f);
        return $"Generador MOXIE (CO₂ → O₂): Oxígeno {OxigenoActual:F0}% | Energía {EnergiaActual:F0}%";
    }

    /// <summary>
    /// Filtrado progresivo de orina y humedad acumulada en la Nave (Sistema UPA / ECLSS).
    /// </summary>
    public string FiltrarOrinaRecuperarAguaContinuo(float deltaTime)
    {
        if (AguaActual >= 99.9f)
        {
            AguaActual = 100f;
            return "Filtro UPA (Orina → Agua): Depósito de agua al 100%.";
        }

        if (ReservaOrinaHumedad <= 1f)
        {
            return "Filtro UPA: Sin líquido residual para destilar ahora. Extrae hielo ISRU afuera o espera recuperación.";
        }

        float dt = Mathf.Max(0.01f, deltaTime);
        float costoEnergia = costoEnergiaFiltroOrinaPorSeg * dt;
        if (EnergiaActual <= costoEnergia + 1f)
        {
            return "⚡ Energía insuficiente para el destilador UPA. Recarga en los Paneles Solares [E].";
        }

        float posible = Mathf.Min(recargaFiltroOrinaAguaPorSeg * dt, ReservaOrinaHumedad);
        float espacioLibre = 100f - AguaActual;
        float recuperado = Mathf.Min(posible, espacioLibre);

        EnergiaActual = Mathf.Max(1f, EnergiaActual - costoEnergia);
        ReservaOrinaHumedad = Mathf.Max(0f, ReservaOrinaHumedad - recuperado);
        AguaActual = Mathf.Clamp(AguaActual + recuperado, 0f, 100f);

        return $"Filtro UPA (Orina/Humedad): Agua {AguaActual:F0}% | Reserva filtrable {ReservaOrinaHumedad:F0}%";
    }

    /// <summary>
    /// Extracción progresiva de Agua desde nodos exteriores ISRU de Hielo Subsuperficial.
    /// </summary>
    public bool ExtraerAguaHieloISRUContinuo(float deltaTime, ref float reservaNodoHielo, out string mensaje)
    {
        if (AguaActual >= 99.9f)
        {
            AguaActual = 100f;
            mensaje = "Tu reserva de Agua ya está al 100%.";
            return false;
        }

        if (reservaNodoHielo <= 0.5f)
        {
            mensaje = "Depósito ISRU agotado temporalmente. Busca otro cuadro de hielo o usa el filtro UPA en la Nave.";
            return false;
        }

        float dt = Mathf.Max(0.01f, deltaTime);
        float costoEnergia = costoEnergiaISRUPorSeg * dt;
        if (EnergiaActual <= costoEnergia + 2f)
        {
            mensaje = "⚡ Energía muy baja para el taladro térmico ISRU. Recarga en Paneles Solares [E].";
            return false;
        }

        float extraido = Mathf.Min(extraccionHieloISRUPorSeg * dt, Mathf.Min(reservaNodoHielo, 100f - AguaActual));
        EnergiaActual = Mathf.Max(1f, EnergiaActual - costoEnergia);
        reservaNodoHielo = Mathf.Max(0f, reservaNodoHielo - extraido);
        AguaActual = Mathf.Clamp(AguaActual + extraido, 0f, 100f);

        mensaje = $"Taladro ISRU fundiendo hielo subsuperficial → Agua {AguaActual:F0}% (Veta: {reservaNodoHielo:F0}%)";
        return true;
    }

    /// <summary>
    /// Mantenimiento progresivo del Control Térmico del Traje en la Nave (gasta algo de Agua y Energía).
    /// </summary>
    public string RealizarMantenimientoTermicoContinuo(float deltaTime)
    {
        if (EstadoControlTermico >= 99.9f && IntegridadTraje >= 99.9f)
        {
            EstadoControlTermico = 100f;
            IntegridadTraje = 100f;
            return $"Control Térmico (Nv.{NivelControlTermico}) al 100%. Pulsa [E] otra vez para MEJORARLO (si está debajo de Nv.{MaxNivelControlTermico}).";
        }

        float dt = Mathf.Max(0.01f, deltaTime);
        float costoE = 5f * dt;
        float costoA = 3.5f * dt;
        if (EnergiaActual <= costoE + 2f || AguaActual <= costoA + 2f)
        {
            return "Necesitas Energía y Agua para purgar y rellenar el circuito térmico LCVG del traje.";
        }

        EnergiaActual = Mathf.Max(1f, EnergiaActual - costoE);
        GastarAguaConRetornoOrina(costoA);
        EstadoControlTermico = Mathf.Clamp(EstadoControlTermico + 36f * dt, 0f, 100f);
        IntegridadTraje = Mathf.Clamp(IntegridadTraje + 36f * dt, 0f, 100f);

        return $"Mantenimiento LCVG en curso: Control Térmico {EstadoControlTermico:F0}% (Nv.{NivelControlTermico})";
    }

    /// <summary>
    /// Mejora el nivel del Control Térmico del Traje en la Nave (Nv. 1 -> 2 -> 3) consumiendo Agua y Energía.
    /// </summary>
    public bool IntentarMejorarControlTermico(out string mensaje)
    {
        if (NivelControlTermico >= MaxNivelControlTermico)
        {
            mensaje = $"Control Térmico ya está al Nivel Máximo ({MaxNivelControlTermico}/{MaxNivelControlTermico}).";
            return false;
        }

        if (EstadoControlTermico < 95f)
        {
            mensaje = $"Primero mantén [E] para completar el mantenimiento al 100% (actual: {EstadoControlTermico:F0}%).";
            return false;
        }

        const float costoEnergiaMejora = 16f;
        const float costoAguaMejora = 14f;
        if (EnergiaActual < costoEnergiaMejora + 5f || AguaActual < costoAguaMejora + 5f)
        {
            mensaje = $"Para mejorar el Control Térmico necesitas al menos {costoEnergiaMejora:F0}% Energía y {costoAguaMejora:F0}% Agua.";
            return false;
        }

        EnergiaActual -= costoEnergiaMejora;
        GastarAguaConRetornoOrina(costoAguaMejora);
        NivelControlTermico++;
        EstadoControlTermico = 100f;
        IntegridadTraje = 100f;

        mensaje = $"🔧 ¡Control Térmico mejorado a NIVEL {NivelControlTermico}! Ahora consume menos Agua y resiste mejor las tormentas de calor/FLR.";
        return true;
    }

    /// <summary>
    /// Consumos discretos (cultivos, pantallas del invernadero).
    /// </summary>
    public bool IntentarConsumirRecursosCultivo(float costoEnergia, float costoAgua, string accionDescripcion, out string mensajeResultado)
    {
        if (EnergiaActual < costoEnergia + 1f)
        {
            mensajeResultado = $"⚡ Energía insuficiente ({EnergiaActual:F0}% / requiere {costoEnergia:F0}%) para {accionDescripcion}. Recarga en los Paneles Solares [E].";
            MostrarAvisoEmergencia(mensajeResultado, 4f);
            return false;
        }

        if (AguaActual < costoAgua + 1f)
        {
            mensajeResultado = $"💧 Agua insuficiente ({AguaActual:F0}% / requiere {costoAgua:F0}%) para {accionDescripcion}. Filtra orina en la Nave [E] o busca hielo ISRU.";
            MostrarAvisoEmergencia(mensajeResultado, 4f);
            return false;
        }

        EnergiaActual = Mathf.Clamp(EnergiaActual - costoEnergia, 0f, 100f);
        if (costoAgua > 0f)
            GastarAguaConRetornoOrina(costoAgua);

        mensajeResultado = costoAgua > 0f
            ? $"(-{costoEnergia:F0}% ⚡ | -{costoAgua:F0}% 💧)"
            : $"(-{costoEnergia:F0}% ⚡)";
        return true;
    }

    public void MostrarAvisoEmergencia(string texto, float duracion = 3.5f)
    {
        UltimoMensajeEmergencia = texto;
        MensajeEmergenciaHasta = Time.time + duracion;
        OnAvisoSistemaEnergia?.Invoke(texto);
    }

    public void ReiniciarPartidaCompleta()
    {
        MisionFallida = false;
        MotivoDerrota = "";
        DetalleDerrota = "";

        EnergiaActual = 100f;
        AguaActual = 100f;
        OxigenoActual = 100f;
        ReservaOrinaHumedad = 85f;
        EstadoControlTermico = 100f;
        NivelControlTermico = 1;
        IntegridadTraje = 100f;

        HayTormentaFLRActiva = false;
        EnPreaviso = false;
        TiempoRestantePreaviso = 0f;
        TiempoRestanteEvento = 0f;
        TiempoParaSiguienteFLR = primerEventoFLRSegundos;
        TiempoTranscurridoPartida = 0f;
        DiaActual = 1;
        UltimoMensajeEmergencia = "";
        MensajeEmergenciaHasta = 0f;

        DONKICMEManager.Instance?.ReiniciarEstadoPartida();
        CommsTowerCMEEvent.ResetStaticState();
        MarsSampleNode2D.ResetAllSampleState();
        MarsCropPlot2D.ResetAllCrops();
        MarsIceDepositNode2D.ResetAllIceNodes();
        MarsScenePortal2D.CleanupOnMissionRestart();

        SceneManager.LoadScene("Mars_Natural_Landscape_01", LoadSceneMode.Single);
    }

    private float CalcularIntervaloEntreVentanas()
    {
        float segPorDia = Mathf.Max(22f, duracionPartidaSeg / Mathf.Max(1, totalDiasJuego));
        float solesEntreVentanas = TotalLlamaradasSemana >= 10 ? 1.55f
            : TotalLlamaradasSemana >= 4 ? 1.85f
            : 2.15f;
        return Mathf.Clamp(segPorDia * solesEntreVentanas, 120f, 190f);
    }

    private IEnumerator FlujoCargaEstadisticasFLR()
    {
        if (IntentarCargarCacheLocal(out string jsonCache) && ProcesarJsonEstadisticas(jsonCache))
        {
            FuenteDatosActual = "FLR Nivel 1: Caché Local (6h)";
            yield break;
        }

        string rutaLocalRepo = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "..", "nasa-data", "estadisticas_tormenta_marte.json")
        );
        if (File.Exists(rutaLocalRepo))
        {
            try
            {
                string jsonLocal = File.ReadAllText(rutaLocalRepo);
                if (ProcesarJsonEstadisticas(jsonLocal))
                {
                    GuardarCacheLocal(jsonLocal);
                    FuenteDatosActual = "FLR JSON (nasa-data/estadisticas_tormenta_marte.json)";
                    yield break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[DONKI FLR] No se pudo leer estadisticas_tormenta_marte.json local: " + e.Message);
            }
        }

        if (!string.IsNullOrEmpty(githubRawJsonUrl))
        {
            using (UnityWebRequest req = UnityWebRequest.Get(githubRawJsonUrl))
            {
                req.timeout = 7;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success && ProcesarJsonEstadisticas(req.downloadHandler.text))
                {
                    GuardarCacheLocal(req.downloadHandler.text);
                    FuenteDatosActual = "FLR GitHub Raw (estadisticas_tormenta_marte.json)";
                    yield break;
                }
            }
        }

        string endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string startDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        string urlCCMC = $"https://ccmc.gsfc.nasa.gov/DONKI/WS/get/FLR?startDate={startDate}&endDate={endDate}";

        using (UnityWebRequest req = UnityWebRequest.Get(urlCCMC))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success && ProcesarJsonRawDONKIFLR(req.downloadHandler.text))
            {
                FuenteDatosActual = "API Directa NASA CCMC (/DONKI/WS/get/FLR)";
                yield break;
            }
        }

        string urlNasaApi = $"https://api.nasa.gov/DONKI/FLR?startDate={startDate}&endDate={endDate}&api_key={apiKey}";
        using (UnityWebRequest req = UnityWebRequest.Get(urlNasaApi))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success && ProcesarJsonRawDONKIFLR(req.downloadHandler.text))
            {
                FuenteDatosActual = "API NASA DONKI FLR (DEMO_KEY)";
                yield break;
            }
        }

        CargarEstadisticasPorDefecto();
        FuenteDatosActual = "FLR Respaldo Offline";
    }

    private bool ProcesarJsonEstadisticas(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return false;
        string trimmed = rawJson.Trim();
        if (!trimmed.StartsWith("{")) return false;

        try
        {
            var parsed = JsonUtility.FromJson<EstadisticasFLRNasa>(trimmed);
            if (parsed == null || parsed.duracion_media_tormenta_seg <= 0f)
                return false;

            if (parsed.configuracion_tiempo_juego == null)
                parsed.configuracion_tiempo_juego = new ConfiguracionTiempoFLR();
            if (parsed.distribucion_severidad == null)
                parsed.distribucion_severidad = new DistribucionSeveridadFLR();

            StatsActuales = parsed;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[DONKI FLR] Error al parsear estadisticas_tormenta_marte.json: " + e.Message);
            return false;
        }
    }

    private bool ProcesarJsonRawDONKIFLR(string rawArrayJson)
    {
        if (string.IsNullOrWhiteSpace(rawArrayJson)) return false;
        string trimmed = rawArrayJson.Trim();
        if (!trimmed.StartsWith("[")) return false;

        try
        {
            string wrapped = "{\"items\":" + trimmed + "}";
            var parsed = JsonUtility.FromJson<RawFLRListWrapper>(wrapped);
            if (parsed?.items == null || parsed.items.Length == 0)
                return false;

            int countC = 0, countM = 0, countX = 0;
            foreach (var item in parsed.items)
            {
                if (item == null || string.IsNullOrEmpty(item.classType)) continue;
                char c = char.ToUpperInvariant(item.classType[0]);
                if (c == 'X') countX++;
                else if (c == 'M') countM++;
                else if (c == 'C') countC++;
            }

            int total = countC + countM + countX;
            if (total <= 0) return false;

            float solesMarcianos = 7f / 1.02749f;
            float lambda = total / solesMarcianos;
            float probDia = 1f - Mathf.Exp(-lambda);

            StatsActuales = new EstadisticasFLRNasa
            {
                ultima_actualizacion_utc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                total_llamaradas_semana = total,
                probabilidad_evento_por_dia = Mathf.Clamp(probDia, 0.15f, 0.98f),
                duracion_media_tormenta_seg = 16.8f,
                configuracion_tiempo_juego = new ConfiguracionTiempoFLR
                {
                    duracion_max_partida_seg = 420f,
                    total_dias_juego = 5,
                    segundos_por_dia = 84f
                },
                distribucion_severidad = new DistribucionSeveridadFLR
                {
                    C = (float)countC / total,
                    M = (float)countM / total,
                    X = (float)countX / total
                }
            };

            GuardarCacheLocal(JsonUtility.ToJson(StatsActuales));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[DONKI FLR] Error al procesar /DONKI/WS/get/FLR: " + e.Message);
            return false;
        }
    }

    private void CargarEstadisticasPorDefecto()
    {
        StatsActuales = new EstadisticasFLRNasa
        {
            ultima_actualizacion_utc = "2026-11-14T20:34:00Z",
            total_llamaradas_semana = 7,
            probabilidad_evento_por_dia = 0.6421f,
            duracion_media_tormenta_seg = 16.8f,
            configuracion_tiempo_juego = new ConfiguracionTiempoFLR
            {
                duracion_max_partida_seg = 420f,
                total_dias_juego = 5,
                segundos_por_dia = 84f
            },
            distribucion_severidad = new DistribucionSeveridadFLR
            {
                C = 0.0f,
                M = 1.0f,
                X = 0.0f
            }
        };
    }

    private static string SortearSeveridad(DistribucionSeveridadFLR dist)
    {
        if (dist == null) return "M";
        float total = dist.C + dist.M + dist.X;
        if (total <= 0f) return "M";

        float tirada = UnityEngine.Random.value * total;
        if (tirada <= dist.C) return "C";
        if (tirada <= dist.C + dist.M) return "M";
        return "X";
    }

    private bool IntentarCargarCacheLocal(out string json)
    {
        json = null;
        if (!PlayerPrefs.HasKey(PREF_FLR_CACHE_JSON) || !PlayerPrefs.HasKey(PREF_FLR_CACHE_TIME))
            return false;

        string timeStr = PlayerPrefs.GetString(PREF_FLR_CACHE_TIME, "");
        if (!long.TryParse(timeStr, out long binaryTime))
            return false;

        DateTime savedTime = DateTime.FromBinary(binaryTime);
        if ((DateTime.UtcNow - savedTime).TotalHours > cacheVigenciaHoras)
            return false;

        json = PlayerPrefs.GetString(PREF_FLR_CACHE_JSON, "");
        return !string.IsNullOrEmpty(json);
    }

    private static void GuardarCacheLocal(string json)
    {
        PlayerPrefs.SetString(PREF_FLR_CACHE_JSON, json);
        PlayerPrefs.SetString(PREF_FLR_CACHE_TIME, DateTime.UtcNow.ToBinary().ToString());
        PlayerPrefs.Save();
    }
}

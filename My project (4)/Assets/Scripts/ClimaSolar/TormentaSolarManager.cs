using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace MarteSim.ClimaEspacial
{
    public class TormentaSolarManager : MonoBehaviour
    {
        public static TormentaSolarManager Instance { get; private set; }

        [Header("Fuente Centralizada (GitHub Raw)")]
        [SerializeField]
        private string urlJsonRemoto =
            "https://raw.githubusercontent.com/Christian-Davidd/NASA_CHALLENGE/main/nasa-data/estadisticas_tormenta_marte.json";

        [Header("Configuración de Tiempo en Juego")]
        [Tooltip("Usa 60 para pruebas rápidas (12s/día) o 420 para la partida real de 7 min (84s/día)")]
        [SerializeField] private float duracionPartidaSeg = 60f;
        [SerializeField] private int totalDiasJuego = 5;

        [Header("Eventos para conectar en el Inspector (Opcional)")]
        public UnityEvent<int> onNuevoDiaIniciado;
        public UnityEvent<DatosTormentaActiva> onTormentaIniciada;
        public UnityEvent onTormentaFinalizada;

        public event Action<int> OnNuevoDia;
        public event Action<DatosTormentaActiva> OnAlertaTormentaIniciada;
        public event Action<DatosTormentaActiva, float> OnTormentaEnCursoTick;
        public event Action OnAlertaTormentaFinalizada;

        public EstadisticasClimaNasa StatsActuales { get; private set; }
        public bool HayTormentaActiva { get; private set; }
        public int DiaActual { get; private set; } = 0;
        public float TiempoTranscurridoPartida { get; private set; } = 0f;

        private bool _partidaIniciada = false;
        private bool _tormentaProgramadaHoy = false;
        private float _segundoInicioTormentaHoy = -1f;
        private float _segundoFinTormentaHoy = -1f;
        private DatosTormentaActiva _tormentaDelDia;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            StartCoroutine(InicializarPartidaCoroutine());
        }

        private IEnumerator InicializarPartidaCoroutine()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(urlJsonRemoto))
            {
                request.timeout = 8;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    StatsActuales = JsonUtility.FromJson<EstadisticasClimaNasa>(request.downloadHandler.text);
                    Debug.Log($"[NASA DONKI] JSON cargado desde nasa-data (UTC: {StatsActuales.ultima_actualizacion_utc} | Prob/día: {StatsActuales.probabilidad_evento_por_dia:P1})");
                }
                else
                {
                    Debug.LogWarning($"[NASA DONKI] Sin conexión ({request.error}). Usando parámetros offline por defecto.");
                    CargarRespaldoOffline();
                }
            }

            AplicarConfiguracionTiempo();

            TiempoTranscurridoPartida = 0f;
            DiaActual = 0;
            _partidaIniciada = true;
        }

        private void AplicarConfiguracionTiempo()
        {
            float duracionBaseJson = StatsActuales.configuracion_tiempo_juego != null &&
                                     StatsActuales.configuracion_tiempo_juego.duracion_max_partida_seg > 0f
                ? StatsActuales.configuracion_tiempo_juego.duracion_max_partida_seg
                : 420f;

            float factorEscala = duracionPartidaSeg / duracionBaseJson;

            StatsActuales.configuracion_tiempo_juego = new ConfiguracionTiempoJuego
            {
                duracion_max_partida_seg = duracionPartidaSeg,
                total_dias_juego = totalDiasJuego,
                segundos_por_dia = duracionPartidaSeg / totalDiasJuego
            };

            // Escala la duración de la tormenta para que quepa dentro de días cortos (ej. 12s en pruebas de 60s)
            StatsActuales.duracion_media_tormenta_seg = Mathf.Max(3f, StatsActuales.duracion_media_tormenta_seg * factorEscala);
        }

        private void CargarRespaldoOffline()
        {
            StatsActuales = new EstadisticasClimaNasa
            {
                ultima_actualizacion_utc = "OFFLINE_DEFAULT",
                total_llamaradas_semana = 2,
                probabilidad_evento_por_dia = 0.25f,
                duracion_media_tormenta_seg = 22.0f,
                configuracion_tiempo_juego = new ConfiguracionTiempoJuego
                {
                    duracion_max_partida_seg = 420f,
                    total_dias_juego = totalDiasJuego,
                    segundos_por_dia = 84f
                },
                distribucion_severidad = new DistribucionSeveridad
                {
                    C = 0.70f,
                    M = 0.25f,
                    X = 0.05f
                }
            };
        }

        private void Update()
        {
            if (!_partidaIniciada || StatsActuales == null) return;

            float duracionMax = StatsActuales.configuracion_tiempo_juego.duracion_max_partida_seg;
            if (TiempoTranscurridoPartida >= duracionMax) return;

            TiempoTranscurridoPartida += Time.deltaTime;

            float segPorDia = StatsActuales.configuracion_tiempo_juego.segundos_por_dia;
            int totalDias = StatsActuales.configuracion_tiempo_juego.total_dias_juego;
            int diaCalculado = Mathf.Min(totalDias, Mathf.FloorToInt(TiempoTranscurridoPartida / segPorDia) + 1);

            if (diaCalculado > DiaActual)
            {
                DiaActual = diaCalculado;
                OnNuevoDia?.Invoke(DiaActual);
                onNuevoDiaIniciado?.Invoke(DiaActual);
                EvaluarInicioDeDia(DiaActual);
            }

            if (_tormentaProgramadaHoy)
            {
                if (!HayTormentaActiva && TiempoTranscurridoPartida >= _segundoInicioTormentaHoy)
                {
                    HayTormentaActiva = true;
                    OnAlertaTormentaIniciada?.Invoke(_tormentaDelDia);
                    onTormentaIniciada?.Invoke(_tormentaDelDia);
                }

                if (HayTormentaActiva)
                {
                    if (TiempoTranscurridoPartida < _segundoFinTormentaHoy)
                    {
                        float restante = _segundoFinTormentaHoy - TiempoTranscurridoPartida;
                        OnTormentaEnCursoTick?.Invoke(_tormentaDelDia, restante);
                    }
                    else
                    {
                        HayTormentaActiva = false;
                        _tormentaProgramadaHoy = false;
                        OnAlertaTormentaFinalizada?.Invoke();
                        onTormentaFinalizada?.Invoke();
                    }
                }
            }
        }

        private void EvaluarInicioDeDia(int dia)
        {
            HayTormentaActiva = false;
            _tormentaProgramadaHoy = false;

            if (UnityEngine.Random.value > StatsActuales.probabilidad_evento_por_dia)
            {
                Debug.Log($"[Clima Marte] Día {dia}: Despejado.");
                return;
            }

            string clase = SortearSeveridad(StatsActuales.distribucion_severidad);
            float duracion = StatsActuales.duracion_media_tormenta_seg;
            float segPorDia = StatsActuales.configuracion_tiempo_juego.segundos_por_dia;
            float inicioDiaSeg = (dia - 1) * segPorDia;

            float bateriaTotalPct;
            float danoTotalPct;
            string nivelTexto;

            switch (clase)
            {
                case "X":
                    nivelTexto = "ALERTA CRÍTICA (Clase X)";
                    bateriaTotalPct = 75f;
                    danoTotalPct = 85f;
                    break;
                case "M":
                    nivelTexto = "ALERTA MODERADA (Clase M)";
                    bateriaTotalPct = 45f;
                    danoTotalPct = 40f;
                    break;
                default:
                    nivelTexto = "ALERTA MENOR (Clase C)";
                    bateriaTotalPct = 20f;
                    danoTotalPct = 15f;
                    break;
            }

            float margenInicio = Mathf.Min(8f, segPorDia * 0.15f);
            float margenFin = Mathf.Min(5f, segPorDia * 0.10f);
            float maxDesfase = Mathf.Max(margenInicio + 0.5f, segPorDia - duracion - margenFin);

            _segundoInicioTormentaHoy = inicioDiaSeg + UnityEngine.Random.Range(margenInicio, maxDesfase);
            _segundoFinTormentaHoy = _segundoInicioTormentaHoy + duracion;

            _tormentaDelDia = new DatosTormentaActiva
            {
                dia = dia,
                claseSolar = clase,
                nivelAlerta = nivelTexto,
                duracionTotalSeg = duracion,
                drenajeBateriaPorSeg = bateriaTotalPct / duracion,
                danoRadiacionPorSeg = danoTotalPct / duracion
            };

            _tormentaProgramadaHoy = true;
            Debug.Log($"[Clima Marte] Día {dia}: {nivelTexto} programada en el segundo {_segundoInicioTormentaHoy:F1}s.");
        }

        private string SortearSeveridad(DistribucionSeveridad dist)
        {
            float total = dist.C + dist.M + dist.X;
            if (total <= 0f) return "C";

            float tirada = UnityEngine.Random.value * total;
            if (tirada <= dist.C) return "C";
            if (tirada <= dist.C + dist.M) return "M";
            return "X";
        }
    }
}
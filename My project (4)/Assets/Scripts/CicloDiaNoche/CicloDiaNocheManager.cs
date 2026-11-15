using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using MarteSim.ClimaEspacial;

namespace MarteSim.CicloDiaNoche
{
    public class CicloDiaNocheManager : MonoBehaviour
    {
        public static CicloDiaNocheManager Instance { get; private set; }

        [Header("Escala de juego (7 min = 5 soles, 84 s por sol)")]
        [SerializeField] private float duracionTotalPartidaSeg = 420f;
        [SerializeField] private int totalDiasJuego = 5;
        [Range(0f, 1f)]
        [Tooltip("0.25 = 06:00 AM (Amanecer), 0.50 = 12:00 PM (Mediodía)")]
        [SerializeField] private float horaInicialNormalizada = 0.25f;

        [Header("Referencias de Escena")]
        [SerializeField] private Light2D luzGlobal2D;
        [SerializeField] private Material materialSkybox;
        [SerializeField] private Camera camaraPrincipal;

        [Header("Curvas Visuales del Ciclo Marciano")]
        [SerializeField] private Gradient gradienteLuzSolar;
        [SerializeField] private Gradient gradienteSkyboxHorizonte;
        [SerializeField] private AnimationCurve curvaIntensidadLuz;

        [Header("Monitor en Vivo (Solo lectura)")]
        [SerializeField] private int diaActualMonitor = 1;
        [SerializeField] private string relojMarcianoMonitor = "Día 1 - 06:00";
        [SerializeField] private bool esDeNocheMonitor = false;

        [Header("Pruebas Rápidas (Dejar en 1 para el juego real)")]
        [Range(1f, 20f)]
        [SerializeField] private float multiplicadorVelocidad = 1f;

        // Eventos para conectar HUD, luces nocturnas o mecánicas
        public event Action<int> OnNuevoDia;
        public event Action<bool> OnCambioEstadoNoche; // true = Noche, false = Día
        public event Action<float> OnHoraActualizada;  // 0.0 a 1.0

        public int DiaActual { get; private set; } = 0;
        public float TiempoTranscurridoSeg { get; private set; } = 0f;
        public float HoraNormalizada { get; private set; } = 0.25f;
        public bool EsDeNoche { get; private set; } = false;

        private static readonly int PropColorCenit = Shader.PropertyToID("_ColorCenit");
        private static readonly int PropColorHorizonte = Shader.PropertyToID("_ColorHorizonte");
        private static readonly int PropBrillo = Shader.PropertyToID("_BrilloAtmosferico");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (gradienteLuzSolar == null || gradienteSkyboxHorizonte == null || curvaIntensidadLuz == null)
                Reset();

            SceneManager.sceneLoaded += AlCargarEscena;
            VincularReferenciasDeEscena();
        }

        private void Reset()
        {
            // Colores atmosféricos marcianos (incluye el atardecer azulado característico de Marte)
            gradienteLuzSolar = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.10f, 0.13f, 0.28f), 0.00f), // 00:00 Medianoche
                    new GradientColorKey(new Color(0.85f, 0.55f, 0.45f), 0.25f), // 06:00 Amanecer
                    new GradientColorKey(new Color(1.00f, 0.92f, 0.82f), 0.50f), // 12:00 Mediodía
                    new GradientColorKey(new Color(0.42f, 0.50f, 0.78f), 0.75f), // 18:00 Ocaso azul
                    new GradientColorKey(new Color(0.10f, 0.13f, 0.28f), 1.00f)  // 24:00 Medianoche
                },
                alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            };

            gradienteSkyboxHorizonte = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.03f, 0.04f, 0.10f), 0.00f),
                    new GradientColorKey(new Color(0.75f, 0.35f, 0.20f), 0.25f),
                    new GradientColorKey(new Color(0.88f, 0.58f, 0.38f), 0.50f),
                    new GradientColorKey(new Color(0.25f, 0.35f, 0.62f), 0.75f),
                    new GradientColorKey(new Color(0.03f, 0.04f, 0.10f), 1.00f)
                },
                alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            };

            curvaIntensidadLuz = new AnimationCurve(
                new Keyframe(0.00f, 0.22f),
                new Keyframe(0.25f, 0.65f),
                new Keyframe(0.50f, 1.15f),
                new Keyframe(0.75f, 0.55f),
                new Keyframe(1.00f, 0.22f)
            );
        }

        private void Start()
        {
            DiaActual = 1;
            HoraNormalizada = horaInicialNormalizada;
            ActualizarAmbienteVisual();
            EvaluarDiaONoche(forzarEvento: true);
            ActualizarMonitorInspector();
        }

        private void Update()
        {
            // El gestor de misión es el reloj maestro para tormentas, HUD y ciclo solar.
            if (DONKIFLRManager.Instance != null)
            {
                TiempoTranscurridoSeg = DONKIFLRManager.Instance.TiempoTranscurridoPartida;
            }
            else if (TormentaSolarManager.Instance != null && TormentaSolarManager.Instance.TiempoTranscurridoPartida > 0f)
            {
                TiempoTranscurridoSeg = TormentaSolarManager.Instance.TiempoTranscurridoPartida;
            }
            else
            {
                TiempoTranscurridoSeg += Time.deltaTime * multiplicadorVelocidad;
            }

            float segundosPorDia = duracionTotalPartidaSeg / Mathf.Max(1, totalDiasJuego);
            int diaCalculado = Mathf.FloorToInt(TiempoTranscurridoSeg / segundosPorDia) + 1;

            if (diaCalculado != DiaActual)
            {
                DiaActual = diaCalculado;
                OnNuevoDia?.Invoke(DiaActual);
                Debug.Log($"[Ciclo Solar] Inicio del Día {DiaActual} de {totalDiasJuego}");
            }

            float progresoEnElDia = Mathf.Repeat(TiempoTranscurridoSeg / segundosPorDia, 1f);
            HoraNormalizada = (horaInicialNormalizada + progresoEnElDia) % 1f;

            if (EsEscenaExteriorMarte())
                ActualizarAmbienteVisual();
            EvaluarDiaONoche(forzarEvento: false);
            ActualizarMonitorInspector();

            OnHoraActualizada?.Invoke(HoraNormalizada);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            if (Instance == this)
                Instance = null;
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            VincularReferenciasDeEscena();
            if (EsEscenaExteriorMarte())
                ActualizarAmbienteVisual();
        }

        private void VincularReferenciasDeEscena()
        {
            bool superficie = EsEscenaExteriorMarte();
            camaraPrincipal = superficie ? Camera.main : null;
            luzGlobal2D = superficie ? FindFirstObjectByType<Light2D>() : null;
        }

        private static bool EsEscenaExteriorMarte()
        {
            return SceneManager.GetActiveScene().name == "Mars_Natural_Landscape_01";
        }

        private void ActualizarAmbienteVisual()
        {
            if (gradienteLuzSolar == null || curvaIntensidadLuz == null || gradienteSkyboxHorizonte == null)
                return;

            Color colorLuz = gradienteLuzSolar.Evaluate(HoraNormalizada);
            float intensidad = curvaIntensidadLuz.Evaluate(HoraNormalizada);

            if (luzGlobal2D != null)
            {
                luzGlobal2D.color = colorLuz;
                luzGlobal2D.intensity = intensidad;
            }

            Color colorHorizonte = gradienteSkyboxHorizonte.Evaluate(HoraNormalizada);

            if (materialSkybox != null)
            {
                materialSkybox.SetColor(PropColorHorizonte, colorHorizonte);
                materialSkybox.SetColor(PropColorCenit, colorHorizonte * 0.45f);
                materialSkybox.SetFloat(PropBrillo, Mathf.Clamp(intensidad, 0.25f, 1.4f));
            }

            if (camaraPrincipal != null)
            {
                camaraPrincipal.backgroundColor = colorHorizonte;
            }
        }

        private void EvaluarDiaONoche(bool forzarEvento)
        {
            // Se considera noche entre las 19:12 (0.80) y las 05:30 (0.23)
            bool nocheActual = HoraNormalizada >= 0.80f || HoraNormalizada < 0.23f;
            if (forzarEvento || nocheActual != EsDeNoche)
            {
                EsDeNoche = nocheActual;
                OnCambioEstadoNoche?.Invoke(EsDeNoche);
                if (!forzarEvento)
                {
                    Debug.Log(EsDeNoche ? "[Ciclo Solar] Ha anochecido en Marte." : "[Ciclo Solar] Amanece en Marte.");
                }
            }
        }

        private void ActualizarMonitorInspector()
        {
            diaActualMonitor = DiaActual;
            relojMarcianoMonitor = ObtenerRelojFormateado();
            esDeNocheMonitor = EsDeNoche;
        }

        public string ObtenerRelojFormateado()
        {
            float horasTotales = HoraNormalizada * 24f;
            int horas = Mathf.FloorToInt(horasTotales);
            int minutos = Mathf.FloorToInt((horasTotales - horas) * 60f);
            return $"Día {Mathf.Max(1, DiaActual)} - {horas:00}:{minutos:00}";
        }
    }
}

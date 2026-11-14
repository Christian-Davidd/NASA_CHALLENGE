using System.Collections.Generic;
using UnityEngine;

namespace JuniorAstronaut.TerrenoCME
{
    public class CommsTowerCMEEvent : MonoBehaviour
    {
        [Header("=== REFERENCIAS ===")]
        public DONKICMEManager donkiManager;
        public Astronaut2DController astronauta;
        public ProceduralBox2D visualTorre;

        [Header("=== RECURSOS DEL PUESTO (Trade-off) ===")]
        [Range(0f, 100f)] public float bateria = 100f;
        public int datosCientificosEnviados = 0;
        public int kitsReparacion = 2;

        [Header("=== ESTADO DE LA TORRE DE COMMS ===")]
        public bool antenaEncendida = true;
        public bool antenaDanada = false;

        [Header("=== EVENTO CME EN CURSO ===")]
        public bool alertaActiva = false;
        public float tiempoParaImpacto = 0f;
        public float duracionAvisoSegundos = 10f;

        private List<DONKICMEEventData> eventosDePartida = new List<DONKICMEEventData>();
        private int indiceEventoActual = 0;
        private DONKICMEEventData eventoActual;
        private float velocidadActualKmS = 0f;
        private string tipoActualNasa = "S";
        private string mensajeResultado = "Sistema nominal. Acércate a la torre y pulsa [E] para operar.";
        private float acumuladorCiencia = 0f;
        private bool datosCargados = false;
        private bool mostrarTarjetaEducativa = false;
        private string tituloTarjetaEducativa = "";
        private string cuerpoTarjetaEducativa = "";

        private void Awake()
        {
            if (visualTorre == null) visualTorre = GetComponent<ProceduralBox2D>();
            if (donkiManager == null) donkiManager = FindFirstObjectByType<DONKICMEManager>();
            if (astronauta == null) astronauta = FindFirstObjectByType<Astronaut2DController>();
        }

        private void OnEnable()
        {
            if (donkiManager != null)
            {
                donkiManager.OnCMEDatosListos += AlRecibirDatosNasa;
            }
        }

        private void OnDisable()
        {
            if (donkiManager != null)
            {
                donkiManager.OnCMEDatosListos -= AlRecibirDatosNasa;
            }
        }

        private void AlRecibirDatosNasa(DONKICMEEventData masFuerte, float maxVel, string tipo)
        {
            eventosDePartida = donkiManager.ObtenerEventosParaPartida();
            indiceEventoActual = 0;
            datosCargados = true;

            if (eventosDePartida.Count > 0)
            {
                mensajeResultado = $"Datos listos ({donkiManager.origenDeLosDatos}). {eventosDePartida.Count} tormenta(s) CME programada(s).";
                Invoke(nameof(IniciarSiguienteTormentaCME), 3.5f);
            }
            else
            {
                mensajeResultado = "Semana solar muy tranquila: 0 tormentas CME detectadas.";
            }

            ActualizarColorTorre();
        }

        public void IniciarSiguienteTormentaCME()
        {
            if (!datosCargados || eventosDePartida == null || eventosDePartida.Count == 0) return;

            if (indiceEventoActual >= eventosDePartida.Count)
            {
                indiceEventoActual = 0;
            }

            eventoActual = eventosDePartida[indiceEventoActual];
            indiceEventoActual++;

            velocidadActualKmS = 0f;
            tipoActualNasa = "S";
            if (eventoActual.cmeAnalyses != null && eventoActual.cmeAnalyses.Count > 0)
            {
                velocidadActualKmS = eventoActual.cmeAnalyses[0].speed;
                tipoActualNasa = string.IsNullOrEmpty(eventoActual.cmeAnalyses[0].type) ? "S" : eventoActual.cmeAnalyses[0].type;
            }

            mostrarTarjetaEducativa = false;
            alertaActiva = true;
            tiempoParaImpacto = duracionAvisoSegundos;
            ActualizarColorTorre();

            Debug.Log($"<color=#FFEB3B><b>[TORRE COMMS]</b></color> ⚠️ ¡Nube de plasma CME ({eventoActual.activityID}) acercándose a {velocidadActualKmS} km/s!");
        }

        private void Update()
        {
            // 1. Generar datos científicos y consumir batería si la antena está encendida y sana
            if (antenaEncendida && !antenaDanada && bateria > 0f)
            {
                bateria = Mathf.Clamp(bateria - 1.2f * Time.deltaTime, 0f, 100f);
                acumuladorCiencia += Time.deltaTime;
                if (acumuladorCiencia >= 1.5f)
                {
                    acumuladorCiencia = 0f;
                    datosCientificosEnviados += 10;
                }
            }
            else if (!antenaEncendida && !antenaDanada)
            {
                bateria = Mathf.Clamp(bateria + 2.5f * Time.deltaTime, 0f, 100f);
            }

            // 2. Cuenta regresiva de impacto CME
            if (alertaActiva)
            {
                tiempoParaImpacto -= Time.deltaTime;
                if (tiempoParaImpacto <= 0f)
                {
                    ResolverImpactoCME();
                }
            }

            ActualizarColorTorre();
        }

        private void ResolverImpactoCME()
        {
            alertaActiva = false;

            if (!antenaEncendida)
            {
                mensajeResultado = $"✅ ¡Bien hecho! Apagaste la antena a tiempo. La CME de {velocidadActualKmS} km/s pasó sin dañar los circuitos.";
                Debug.Log($"<color=#4CAF50><b>[TORRE COMMS]</b></color> {mensajeResultado}");
            }
            else if (velocidadActualKmS >= 500f)
            {
                antenaDanada = true;
                antenaEncendida = false;
                float danoBateria = velocidadActualKmS >= 1000f ? 35f : 20f;
                bateria = Mathf.Clamp(bateria - danoBateria, 0f, 100f);

                mensajeResultado = $"💥 ¡SOBRECARGA! La CME ({velocidadActualKmS} km/s, Tipo {tipoActualNasa}) quemó fusibles. Necesitas 1 Kit de Reparación.";
                Debug.LogWarning($"<color=#F44336><b>[TORRE COMMS]</b></color> {mensajeResultado}");
            }
            else
            {
                bateria = Mathf.Clamp(bateria - 5f, 0f, 100f);
                mensajeResultado = $"⚡ Estática menor por CME lenta ({velocidadActualKmS} km/s). La antena resistió (-5% batería).";
                Debug.Log($"[TORRE COMMS] {mensajeResultado}");
            }

            ActualizarColorTorre();
            PrepararTarjetaEducativa();
        }

        private void PrepararTarjetaEducativa()
        {
            mostrarTarjetaEducativa = true;
            string descripcionTipo = tipoActualNasa switch
            {
                "S" => "Tipo S (Lenta, menos de 500 km/s)",
                "C" => "Tipo C (Común, 500 a 999 km/s)",
                "O" => "Tipo O (Ocasional y Rápida, 1,000 a 1,999 km/s)",
                "R" => "Tipo R (Rara y Muy Peligrosa, 2,000 a 2,999 km/s)",
                "ER" => "Tipo ER (Extremadamente Rara, más de 3,000 km/s)",
                _ => $"Tipo {tipoActualNasa}"
            };

            tituloTarjetaEducativa = $"💡 Ciencia NASA para Astronautas Junior ({eventoActual?.activityID})";
            cuerpoTarjetaEducativa =
                $"¿Qué es una CME? Una Eyección de Masa Coronal es como una burbuja gigante de viento eléctrico que el Sol lanza al espacio.\n" +
                $"• Esta tormenta real viajó a {velocidadActualKmS:F0} km/s — Escala oficial: {descripcionTipo}.\n" +
                $"• Decisión de ingeniería: Apagar la antena detiene el envío de datos a la Tierra, pero protege los circuitos contra sobrecargas.\n" +
                $"🔬 Fuente real: NASA DONKI (Space Weather Database)";
        }

        public void InteractuarDesdeAstronauta()
        {
            if (antenaDanada)
            {
                if (kitsReparacion > 0)
                {
                    kitsReparacion--;
                    antenaDanada = false;
                    antenaEncendida = true;
                    mensajeResultado = $"🔧 Antena reparada con éxito. Te quedan {kitsReparacion} Kit(s) de Reparación.";
                    Debug.Log($"[TORRE COMMS] {mensajeResultado}");
                }
                else
                {
                    mensajeResultado = "❌ ¡No te quedan Kits de Reparación! No puedes reparar la antena.";
                    Debug.LogWarning($"[TORRE COMMS] {mensajeResultado}");
                }
            }
            else
            {
                antenaEncendida = !antenaEncendida;
                mensajeResultado = antenaEncendida
                    ? "📡 Antena ENCENDIDA: Enviando datos a la Tierra (gasta batería)."
                    : "🔌 Antena APAGADA: Protegida contra sobrecargas (no envía datos).";
                Debug.Log($"[TORRE COMMS] {mensajeResultado}");
            }

            ActualizarColorTorre();
        }

        private void ActualizarColorTorre()
        {
            if (visualTorre == null) return;

            if (antenaDanada)
            {
                visualTorre.CambiarColor(new Color(0.9f, 0.2f, 0.2f));
            }
            else if (alertaActiva)
            {
                visualTorre.CambiarColor(new Color(1f, 0.8f, 0.1f));
            }
            else if (antenaEncendida)
            {
                visualTorre.CambiarColor(new Color(0.2f, 0.85f, 0.4f));
            }
            else
            {
                visualTorre.CambiarColor(new Color(0.5f, 0.55f, 0.6f));
            }
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(15, 15, 520, 115), "MARS: OUTPOST 01 — Zona 5: Torre de Comunicaciones (DONKI CME)");

            string estadoAntenaTexto = antenaDanada
                ? "DAÑADA (Requiere Kit)"
                : (antenaEncendida ? "ENCENDIDA (Enviando Datos)" : "APAGADA (Modo Seguro)");

            GUI.Label(new Rect(30, 42, 490, 22),
                $"🔋 Batería: {bateria:F0}%   |   📡 Ciencia Enviada: {datosCientificosEnviados}   |   🔧 Kits: {kitsReparacion}");
            GUI.Label(new Rect(30, 66, 490, 22),
                $"Estado Antena: {estadoAntenaTexto}");
            GUI.Label(new Rect(30, 90, 490, 22),
                $"Controles: [A / D] o [Flechas] Caminar   |   [E] Interactuar cerca de la Torre");

            if (datosCargados && !alertaActiva && eventosDePartida.Count > 0)
            {
                if (GUI.Button(new Rect(550, 15, 220, 35), "⚡ Simular Siguiente CME Real"))
                {
                    IniciarSiguienteTormentaCME();
                }
            }

            if (astronauta != null && astronauta.EstaCercaDeLaTorre)
            {
                string accion = antenaDanada
                    ? "[E] Reparar Antena (Usa 1 Kit)"
                    : (antenaEncendida ? "[E] Apagar Antena (Modo Seguro)" : "[E] Encender Antena");
                GUI.Box(new Rect(15, 140, 340, 34), accion);
            }

            if (alertaActiva && eventoActual != null)
            {
                GUI.color = new Color(1f, 0.85f, 0.3f);
                GUI.Box(new Rect(15, 185, 520, 145), "⚠️ ¡ALERTA DE CLIMA ESPACIAL NASA (DONKI CME)!");
                GUI.color = Color.white;

                GUI.Label(new Rect(30, 212, 490, 22), $"Evento Real: {eventoActual.activityID}");
                GUI.Label(new Rect(30, 234, 490, 22),
                    $"Velocidad del Plasma: {velocidadActualKmS:F0} km/s (Escala NASA: Tipo {tipoActualNasa})");
                GUI.Label(new Rect(30, 256, 490, 22),
                    $"Impacto en: {tiempoParaImpacto:F1} segundos — ¡Corre a la torre derecha y apágala con [E]!");
                GUI.Label(new Rect(30, 280, 490, 40),
                    "Si la dejas encendida durante una CME rápida (>= 500 km/s), la sobrecarga quemará la antena.");
            }
            else
            {
                GUI.Box(new Rect(15, 185, 520, 55), "Bitácora de Misión");
                GUI.Label(new Rect(30, 210, 490, 25), mensajeResultado);
            }

            // Tarjeta Educativa NASA para niños (7-12 años) tras cada tormenta
            if (mostrarTarjetaEducativa)
            {
                GUI.color = new Color(0.25f, 0.75f, 1f);
                GUI.Box(new Rect(15, 250, 560, 155), tituloTarjetaEducativa);
                GUI.color = Color.white;

                GUI.Label(new Rect(30, 278, 530, 90), cuerpoTarjetaEducativa);
                if (GUI.Button(new Rect(445, 368, 115, 28), "Entendido 👍"))
                {
                    mostrarTarjetaEducativa = false;
                }
            }
        }
    }
}

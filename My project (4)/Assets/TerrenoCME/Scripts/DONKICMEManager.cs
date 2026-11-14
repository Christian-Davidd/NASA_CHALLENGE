using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace JuniorAstronaut.TerrenoCME
{
    [Serializable]
    public class CMEAnalysisData
    {
        public string time21_5;
        public float latitude;
        public float longitude;
        public float halfAngle;
        public float speed;
        public string type;
        public bool isMostAccurate;
    }

    [Serializable]
    public class DONKICMEEventData
    {
        public string activityID;
        public string catalog;
        public string startTime;
        public string sourceLocation;
        public int activeRegionNum;
        public string note;
        public List<CMEAnalysisData> cmeAnalyses;
    }

    [Serializable]
    public class DONKICMEListWrapper
    {
        public List<DONKICMEEventData> eventos;
    }

    public enum ModoSeleccionCME
    {
        MasFuerteDeLaSemana,
        AleatorioDeLaSemana,
        FrecuenciaDinamicaSemana
    }

    public class DONKICMEManager : MonoBehaviour
    {
        [Header("=== MODO DE SELECCION DE EVENTOS ===")]
        [Tooltip("Elige como usar los eventos reales de la semana en tu partida")]
        public ModoSeleccionCME modoSeleccion = ModoSeleccionCME.FrecuenciaDinamicaSemana;

        [Header("=== CONFIGURACION API NASA (DONKI CME) ===")]
        [Tooltip("Si esta activo, usa primero el servidor directo CCMC de la NASA (no requiere API Key ni gasta tokens)")]
        public bool usarCCMCSinTokenPrimero = true;
        [Tooltip("API Key de respaldo en api.nasa.gov por si CCMC falla")]
        public string apiKeyRespaldo = "Mqw6nBgC8HQqF9H7zgr9qMncbMKNRjfzWDzNCDnb";
        public int diasAConsultar = 7;
        public bool ejecutarAlIniciar = true;

        [Header("=== NIVEL 1: CACHE LOCAL (Ahorro de peticiones) ===")]
        public bool usarCacheLocal = true;
        public float horasExpiracionCache = 6f;
        [Tooltip("Activa esto si quieres obligar a consultar la API ignorando el cache guardado")]
        public bool forzarDescargaEnVivo = false;

        [Header("=== NIVEL 2: RESPALDO OFFLINE (Resources) ===")]
        public string archivoRespaldoResources = "cme_respaldo";

        [Header("=== NIVEL 3: SERVIDOR / JSON ESTATICO (Opcional) ===")]
        [Tooltip("Si colocan un JSON actualizado en GitHub Raw/Pages, pon el link aqui para no gastar API Key")]
        public string urlJsonEstaticoNivel3 = "";

        [Header("=== RESULTADOS EN TIEMPO REAL (Ultima Semana) ===")]
        public string origenDeLosDatos = "Sin consultar";
        public int totalEventosEncontrados = 0;
        public float velocidadMaximaKmS = 0f;
        public string tipoNasaMasFuerte = "N/A";
        public string riesgoEnJuegoMasFuerte = "N/A";
        public DONKICMEEventData eventoMasFuerte;
        public List<DONKICMEEventData> listaEventosSemana = new List<DONKICMEEventData>();

        public event Action<DONKICMEEventData, float, string> OnCMEDatosListos;

        private string RutaCacheLocal => Path.Combine(Application.persistentDataPath, "cache_donki_cme.json");

        private void Start()
        {
            if (ejecutarAlIniciar)
            {
                ConsultarCMESemanal();
            }
        }

        [ContextMenu("Consultar CME Ultima Semana")]
        public void ConsultarCMESemanal()
        {
            StartCoroutine(FlujoTresNivelesCME());
        }

        [ContextMenu("Borrar Cache Local y Reconsultar")]
        public void BorrarCacheYReconsultar()
        {
            if (File.Exists(RutaCacheLocal))
            {
                File.Delete(RutaCacheLocal);
                Debug.Log("[DONKI CME] Cache local eliminado correctamente.");
            }
            ConsultarCMESemanal();
        }

        private IEnumerator FlujoTresNivelesCME()
        {
            string jsonCrudo = null;
            string endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string startDate = DateTime.UtcNow.AddDays(-diasAConsultar).ToString("yyyy-MM-dd");

            Debug.Log($"<b>[DONKI CME]</b> Iniciando consulta de la ultima semana ({startDate} a {endDate})...");

            // ------------------------------------------------------------------
            // NIVEL 1: Revisar Cache Local (< 6 horas) para no gastar API Key
            // ------------------------------------------------------------------
            if (usarCacheLocal && !forzarDescargaEnVivo && File.Exists(RutaCacheLocal))
            {
                DateTime fechaArchivo = File.GetLastWriteTimeUtc(RutaCacheLocal);
                double horasTranscurridas = (DateTime.UtcNow - fechaArchivo).TotalHours;

                if (horasTranscurridas < horasExpiracionCache)
                {
                    jsonCrudo = File.ReadAllText(RutaCacheLocal);
                    origenDeLosDatos = $"NIVEL 1: Cache Local (guardado hace {horasTranscurridas:F1} h - 0 peticiones gastadas)";
                    Debug.Log($"<color=#4CAF50><b>[DONKI CME - NIVEL 1]</b></color> Leyendo desde Cache Local en: {RutaCacheLocal}");
                }
            }

            // ------------------------------------------------------------------
            // NIVEL 3 (Opcional): Descargar desde JSON estatico (GitHub Raw/Proxy)
            // ------------------------------------------------------------------
            if (string.IsNullOrEmpty(jsonCrudo) && !string.IsNullOrEmpty(urlJsonEstaticoNivel3))
            {
                Debug.Log($"<color=#2196F3><b>[DONKI CME - NIVEL 3]</b></color> Consultando endpoint estatico: {urlJsonEstaticoNivel3}");
                using (UnityWebRequest reqProxy = UnityWebRequest.Get(urlJsonEstaticoNivel3))
                {
                    reqProxy.timeout = 8;
                    yield return reqProxy.SendWebRequest();

                    if (reqProxy.result == UnityWebRequest.Result.Success)
                    {
                        jsonCrudo = reqProxy.downloadHandler.text;
                        origenDeLosDatos = "NIVEL 3: Servidor Estatico / Proxy (0 peticiones a NASA)";
                        GuardarEnCacheLocal(jsonCrudo);
                    }
                    else
                    {
                        Debug.LogWarning($"[DONKI CME - NIVEL 3] No se pudo leer el proxy estatico ({reqProxy.error}). Pasando a API NASA directa...");
                    }
                }
            }

            // ------------------------------------------------------------------
            // API NASA EN VIVO (1ro: CCMC Directo sin token | 2do: api.nasa.gov)
            // ------------------------------------------------------------------
            if (string.IsNullOrEmpty(jsonCrudo) && usarCCMCSinTokenPrimero)
            {
                string urlCcmc = $"https://ccmc.gsfc.nasa.gov/DONKI/WS/get/CME?startDate={startDate}&endDate={endDate}";
                Debug.Log($"<color=#00BCD4><b>[DONKI CME - CCMC SIN TOKEN]</b></color> Conectando directo a Goddard: {urlCcmc}");

                using (UnityWebRequest reqCcmc = UnityWebRequest.Get(urlCcmc))
                {
                    reqCcmc.timeout = 10;
                    yield return reqCcmc.SendWebRequest();

                    if (reqCcmc.result == UnityWebRequest.Result.Success)
                    {
                        jsonCrudo = reqCcmc.downloadHandler.text;
                        origenDeLosDatos = $"CCMC NASA Directo sin Token ({startDate} a {endDate})";
                        GuardarEnCacheLocal(jsonCrudo);
                        Debug.Log("<color=#00E676><b>[DONKI CME - CCMC]</b></color> Datos recibidos sin gastar tokens y guardados en cache.");
                    }
                    else
                    {
                        Debug.LogWarning($"<color=#FF9800><b>[DONKI CME - CCMC]</b></color> Fallo CCMC ({reqCcmc.error}). Probando respaldo en api.nasa.gov...");
                    }
                }
            }

            if (string.IsNullOrEmpty(jsonCrudo) && !string.IsNullOrEmpty(apiKeyRespaldo))
            {
                string urlNasa = $"https://api.nasa.gov/DONKI/CME?startDate={startDate}&endDate={endDate}&api_key={apiKeyRespaldo}";
                Debug.Log($"<color=#00BCD4><b>[DONKI CME - API.NASA.GOV]</b></color> Conectando a respaldo oficial: {urlNasa}");

                using (UnityWebRequest reqNasa = UnityWebRequest.Get(urlNasa))
                {
                    reqNasa.timeout = 12;
                    yield return reqNasa.SendWebRequest();

                    if (reqNasa.result == UnityWebRequest.Result.Success)
                    {
                        jsonCrudo = reqNasa.downloadHandler.text;
                        origenDeLosDatos = $"api.nasa.gov en Vivo ({startDate} a {endDate})";
                        GuardarEnCacheLocal(jsonCrudo);
                        Debug.Log("<color=#00E676><b>[DONKI CME - API.NASA.GOV]</b></color> Datos recibidos y guardados en cache local.");
                    }
                    else
                    {
                        Debug.LogWarning($"<color=#FF9800><b>[DONKI CME]</b></color> Fallo api.nasa.gov ({reqNasa.error}). Activando NIVEL 2 (Respaldo Offline)...");
                    }
                }
            }

            // ------------------------------------------------------------------
            // NIVEL 2: Respaldo Offline (Resources/cme_respaldo.json)
            // ------------------------------------------------------------------
            if (string.IsNullOrEmpty(jsonCrudo))
            {
                TextAsset respaldoAsset = Resources.Load<TextAsset>(archivoRespaldoResources);
                if (respaldoAsset != null)
                {
                    jsonCrudo = respaldoAsset.text;
                    origenDeLosDatos = "NIVEL 2: Respaldo Offline (Archivo Historico NASA en Resources)";
                    Debug.Log("<color=#FFC107><b>[DONKI CME - NIVEL 2]</b></color> Datos cargados desde Respaldo Offline en Resources.");
                }
                else
                {
                    Debug.LogError("[DONKI CME] No se encontro el archivo de respaldo en Resources.");
                    yield break;
                }
            }

            // ------------------------------------------------------------------
            // PROCESAR E IMPRIMIR TODOS LOS DATOS EN LA CONSOLA DE UNITY
            // ------------------------------------------------------------------
            ProcesarYMostrarEnConsola(jsonCrudo, startDate, endDate);
        }

        private void GuardarEnCacheLocal(string json)
        {
            try
            {
                if (!string.IsNullOrEmpty(json) && json.Trim().StartsWith("["))
                {
                    File.WriteAllText(RutaCacheLocal, json);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DONKI CME] No se pudo guardar el cache local: {e.Message}");
            }
        }

        private void ProcesarYMostrarEnConsola(string jsonCrudo, string startDate, string endDate)
        {
            if (string.IsNullOrEmpty(jsonCrudo) || !jsonCrudo.Trim().StartsWith("["))
            {
                Debug.LogError("[DONKI CME] El formato JSON recibido no es una lista valida.");
                return;
            }

            // Truco para JsonUtility de Unity: envolver el arreglo raiz [ ... ]
            string jsonEnvuelto = "{\"eventos\":" + jsonCrudo + "}";
            DONKICMEListWrapper contenedor = JsonUtility.FromJson<DONKICMEListWrapper>(jsonEnvuelto);

            if (contenedor == null || contenedor.eventos == null)
            {
                Debug.LogWarning("[DONKI CME] No se pudieron parsear los eventos CME.");
                return;
            }

            listaEventosSemana = contenedor.eventos;
            totalEventosEncontrados = listaEventosSemana.Count;

            Debug.Log("================================================================================");
            Debug.Log($"<b>REPORTE SEMANAL NASA DONKI (CME)</b> | Origen: <b>{origenDeLosDatos}</b>");
            Debug.Log($"Total de Eyecciones de Masa Coronal (CME) encontradas: <b>{totalEventosEncontrados}</b>");
            Debug.Log("================================================================================");

            velocidadMaximaKmS = 0f;
            eventoMasFuerte = null;
            tipoNasaMasFuerte = "S";
            riesgoEnJuegoMasFuerte = "BAJO";

            for (int i = 0; i < listaEventosSemana.Count; i++)
            {
                DONKICMEEventData cme = listaEventosSemana[i];

                float velocidad = 0f;
                string tipo = "N/A";
                float angulo = 0f;

                if (cme.cmeAnalyses != null && cme.cmeAnalyses.Count > 0)
                {
                    velocidad = cme.cmeAnalyses[0].speed;
                    tipo = string.IsNullOrEmpty(cme.cmeAnalyses[0].type) ? "S" : cme.cmeAnalyses[0].type;
                    angulo = cme.cmeAnalyses[0].halfAngle;
                }

                string riesgoJuego = CalcularRiesgoEnJuego(velocidad);

                // Guardar si es la CME mas rapida de la semana
                if (eventoMasFuerte == null || velocidad > velocidadMaximaKmS)
                {
                    velocidadMaximaKmS = velocidad;
                    tipoNasaMasFuerte = tipo;
                    riesgoEnJuegoMasFuerte = riesgoJuego;
                    eventoMasFuerte = cme;
                }

                string notaResumen = string.IsNullOrEmpty(cme.note)
                    ? "Sin nota"
                    : (cme.note.Length > 140 ? cme.note.Substring(0, 140) + "..." : cme.note);

                Debug.Log(
                    $"<b>[Evento CME #{i + 1}]</b> ID: {cme.activityID}\n" +
                    $"   • Fecha/Hora: {cme.startTime} | Ubicacion Solar: {cme.sourceLocation} (Region: {cme.activeRegionNum})\n" +
                    $"   • Velocidad: <b>{velocidad} km/s</b> (Tipo NASA: <b>{tipo}</b> | Semi-angulo: {angulo}°)\n" +
                    $"   • Impacto en Juego: <b>{riesgoJuego}</b>\n" +
                    $"   • Nota NASA: {notaResumen}"
                );
            }

            if (eventoMasFuerte != null)
            {
                Debug.Log(
                    $"<color=#FF5252><b>[ACONTECIMIENTO SELECCIONADO PARA LA PARTIDA]</b></color>\n" +
                    $"   • Modo activo: <b>{modoSeleccion}</b>\n" +
                    $"   • CME Mas Potente de la Semana: <b>{eventoMasFuerte.activityID}</b> ({eventoMasFuerte.startTime})\n" +
                    $"   • Velocidad Maxima: <b>{velocidadMaximaKmS} km/s</b> (Clasificacion NASA: <b>{tipoNasaMasFuerte}</b>)\n" +
                    $"   • Consecuencia en Marte: <b>{riesgoEnJuegoMasFuerte}</b>"
                );
            }

            OnCMEDatosListos?.Invoke(eventoMasFuerte, velocidadMaximaKmS, tipoNasaMasFuerte);
        }

        /// <summary>
        /// Devuelve los eventos reales de la semana que ocurriran en la partida segun el ModoSeleccionCME elegido.
        /// </summary>
        public List<DONKICMEEventData> ObtenerEventosParaPartida()
        {
            var seleccionados = new List<DONKICMEEventData>();
            if (listaEventosSemana == null || listaEventosSemana.Count == 0) return seleccionados;

            if (modoSeleccion == ModoSeleccionCME.MasFuerteDeLaSemana)
            {
                if (eventoMasFuerte != null) seleccionados.Add(eventoMasFuerte);
                return seleccionados;
            }

            if (modoSeleccion == ModoSeleccionCME.AleatorioDeLaSemana)
            {
                int idx = UnityEngine.Random.Range(0, listaEventosSemana.Count);
                seleccionados.Add(listaEventosSemana[idx]);
                return seleccionados;
            }

            // FrecuenciaDinamicaSemana (Combina cantidad real de la semana + seleccion aleatoria)
            int cantidadTormentas = 1;
            if (listaEventosSemana.Count >= 8) cantidadTormentas = 3;
            else if (listaEventosSemana.Count >= 4) cantidadTormentas = 2;

            var copia = new List<DONKICMEEventData>(listaEventosSemana);
            for (int i = 0; i < cantidadTormentas && copia.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, copia.Count);
                seleccionados.Add(copia[idx]);
                copia.RemoveAt(idx);
            }

            return seleccionados;
        }

        private string CalcularRiesgoEnJuego(float velocidadKmS)
        {
            if (velocidadKmS >= 1000f)
            {
                return "CRITICO (Apagon total de Antena en Zona 5 + Riesgo de quemar fusibles)";
            }
            if (velocidadKmS >= 500f)
            {
                return "MEDIO (Interferencia electromagnetica fuerte en Comunicaciones)";
            }
            return "BAJO (Estatica menor en sensores exteriores)";
        }
    }
}


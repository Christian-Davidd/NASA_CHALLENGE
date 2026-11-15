using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

/// <summary>
/// [PERSONA 1] Gestor de datos reales de la NASA para Nubes de Plasma (/DONKI/CME).
/// Implementa los 3 niveles de carga:
///   - Nivel 1: Caché local en PlayerPrefs (6 horas)
///   - Nivel 3: Archivo pre-descargado por GitHub Action (nasa-data/cme_semana.json)
///   - API Directa CCMC sin token (ccmc.gsfc.nasa.gov/DONKI/WS/get/CME) + respaldo api.nasa.gov
///   - Nivel 2: Respaldo offline en Assets/Resources/cme_respaldo.json
/// </summary>
public sealed class DONKICMEManager : MonoBehaviour
{
    public static DONKICMEManager Instance { get; private set; }

    [Serializable]
    public class CMEAnalysis
    {
        public float speed;
        public string type;
        public bool isMostAccurate;
    }

    [Serializable]
    public class CMEEventData
    {
        public string activityID;
        public string startTime;
        public string sourceLocation;
        public string note;
        public CMEAnalysis[] cmeAnalyses;
    }

    [Serializable]
    private class CMEEventListWrapper
    {
        public CMEEventData[] items;
    }

    [Header("Configuración de Conexión NASA DONKI")]
    [SerializeField] private string apiKey = "DEMO_KEY";
    [SerializeField] private string githubRawJsonUrl = "https://raw.githubusercontent.com/Christian-Davidd/NASA_CHALLENGE/main/nasa-data/cme_semana.json";
    [SerializeField] private float cacheVigenciaHoras = 6f;

    [Header("Frecuencia y Duración de Nubes de Plasma (CME)")]
    [SerializeField] private float primerEventoSegundos = 22f;
    [SerializeField] private float duracionEventoSegundos = 45f;

    public List<CMEEventData> EventosCargados { get; private set; } = new List<CMEEventData>();
    public CMEEventData EventoActivo { get; private set; }
    public bool HayTormentaCMEActiva { get; private set; }
    public float VelocidadActualKmS { get; private set; }
    public float TiempoRestanteEvento { get; private set; }
    public float TiempoParaSiguienteCME { get; private set; }
    public float IntervaloCalculadoSegundos { get; private set; } = 60f;
    public string FuenteDatosActual { get; private set; } = "Cargando datos NASA...";
    public string ReglaFrecuenciaTexto { get; private set; } = "Calculando frecuencia semanal...";

    public event Action<CMEEventData, float> OnCMEIniciada;
    public event Action OnCMEFinalizada;

    private const string PREF_CACHE_JSON = "NASA_DONKI_CME_JSON_V2";
    private const string PREF_CACHE_TIME = "NASA_DONKI_CME_TIME_V2";
    private int indiceEventoActual;

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
        TiempoParaSiguienteCME = primerEventoSegundos;
        StartCoroutine(FlujoCarga3Niveles());
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tKey.wasPressedThisFrame)
        {
            DispararNubeDePlasma();
        }

        if (HayTormentaCMEActiva)
        {
            TiempoRestanteEvento -= Time.deltaTime;
            if (TiempoRestanteEvento <= 0f)
            {
                FinalizarNubeDePlasma();
            }
        }
        else if (EventosCargados.Count > 0)
        {
            TiempoParaSiguienteCME -= Time.deltaTime;
            if (TiempoParaSiguienteCME <= 0f)
            {
                DispararNubeDePlasma();
            }
        }
    }

    private IEnumerator FlujoCarga3Niveles()
    {
        // 1. Nivel 1: Caché local (vigencia 6 horas)
        if (IntentarCargarCacheLocal(out string jsonCache))
        {
            if (ProcesarJsonCME(jsonCache))
            {
                FuenteDatosActual = "Nivel 1: Caché Local (6h)";
                CalcularFrecuenciaSemanal();
                yield break;
            }
        }

        // 2. Nivel 3 Local: Si existe nasa-data/cme_semana.json en el repositorio local
        string rutaLocalRepo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "nasa-data", "cme_semana.json"));
        if (File.Exists(rutaLocalRepo))
        {
            try
            {
                string jsonLocalRepo = File.ReadAllText(rutaLocalRepo);
                if (ProcesarJsonCME(jsonLocalRepo))
                {
                    GuardarCacheLocal(jsonLocalRepo);
                    FuenteDatosActual = "Nivel 3: GitHub Action JSON (nasa-data/cme_semana.json)";
                    CalcularFrecuenciaSemanal();
                    yield break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[DONKI CME] No se pudo leer nasa-data/cme_semana.json local: " + e.Message);
            }
        }

        // 3. Nivel 3 Remoto: JSON de GitHub Action en el repositorio
        if (!string.IsNullOrEmpty(githubRawJsonUrl))
        {
            using (UnityWebRequest req = UnityWebRequest.Get(githubRawJsonUrl))
            {
                req.timeout = 7;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success && ProcesarJsonCME(req.downloadHandler.text))
                {
                    GuardarCacheLocal(req.downloadHandler.text);
                    FuenteDatosActual = "Nivel 3: GitHub Action (Remoto)";
                    CalcularFrecuenciaSemanal();
                    yield break;
                }
            }
        }

        // 4. API Directa CCMC NASA (sin token)
        string endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string startDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        string urlCCMC = $"https://ccmc.gsfc.nasa.gov/DONKI/WS/get/CME?startDate={startDate}&endDate={endDate}";

        using (UnityWebRequest req = UnityWebRequest.Get(urlCCMC))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success && ProcesarJsonCME(req.downloadHandler.text))
            {
                GuardarCacheLocal(req.downloadHandler.text);
                FuenteDatosActual = "API Directa CCMC NASA (Sin Token)";
                CalcularFrecuenciaSemanal();
                yield break;
            }
        }

        // 5. API api.nasa.gov con DEMO_KEY
        string urlNasaApi = $"https://api.nasa.gov/DONKI/CME?startDate={startDate}&endDate={endDate}&api_key={apiKey}";
        using (UnityWebRequest req = UnityWebRequest.Get(urlNasaApi))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success && ProcesarJsonCME(req.downloadHandler.text))
            {
                GuardarCacheLocal(req.downloadHandler.text);
                FuenteDatosActual = "API NASA DONKI (DEMO_KEY)";
                CalcularFrecuenciaSemanal();
                yield break;
            }
        }

        // 6. Nivel 2: Respaldo offline en Resources/cme_respaldo.json
        CargarRespaldoOffline();
    }

    private void CargarRespaldoOffline()
    {
        TextAsset respaldo = Resources.Load<TextAsset>("cme_respaldo");
        if (respaldo != null && ProcesarJsonCME(respaldo.text))
        {
            FuenteDatosActual = "Nivel 2: Respaldo Offline (cme_respaldo.json)";
        }
        else
        {
            FuenteDatosActual = "Nivel 2: Datos CME Integrados";
            EventosCargados.Add(new CMEEventData
            {
                activityID = "2026-11-12T08:12:00-CME-001",
                startTime = "2026-11-12T08:12Z",
                sourceLocation = "S12E08",
                note = "Nube de plasma solar rápida con frente magnético detectada por SOHO.",
                cmeAnalyses = new[] { new CMEAnalysis { speed = 860f, type = "O", isMostAccurate = true } }
            });
        }
        CalcularFrecuenciaSemanal();
    }

    private void CalcularFrecuenciaSemanal()
    {
        int totalSemana = EventosCargados.Count;
        if (totalSemana >= 10)
        {
            IntervaloCalculadoSegundos = 35f;
            ReglaFrecuenciaTexto = $"Sol muy activo ({totalSemana} CMEs/semana) → cada 35s";
        }
        else if (totalSemana >= 4)
        {
            IntervaloCalculadoSegundos = 55f;
            ReglaFrecuenciaTexto = $"Actividad solar media ({totalSemana} CMEs/semana) → cada 55s";
        }
        else
        {
            IntervaloCalculadoSegundos = 75f;
            ReglaFrecuenciaTexto = $"Sol tranquilo ({totalSemana} CMEs/semana) → cada 75s";
        }

        TiempoParaSiguienteCME = Mathf.Min(TiempoParaSiguienteCME, primerEventoSegundos);
    }

    public void DispararNubeDePlasma()
    {
        if (EventosCargados.Count == 0)
            CargarRespaldoOffline();

        indiceEventoActual = (indiceEventoActual + 1) % EventosCargados.Count;
        EventoActivo = EventosCargados[indiceEventoActual];
        VelocidadActualKmS = ExtraerVelocidadKmS(EventoActivo);
        HayTormentaCMEActiva = true;
        TiempoRestanteEvento = duracionEventoSegundos;

        OnCMEIniciada?.Invoke(EventoActivo, VelocidadActualKmS);
    }

    public void AcelerarFinDeTormenta(float maxSegundosRestantes)
    {
        if (!HayTormentaCMEActiva) return;
        TiempoRestanteEvento = Mathf.Min(TiempoRestanteEvento, Mathf.Max(0.5f, maxSegundosRestantes));
    }

    public void FinalizarNubeDePlasma()
    {
        if (!HayTormentaCMEActiva) return;

        HayTormentaCMEActiva = false;
        TiempoRestanteEvento = 0f;
        TiempoParaSiguienteCME = IntervaloCalculadoSegundos;
        OnCMEFinalizada?.Invoke();
    }

    private static float ExtraerVelocidadKmS(CMEEventData ev)
    {
        if (ev?.cmeAnalyses != null && ev.cmeAnalyses.Length > 0)
        {
            foreach (var a in ev.cmeAnalyses)
            {
                if (a != null && a.isMostAccurate && a.speed > 0f)
                    return a.speed;
            }
            if (ev.cmeAnalyses[0] != null && ev.cmeAnalyses[0].speed > 0f)
                return ev.cmeAnalyses[0].speed;
        }
        return 680f;
    }

    private bool ProcesarJsonCME(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return false;
        string trimmed = rawJson.Trim();
        if (!trimmed.StartsWith("[")) return false;

        try
        {
            string wrapped = "{\"items\":" + trimmed + "}";
            var parsed = JsonUtility.FromJson<CMEEventListWrapper>(wrapped);
            if (parsed?.items == null || parsed.items.Length == 0)
                return false;

            EventosCargados.Clear();
            EventosCargados.AddRange(parsed.items);
            indiceEventoActual = EventosCargados.Count - 1;
            EventoActivo = EventosCargados[indiceEventoActual];
            VelocidadActualKmS = ExtraerVelocidadKmS(EventoActivo);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[DONKI CME] Error al parsear JSON CME: " + e.Message);
            return false;
        }
    }

    private bool IntentarCargarCacheLocal(out string json)
    {
        json = null;
        if (!PlayerPrefs.HasKey(PREF_CACHE_JSON) || !PlayerPrefs.HasKey(PREF_CACHE_TIME))
            return false;

        string timeStr = PlayerPrefs.GetString(PREF_CACHE_TIME, "");
        if (!long.TryParse(timeStr, out long binaryTime))
            return false;

        DateTime savedTime = DateTime.FromBinary(binaryTime);
        if ((DateTime.UtcNow - savedTime).TotalHours > cacheVigenciaHoras)
            return false;

        json = PlayerPrefs.GetString(PREF_CACHE_JSON, "");
        return !string.IsNullOrEmpty(json);
    }

    private static void GuardarCacheLocal(string json)
    {
        PlayerPrefs.SetString(PREF_CACHE_JSON, json);
        PlayerPrefs.SetString(PREF_CACHE_TIME, DateTime.UtcNow.ToBinary().ToString());
        PlayerPrefs.Save();
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cuadro placeholder para nodos ISRU (In-Situ Resource Utilization) de Hielo Subsuperficial en Marte.
/// Permite mantener aplastado [E] para excavar y derretir hielo subterráneo (gastando algo de Energía)
/// y recargar el tanque de Agua en el exterior. Listo para colocar después el asset definitivo.
/// </summary>
[RequireComponent(typeof(MarsWorldPlaceholder2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MarsIceDepositNode2D : MonoBehaviour
{
    public static readonly List<MarsIceDepositNode2D> AllIceNodes = new List<MarsIceDepositNode2D>();
    private static readonly Dictionary<string, float> PersistentReserves = new Dictionary<string, float>();

    [SerializeField] private string nodeName = "Depósito de Hielo Subsuperficial ISRU";
    [SerializeField] private string labelTitle = "[HIELO ISRU]";
    [SerializeField] private float frostRegenPerSec = 0.6f;
    [SerializeField] private Sprite extractPulseSprite;
    [SerializeField] private Vector3 extractPulseOffset = new Vector3(0f, 0.1f, 0f);

    private MarsWorldPlaceholder2D placeholder;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer extractPulseRenderer;
    private bool usingGeneratedFallbackSprite;
    private float activeExtractTimer;
    private static Sprite icePlaceholderSprite;

    private string NodeKey => gameObject.name;

    private float CurrentReserve
    {
        get => PersistentReserves.TryGetValue(NodeKey, out float val) ? val : 100f;
        set => PersistentReserves[NodeKey] = Mathf.Clamp(value, 0f, 100f);
    }

    public static void ResetAllIceNodes()
    {
        PersistentReserves.Clear();
        foreach (var node in AllIceNodes)
        {
            if (node == null) continue;
            PersistentReserves[node.NodeKey] = 100f;
            node.UpdateVisuals();
        }
    }

    private void Awake()
    {
        if (!AllIceNodes.Contains(this))
            AllIceNodes.Add(this);
        if (!PersistentReserves.ContainsKey(NodeKey))
            PersistentReserves[NodeKey] = 100f;

        placeholder = GetComponent<MarsWorldPlaceholder2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = GetOrCreateIceSprite();
                usingGeneratedFallbackSprite = true;
            }
            else
            {
                usingGeneratedFallbackSprite = false;
                spriteRenderer.color = Color.white;
            }
        }

        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            if (col.size.sqrMagnitude < 0.05f)
                col.size = new Vector2(2.2f, 1.75f);
        }

        EnsureExtractPulseRenderer();
        UpdateVisuals();
    }

    private void OnDestroy()
    {
        AllIceNodes.Remove(this);
    }

    private void Update()
    {
        if (activeExtractTimer > 0f)
            activeExtractTimer = Mathf.Max(0f, activeExtractTimer - Time.deltaTime);

        if (CurrentReserve < 100f && frostRegenPerSec > 0f)
            CurrentReserve = Mathf.Min(100f, CurrentReserve + frostRegenPerSec * Time.deltaTime);

        UpdateExtractPulseEffect();
        UpdateVisuals();
    }

    public string InteractHoldFromPlayer(float deltaTime)
    {
        activeExtractTimer = 0.16f;
        var flr = DONKIFLRManager.Instance;
        if (flr == null)
            return $"{nodeName}: Hielo disponible ({CurrentReserve:F0}%).";

        float reserve = CurrentReserve;
        flr.ExtraerAguaHieloISRUContinuo(deltaTime, ref reserve, out string msg);
        CurrentReserve = reserve;
        UpdateExtractPulseEffect();
        UpdateVisuals();
        return msg;
    }

    private void EnsureExtractPulseRenderer()
    {
        if (extractPulseSprite == null || extractPulseRenderer != null) return;

        var fxGo = new GameObject("ExtractPulseFX");
        fxGo.transform.SetParent(transform, false);
        fxGo.transform.localPosition = extractPulseOffset;
        fxGo.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        extractPulseRenderer = fxGo.AddComponent<SpriteRenderer>();
        extractPulseRenderer.sprite = extractPulseSprite;
        extractPulseRenderer.enabled = false;
    }

    private void UpdateExtractPulseEffect()
    {
        if (extractPulseRenderer == null) return;

        bool extracting = activeExtractTimer > 0f && CurrentReserve > 0.5f;
        extractPulseRenderer.enabled = extracting;
        if (!extracting) return;

        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * 11f);
        if (spriteRenderer != null)
            extractPulseRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
        extractPulseRenderer.color = new Color(0.75f, 0.96f, 1f, Mathf.Lerp(0.45f, 0.92f, wave));
        float scale = Mathf.Lerp(0.84f, 1.02f, wave);
        extractPulseRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        extractPulseRenderer.transform.localPosition = extractPulseOffset + new Vector3(0f, 0.05f * wave, 0f);
    }

    private void UpdateVisuals()
    {
        if (placeholder == null) return;

        float res = CurrentReserve;
        Color c = activeExtractTimer > 0f && res > 0.5f
            ? new Color(0.28f, 0.92f, 1.0f, 0.98f)
            : res > 20f
                ? new Color(0.45f, 0.92f, 1.0f, 0.92f)
                : new Color(0.45f, 0.62f, 0.72f, 0.72f);

        string subtitle = activeExtractTimer > 0f && res > 0.5f
            ? $"💧 Fundiendo hielo ISRU... ({res:F0}%)"
            : res > 5f
                ? $"Hielo {res:F0}% — Mantén [E] para extraer Agua (-⚡)"
                : $"Escarcha regenerándose ({res:F0}%)";

        placeholder.SetVisualState(c, labelTitle, subtitle, usingGeneratedFallbackSprite);
    }

    private static Sprite GetOrCreateIceSprite()
    {
        if (icePlaceholderSprite != null)
            return icePlaceholderSprite;

        const int w = 72;
        const int h = 72;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool border = x < 3 || x >= w - 3 || y < 3 || y >= h - 3;
                bool crystal = Mathf.Abs(x - y) <= 2 || Mathf.Abs((w - 1 - x) - y) <= 2;

                if (border)
                    pixels[y * w + x] = new Color(0.85f, 0.98f, 1.0f, 1f);
                else if (crystal)
                    pixels[y * w + x] = new Color(0.75f, 0.96f, 1.0f, 0.95f);
                else
                    pixels[y * w + x] = new Color(0.32f, 0.78f, 0.95f, 0.88f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        icePlaceholderSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 40f);
        return icePlaceholderSprite;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureDefaultNodesInScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureDefaultNodesInScene(scene);
    }

    private static void EnsureDefaultNodesInScene(Scene scene)
    {
        if (scene.name != "Mars_Natural_Landscape_01") return;
        if (AllIceNodes.Count > 0) return;

        CreateRuntimeNode("Hielo_ISRU_01_CraterNW", "Hielo Subsuperficial Cráter NW", "[HIELO ISRU #1]", new Vector3(-16.5f, 4.5f, 0f));
        CreateRuntimeNode("Hielo_ISRU_02_CanalSW", "Hielo Subsuperficial Canal SW", "[HIELO ISRU #2]", new Vector3(-11.0f, -10.5f, 0f));
        CreateRuntimeNode("Hielo_ISRU_03_FosaNE", "Hielo Subsuperficial Fosa NE", "[HIELO ISRU #3]", new Vector3(14.5f, 2.0f, 0f));
    }

    private static void CreateRuntimeNode(string objName, string nName, string title, Vector3 pos)
    {
        var go = new GameObject(objName);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateIceSprite();

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.75f, 1.35f);

        var ph = go.AddComponent<MarsWorldPlaceholder2D>();
        ph.LabelTitle = title;
        ph.LabelSubtitle = "Hielo 100% — Mantén [E] para extraer Agua (-⚡)";

        var node = go.AddComponent<MarsIceDepositNode2D>();
        node.nodeName = nName;
        node.labelTitle = title;
    }
}

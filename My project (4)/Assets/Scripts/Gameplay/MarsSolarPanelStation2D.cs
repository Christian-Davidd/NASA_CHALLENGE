using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Estación de Paneles Solares en Marte (estilo NASA Surface Power Array / InSight UltraFlex).
/// Al mantener aplastado [E] junto al panel, la Energía sube progresivamente (en vez de
/// llenarse instantáneamente), simulando carga real de baterías fotovoltaicas.
/// </summary>
[RequireComponent(typeof(MarsWorldPlaceholder2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MarsSolarPanelStation2D : MonoBehaviour
{
    public static readonly List<MarsSolarPanelStation2D> AllStations = new List<MarsSolarPanelStation2D>();

    [SerializeField] private string panelName = "Paneles Solares";
    [SerializeField] private string labelTitle = "[PANELES SOLARES]";
    [SerializeField] private Sprite chargePulseSprite;
    [SerializeField] private Vector3 chargePulseOffset = new Vector3(0f, 0.25f, 0f);

    private MarsWorldPlaceholder2D placeholder;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer chargePulseRenderer;
    private bool usingGeneratedFallbackSprite;
    private float activeChargeTimer;
    private static Sprite solarGridPlaceholderSprite;

    private void Awake()
    {
        if (!AllStations.Contains(this))
            AllStations.Add(this);

        placeholder = GetComponent<MarsWorldPlaceholder2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = GetOrCreateSolarGridSprite();
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
                col.size = new Vector2(2.6f, 2.0f);
        }

        EnsureChargePulseRenderer();
        UpdateVisuals();
    }

    private void OnDestroy()
    {
        AllStations.Remove(this);
    }

    private void Update()
    {
        if (activeChargeTimer > 0f)
            activeChargeTimer = Mathf.Max(0f, activeChargeTimer - Time.deltaTime);

        UpdateChargePulseEffect();
        UpdateVisuals();
    }

    public string InteractHoldFromPlayer(float deltaTime)
    {
        activeChargeTimer = 0.16f;
        var flr = DONKIFLRManager.Instance;
        if (flr == null)
            return $"{panelName}: Celdas fotovoltaicas activas.";

        string msg = flr.RecargarEnPanelSolarContinuo(panelName, deltaTime);
        UpdateChargePulseEffect();
        UpdateVisuals();
        return msg;
    }

    private void EnsureChargePulseRenderer()
    {
        if (chargePulseSprite == null || chargePulseRenderer != null) return;

        var fxGo = new GameObject("ChargePulseFX");
        fxGo.transform.SetParent(transform, false);
        fxGo.transform.localPosition = chargePulseOffset;
        fxGo.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

        chargePulseRenderer = fxGo.AddComponent<SpriteRenderer>();
        chargePulseRenderer.sprite = chargePulseSprite;
        chargePulseRenderer.enabled = false;
    }

    private void UpdateChargePulseEffect()
    {
        if (chargePulseRenderer == null) return;

        bool charging = activeChargeTimer > 0f;
        chargePulseRenderer.enabled = charging;
        if (!charging) return;

        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * 12f);
        if (spriteRenderer != null)
            chargePulseRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
        chargePulseRenderer.color = new Color(1f, 0.95f, 0.65f, Mathf.Lerp(0.45f, 0.92f, wave));
        float scale = Mathf.Lerp(0.86f, 1.04f, wave);
        chargePulseRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        chargePulseRenderer.transform.localPosition = chargePulseOffset + new Vector3(0f, 0.05f * wave, 0f);
    }

    private void UpdateVisuals()
    {
        if (placeholder == null) return;

        var flr = DONKIFLRManager.Instance;
        float energia = flr != null ? flr.EnergiaActual : 100f;
        bool necesitaCarga = energia < 40f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4.5f);

        Color baseColor;
        string subtitle;

        if (activeChargeTimer > 0f)
        {
            baseColor = new Color(0.28f, 0.95f, 0.68f, 0.98f);
            subtitle = $"⚡ Cargando desde celdas solares... ({energia:F0}%)";
        }
        else if (necesitaCarga)
        {
            baseColor = Color.Lerp(
                new Color(0.18f, 0.72f, 1.0f, 0.95f),
                new Color(1.0f, 0.84f, 0.22f, 0.98f),
                pulse
            );
            subtitle = $"⚡ ¡Batería Baja ({energia:F0}%)! — Mantén [E] para cargar";
        }
        else if (energia < 99f)
        {
            baseColor = new Color(0.18f, 0.68f, 0.96f, 0.94f);
            subtitle = $"Energía: {energia:F0}% — Mantén [E] para recargar";
        }
        else
        {
            baseColor = new Color(0.16f, 0.58f, 0.92f, 0.92f);
            subtitle = "Celdas al 100% — Mantén [E] cuando necesites energía";
        }

        placeholder.SetVisualState(baseColor, labelTitle, subtitle, usingGeneratedFallbackSprite);
    }

    private static Sprite GetOrCreateSolarGridSprite()
    {
        if (solarGridPlaceholderSprite != null)
            return solarGridPlaceholderSprite;

        const int w = 80;
        const int h = 64;
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
                bool outerBorder = x < 3 || x >= w - 3 || y < 3 || y >= h - 3;
                bool cellGridX = (x % 20 == 0) || (x % 20 == 1);
                bool cellGridY = (y % 16 == 0) || (y % 16 == 1);

                if (outerBorder)
                {
                    pixels[y * w + x] = new Color(0.95f, 0.88f, 0.45f, 1f);
                }
                else if (cellGridX || cellGridY)
                {
                    pixels[y * w + x] = new Color(0.55f, 0.85f, 1.0f, 0.95f);
                }
                else
                {
                    float sheen = ((x + y) % 14 < 3) ? 0.92f : 0.78f;
                    pixels[y * w + x] = new Color(sheen * 0.45f, sheen * 0.78f, sheen, 0.92f);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        solarGridPlaceholderSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 40f);
        return solarGridPlaceholderSprite;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureDefaultStationsInScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureDefaultStationsInScene(scene);
    }

    private static void EnsureDefaultStationsInScene(Scene scene)
    {
        if (scene.name != "Mars_Natural_Landscape_01") return;
        if (AllStations.Count > 0) return;

        CreateRuntimeStation("Panel_Solar_01_Oeste", "Paneles Solares Oeste", "[PANELES SOLARES 1]", new Vector3(-6.8f, -4.6f, 0f));
        CreateRuntimeStation("Panel_Solar_02_Invernadero", "Paneles Solares del Invernadero", "[PANELES SOLARES 2]", new Vector3(4.2f, -6.8f, 0f));
    }

    private static void CreateRuntimeStation(string objName, string pName, string title, Vector3 pos)
    {
        var go = new GameObject(objName);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateSolarGridSprite();

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.8f, 1.4f);

        var ph = go.AddComponent<MarsWorldPlaceholder2D>();
        ph.LabelTitle = title;
        ph.LabelSubtitle = "Energía: 100% — Mantén [E] para recargar";

        var station = go.AddComponent<MarsSolarPanelStation2D>();
        station.panelName = pName;
        station.labelTitle = title;
    }
}

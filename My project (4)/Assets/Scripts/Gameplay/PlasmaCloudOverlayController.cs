using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PERSONA 1 - DEIVIS] Frente de Tormenta CME (Nube de Plasma) en coordenadas reales del mundo:
/// Compuesto exclusivamente por 8 siluetas orgánicas distintas sin bordes cuadrados (cero tiles rectangulares).
/// Un muro continuo de nubes de tormenta asoma desde el borde derecho y barre el mapa de forma independiente
/// al movimiento del astronauta o de la cámara, guiado por la velocidad real de la NASA (/DONKI/CME, en km/s).
/// </summary>
public sealed class PlasmaCloudOverlayController : MonoBehaviour
{
    public static PlasmaCloudOverlayController Instance { get; private set; }

    private sealed class WorldStormPuff
    {
        public SpriteRenderer renderer;
        public Transform transform;
        public Vector2 worldPos;
        public float speedMul;
        public float baseScale;
        public float baseAngle;
        public float rotSpeed;
        public float pulseAmp;
        public float pulseFreq;
        public float phase;
        public float baseAlpha;
        public float crestOffsetX;
        public bool isCrestNode;
    }

    [Header("8 Siluetas Orgánicas de Nubes de Plasma (Sin bordes cuadrados)")]
    [SerializeField] private Sprite foregroundBandSprite;
    [SerializeField] private Sprite cloudLargeSprite;
    [SerializeField] private Sprite cloudMediumSprite;
    [SerializeField] private Sprite cloudWispySprite;
    [SerializeField] private Sprite plasmaWaveSprite;
    [SerializeField] private Sprite vanguardWispSprite;
    [SerializeField] private Sprite interferenceArcSprite;
    [SerializeField] private Sprite mistOverlaySprite;

    [Header("Parámetros de Mapa y Barrido")]
    [SerializeField] private Vector2 mapSize = new Vector2(108f, 72f);
    [SerializeField] private int totalWorldClouds = 135;
    [SerializeField] private float dissipationSeconds = 4.5f;

    public float FillProgress { get; private set; }
    public float MaxTargetDensity { get; private set; } = 1f;
    public float CoberturaPorcentaje => Mathf.Clamp01(FillProgress) * 100f;

    public string FaseActualTexto
    {
        get
        {
            if (StormVisibilityAlpha <= 0.01f) return "Despejado";
            if (FillProgress < 0.30f) return "Muro de plasma ingresando por el horizonte";
            if (FillProgress < 0.85f) return "Tormenta solar barriendo el sector";
            return "Sector envuelto en tormenta de plasma";
        }
    }

    private float StormVisibilityAlpha { get; set; }

    private readonly List<WorldStormPuff> stormPuffs = new List<WorldStormPuff>();

    private Transform containerRoot;
    private bool wasStormActive;
    private bool stormWallInitialized;
    private float wallFrontWorldX;
    private float wallCrestWidth = 12f;
    private float stormTime;

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
    }

    private void Start()
    {
        BuildOrganicCloudPool();
    }

    private void Update()
    {
        bool isExteriorScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Mars_Natural_Landscape_01";
        if (containerRoot != null && containerRoot.gameObject.activeSelf != isExteriorScene)
        {
            containerRoot.gameObject.SetActive(isExteriorScene);
        }
        var cme = DONKICMEManager.Instance;
        bool tormentaActiva = cme != null && cme.HayTormentaCMEActiva;

        float speedKmS = cme != null && cme.VelocidadActualKmS > 0f ? cme.VelocidadActualKmS : 680f;
        float speedNorm = Mathf.Clamp01(Mathf.InverseLerp(350f, 1200f, speedKmS));

        if (tormentaActiva)
        {
            if (!wasStormActive && StormVisibilityAlpha <= 0.02f)
            {
                InitializeStormEntryFromRightEdge();
            }

            // A mayor velocidad NASA (km/s), más rápido barre la tormenta el mapa
            float fillDuration = Mathf.Lerp(22f, 7.5f, speedNorm);
            MaxTargetDensity = Mathf.Lerp(0.85f, 1.0f, speedNorm);
            FillProgress = Mathf.MoveTowards(FillProgress, MaxTargetDensity, Time.deltaTime / fillDuration);
            StormVisibilityAlpha = Mathf.MoveTowards(StormVisibilityAlpha, 1f, Time.deltaTime / 0.8f);
        }
        else
        {
            StormVisibilityAlpha = Mathf.MoveTowards(StormVisibilityAlpha, 0f, Time.deltaTime / Mathf.Max(0.5f, dissipationSeconds));
            if (StormVisibilityAlpha <= 0.002f)
            {
                FillProgress = 0f;
                stormWallInitialized = false;
            }
        }

        wasStormActive = tormentaActiva;

        float wallAdvanceSpeed = Mathf.Lerp(2.6f, 7.0f, speedNorm);
        float windSpeed = wallAdvanceSpeed * 0.56f;
        stormTime += Time.deltaTime * (0.75f + speedNorm * 0.85f);

        UpdateWorldStorm(wallAdvanceSpeed, windSpeed, tormentaActiva);
    }

    private void InitializeStormEntryFromRightEdge()
    {
        var cam = Camera.main;
        float camRight = 12f;
        float camW = 24f;
        if (cam != null)
        {
            camW = cam.orthographicSize * 2f * cam.aspect;
            camRight = cam.transform.position.x + camW * 0.5f;
        }

        wallCrestWidth = Mathf.Max(9f, camW * 0.45f);
        wallFrontWorldX = camRight + 3.5f;
        stormWallInitialized = true;
    }

    private void BuildOrganicCloudPool()
    {
        containerRoot = new GameObject("PlasmaStormWorld_OrganicClouds").transform;
        containerRoot.SetParent(transform, false);

        var allSprites = new List<Sprite>();
        if (cloudLargeSprite != null) allSprites.Add(cloudLargeSprite);
        if (cloudMediumSprite != null) allSprites.Add(cloudMediumSprite);
        if (cloudWispySprite != null) allSprites.Add(cloudWispySprite);
        if (vanguardWispSprite != null) allSprites.Add(vanguardWispSprite);
        if (plasmaWaveSprite != null) allSprites.Add(plasmaWaveSprite);
        if (foregroundBandSprite != null) allSprites.Add(foregroundBandSprite);
        if (interferenceArcSprite != null) allSprites.Add(interferenceArcSprite);
        if (mistOverlaySprite != null) allSprites.Add(mistOverlaySprite);

        if (allSprites.Count == 0) return;

        var rng = new System.Random(20261114);
        float worldSpanW = mapSize.x + 32f; // [-70 .. +70]
        float worldSpanH = mapSize.y + 20f; // [-46 .. +46]

        // 1. Banco frontal orgánico del muro de tormenta (42 nubes solapadas en 2-3 capas verticales irregulares)
        int crestCount = 42;
        for (int i = 0; i < crestCount; i++)
        {
            Sprite sprite = allSprites[(i * 3 + rng.Next(allSprites.Count)) % allSprites.Count];
            var go = new GameObject($"StormWallCrestCloud_{i:00}");
            go.transform.SetParent(containerRoot, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 105 + (i % 4);
            sr.flipX = (rng.NextDouble() > 0.5);
            sr.flipY = (rng.NextDouble() > 0.5);
            float tint = 0.88f + (float)rng.NextDouble() * 0.12f;
            sr.color = new Color(tint, tint * 0.84f, 1f, 0f);
            sr.enabled = false;

            float spriteW = Mathf.Max(0.5f, sprite.bounds.size.x);
            float desiredW = 8.5f + (float)rng.NextDouble() * 6.0f;
            float wy = Mathf.Lerp(-worldSpanH * 0.5f, worldSpanH * 0.5f, i / (float)(crestCount - 1))
                       + ((float)rng.NextDouble() * 2f - 1f) * 2.2f;

            stormPuffs.Add(new WorldStormPuff
            {
                renderer = sr,
                transform = go.transform,
                worldPos = new Vector2(0f, wy),
                crestOffsetX = ((float)rng.NextDouble() * 6.8f) + (i % 2) * 1.8f,
                speedMul = 1f,
                baseScale = desiredW / spriteW,
                baseAngle = ((float)rng.NextDouble() * 2f - 1f) * 32f,
                rotSpeed = ((float)rng.NextDouble() * 2f - 1f) * 2.5f,
                pulseAmp = 0.05f + (float)rng.NextDouble() * 0.06f,
                pulseFreq = 0.45f + (float)rng.NextDouble() * 0.55f,
                phase = (float)(rng.NextDouble() * Mathf.PI * 2.0),
                baseAlpha = 0.24f + (float)rng.NextDouble() * 0.10f,
                isCrestNode = true
            });
        }

        // 2. Nubes distribuidas por todo el escenario en coordenadas del mundo (sin cuadrícula rígida ni bordes)
        int cols = 15;
        int rows = Mathf.Max(10, Mathf.CeilToInt(totalWorldClouds / (float)cols));
        float stepX = worldSpanW / cols;
        float stepY = worldSpanH / rows;
        int bodyIdx = 0;

        for (int r = 0; r < rows; r++)
        {
            float rowOffset = (r % 2 == 0) ? 0f : stepX * 0.50f;
            float baseY = -worldSpanH * 0.5f + (r + 0.5f) * stepY;

            for (int c = 0; c < cols; c++)
            {
                float baseX = -worldSpanW * 0.5f + (c + 0.5f) * stepX + rowOffset - stepX * 0.25f;
                float jx = ((float)rng.NextDouble() * 2f - 1f) * (stepX * 0.45f);
                float jy = ((float)rng.NextDouble() * 2f - 1f) * (stepY * 0.45f);

                Sprite sprite = allSprites[(bodyIdx + rng.Next(allSprites.Count)) % allSprites.Count];
                var go = new GameObject($"StormBodyCloud_{bodyIdx:000}");
                go.transform.SetParent(containerRoot, false);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 100 + (bodyIdx % 5);
                sr.flipX = (rng.NextDouble() > 0.5);
                sr.flipY = (rng.NextDouble() > 0.5);
                float tint = 0.87f + (float)rng.NextDouble() * 0.13f;
                sr.color = new Color(tint, tint * 0.85f, 1f, 0f);
                sr.enabled = false;

                float spriteW = Mathf.Max(0.5f, sprite.bounds.size.x);
                float desiredW = 10.5f + (float)rng.NextDouble() * 6.5f;

                stormPuffs.Add(new WorldStormPuff
                {
                    renderer = sr,
                    transform = go.transform,
                    worldPos = new Vector2(baseX + jx, baseY + jy),
                    crestOffsetX = 0f,
                    speedMul = 0.82f + (float)rng.NextDouble() * 0.36f,
                    baseScale = desiredW / spriteW,
                    baseAngle = ((float)rng.NextDouble() * 2f - 1f) * 35f,
                    rotSpeed = ((float)rng.NextDouble() * 2f - 1f) * 2.0f,
                    pulseAmp = 0.04f + (float)rng.NextDouble() * 0.06f,
                    pulseFreq = 0.40f + (float)rng.NextDouble() * 0.55f,
                    phase = (float)(rng.NextDouble() * Mathf.PI * 2.0),
                    baseAlpha = 0.19f + (float)rng.NextDouble() * 0.10f,
                    isCrestNode = false
                });
                bodyIdx++;
            }
        }
    }

    private void UpdateWorldStorm(float wallAdvanceSpeed, float windSpeed, bool tormentaActiva)
    {
        if (StormVisibilityAlpha <= 0.002f)
        {
            DisableAllSprites();
            return;
        }

        if (!stormWallInitialized)
            InitializeStormEntryFromRightEdge();

        var cam = Camera.main;
        float camH = cam != null ? cam.orthographicSize * 2f : 12f;
        float camW = cam != null ? camH * cam.aspect : 22f;
        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
        float camLeft = camPos.x - camW * 0.5f;
        float camRight = camPos.x + camW * 0.5f;
        float camBottom = camPos.y - camH * 0.5f;
        float camTop = camPos.y + camH * 0.5f;

        float minWorldX = -mapSize.x * 0.5f - 16f;
        float maxWorldX = mapSize.x * 0.5f + 16f;
        float worldSpanW = maxWorldX - minWorldX;
        float wallMinLimitX = minWorldX - wallCrestWidth;

        // El muro barre el mapa en coordenadas reales del mundo (totalmente independiente del astronauta)
        if (tormentaActiva && wallFrontWorldX > wallMinLimitX)
        {
            wallFrontWorldX = Mathf.Max(wallMinLimitX, wallFrontWorldX - wallAdvanceSpeed * Time.deltaTime);
        }

        for (int i = 0; i < stormPuffs.Count; i++)
        {
            var p = stormPuffs[i];

            float worldX;
            if (p.isCrestNode)
            {
                worldX = wallFrontWorldX + p.crestOffsetX;
            }
            else
            {
                p.worldPos.x -= windSpeed * p.speedMul * Time.deltaTime;
                if (p.worldPos.x < minWorldX)
                    p.worldPos.x += worldSpanW;
                worldX = p.worldPos.x;
            }

            // Ninguna nube aparece por delante del frente del muro
            float distBehindFront = worldX - wallFrontWorldX;
            float reveal = p.isCrestNode
                ? Mathf.Clamp01(distBehindFront / 1.5f + 0.65f)
                : Mathf.Clamp01(distBehindFront / 4.2f);
            if (reveal <= 0.005f)
            {
                if (p.renderer.enabled) p.renderer.enabled = false;
                continue;
            }

            float puffHalfW = p.renderer.sprite.bounds.size.x * p.baseScale * 0.60f;
            float puffHalfH = p.renderer.sprite.bounds.size.y * p.baseScale * 0.60f;

            // Ocultar fuera de cámara sin mover su posición en el mundo
            if (worldX + puffHalfW < camLeft - 2f || worldX - puffHalfW > camRight + 2f ||
                p.worldPos.y + puffHalfH < camBottom - 2f || p.worldPos.y - puffHalfH > camTop + 2f)
            {
                if (p.renderer.enabled) p.renderer.enabled = false;
                continue;
            }

            if (!p.renderer.enabled)
                p.renderer.enabled = true;

            float t = stormTime * p.pulseFreq + p.phase;
            float swayX = p.isCrestNode ? Mathf.Cos(t * 0.65f) * 0.85f : 0f;
            float swayY = Mathf.Sin(t * 0.85f) * 0.60f;

            p.transform.position = new Vector3(worldX + swayX, p.worldPos.y + swayY, 0f);
            p.transform.rotation = Quaternion.Euler(0f, 0f, p.baseAngle + Mathf.Sin(t * 0.45f) * (3f + p.rotSpeed));

            float scaleBreath = 1f + Mathf.Sin(t) * p.pulseAmp;
            float s = p.baseScale * scaleBreath;
            p.transform.localScale = new Vector3(s, s, 1f);

            Color c = p.renderer.color;
            c.a = p.baseAlpha * reveal * StormVisibilityAlpha;
            p.renderer.color = c;
        }
    }

    private void DisableAllSprites()
    {
        for (int i = 0; i < stormPuffs.Count; i++)
            if (stormPuffs[i].renderer.enabled) stormPuffs[i].renderer.enabled = false;
    }
}

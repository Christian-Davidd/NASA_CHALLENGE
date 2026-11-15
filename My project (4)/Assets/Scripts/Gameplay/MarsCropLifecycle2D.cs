using UnityEngine;

/// <summary>Interactive seed, growth, maturity and harvest cycle for one crop bank.</summary>
public sealed class MarsCropLifecycle2D : MonoBehaviour
{
    private enum CropStage { Seed, Growing, Mature }

    [SerializeField] private string cropName = "Cultivo";
    [SerializeField, TextArea(2, 5)] private string cropDescription;
    [SerializeField] private Sprite seedSprite;
    [SerializeField] private Sprite growingSprite;
    [SerializeField] private Sprite matureSprite;
    [SerializeField] private SpriteRenderer[] plants;
    [SerializeField, Min(1f)] private float seedStageSeconds = 18f;
    [SerializeField, Min(1f)] private float growingStageSeconds = 42f;
    [SerializeField, Min(0.01f)] private float seedPlantScale = 0.82f;
    [SerializeField, Min(0.01f)] private float canopyPlantScale = 0.44f;

    private CropStage stage;
    private float stageElapsed;
    public int HarvestedCount { get; private set; }
    public string CropName => cropName;
    public string CropDescription => cropDescription;
    public Sprite CurrentSprite => StageSprite;
    public bool CanHarvest => stage == CropStage.Mature;
    public float StageProgress => stage switch
    {
        CropStage.Seed => Mathf.Clamp01(stageElapsed / Mathf.Max(1f, seedStageSeconds)),
        CropStage.Growing => Mathf.Clamp01(stageElapsed / Mathf.Max(1f, growingStageSeconds)),
        _ => 1f
    };
    public string StageTimeLabel => stage switch
    {
        CropStage.Seed => $"Germinación en {FormatTime(seedStageSeconds - stageElapsed)}",
        CropStage.Growing => $"Madurez estimada en {FormatTime(growingStageSeconds - stageElapsed)}",
        _ => "Cosecha disponible"
    };
    public string StageLabel => stage switch
    {
        CropStage.Seed => "SEMILLA · sustrato hidratado",
        CropStage.Growing => "CRECIMIENTO · hojas en desarrollo",
        _ => "MADURO · listo para cosechar"
    };

    public string ActionPrompt => stage switch
    {
        CropStage.Seed => "Germinación en curso · crecimiento automático",
        CropStage.Growing => "Desarrollo en curso · crecimiento automático",
        _ => "[E] Cosechar y reiniciar el ciclo"
    };

    public string InteractionPrompt => $"[E] Inspeccionar cultivo: {cropName}";

    private Sprite StageSprite => stage switch
    {
        CropStage.Seed => seedSprite,
        CropStage.Growing => growingSprite,
        _ => matureSprite
    };

    private void Awake() => ApplyStageSprite();

    private void Update()
    {
        if (stage == CropStage.Mature) return;
        stageElapsed += Time.deltaTime;

        if (stage == CropStage.Seed && stageElapsed >= seedStageSeconds)
        {
            stageElapsed -= seedStageSeconds;
            stage = CropStage.Growing;
            ApplyStageSprite();
        }

        if (stage == CropStage.Growing && stageElapsed >= growingStageSeconds)
        {
            stage = CropStage.Mature;
            stageElapsed = 0f;
            ApplyStageSprite();
        }
    }

    public bool Harvest()
    {
        if (stage != CropStage.Mature) return false;
        HarvestedCount++;
        stage = CropStage.Seed;
        stageElapsed = 0f;
        ApplyStageSprite();
        return true;
    }

    private void ApplyStageSprite()
    {
        if (plants == null) return;
        foreach (var plant in plants)
        {
            if (plant == null) continue;
            plant.sprite = StageSprite;
            plant.transform.localScale = Vector3.one * (stage == CropStage.Seed ? seedPlantScale : canopyPlantScale);
        }
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}

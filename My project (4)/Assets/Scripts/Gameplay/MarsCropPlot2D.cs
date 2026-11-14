using UnityEngine;

/// <summary>
/// [PERSONA 2] Cuadrado placeholder para la Zona de Cultivos / Invernadero Marciano.
/// Permite interactuar con [E] para avanzar las etapas base del cultivo y deja listo
/// el objeto con su etiqueta para colocar después el asset definitivo.
/// </summary>
[RequireComponent(typeof(MarsWorldPlaceholder2D))]
public sealed class MarsCropPlot2D : MonoBehaviour
{
    public static MarsCropPlot2D Instance { get; private set; }

    [SerializeField] private string zoneLabel = "[ZONA DE CULTIVOS]";
    private MarsWorldPlaceholder2D placeholder;
    private int currentStep;
    public int HarvestedCropsCount { get; private set; }

    public string CurrentStatusText
    {
        get
        {
            switch (currentStep)
            {
                case 0: return "Suelo listo — [E] Preparar sustrato";
                case 1: return "Sustrato preparado — [E] Sembrar semillas";
                case 2: return "Semillas plantadas — [E] Regar cultivo";
                default: return "¡Brote maduro! — [E] Cosechar cultivo";
            }
        }
    }

    private void Awake()
    {
        Instance = this;
        placeholder = GetComponent<MarsWorldPlaceholder2D>();
        UpdateVisuals();
    }

    public string InteractFromPlayer()
    {
        string msg;
        switch (currentStep)
        {
            case 0:
                currentStep = 1;
                msg = "Zona de Cultivos: Sustrato marciano oxigenado. Presiona [E] para sembrar.";
                break;
            case 1:
                currentStep = 2;
                msg = "Zona de Cultivos: Semillas plantadas. Presiona [E] para regar.";
                break;
            case 2:
                currentStep = 3;
                msg = "Zona de Cultivos: Cultivo regado y listo para cosechar con [E].";
                break;
            default:
                currentStep = 0;
                HarvestedCropsCount++;
                msg = $"¡Cultivo cosechado con éxito! Total cosechas: {HarvestedCropsCount}.";
                break;
        }

        UpdateVisuals();
        return msg;
    }

    private void UpdateVisuals()
    {
        if (placeholder == null) return;

        Color c;
        switch (currentStep)
        {
            case 0: c = new Color(0.28f, 0.78f, 0.42f, 0.92f); break;
            case 1: c = new Color(0.36f, 0.85f, 0.35f, 0.92f); break;
            case 2: c = new Color(0.20f, 0.90f, 0.60f, 0.95f); break;
            default: c = new Color(0.45f, 1.00f, 0.30f, 0.96f); break;
        }

        string subtitle = HarvestedCropsCount > 0
            ? $"{CurrentStatusText} (Cosechados: {HarvestedCropsCount})"
            : CurrentStatusText;

        placeholder.SetVisualState(c, zoneLabel, subtitle);
    }
}

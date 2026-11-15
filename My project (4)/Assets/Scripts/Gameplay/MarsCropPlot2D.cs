using UnityEngine;

/// <summary>
/// [PERSONA 2] Cuadrado placeholder para la Zona de Cultivos / Invernadero Marciano.
/// Permite interactuar con [E] para avanzar las etapas base del cultivo consumiendo
/// Energía (luces/bombas) y Agua al regar.
/// </summary>
[RequireComponent(typeof(MarsWorldPlaceholder2D))]
public sealed class MarsCropPlot2D : MonoBehaviour
{
    public static MarsCropPlot2D Instance { get; private set; }
    public static int TotalHarvestedCrops { get; private set; }

    public static void RegisterInteriorHarvestCycle()
    {
        TotalHarvestedCrops++;
    }

    public static void ResetAllCrops()
    {
        TotalHarvestedCrops = 0;
        if (Instance != null)
        {
            Instance.currentStep = 0;
            Instance.UpdateVisuals();
        }
    }

    [SerializeField] private string zoneLabel = "[ZONA DE CULTIVOS]";
    private MarsWorldPlaceholder2D placeholder;
    private int currentStep;
    public int HarvestedCropsCount => TotalHarvestedCrops;

    public string CurrentStatusText
    {
        get
        {
            switch (currentStep)
            {
                case 0: return "Suelo listo — [E] Preparar sustrato";
                case 1: return "Sustrato preparado — [E] Sembrar semillas";
                case 2: return "Semillas plantadas — [E] Regar (usa Agua y ⚡)";
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
        float costoEnergia;
        float costoAgua = 0f;
        string accion;
        switch (currentStep)
        {
            case 0:
                costoEnergia = 5f;
                accion = "oxigenar sustrato";
                break;
            case 1:
                costoEnergia = 5f;
                accion = "sembrar semillas";
                break;
            case 2:
                costoEnergia = 8f;
                costoAgua = 12f;
                accion = "bombear y regar agua al cultivo";
                break;
            default:
                costoEnergia = 6f;
                accion = "cosechar y procesar cultivo";
                break;
        }

        string notaCosto = "";
        if (DONKIFLRManager.Instance != null)
        {
            if (!DONKIFLRManager.Instance.IntentarConsumirRecursosCultivo(costoEnergia, costoAgua, accion, out string msgCosto))
            {
                return msgCosto;
            }
            notaCosto = " " + msgCosto;
        }

        string msg;
        switch (currentStep)
        {
            case 0:
                currentStep = 1;
                msg = $"Zona de Cultivos: Sustrato marciano oxigenado.{notaCosto} Presiona [E] para sembrar.";
                break;
            case 1:
                currentStep = 2;
                msg = $"Zona de Cultivos: Semillas plantadas.{notaCosto} Presiona [E] para regar agua.";
                break;
            case 2:
                currentStep = 3;
                msg = $"Zona de Cultivos: Agua y nutrientes bombeados al cultivo.{notaCosto} Listo para cosechar con [E].";
                break;
            default:
                currentStep = 0;
                TotalHarvestedCrops++;
                msg = $"¡Cultivo cosechado con éxito! Total cosechas: {TotalHarvestedCrops}.{notaCosto}";
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

        string subtitle = TotalHarvestedCrops > 0
            ? $"{CurrentStatusText} (Cosechados: {TotalHarvestedCrops})"
            : CurrentStatusText;

        placeholder.SetVisualState(c, zoneLabel, subtitle);
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MarsInteractable2D : MonoBehaviour
{
    [SerializeField] private string displayName = "Formación geológica";
    [SerializeField, TextArea] private string result = "Muestra geológica registrada.";
    [SerializeField] private bool repeatable;
    private bool isComplete;

    public string DisplayName => displayName;
    public bool IsComplete => isComplete && !repeatable;
    public string InteractionPrompt => $"[E] Abrir monitor: {displayName}";

    public string Interact()
    {
        if (isComplete && !repeatable)
        {
            MarsMonitorScreen2D.OpenInformation(displayName, result);
            return displayName + ": ya se registró la muestra.";
        }

        // En el interior del Módulo Agrícola (Cultivation_Module_Interior), las pantallas
        // consumen Energía y el circuito de riego de agua/nutrientes consume Energía + Agua.
        string resourceNote = "";
        if (SceneManager.GetActiveScene().name == "Cultivation_Module_Interior" && DONKIFLRManager.Instance != null)
        {
            string lower = (displayName ?? "").ToLowerInvariant();
            bool esRiegoAgua = lower.Contains("agua") || lower.Contains("nutriente") || lower.Contains("riego");
            float costoEnergia = esRiegoAgua ? 7f : 4f;
            float costoAgua = esRiegoAgua ? 10f : 0f;
            string motivo = esRiegoAgua
                ? "bombear agua y nutrientes en el invernadero"
                : $"mostrar datos en pantalla ({displayName})";

            if (!DONKIFLRManager.Instance.IntentarConsumirRecursosCultivo(costoEnergia, costoAgua, motivo, out string msgCosto))
            {
                return msgCosto;
            }

            if (esRiegoAgua)
            {
                MarsCropPlot2D.RegisterInteriorHarvestCycle();
                resourceNote = $" {msgCosto} | Cultivos regados/cosechados: {MarsCropPlot2D.TotalHarvestedCrops}";
            }
            else
            {
                resourceNote = " " + msgCosto;
            }
        }

        if (!repeatable) isComplete = true;
        Debug.Log("[Mars Survey] " + displayName + " — " + result, this);
        MarsMonitorScreen2D.OpenInformation(displayName, result);
        return displayName + " — " + result + resourceNote;
    }
}

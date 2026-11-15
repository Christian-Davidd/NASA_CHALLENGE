using UnityEngine;

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

        if (!repeatable) isComplete = true;
        Debug.Log("[Mars Survey] " + displayName + " — " + result, this);
        MarsMonitorScreen2D.OpenInformation(displayName, result);
        return displayName + " — " + result;
    }
}

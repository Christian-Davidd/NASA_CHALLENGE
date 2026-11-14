using UnityEngine;

public sealed class MarsInteractable2D : MonoBehaviour
{
    [SerializeField] private string displayName = "Formación geológica";
    [SerializeField, TextArea] private string result = "Muestra geológica registrada.";
    private bool isComplete;

    public string DisplayName => displayName;
    public bool IsComplete => isComplete;

    public string Interact()
    {
        if (isComplete)
            return displayName + ": ya se registró la muestra.";

        isComplete = true;
        Debug.Log("[Mars Survey] " + displayName + " — " + result, this);
        return displayName + " — " + result;
    }
}

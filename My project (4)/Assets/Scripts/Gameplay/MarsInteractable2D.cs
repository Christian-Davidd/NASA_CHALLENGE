using UnityEngine;

public sealed class MarsInteractable2D : MonoBehaviour
{
    [SerializeField] private string displayName = "Formación geológica";
    [SerializeField, TextArea] private string result = "Muestra geológica registrada.";
    [SerializeField] private bool repeatable;
    private bool isComplete;

    public string DisplayName => displayName;
    public bool IsComplete => isComplete && !repeatable;

    public string Interact()
    {
        if (isComplete && !repeatable)
            return displayName + ": ya se registró la muestra.";

        if (!repeatable) isComplete = true;
        Debug.Log("[Mars Survey] " + displayName + " — " + result, this);
        return displayName + " — " + result;
    }
}

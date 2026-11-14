using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PERSONA 2] Nodo cuadrado de Muestra Geológica esparcida por el mapa.
/// Muestra su etiqueta flotante en el mundo y actualiza el contador global de muestras en el HUD.
/// </summary>
[RequireComponent(typeof(MarsWorldPlaceholder2D))]
public sealed class MarsSampleNode2D : MonoBehaviour
{
    private static readonly HashSet<MarsSampleNode2D> AllNodes = new HashSet<MarsSampleNode2D>();
    public static int TotalSamples => AllNodes.Count;
    public static int CollectedSamples { get; private set; }

    [Header("Datos de la Muestra")]
    [SerializeField] private string sampleLabel = "[MUESTRA GEOLÓGICA #1]";
    [SerializeField] private string sampleDescription = "Muestra de regolito y minerales marcianos.";

    public bool IsCollected { get; private set; }
    private MarsWorldPlaceholder2D placeholder;

    private static readonly Color UncollectedColor = new Color(1.0f, 0.78f, 0.18f, 0.94f);
    private static readonly Color CollectedColor = new Color(0.42f, 0.55f, 0.62f, 0.65f);

    private void Awake()
    {
        placeholder = GetComponent<MarsWorldPlaceholder2D>();
    }

    private void OnEnable()
    {
        if (AllNodes.Add(this) && IsCollected)
        {
            RecalculateCollectedCount();
        }
        RecalculateCollectedCount();
        UpdateVisuals();
    }

    private void OnDisable()
    {
        AllNodes.Remove(this);
        RecalculateCollectedCount();
    }

    private static void RecalculateCollectedCount()
    {
        int count = 0;
        foreach (var node in AllNodes)
        {
            if (node != null && node.IsCollected)
                count++;
        }
        CollectedSamples = count;
    }

    public string CollectSample()
    {
        if (IsCollected)
            return $"{sampleLabel}: ya fue recolectada.";

        IsCollected = true;
        RecalculateCollectedCount();
        UpdateVisuals();
        return $"{sampleLabel} recolectada ({CollectedSamples}/{TotalSamples}) — {sampleDescription}";
    }

    private void UpdateVisuals()
    {
        if (placeholder == null)
            placeholder = GetComponent<MarsWorldPlaceholder2D>();
        if (placeholder == null) return;

        if (IsCollected)
        {
            placeholder.SetVisualState(
                CollectedColor,
                sampleLabel,
                "Recolectada ✓"
            );
        }
        else
        {
            placeholder.SetVisualState(
                UncollectedColor,
                sampleLabel,
                "Presiona [E] para recoger muestra"
            );
        }
    }
}

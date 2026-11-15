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
    private static int bonusSamples;
    public static int TotalSamples => AllNodes.Count + bonusSamples;
    public static int CollectedSamples { get; private set; }

    [Header("Datos de la Muestra")]
    [SerializeField] private string sampleLabel = "[MUESTRA GEOLÓGICA #1]";
    [SerializeField] private string sampleDescription = "Muestra de regolito y minerales marcianos.";

    public bool IsCollected { get; private set; }
    public bool IsAvailable => isActiveAndEnabled && !IsCollected && reservedBy == null;
    private RobotCompanionFollow2D reservedBy;
    private MarsWorldPlaceholder2D placeholder;

    public static void AddBonusSample()
    {
        bonusSamples++;
        RecalculateCollectedCount();
    }

    public static bool AllMapSamplesCollected =>
        AllNodes.Count > 0 && CollectedSamples - bonusSamples == AllNodes.Count;

    public static void GetAvailableSamples(UnityEngine.SceneManagement.Scene scene, List<MarsSampleNode2D> results)
    {
        results.Clear();
        foreach (var node in AllNodes)
        {
            if (node != null && node.gameObject.scene == scene && node.IsAvailable)
                results.Add(node);
        }
    }

    public bool TryReserveForRover(RobotCompanionFollow2D rover)
    {
        if (rover == null || !IsAvailable) return false;
        reservedBy = rover;
        UpdateVisuals();
        return true;
    }

    public void ReleaseRoverReservation(RobotCompanionFollow2D rover)
    {
        if (reservedBy != rover) return;
        reservedBy = null;
        UpdateVisuals();
    }

    public bool TryDeliverFromRover(RobotCompanionFollow2D rover, out string message)
    {
        message = null;
        if (!isActiveAndEnabled || IsCollected || rover == null || reservedBy != rover) return false;

        reservedBy = null;
        message = CollectSample();
        return true;
    }

    private static readonly Color UncollectedColor = new Color(1.0f, 0.78f, 0.18f, 0.94f);
    private static readonly Color CollectedColor = new Color(0.42f, 0.55f, 0.62f, 0.65f);

    private void Awake()
    {
        placeholder = GetComponent<MarsWorldPlaceholder2D>();
    }

    private void OnEnable()
    {
        AllNodes.Add(this);
        RecalculateCollectedCount();
        UpdateVisuals();
    }

    private void OnDisable()
    {
        reservedBy = null;
        AllNodes.Remove(this);
        if (AllNodes.Count == 0)
            bonusSamples = 0;
        RecalculateCollectedCount();
    }

    private static void RecalculateCollectedCount()
    {
        int count = bonusSamples;
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
        if (reservedBy != null)
            return "El rover lleva esta muestra.";

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
        else if (reservedBy != null)
        {
            placeholder.SetVisualState(CollectedColor, sampleLabel, "Recogida por el rover");
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

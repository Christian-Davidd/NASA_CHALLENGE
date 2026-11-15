using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PERSONA 2] Hallazgo científico que puede recoger el astronauta o el rover.
/// Muestra su etiqueta flotante en el mundo y actualiza el contador global de muestras en el HUD.
/// </summary>
[RequireComponent(typeof(MarsWorldPlaceholder2D))]
public sealed class MarsSampleNode2D : MonoBehaviour
{
    private static readonly HashSet<MarsSampleNode2D> AllNodes = new HashSet<MarsSampleNode2D>();
    private static int bonusSamples;
    private static int mapSampleCount;
    public static int TotalSamples => mapSampleCount + bonusSamples;
    internal static MarsSampleNode2D LatestDiscovery { get; private set; }
    internal static int DiscoveryVersion { get; private set; }
    public static int CollectedSamples { get; private set; }

    [Header("Datos de la Muestra")]
    [SerializeField] private string sampleLabel = "[MUESTRA GEOLÓGICA #1]";
    [SerializeField] private string sampleDescription = "Muestra de regolito y minerales marcianos.";

    [SerializeField] private string discoveryId;

    internal MarsDiscovery Discovery { get; private set; }
    internal bool DeliveredByRover { get; private set; }
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
        mapSampleCount > 0 && CollectedSamples - bonusSamples == mapSampleCount;

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
        message = CollectSample(true);
        return true;
    }

    private static readonly Color UncollectedColor = new Color(1.0f, 0.78f, 0.18f, 0.94f);
    private static readonly Color CollectedColor = new Color(0.42f, 0.55f, 0.62f, 0.65f);

    private void Awake()
    {
        placeholder = GetComponent<MarsWorldPlaceholder2D>();
        Discovery = MarsDiscovery.Find(discoveryId);
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
        if (LatestDiscovery == this)
            LatestDiscovery = null;
        if (AllNodes.Count == 0)
            bonusSamples = 0;
        RecalculateCollectedCount();
    }

    private static void RecalculateCollectedCount()
    {
        int count = bonusSamples;
        mapSampleCount = 0;
        foreach (var node in AllNodes)
        {
            if (node == null || node.Discovery?.isEquipment == true) continue;
            mapSampleCount++;
            if (node.IsCollected)
                count++;
        }
        CollectedSamples = count;
    }

    public string CollectSample() => CollectSample(false);

    private string CollectSample(bool deliveredByRover)
    {
        if (IsCollected)
            return $"{sampleLabel}: ya fue recolectada.";
        if (reservedBy != null)
            return "El rover lleva esta muestra.";

        IsCollected = true;
        DeliveredByRover = deliveredByRover;
        if (Discovery != null)
        {
            LatestDiscovery = this;
            DiscoveryVersion++;
        }

        RecalculateCollectedCount();
        UpdateVisuals();
        if (Discovery != null)
            return $"Hallazgo registrado: {Discovery.title}.";

        return $"{sampleLabel} recolectada ({CollectedSamples}/{TotalSamples}) — {sampleDescription}";
    }

    private void UpdateVisuals()
    {
        if (placeholder == null)
            placeholder = GetComponent<MarsWorldPlaceholder2D>();
        if (placeholder == null) return;

        if (Discovery != null)
        {
            bool visible = IsAvailable;
            GetComponent<SpriteRenderer>().enabled = visible;
            placeholder.enabled = visible;
            if (visible)
                placeholder.SetVisualState(Color.white, Discovery.title, "Pulsa [E] para recoger");
            return;
        }

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

[System.Serializable]
internal sealed class MarsDiscovery
{
    public string id;
    public string title;
    public string instrument;
    public string description;
    public string source;
    public string credit;
    public bool isEquipment;

    [System.Serializable]
    private sealed class Catalog
    {
        public MarsDiscovery[] entries;
    }

    private const string ResourceFolder = "MarsDiscoveries/";
    private static Catalog catalog;

    public static MarsDiscovery Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (catalog == null)
        {
            var data = Resources.Load<TextAsset>(ResourceFolder + "Catalog");
            if (data == null) return null;
            catalog = JsonUtility.FromJson<Catalog>(data.text);
        }

        return System.Array.Find(catalog.entries, entry => entry.id == id);
    }

    public Texture2D LoadReference() => Resources.Load<Texture2D>(ResourceFolder + "photo_" + id);
}

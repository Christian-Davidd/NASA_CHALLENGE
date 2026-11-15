using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PERSONA 2] Hallazgo científico / Muestra Geológica que puede recoger el astronauta o el rover.
/// Integra el catálogo NASA (MAHLI/SAM/CheMin + Hallazgos) con el bucle realista EVA:
/// el astronauta lleva 1 muestra física a la vez (1/1) y vuelve a la Nave para depositarla antes de salir por otra.
/// </summary>
[RequireComponent(typeof(MarsWorldPlaceholder2D))]
public sealed class MarsSampleNode2D : MonoBehaviour
{
    private static readonly HashSet<MarsSampleNode2D> AllNodes = new HashSet<MarsSampleNode2D>();
    private static readonly HashSet<string> KnownMapSampleKeys = new HashSet<string>();
    private static readonly HashSet<string> PickedNodeKeys = new HashSet<string>();
    private static readonly HashSet<string> PickedEquipmentKeys = new HashSet<string>();
    private static readonly HashSet<string> AnalyzedNodeKeys = new HashSet<string>();

    private static int knownMapSamplesTotal = 9;
    private static int bonusAnalyzedCount;

    public static string HeldSampleKey { get; private set; }
    public static string HeldSampleLabel { get; private set; }
    public static string HeldSampleDescription { get; private set; }
    public static string HeldDiscoveryId { get; private set; }
    internal static MarsDiscovery HeldDiscovery => MarsDiscovery.Find(HeldDiscoveryId);
    public static bool HasHeldSample => !string.IsNullOrEmpty(HeldSampleKey);

    public static int TotalSamples => Mathf.Max(knownMapSamplesTotal, KnownMapSampleKeys.Count) + bonusAnalyzedCount;
    public static int CollectedSamples => AnalyzedNodeKeys.Count + bonusAnalyzedCount;

    internal static MarsSampleNode2D LatestDiscovery { get; private set; }
    internal static int DiscoveryVersion { get; private set; }

    [Header("Datos de la Muestra")]
    [SerializeField] private string sampleLabel = "[MUESTRA GEOLÓGICA #1]";
    [SerializeField] private string sampleDescription = "Muestra de regolito y minerales marcianos.";
    [SerializeField] private string discoveryId;

    internal MarsDiscovery Discovery { get; private set; }
    internal bool DeliveredByRover { get; private set; }
    public bool IsCollected { get; private set; }
    public bool IsAvailable =>
        isActiveAndEnabled &&
        !IsCollected &&
        !PickedNodeKeys.Contains(NodeKey) &&
        !PickedEquipmentKeys.Contains(NodeKey) &&
        reservedBy == null;

    private RobotCompanionFollow2D reservedBy;
    private MarsWorldPlaceholder2D placeholder;
    private bool lastVisualHeldState;

    private string NodeKey => gameObject.name;

    private void Update()
    {
        if (lastVisualHeldState != HasHeldSample)
        {
            lastVisualHeldState = HasHeldSample;
            UpdateVisuals();
        }
    }

    public static void ResetAllSampleState()
    {
        PickedNodeKeys.Clear();
        PickedEquipmentKeys.Clear();
        AnalyzedNodeKeys.Clear();
        bonusAnalyzedCount = 0;
        HeldSampleKey = null;
        HeldSampleLabel = null;
        HeldSampleDescription = null;
        HeldDiscoveryId = null;
        LatestDiscovery = null;
        DiscoveryVersion = 0;
        foreach (var node in AllNodes)
        {
            if (node == null) continue;
            node.IsCollected = false;
            node.DeliveredByRover = false;
            node.reservedBy = null;
            node.UpdateVisuals();
        }
    }

    public static bool TryPickupBonusFromRover(string fact, out string message)
    {
        if (HasHeldSample)
        {
            message = $"Contenedor lleno (1/1): lleva '{HeldSampleLabel}' a la Nave antes de recibir la muestra del rover.";
            return false;
        }

        HeldSampleKey = $"ROVER_BONUS_{System.Guid.NewGuid():N}";
        HeldSampleLabel = "[MUESTRA ROVER EXTRA]";
        HeldSampleDescription = fact;
        HeldDiscoveryId = null;
        message = $"¡Recibiste la muestra extra del rover! Vuelve a la Nave para analizarla. {fact}";
        return true;
    }

    public static bool TryDeliverHeldSampleToShip(out string message)
    {
        if (!HasHeldSample)
        {
            message = "No llevas ninguna muestra en tu contenedor EVA. Sal a explorar Marte para recoger una.";
            return false;
        }

        if (CommsTowerCMEEvent.HaySobrecargaActivaPorCME)
        {
            message = "📡 ¡Interferencia CME! La Antena HGA encendida bloquea el laboratorio de la Nave. Ve a apagar la antena [E] primero.";
            return false;
        }

        string label = HeldSampleLabel;
        if (HeldSampleKey.StartsWith("ROVER_BONUS_"))
        {
            bonusAnalyzedCount++;
        }
        else
        {
            AnalyzedNodeKeys.Add(HeldSampleKey);
        }

        HeldSampleKey = null;
        HeldSampleLabel = null;
        HeldSampleDescription = null;
        HeldDiscoveryId = null;

        message = $"✅ Muestra {label} analizada y guardada en la Nave ({CollectedSamples}/{TotalSamples}). ¡Ya puedes salir por otra!";
        return true;
    }

    public static bool AllMapSamplesCollected =>
        KnownMapSampleKeys.Count > 0 && PickedNodeKeys.Count >= KnownMapSampleKeys.Count;

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

        bool isEquip = Discovery != null && Discovery.isEquipment;
        if (!isEquip && HasHeldSample)
        {
            message = $"Contenedor lleno (1/1): entrega '{HeldSampleLabel}' en la Nave primero.";
            return false;
        }

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
        if (Discovery == null && !string.IsNullOrEmpty(discoveryId))
            Discovery = MarsDiscovery.Find(discoveryId);

        AllNodes.Add(this);
        bool isEquip = Discovery != null && Discovery.isEquipment;
        if (!isEquip)
        {
            KnownMapSampleKeys.Add(NodeKey);
            if (KnownMapSampleKeys.Count > knownMapSamplesTotal)
                knownMapSamplesTotal = KnownMapSampleKeys.Count;
        }

        if (PickedEquipmentKeys.Contains(NodeKey) || PickedNodeKeys.Contains(NodeKey) || AnalyzedNodeKeys.Contains(NodeKey))
            IsCollected = true;

        UpdateVisuals();
    }

    private void OnDisable()
    {
        reservedBy = null;
        AllNodes.Remove(this);
        if (LatestDiscovery == this)
            LatestDiscovery = null;
    }

    public string CollectSample() => CollectSample(false);

    private string CollectSample(bool deliveredByRover)
    {
        bool isEquip = Discovery != null && Discovery.isEquipment;

        if (IsCollected || (isEquip ? PickedEquipmentKeys.Contains(NodeKey) : PickedNodeKeys.Contains(NodeKey)))
            return $"{(Discovery != null ? Discovery.title : sampleLabel)}: ya fue registrada.";
        if (reservedBy != null)
            return "El rover lleva esta muestra.";

        if (isEquip)
        {
            IsCollected = true;
            DeliveredByRover = deliveredByRover;
            PickedEquipmentKeys.Add(NodeKey);
            LatestDiscovery = this;
            DiscoveryVersion++;
            UpdateVisuals();
            return $"Hallazgo registrado: {Discovery.title}.";
        }

        if (HasHeldSample)
            return $"Contenedor lleno (1/1): lleva '{HeldSampleLabel}' a la Nave [E] antes de recoger otra muestra.";

        IsCollected = true;
        DeliveredByRover = deliveredByRover;
        PickedNodeKeys.Add(NodeKey);
        HeldSampleKey = NodeKey;
        HeldSampleLabel = Discovery != null ? Discovery.title : sampleLabel;
        HeldSampleDescription = Discovery != null ? $"{Discovery.instrument}: {Discovery.title}" : sampleDescription;
        HeldDiscoveryId = Discovery != null ? Discovery.id : discoveryId;

        if (Discovery != null)
        {
            LatestDiscovery = this;
            DiscoveryVersion++;
        }

        UpdateVisuals();
        if (Discovery != null)
            return $"Muestra '{Discovery.title}' guardada (1/1) — Tras análisis rápido, deposítala en la Nave [E].";

        return $"{sampleLabel} guardada en contenedor (1/1) — Vuelve a la Nave [E] para depositarla. ({sampleDescription})";
    }

    private void UpdateVisuals()
    {
        if (placeholder == null)
            placeholder = GetComponent<MarsWorldPlaceholder2D>();
        if (placeholder == null) return;

        if (Discovery != null)
        {
            bool visible = IsAvailable;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = visible;
            placeholder.enabled = visible;
            if (visible)
            {
                string sub = (HasHeldSample && !Discovery.isEquipment)
                    ? "Entrega tu muestra en la Nave (1/1)"
                    : "Pulsa [E] para recoger";
                placeholder.SetVisualState(Color.white, Discovery.title, sub);
            }
            return;
        }

        bool analyzed = AnalyzedNodeKeys.Contains(NodeKey);
        if (analyzed)
        {
            placeholder.SetVisualState(CollectedColor, sampleLabel, "Analizada en Nave ✓");
        }
        else if (IsCollected || PickedNodeKeys.Contains(NodeKey))
        {
            placeholder.SetVisualState(
                CollectedColor,
                sampleLabel,
                HeldSampleKey == NodeKey ? "En mano (1/1) → Llévala a la Nave" : "Recogida ✓"
            );
        }
        else if (reservedBy != null)
        {
            placeholder.SetVisualState(CollectedColor, sampleLabel, "Recogida por el rover");
        }
        else if (HasHeldSample)
        {
            placeholder.SetVisualState(
                new Color(0.85f, 0.56f, 0.20f, 0.90f),
                sampleLabel,
                "Entrega tu muestra en la Nave primero"
            );
        }
        else
        {
            placeholder.SetVisualState(
                UncollectedColor,
                sampleLabel,
                "Presiona [E] para recoger muestra (1/1)"
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

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AgriculturalModuleBuilder
{
    private const string ArtRoot = "Assets/Art/Buildings/Agricultural_Module";
    private const string ExteriorPrefabPath = ArtRoot + "/Prefabs/Agricultural_Module_Exterior.prefab";
    private const string AstronautPrefabPath = ArtRoot + "/Prefabs/Astronaut_Player_Reusable.prefab";
    private const string ExteriorScenePath = "Assets/Scenes/Mars_Natural_Landscape_01.unity";
    private const string InteriorScenePath = "Assets/Scenes/Cultivation_Module_Interior.unity";
    private const string PipeMaterialPath = ArtRoot + "/Lighting/Module_Pipe_Line.mat";

    [MenuItem("MARS/Build Agricultural Module")]
    public static void BuildAll()
    {
        EnsureFolders();
        EnsureClosedCenterHatchPlateAsset();
        AssetDatabase.Refresh();
        ConfigureSpriteImports();
        EnsurePipeMaterial();
        BuildExteriorPrefab();
        IntegrateExteriorPrefab();
        BuildInteriorScene();
        FirstOnMarsCinematicBuilder.BuildIntroScene();
        AddInteriorToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Agricultural module assets, prefab, exterior integration and interior scene are ready.");
    }

    [MenuItem("MARS/Rebuild Agricultural Interior Only")]
    public static void RebuildInteriorOnly()
    {
        EnsureFolders();
        AssetDatabase.Refresh();
        ImportRedesignedInteriorSprites();
        EnsurePipeMaterial();
        BuildInteriorScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Agricultural interior was rebuilt without regenerating exterior or intro scenes.");
    }

    private static void ImportRedesignedInteriorSprites()
    {
        foreach (var file in Directory.GetFiles(ArtRoot, "*.png", SearchOption.AllDirectories))
        {
            string assetPath = file.Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(assetPath);
            bool selected = name == "hydroponic_bench_3row" || name == "room_bulkhead_segment" ||
                name == "airlock_hatch_wall_frame_side" || name == "airlock_pressure_gate_leaf_side" ||
                name == "decontamination_wall_console" || name == "pressure_gate_wall_console" ||
                name == "decon_air_jet_nozzle" || name.StartsWith("crop_label_") || name.StartsWith("plant_cycle_");
            if (!selected) continue;

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = GetPixelsPerUnit(assetPath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void EnsureFolders()
    {
        foreach (var folder in new[]
        {
            "Assets/Art/Buildings",
            ArtRoot,
            ArtRoot + "/Exterior",
            ArtRoot + "/Interior",
            ArtRoot + "/Airlock",
            ArtRoot + "/Cultivation",
            ArtRoot + "/Water_System",
            ArtRoot + "/Lighting",
            ArtRoot + "/Plants",
            ArtRoot + "/Prefabs",
            "Assets/Scenes",
            "Assets/Scripts/Gameplay",
            "Assets/Editor"
        })
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                var name = Path.GetFileName(folder);
                if (AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder(parent, name);
            }
        }
    }

    private static void ConfigureSpriteImports()
    {
        var files = Directory.GetFiles(ArtRoot, "*.png", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var assetPath = file.Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = GetPixelsPerUnit(assetPath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void EnsureClosedCenterHatchPlateAsset()
    {
        const int width = 150;
        const int height = 125;
        const float cornerRadius = 11f;
        const float borderWidth = 2f;
        string assetPath = ArtRoot + "/Exterior/center_hatch_closed_hull_plate.png";
        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        if (File.Exists(fullPath)) return;

        var border = new Color32(183, 185, 180, 255);
        var top = new Color32(242, 240, 233, 255);
        var bottom = new Color32(215, 214, 207, 255);
        var seam = new Color32(203, 201, 193, 255);
        var screw = new Color32(122, 128, 128, 255);
        var highlight = new Color32(238, 236, 229, 255);
        var pixels = new Color32[width * height];
        var screwCenters = new[]
        {
            new Vector2(11f, 11f), new Vector2(width - 12f, 11f),
            new Vector2(11f, height - 12f), new Vector2(width - 12f, height - 12f)
        };

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float qx = Mathf.Abs(x - (width - 1) * 0.5f) - (width * 0.5f - cornerRadius);
            float qy = Mathf.Abs(y - (height - 1) * 0.5f) - (height * 0.5f - cornerRadius);
            float outsideX = Mathf.Max(qx, 0f);
            float outsideY = Mathf.Max(qy, 0f);
            float signedDistance = Mathf.Min(Mathf.Max(qx, qy), 0f) + Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY) - cornerRadius;
            if (signedDistance > 0f) continue;

            Color32 color;
            if (signedDistance > -borderWidth)
                color = border;
            else
            {
                color = Color.Lerp(bottom, top, Mathf.InverseLerp(height - 10f, 10f, y));
                if (Mathf.Abs(y - 14f) < 1.1f || Mathf.Abs(y - (height - 15f)) < 1.1f)
                    color = seam;
                if ((Mathf.Abs(x - 13f) < 1.1f || Mathf.Abs(x - (width - 14f)) < 1.1f) && y > 24 && y < height - 25)
                    color = highlight;
            }

            foreach (var center in screwCenters)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= 3.2f) color = screw;
                if (distance <= 1.2f) color = highlight;
            }
            pixels[y * width + x] = color;
        }

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static float GetPixelsPerUnit(string assetPath)
    {
        if (assetPath.Contains("/Plants/")) return 400f;
        if (assetPath.Contains("hydroponic_bench_3row")) return 200f;
        if (assetPath.Contains("room_bulkhead_segment")) return 200f;
        if (assetPath.Contains("airlock_hatch_wall_frame_side") || assetPath.Contains("airlock_pressure_gate_leaf_side") ||
            assetPath.Contains("decontamination_wall_console") || assetPath.Contains("pressure_gate_wall_console") ||
            assetPath.Contains("decon_air_jet_nozzle")) return 200f;
        if (assetPath.Contains("/Exterior/")) return 140f;
        if (assetPath.Contains("crop_label_")) return 160f;
        if (assetPath.Contains("/Cultivation/")) return assetPath.Contains("bench") ? 100f : 110f;
        if (assetPath.Contains("/Airlock/"))
        {
            if (assetPath.Contains("pressure_gate")) return 120f;
            if (assetPath.Contains("decon_grate")) return 130f;
            if (assetPath.Contains("cycle_lamp")) return 120f;
            return assetPath.Contains("pressure_hatch") ? 115f : 110f;
        }
        if (assetPath.Contains("/Water_System/")) return assetPath.Contains("nutrient") ? 110f : 112f;
        if (assetPath.Contains("/Lighting/")) return assetPath.Contains("fan") ? 120f : 115f;
        return 100f;
    }

    private static Sprite LoadSprite(string folder, string name)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{folder}/{name}.png");
        if (sprite == null) throw new FileNotFoundException($"Missing sprite: {folder}/{name}.png");
        return sprite;
    }

    private static void BuildExteriorPrefab()
    {
        var root = new GameObject("Agricultural_Module_Exterior");
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite("Exterior", "Agricultural_Module_Exterior");
        renderer.sortingOrder = 1;

        var solidBody = root.AddComponent<BoxCollider2D>();
        solidBody.size = new Vector2(6.4f, 2.7f);
        solidBody.offset = new Vector2(0f, 0.65f);

        // The left airlock is the only visible entrance. Cover the central equipment hatch
        // in the source artwork with a flush hull plate so it cannot read as a second door.
        var closedCenterHatch = new GameObject("Center_Hatch_Closed_Hull_Plate");
        closedCenterHatch.transform.SetParent(root.transform, false);
        closedCenterHatch.transform.localPosition = new Vector3(3.82f, 2.02f, -0.02f);
        var closedHatchRenderer = closedCenterHatch.AddComponent<SpriteRenderer>();
        closedHatchRenderer.sprite = LoadSprite("Exterior", "center_hatch_closed_hull_plate");
        if (closedHatchRenderer.sprite == null)
            throw new FileNotFoundException("Missing sprite: Exterior/center_hatch_closed_hull_plate.png");
        closedHatchRenderer.sortingOrder = 2;

        var entry = new GameObject("Exterior_Entry_Portal");
        entry.transform.SetParent(root.transform, false);
        // Match the visible airlock on the left auxiliary pod in the exterior artwork.
        entry.transform.localPosition = new Vector3(-2.95f, -1.58f, 0f);
        var trigger = entry.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.8f, 1.3f);
        var portal = entry.AddComponent<MarsScenePortal2D>();
        SetPortal(portal, "Cultivation_Module_Interior", "Cultivation_Interior_Spawn", false);

        PrefabUtility.SaveAsPrefabAsset(root, ExteriorPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void IntegrateExteriorPrefab()
    {
        var scene = EditorSceneManager.OpenScene(ExteriorScenePath, OpenSceneMode.Single);
        var oldCrop = GameObject.Find("Persona2_Zona_De_Cultivos");
        Vector3 location = oldCrop != null ? oldCrop.transform.position : new Vector3(3.8f, -3f, 0f);

        var astronaut = GameObject.Find("Astronaut_Player");
        if (astronaut != null)
            PrefabUtility.SaveAsPrefabAsset(astronaut, AstronautPrefabPath);

        var existing = GameObject.Find("Agricultural_Module_Exterior");
        if (existing != null) Object.DestroyImmediate(existing);
        if (oldCrop != null) Object.DestroyImmediate(oldCrop);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExteriorPrefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.position = location;
        instance.transform.localScale = Vector3.one;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildInteriorScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var floor = SpriteObject("Interior_Floor_And_Pressure_Walls", "Interior", "interior_floor_walls", Vector3.zero, Vector3.one, -100);
        floor.transform.position = Vector3.zero;
        BuildBoundaryColliders();

        // Four clearly separated, single-crop hydroponic groups. Each rack has three
        // rows of seven plants; each bank grows through its own timed crop cycle.
        const float rackPixelsPerUnit = 200f;
        const float rackScale = 0.68f;
        var cropGroups = new[]
        {
            new CropGroup("Northwest", -4.2f, 2.8f, "red_romaine", "LECHUGA ROMANA ROJA",
                "Outredgeous · hojas comestibles",
                "Lechuga romana roja Outredgeous. Se consume la hoja. En este módulo el cultivo simula germinación en almohadilla porosa, desarrollo vegetativo y cosecha; el tiempo está acelerado."),
            new CropGroup("Northeast", 4.2f, 2.8f, "mizuna", "MIZUNA",
                "Mostaza japonesa · hojas comestibles",
                "Mizuna, una mostaza de hoja. Se cosechan sus hojas. El bancal representa riego hidropónico, circulación de aire e iluminación LED; las etapas y tiempos son una simulación de juego."),
            new CropGroup("Southwest", -4.2f, -2.8f, "dwarf_wheat", "TRIGO ENANO",
                "Apogee · grano experimental",
                "Trigo enano Apogee, estudiado por la NASA en experimentos de plantas. Se muestra por separado porque su porte y producto son distintos; este ciclo de crecimiento es una simulación acelerada."),
            new CropGroup("Southeast", 4.2f, -2.8f, "red_robin_tomato", "TOMATE CHERRY ENANO",
                "Red Robin · fruto comestible",
                "Tomate enano Red Robin. En la fase madura aparecen frutos rojos para que el producto sea reconocible. La cosecha reinicia el ciclo; la producción del juego no representa un rendimiento real de vuelo.")
        };

        foreach (var group in cropGroups)
        {
            var center = new Vector3(group.X, group.Y, 0f);
            var bench = SpriteObject($"Hydroponic_Bench_{group.Id}", "Cultivation", "hydroponic_bench_3row",
                center, new Vector3(rackScale, rackScale, 1f), 0);
            var benchRenderer = bench.GetComponent<SpriteRenderer>();
            var footprint = Vector2.Scale(benchRenderer.sprite.bounds.size, new Vector2(rackScale, rackScale));
            AddSolidCollider(bench, benchRenderer.sprite.bounds.size * 0.93f, Vector2.zero);
            var access = bench.AddComponent<BoxCollider2D>();
            access.size = (footprint + new Vector2(0.20f, 0.24f)) / rackScale;
            access.isTrigger = true;

            var plants = PlacePlants(center, group.Species, rackScale, rackPixelsPerUnit);
            var lifecycle = bench.AddComponent<MarsCropLifecycle2D>();
            var cropConfiguration = new SerializedObject(lifecycle);
            cropConfiguration.FindProperty("cropName").stringValue = group.DisplayName + " · " + group.Cultivar.Split('·')[0].Trim();
            cropConfiguration.FindProperty("cropDescription").stringValue = group.Description;
            cropConfiguration.FindProperty("seedSprite").objectReferenceValue = LoadSprite("Plants", $"plant_cycle_{group.Species}_seed");
            cropConfiguration.FindProperty("growingSprite").objectReferenceValue = LoadSprite("Plants", $"plant_cycle_{group.Species}_growing");
            cropConfiguration.FindProperty("matureSprite").objectReferenceValue = LoadSprite("Plants", $"plant_cycle_{group.Species}_mature");
            var plantArray = cropConfiguration.FindProperty("plants");
            plantArray.arraySize = plants.Length;
            for (int i = 0; i < plants.Length; i++)
                plantArray.GetArrayElementAtIndex(i).objectReferenceValue = plants[i];
            cropConfiguration.ApplyModifiedPropertiesWithoutUndo();

            float labelY = group.Y > 0f ? 4.63f : -4.63f;
            SpriteObject($"Crop_Label_{group.Id}", "Cultivation", $"crop_label_{group.Species}",
                new Vector3(group.X, labelY, -0.04f), new Vector3(0.68f, 0.68f, 1f), 5);
        }

        var controlGate = BuildRoomBulkheads(out var controlGateCollider);

        // Side-view, wall-mounted entry hardware. The old top-down circular hatch
        // sprite is intentionally not placed in this scene.
        var dustGrate = SpriteObject("Suit_Dust_Removal_Grate", "Airlock", "airlock_decon_grate",
            new Vector3(-11.2f, -2.2f, -0.03f), Vector3.one * 0.24f, 4);

        var outerGate = PlaceWallGate("Outer_Pressure_Gate", -13f, 0f, 0.72f, out _);
        var innerGate = PlaceWallGate("Inner_Pressure_Gate", -9f, 0f, 0.72f, out var innerGateCollider);

        var deconConsole = SpriteObject("Decontamination_Wall_Console", "Airlock", "decontamination_wall_console",
            new Vector3(-12.05f, 1.48f, -0.05f), new Vector3(0.42f, 0.42f, 1f), 8);
        var deconTrigger = deconConsole.AddComponent<BoxCollider2D>();
        deconTrigger.size = new Vector2(0.9f, 1.55f);
        deconTrigger.isTrigger = true;
        var deconPanel = deconConsole.AddComponent<MarsDecontaminationPanel2D>();
        var purge = CreateDecontaminationPurge();
        var deconConfiguration = new SerializedObject(deconPanel);
        deconConfiguration.FindProperty("purgeMist").objectReferenceValue = purge;
        deconConfiguration.FindProperty("purgeSeconds").floatValue = 4f;
        deconConfiguration.ApplyModifiedPropertiesWithoutUndo();

        var lamps = new SpriteRenderer[3];
        float[] deconLampY = { 2.085f, 2.03f, 1.976f };
        for (int i = 0; i < lamps.Length; i++)
        {
            var lamp = SpriteObject($"Airlock_Cycle_Lamp_{i + 1}", "Airlock", "airlock_cycle_lamp",
                new Vector3(-12.11f, deconLampY[i], -0.08f), Vector3.one * 0.018f, 9);
            lamps[i] = lamp.GetComponent<SpriteRenderer>();
            lamps[i].color = new Color(0.27f, 0.29f, 0.28f);
        }

        AddPressureDoorPanel("Crop_Hall_Pressure_Access_Panel", -9f, 1.45f,
            innerGate, new Vector3(-9f, 3.55f, -0.08f), innerGateCollider, deconPanel);
        AddPressureDoorPanel("Control_Room_Pressure_Access_Panel", 9f, 1.45f,
            controlGate, new Vector3(9f, 3.55f, -0.08f), controlGateCollider, null);

        var spawn = new GameObject("Cultivation_Interior_Spawn");
        spawn.transform.position = new Vector3(-13.35f, 0f, 0f);
        var vestibulePoint = new GameObject("Entry_Vestibule_Arrival_Point");
        vestibulePoint.transform.position = new Vector3(-11.2f, 0f, 0f);
        var directSceneSpawn = new GameObject("Direct_Interior_Spawn");
        directSceneSpawn.transform.position = vestibulePoint.transform.position;

        var returnPortalObject = new GameObject("Interior_Exterior_Portal");
        returnPortalObject.transform.position = new Vector3(-12.55f, -2.85f, 0f);
        var exitTrigger = returnPortalObject.AddComponent<BoxCollider2D>();
        exitTrigger.isTrigger = true;
        exitTrigger.size = new Vector2(1.0f, 0.85f);
        var returnPortal = returnPortalObject.AddComponent<MarsScenePortal2D>();
        SetPortal(returnPortal, "Mars_Natural_Landscape_01", "", true);

        var sequenceRoot = new GameObject("Airlock_Cycle_And_Astronaut_Entry");
        var airlockCycle = sequenceRoot.AddComponent<MarsAirlockCycle2D>();
        var sequence = new SerializedObject(airlockCycle);
        sequence.FindProperty("fallbackAstronautPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(AstronautPrefabPath);
        sequence.FindProperty("entrySpawn").objectReferenceValue = spawn.transform;
        sequence.FindProperty("vestibuleDestination").objectReferenceValue = vestibulePoint.transform;
        sequence.FindProperty("entryPressureGate").objectReferenceValue = outerGate.transform;
        sequence.FindProperty("entryGateOpenPosition").vector3Value = new Vector3(-13f, 3.55f, -0.08f);
        sequence.FindProperty("fallbackInteriorSpawn").objectReferenceValue = directSceneSpawn.transform;
        var lampArray = sequence.FindProperty("cycleLamps");
        lampArray.arraySize = lamps.Length;
        for (int i = 0; i < lamps.Length; i++) lampArray.GetArrayElementAtIndex(i).objectReferenceValue = lamps[i];
        sequence.ApplyModifiedPropertiesWithoutUndo();

        // Adapted environmental/life-support control bay; interaction opens a large in-camera display.
        var environmental = SpriteObject("Environmental_Control_Station", "Lighting", "environmental_control_panel", new Vector3(12.0f, 2.82f, 0f), Vector3.one * 0.25f, 4);
        AddMonitorInteraction(environmental, "Monitoreo ambiental", "Muestra seguimiento de temperatura, humedad relativa, CO2, oxígeno y filtración del aire.");
        var rootMonitor = SpriteObject("Crop_Root_Zone_Monitor", "Lighting", "crop_root_zone_monitor_panel", new Vector3(12.0f, 0f, 0f), Vector3.one * 0.25f, 4);
        AddMonitorInteraction(rootMonitor, "Sensores de cultivo", "Muestra humedad y oxígeno de la zona radicular, temperatura del follaje e imágenes de seguimiento.");
        var waterMonitor = SpriteObject("Water_Loop_Monitor", "Water_System", "water_loop_monitor_panel", new Vector3(12.0f, -2.82f, 0f), Vector3.one * 0.25f, 4);
        AddMonitorInteraction(waterMonitor, "Circuito de agua y nutrientes", "Muestra nivel del depósito, flujo de alimentación/retorno y canales de seguimiento de nutrientes.");
        var growChamber = SpriteObject("Sealed_Plant_Growth_Chamber", "Cultivation", "sealed_growth_chamber", new Vector3(10.35f, 0f, 0f), Vector3.one * 0.24f, 3);
        AddSolidCollider(growChamber, growChamber.GetComponent<SpriteRenderer>().sprite.bounds.size * 0.72f, Vector2.zero);
        var fan = SpriteObject("Air_Circulation_Filter", "Lighting", "circulation_fan_filter", new Vector3(10.45f, 3.25f, 0f), Vector3.one * 0.18f, 3);
        AddSolidCollider(fan, fan.GetComponent<SpriteRenderer>().sprite.bounds.size * 0.72f, Vector2.zero);
        var water = SpriteObject("Water_Reservoir_And_Pump", "Water_System", "water_reservoir_pump_unit", new Vector3(10.45f, -3.15f, 0f), Vector3.one * 0.16f, 3);
        AddSolidCollider(water, water.GetComponent<SpriteRenderer>().sprite.bounds.size * 0.72f, Vector2.zero);
        var nutrients = SpriteObject("Nutrient_Cartridge_Station", "Water_System", "nutrient_cartridge_rack", new Vector3(11.55f, -1.75f, 0f), Vector3.one * 0.14f, 3);
        AddSolidCollider(nutrients, nutrients.GetComponent<SpriteRenderer>().sprite.bounds.size * 0.72f, Vector2.zero);

        BuildVisibleGrowPipes();

        BuildInteriorCamera();
        EditorSceneManager.SaveScene(scene, InteriorScenePath);
    }

    private static SpriteRenderer[] PlacePlants(Vector3 center, string species, float rackScale, float pixelsPerUnit)
    {
        int[] pocketCentersX = { 250, 500, 750, 1000, 1250, 1500, 1750 };
        int[] rowCentersY = { 176, 325, 474 };
        var result = new SpriteRenderer[pocketCentersX.Length * rowCentersY.Length];
        int index = 0;
        for (int row = 0; row < rowCentersY.Length; row++)
        for (int col = 0; col < pocketCentersX.Length; col++)
        {
            float localX = (pocketCentersX[col] - 1000f) / pixelsPerUnit * rackScale;
            float localY = (325f - rowCentersY[row]) / pixelsPerUnit * rackScale;
            var plant = SpriteObject($"Plant_{species}_{index + 1:00}", "Plants",
                $"plant_cycle_{species}_seed", center + new Vector3(localX, localY, -0.02f),
                Vector3.one * 0.82f, 3);
            result[index++] = plant.GetComponent<SpriteRenderer>();
        }
        return result;
    }

    private struct CropGroup
    {
        public readonly string Id;
        public readonly float X;
        public readonly float Y;
        public readonly string Species;
        public readonly string DisplayName;
        public readonly string Cultivar;
        public readonly string Description;

        public CropGroup(string id, float x, float y, string species, string displayName, string cultivar, string description)
        {
            Id = id;
            X = x;
            Y = y;
            Species = species;
            DisplayName = displayName;
            Cultivar = cultivar;
            Description = description;
        }
    }

    private static GameObject BuildRoomBulkheads(out BoxCollider2D controlGateCollider)
    {
        // Full wall runs meet the north/south room boundaries while preserving a 2.4 m passage.
        foreach (float x in new[] { -9f, 9f })
        foreach (float y in new[] { -3.3f, 3.3f })
        {
            SpriteObject($"Pressure_Bulkhead_{(x < 0f ? "Entry_Crops" : "Crops_Control")}_{(y < 0f ? "South" : "North")}",
                "Interior", "room_bulkhead_segment", new Vector3(x, y, -0.1f), new Vector3(0.42f, 0.98f, 1f), 3);
        }

        // Both room dividers use operable pressure doors.
        PlaceGateFrame("Crop_Hall_Gate_Frame", -9f, 0f, new Vector3(0.70f, 0.70f, 1f));
        return PlaceWallGate("Control_Room_Pressure_Gate", 9f, 0f, 0.72f, out controlGateCollider);
    }

    private static void AddPressureDoorPanel(string name, float x, float y, GameObject gate,
        Vector3 gateOpenPosition, BoxCollider2D gateCollider, MarsDecontaminationPanel2D decontaminationCycle)
    {
        float primaryOffset = Mathf.Sign(x) * 0.44f;
        var primary = CreatePressureDoorPanelFace(name, x + primaryOffset, y, gate,
            gateOpenPosition, gateCollider, decontaminationCycle, out var primaryLamps);
        var opposite = CreatePressureDoorPanelFace(name + "_Opposite_Side", x - primaryOffset, y, gate,
            gateOpenPosition, gateCollider, decontaminationCycle, out var oppositeLamps);

        var allLamps = new SpriteRenderer[primaryLamps.Length + oppositeLamps.Length];
        primaryLamps.CopyTo(allLamps, 0);
        oppositeLamps.CopyTo(allLamps, primaryLamps.Length);
        var primaryConfiguration = new SerializedObject(primary);
        var primaryLampArray = primaryConfiguration.FindProperty("statusLamps");
        primaryLampArray.arraySize = allLamps.Length;
        for (int i = 0; i < allLamps.Length; i++)
            primaryLampArray.GetArrayElementAtIndex(i).objectReferenceValue = allLamps[i];
        primaryConfiguration.ApplyModifiedPropertiesWithoutUndo();

        var oppositeConfiguration = new SerializedObject(opposite);
        oppositeConfiguration.FindProperty("sharedController").objectReferenceValue = primary;
        oppositeConfiguration.ApplyModifiedPropertiesWithoutUndo();
    }

    private static MarsPressureDoorPanel2D CreatePressureDoorPanelFace(string name, float x, float y,
        GameObject gate, Vector3 gateOpenPosition, BoxCollider2D gateCollider,
        MarsDecontaminationPanel2D decontaminationCycle, out SpriteRenderer[] lamps)
    {
        var panelObject = SpriteObject(name, "Airlock", "pressure_gate_wall_console",
            new Vector3(x, y, -0.05f), new Vector3(0.45f, 0.45f, 1f), 8);
        var trigger = panelObject.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(1.6f, 2.2f);
        trigger.isTrigger = true;

        var panel = panelObject.AddComponent<MarsPressureDoorPanel2D>();
        var configuration = new SerializedObject(panel);
        configuration.FindProperty("pressureGate").objectReferenceValue = gate.transform;
        configuration.FindProperty("gateOpenPosition").vector3Value = gateOpenPosition;
        configuration.FindProperty("gateCollider").objectReferenceValue = gateCollider;
        configuration.FindProperty("panelIndicator").objectReferenceValue = null;
        configuration.FindProperty("decontaminationCycle").objectReferenceValue = decontaminationCycle;
        configuration.FindProperty("sharedController").objectReferenceValue = null;
        configuration.FindProperty("equalizationSeconds").floatValue = 0.85f;
        configuration.FindProperty("slideSeconds").floatValue = 0.48f;

        var lampArray = configuration.FindProperty("statusLamps");
        lampArray.arraySize = 3;
        lamps = new SpriteRenderer[3];
        float[] sourceY = { 190f, 229f, 268f };
        for (int i = 0; i < sourceY.Length; i++)
        {
            float lampX = x + (83f - 110f) / 200f * 0.45f;
            float lampY = y + (280f - sourceY[i]) / 200f * 0.45f;
            var lampObject = SpriteObject($"{name}_Status_Lamp_{i + 1}", "Airlock", "airlock_cycle_lamp",
                new Vector3(lampX, lampY, -0.08f), Vector3.one * 0.018f, 9);
            var renderer = lampObject.GetComponent<SpriteRenderer>();
            renderer.color = new Color(0.27f, 0.29f, 0.28f);
            lamps[i] = renderer;
            lampArray.GetArrayElementAtIndex(i).objectReferenceValue = renderer;
        }
        configuration.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    private static GameObject PlaceWallGate(string name, float x, float y, float scale, out BoxCollider2D gateCollider)
    {
        PlaceGateFrame(name + "_Frame", x, y, new Vector3(scale, scale, 1f));
        var leaf = SpriteObject(name + "_Leaf", "Airlock", "airlock_pressure_gate_leaf_side",
            new Vector3(x, y, -0.16f), new Vector3(scale, scale, 1f), 7);
        gateCollider = leaf.AddComponent<BoxCollider2D>();
        gateCollider.size = new Vector2(0.88f, 3.15f);
        return leaf;
    }

    private static void PlaceGateFrame(string name, float x, float y, Vector3 scale)
    {
        SpriteObject(name, "Airlock", "airlock_hatch_wall_frame_side",
            new Vector3(x, y, -0.12f), scale, 6);
    }

    private static ParticleSystem CreateDecontaminationPurge()
    {
        var effectObject = new GameObject("Vestibule_Filtered_Air_Purge_Effect");
        effectObject.transform.position = new Vector3(-11.15f, 0.15f, -0.2f);
        var effect = effectObject.AddComponent<ParticleSystem>();
        var main = effect.main;
        main.duration = 3.6f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 1.25f;
        main.startSpeed = 0.035f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.18f);
        main.startColor = new Color(1f, 1f, 1f, 0.82f);
        main.maxParticles = 220;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = effect.emission;
        emission.rateOverTime = 78f;
        var shape = effect.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2.8f, 4.8f, 0.04f);
        var renderer = effect.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 9;
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PipeMaterialPath);
        var colorOverLifetime = effect.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var mistGradient = new Gradient();
        mistGradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.68f, 0.18f),
                new GradientAlphaKey(0.58f, 0.72f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = mistGradient;
        return effect;
    }

    private static void BuildVisibleGrowPipes()
    {
        var supply = new Color(0.18f, 0.53f, 0.65f);
        var returnLine = new Color(0.68f, 0.39f, 0.19f);
        const float rackScale = 0.68f;
        const float pixelsPerUnit = 200f;
        int[] pocketCentersX = { 250, 500, 750, 1000, 1250, 1500, 1750 };
        int[] rowCentersY = { 176, 325, 474 };
        foreach (float side in new[] { -1f, 1f })
        {
            float manifoldX = side * 8.05f;
            AddSimplePipe($"Water_Supply_Manifold_{(side < 0 ? "West" : "East")}",
                new Vector3(manifoldX, 4.55f, -0.06f), new Vector3(manifoldX, -4.55f, -0.06f), supply);
            AddSimplePipe($"Nutrient_Return_Manifold_{(side < 0 ? "West" : "East")}",
                new Vector3(manifoldX + side * 0.18f, 4.55f, -0.055f), new Vector3(manifoldX + side * 0.18f, -4.55f, -0.055f), returnLine);

            foreach (float rowSide in new[] { -1f, 1f })
            {
                float rackX = side * 4.2f;
                float rackY = rowSide * 2.8f;
                string name = $"{(side < 0 ? "West" : "East")}_{(rowSide < 0 ? "South" : "North")}";
                for (int row = 0; row < rowCentersY.Length; row++)
                {
                    float rowY = rackY + (325f - rowCentersY[row]) / pixelsPerUnit * rackScale;
                    float supplyY = rowY - 43f / pixelsPerUnit * rackScale;
                    float returnY = rowY - 51f / pixelsPerUnit * rackScale;
                    float westEdge = rackX - 878f / pixelsPerUnit * rackScale;
                    float eastEdge = rackX + 878f / pixelsPerUnit * rackScale;
                    float manifoldTapX = side < 0f ? westEdge : eastEdge;
                    string rowName = $"{name}_Row_{row + 1:00}";

                    AddSimplePipe($"Water_Supply_Branch_{rowName}",
                        new Vector3(manifoldX, supplyY, -0.05f), new Vector3(manifoldTapX, supplyY, -0.05f), supply);
                    AddSimplePipe($"Water_Supply_Rail_{rowName}",
                        new Vector3(westEdge, supplyY, -0.05f), new Vector3(eastEdge, supplyY, -0.05f), supply);
                    AddSimplePipe($"Nutrient_Return_Branch_{rowName}",
                        new Vector3(manifoldX + side * 0.18f, returnY, -0.045f),
                        new Vector3(manifoldTapX, returnY, -0.045f), returnLine);
                    AddSimplePipe($"Nutrient_Return_Rail_{rowName}",
                        new Vector3(westEdge, returnY, -0.045f), new Vector3(eastEdge, returnY, -0.045f), returnLine);

                    for (int col = 0; col < pocketCentersX.Length; col++)
                    {
                        float pocketX = rackX + (pocketCentersX[col] - 1000f) / pixelsPerUnit * rackScale;
                        float connectorX = pocketX + 33f / pixelsPerUnit * rackScale;
                        string pocketName = $"{rowName}_Pocket_{col + 1:00}";
                        AddSimplePipe($"Water_Drop_{pocketName}",
                            new Vector3(connectorX, supplyY, -0.04f), new Vector3(connectorX, rowY - 0.095f, -0.04f), supply);
                        AddSimplePipe($"Nutrient_Return_Drop_{pocketName}",
                            new Vector3(connectorX + 0.07f, returnY, -0.035f),
                            new Vector3(connectorX + 0.07f, rowY - 0.125f, -0.035f), returnLine);
                    }
                }
            }
        }

        // The service loop visibly passes through the control-room threshold to the pump.
        AddSimplePipe("Water_Loop_To_Control_Room", new Vector3(8.05f, 0.9f, -0.04f), new Vector3(9.65f, 0.9f, -0.04f), supply);
        AddSimplePipe("Water_Loop_To_Pump_Drop", new Vector3(9.65f, 0.9f, -0.04f), new Vector3(9.65f, -2.35f, -0.04f), supply);
        AddSimplePipe("Water_Loop_Pump_Connection", new Vector3(9.65f, -2.35f, -0.04f), new Vector3(10.1f, -2.75f, -0.04f), supply);
        AddSimplePipe("Nutrient_Return_To_Control_Room", new Vector3(8.23f, 0.75f, -0.035f), new Vector3(9.45f, 0.75f, -0.035f), returnLine);
        AddSimplePipe("Nutrient_Return_Drop", new Vector3(9.45f, 0.75f, -0.035f), new Vector3(9.45f, -2.2f, -0.035f), returnLine);
        AddSimplePipe("Nutrient_Return_Connection", new Vector3(9.45f, -2.2f, -0.035f), new Vector3(10.05f, -2.75f, -0.035f), returnLine);
    }

    private static void BuildBoundaryColliders()
    {
        // Room walls align with the rendered floor plan. The two internal bulkheads
        // are full partitions with a single 2.4 m doorway centered on each wall.
        AddBoundary("Crop_Hall_Wall_North", new Vector2(0f, 5.4f), new Vector2(18f, 0.35f));
        AddBoundary("Crop_Hall_Wall_South", new Vector2(0f, -5.4f), new Vector2(18f, 0.35f));
        AddBoundary("Entry_To_Crops_Wall_North", new Vector2(-9f, 3.3f), new Vector2(0.35f, 4.2f));
        AddBoundary("Entry_To_Crops_Wall_South", new Vector2(-9f, -3.3f), new Vector2(0.35f, 4.2f));
        AddBoundary("Crops_To_Control_Wall_North", new Vector2(9f, 3.3f), new Vector2(0.35f, 4.2f));
        AddBoundary("Crops_To_Control_Wall_South", new Vector2(9f, -3.3f), new Vector2(0.35f, 4.2f));

        // Entry vestibule shell with a gate-sized opening on its west-facing wall.
        AddBoundary("Entry_Outer_Wall_North", new Vector2(-11f, 3.4f), new Vector2(4f, 0.35f));
        AddBoundary("Entry_Outer_Wall_South", new Vector2(-11f, -3.4f), new Vector2(4f, 0.35f));
        AddBoundary("Entry_Outer_Wall_West_Upper", new Vector2(-12.85f, 2.3f), new Vector2(0.3f, 2.2f));
        AddBoundary("Entry_Outer_Wall_West_Lower", new Vector2(-12.85f, -2.3f), new Vector2(0.3f, 2.2f));

        // Utilities room perimeter; the east wall is sealed and access is from the crop hall.
        AddBoundary("Control_Room_Wall_North", new Vector2(11f, 4.4f), new Vector2(4f, 0.35f));
        AddBoundary("Control_Room_Wall_South", new Vector2(11f, -4.4f), new Vector2(4f, 0.35f));
        AddBoundary("Control_Room_Wall_East", new Vector2(12.85f, 0f), new Vector2(0.3f, 8.8f));
    }

    private static void AddBoundary(string name, Vector2 center, Vector2 size)
    {
        var wall = new GameObject(name);
        wall.transform.position = center;
        var collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static GameObject SpriteObject(string name, string folder, string spriteName, Vector3 position, Vector3 scale, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = scale;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(folder, spriteName);
        renderer.sortingOrder = sortingOrder;
        return go;
    }

    private static void AddSolidCollider(GameObject go, Vector2 size, Vector2 offset)
    {
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = offset;
    }

    private static void AddMonitorInteraction(GameObject go, string displayName, string result)
    {
        var renderer = go.GetComponent<SpriteRenderer>();
        var collider = go.AddComponent<BoxCollider2D>();
        var bounds = renderer.sprite.bounds.size;
        collider.size = new Vector2(bounds.x * 0.92f, bounds.y * 0.92f);
        collider.isTrigger = true;

        var interactable = go.AddComponent<MarsInteractable2D>();
        var serialized = new SerializedObject(interactable);
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("result").stringValue = result;
        serialized.FindProperty("repeatable").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSimplePipe(string name, Vector3 start, Vector3 end, Color color)
    {
        var go = new GameObject(name);
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = 0.035f;
        line.endWidth = 0.035f;
        line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PipeMaterialPath);
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = 1;
    }

    private static void EnsurePipeMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(PipeMaterialPath) != null) return;
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        var material = new Material(shader) { name = "Module_Pipe_Line" };
        AssetDatabase.CreateAsset(material, PipeMaterialPath);
        AssetDatabase.SaveAssets();
    }

    private static void BuildInteriorCamera()
    {
        var cameraObject = new GameObject("Interior_Main_Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 3.9f;
        camera.backgroundColor = new Color(0.78f, 0.77f, 0.73f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        var follow = cameraObject.AddComponent<MarsCameraFollow2D>();
        var so = new SerializedObject(follow);
        so.FindProperty("mapSize").vector2Value = new Vector2(48f, 20f);
        so.FindProperty("damping").floatValue = 6f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPortal(MarsScenePortal2D portal, string scene, string spawnName, bool returning)
    {
        var so = new SerializedObject(portal);
        so.FindProperty("destinationScene").stringValue = scene;
        so.FindProperty("spawnObjectName").stringValue = spawnName;
        so.FindProperty("returnToExterior").boolValue = returning;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddInteriorToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool exists = false;
        foreach (var scene in scenes)
        {
            if (scene.path != InteriorScenePath) continue;
            exists = true;
            scene.enabled = true;
        }
        if (!exists) scenes.Add(new EditorBuildSettingsScene(InteriorScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

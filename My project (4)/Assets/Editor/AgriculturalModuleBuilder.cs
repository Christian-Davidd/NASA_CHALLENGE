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

    private static float GetPixelsPerUnit(string assetPath)
    {
        if (assetPath.Contains("/Plants/")) return 400f;
        if (assetPath.Contains("/Exterior/")) return 140f;
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

        // Three connected pressure rooms: entry/decon, production greenhouse, and utilities/control.
        // Leave a comfortable 1.19 m aisle between arrays; the player's 0.72 m collider
        // should not scrape neighboring trays while moving through the long rows.
        Vector3 rackScale = new Vector3(0.64f, 0.55f, 1f);
        float[] rackColumns = { -5.8f, 0f, 5.8f };
        foreach (float x in rackColumns)
        foreach (float y in new[] { -3.2f, 3.2f })
        {
            string side = y > 0 ? "North" : "South";
            string column = x < -1f ? "West" : x > 1f ? "East" : "Center";
            var bench = SpriteObject($"Hydroponic_Bench_{column}_{side}", "Cultivation", "hydroponic_bench_pillow_array", new Vector3(x, y, 0f), rackScale, 0);
            AddSolidCollider(bench, new Vector2(7.2f, 7.2f), Vector2.zero);
            PlacePlants(bench.transform, y < 0, column);
        }

        // Small west vestibule: the exterior arrival cycle ends here; the astronaut presses the inner-door panel.
        var vestibule = SpriteObject("Pressurized_Entry_Vestibule", "Airlock", "airlock_topdown", new Vector3(-11.2f, 0f, 0f), Vector3.one * 0.42f, 3);
        vestibule.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        var dustGrate = SpriteObject("Suit_Dust_Removal_Grate", "Airlock", "airlock_decon_grate", new Vector3(-11.2f, -0.85f, -0.02f), Vector3.one * 0.3f, 4);

        var outerGate = SpriteObject("Outer_Pressure_Gate", "Airlock", "airlock_pressure_gate_side", new Vector3(-13f, 0f, -0.08f), new Vector3(0.35f, 0.42f, 1f), 7);
        AddSolidCollider(outerGate, new Vector2(1.2f, 5.7f), Vector2.zero);
        var innerGate = SpriteObject("Inner_Pressure_Gate", "Airlock", "airlock_pressure_gate_side", new Vector3(-9f, 0f, -0.08f), new Vector3(0.35f, 0.42f, 1f), 7);
        var innerGateCollider = innerGate.AddComponent<BoxCollider2D>();
        innerGateCollider.size = new Vector2(1.2f, 5.7f);

        var accessPanel = SpriteObject("Crop_Hall_Pressure_Access_Panel", "Airlock", "airlock_cycle_console", new Vector3(-10.4f, 1.8f, -0.04f), Vector3.one * 0.22f, 8);
        var panelCollider = accessPanel.AddComponent<BoxCollider2D>();
        panelCollider.size = new Vector2(3.8f, 2.4f);
        panelCollider.isTrigger = true;
        var doorPanel = accessPanel.AddComponent<MarsPressureDoorPanel2D>();
        var panelConfiguration = new SerializedObject(doorPanel);
        panelConfiguration.FindProperty("pressureGate").objectReferenceValue = innerGate.transform;
        panelConfiguration.FindProperty("gateOpenPosition").vector3Value = new Vector3(-9f, 3.65f, -0.08f);
        panelConfiguration.FindProperty("gateCollider").objectReferenceValue = innerGateCollider;
        panelConfiguration.FindProperty("panelIndicator").objectReferenceValue = accessPanel.GetComponent<SpriteRenderer>();
        panelConfiguration.FindProperty("equalizationSeconds").floatValue = 0.85f;
        panelConfiguration.FindProperty("slideSeconds").floatValue = 0.48f;
        panelConfiguration.ApplyModifiedPropertiesWithoutUndo();

        var spawn = new GameObject("Cultivation_Interior_Spawn");
        spawn.transform.position = new Vector3(-13.55f, 0f, 0f);
        var vestibulePoint = new GameObject("Entry_Vestibule_Arrival_Point");
        vestibulePoint.transform.position = new Vector3(-11.2f, 0f, 0f);
        var directSceneSpawn = new GameObject("Direct_Interior_Spawn");
        directSceneSpawn.transform.position = vestibulePoint.transform.position;

        var returnPortalObject = new GameObject("Interior_Exterior_Portal");
        returnPortalObject.transform.position = new Vector3(-12.5f, -0.05f, 0f);
        var exitTrigger = returnPortalObject.AddComponent<BoxCollider2D>();
        exitTrigger.isTrigger = true;
        exitTrigger.size = new Vector2(1.1f, 1.5f);
        var returnPortal = returnPortalObject.AddComponent<MarsScenePortal2D>();
        SetPortal(returnPortal, "Mars_Natural_Landscape_01", "", true);

        var lamps = new SpriteRenderer[3];
        Color off = new Color(0.27f, 0.29f, 0.28f);
        for (int i = 0; i < lamps.Length; i++)
        {
            var lamp = SpriteObject($"Airlock_Cycle_Lamp_{i + 1}", "Airlock", "airlock_cycle_lamp",
                new Vector3(-11.85f + i * 0.42f, 2.8f, -0.06f), Vector3.one * 0.11f, 8);
            lamps[i] = lamp.GetComponent<SpriteRenderer>();
            lamps[i].color = off;
        }

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

        // Third room: an adapted environmental/life-support control bay, not a claim about ISS room layout.
        var environmental = SpriteObject("Environmental_Control_Station", "Lighting", "environmental_control_panel", new Vector3(12.15f, 3.35f, 0f), Vector3.one * 0.16f, 3);
        AddMonitorInteraction(environmental, "Monitoreo ambiental", "Muestra seguimiento de temperatura, humedad relativa, CO2, oxígeno y filtración del aire.");
        var rootMonitor = SpriteObject("Crop_Root_Zone_Monitor", "Lighting", "crop_root_zone_monitor_panel", new Vector3(12.15f, 1.95f, 0f), Vector3.one * 0.16f, 3);
        AddMonitorInteraction(rootMonitor, "Sensores de cultivo", "Muestra humedad y oxígeno de la zona radicular, temperatura del follaje e imágenes de seguimiento.");
        var waterMonitor = SpriteObject("Water_Loop_Monitor", "Water_System", "water_loop_monitor_panel", new Vector3(12.15f, 0.55f, 0f), Vector3.one * 0.16f, 3);
        AddMonitorInteraction(waterMonitor, "Circuito de agua y nutrientes", "Muestra nivel del depósito, flujo de alimentación/retorno y canales de seguimiento de nutrientes.");
        var growChamber = SpriteObject("Sealed_Plant_Growth_Chamber", "Cultivation", "sealed_growth_chamber", new Vector3(12.1f, -0.9f, 0f), Vector3.one * 0.13f, 3);
        AddSolidCollider(growChamber, growChamber.GetComponent<SpriteRenderer>().sprite.bounds.size * 0.86f, Vector2.zero);
        var fan = SpriteObject("Air_Circulation_Filter", "Lighting", "circulation_fan_filter", new Vector3(10.75f, 3.25f, 0f), Vector3.one * 0.15f, 3);
        AddSolidCollider(fan, fan.GetComponent<SpriteRenderer>().sprite.bounds.size * 0.84f, Vector2.zero);
        var water = SpriteObject("Water_Reservoir_And_Pump", "Water_System", "water_reservoir_pump_unit", new Vector3(10.8f, -2.9f, 0f), Vector3.one * 0.15f, 2);
        AddSolidCollider(water, water.GetComponent<SpriteRenderer>().sprite.bounds.size * 0.84f, Vector2.zero);
        var nutrients = SpriteObject("Nutrient_Cartridge_Station", "Water_System", "nutrient_cartridge_rack", new Vector3(12.15f, -3.55f, 0f), Vector3.one * 0.12f, 3);
        AddSolidCollider(nutrients, nutrients.GetComponent<SpriteRenderer>().sprite.bounds.size * 0.84f, Vector2.zero);

        AddSimplePipe("Water_Supply_Line", new Vector3(11.45f, -2.65f, 0f), new Vector3(12.1f, -3.1f, 0f), new Color(0.20f, 0.42f, 0.57f));
        AddSimplePipe("Nutrient_Return_Line", new Vector3(11.65f, -2.55f, 0f), new Vector3(12.3f, -3.15f, 0f), new Color(0.67f, 0.27f, 0.21f));

        BuildInteriorCamera();
        EditorSceneManager.SaveScene(scene, InteriorScenePath);
    }

    private static void PlacePlants(Transform bench, bool rightSide, string column)
    {
        string[] species = { "romaine_red", "lettuce_green", "mizuna", "dwarf_wheat" };
        string[] stages = { "sprout", "young", "developed", "mature" };
        const float pixelsPerUnit = 100f;
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int trayIndex = row * 3 + col;
                for (int pocket = 0; pocket < 6; pocket++)
                {
                    int index = trayIndex * 6 + pocket;
                    int columnOffset = column == "East" ? 2 : column == "Center" ? 1 : 0;
                    int speciesIndex = (trayIndex + pocket + (rightSide ? 1 : 0) + columnOffset) % species.Length;
                    int stageIndex = (row + col + pocket + (rightSide ? 1 : 0) + columnOffset) % stages.Length;
                    float svgX = 89f + col * 208f + (pocket % 3) * 48f;
                    float svgY = 90f + row * 151f + (pocket / 3) * 48f;
                    float localX = (svgX - 360f) / pixelsPerUnit * bench.localScale.x;
                    float localY = (360f - svgY) / pixelsPerUnit * bench.localScale.y;
                    var plant = SpriteObject($"Plant_{(rightSide ? "R" : "L")}_{index + 1:000}", "Plants",
                        $"plant_{species[speciesIndex]}_{stages[stageIndex]}",
                        bench.position + new Vector3(localX, localY, -0.02f), Vector3.one * 0.58f, 2);
                    plant.transform.SetParent(bench, true);
                }
            }
        }
    }

    private static void BuildBoundaryColliders()
    {
        // Central crop room is modestly shorter and narrower; both door openings exceed the hatch collider.
        AddBoundary("Crop_Hall_Wall_North", new Vector2(0f, 5.4f), new Vector2(18f, 0.35f));
        AddBoundary("Crop_Hall_Wall_South", new Vector2(0f, -5.4f), new Vector2(18f, 0.35f));
        AddBoundary("Entry_To_Crops_Wall_North", new Vector2(-9f, 3.525f), new Vector2(0.35f, 3.75f));
        AddBoundary("Entry_To_Crops_Wall_South", new Vector2(-9f, -3.525f), new Vector2(0.35f, 3.75f));
        AddBoundary("Crops_To_Control_Wall_North", new Vector2(9f, 3.025f), new Vector2(0.35f, 2.75f));
        AddBoundary("Crops_To_Control_Wall_South", new Vector2(9f, -3.025f), new Vector2(0.35f, 2.75f));

        // Small vestibule exterior shell, with a gate-sized opening at the west wall.
        AddBoundary("Entry_Outer_Wall_North", new Vector2(-11f, 3.4f), new Vector2(4f, 0.35f));
        AddBoundary("Entry_Outer_Wall_South", new Vector2(-11f, -3.4f), new Vector2(4f, 0.35f));
        AddBoundary("Entry_Outer_Wall_West_Upper", new Vector2(-12.85f, 2.5f), new Vector2(0.3f, 1.8f));
        AddBoundary("Entry_Outer_Wall_West_Lower", new Vector2(-12.85f, -2.5f), new Vector2(0.3f, 1.8f));

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
        line.startWidth = 0.08f;
        line.endWidth = 0.08f;
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
        camera.orthographicSize = 6.5f;
        camera.backgroundColor = new Color(0.78f, 0.77f, 0.73f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        var follow = cameraObject.AddComponent<MarsCameraFollow2D>();
        var so = new SerializedObject(follow);
        so.FindProperty("mapSize").vector2Value = new Vector2(28f, 14f);
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

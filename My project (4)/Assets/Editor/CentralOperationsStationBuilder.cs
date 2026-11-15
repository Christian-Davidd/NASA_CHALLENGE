using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CentralOperationsStationBuilder
{
    private const string ArtworkPath = "Assets/Art/Buildings/Central_Operations_Station/Exterior/central_operations_station_exterior.png";
    private const string PrefabPath = "Assets/Art/Buildings/Central_Operations_Station/Prefabs/Central_Operations_Station_Exterior.prefab";
    private const string ScenePath = "Assets/Scenes/Mars_Natural_Landscape_01.unity";
    private const float PixelsPerUnit = 140f;

    // Bounds of the opaque structure in the original 2048x1152 illustration.
    // The sprite pivot is aligned to the center of these bounds and the ground line.
    private const float ArtCenterX = (160f + 1916f) * 0.5f;
    private const float ArtGroundY = 986f;
    private const float ArtHalfWidth = (1916f - 160f) * 0.5f / PixelsPerUnit;
    private const float ModuleGap = 1.2f;

    [MenuItem("MARS/Build Central Operations Station")]
    public static void BuildAndIntegrate()
    {
        ConfigureArtwork();
        AssetDatabase.Refresh();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtworkPath);
        if (sprite == null)
            throw new FileNotFoundException("Central operations station sprite could not be imported: " + ArtworkPath);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
            throw new FileNotFoundException("Could not open the Mars exterior scene: " + ScenePath);

        var cropModule = GameObject.Find("Agricultural_Module_Exterior");
        if (cropModule == null)
            throw new MissingReferenceException("Agricultural_Module_Exterior was not found in the Mars scene.");

        var cropRenderer = cropModule.GetComponent<SpriteRenderer>();
        if (cropRenderer == null || cropRenderer.sprite == null)
            throw new MissingReferenceException("The agricultural exterior has no SpriteRenderer or sprite.");

        var oldStation = GameObject.Find("Central_Operations_Station_Exterior");
        if (oldStation != null)
            Object.DestroyImmediate(oldStation);

        var prefabRoot = CreateStationPrefab(sprite);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        Object.DestroyImmediate(prefabRoot);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            throw new FileNotFoundException("Central station prefab was not created: " + PrefabPath);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "Central_Operations_Station_Exterior";
        // Use the agricultural module's actual rendered edge and ground line. This keeps
        // the new habitat at a consistent scale and avoids its artwork overlapping the farm.
        instance.transform.position = new Vector3(
            cropRenderer.bounds.max.x + ArtHalfWidth + ModuleGap,
            cropRenderer.bounds.min.y,
            0f);
        instance.transform.localScale = Vector3.one;

        Physics2D.SyncTransforms();
        int clearanceSteps = 0;
        while (HasBlockingOverlap(instance) && clearanceSteps < 12)
        {
            instance.transform.position += Vector3.right * 1.5f;
            Physics2D.SyncTransforms();
            clearanceSteps++;
        }

        if (HasBlockingOverlap(instance))
            Debug.LogWarning("Central station still overlaps a solid scene object after moving right for clearance.");
        else if (clearanceSteps > 0)
            Debug.LogWarning("Central station shifted " + (clearanceSteps * 1.5f).ToString("0.0") +
                " m farther right to clear existing solid objects. No existing assets were moved.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Central operations station integrated to the right of the agricultural module with three solid hull colliders. Position: " + instance.transform.position);
    }

    private static void ConfigureArtwork()
    {
        var importer = AssetImporter.GetAtPath(ArtworkPath) as TextureImporter;
        if (importer == null)
            throw new FileNotFoundException("Artwork texture importer is unavailable: " + ArtworkPath);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
        textureSettings.spritePivot = new Vector2(ArtCenterX / 2048f, (1152f - ArtGroundY) / 1152f);
        importer.SetTextureSettings(textureSettings);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static GameObject CreateStationPrefab(Sprite sprite)
    {
        var root = new GameObject("Central_Operations_Station_Exterior");
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;

        // The closed pressure hull blocks the astronaut. The door approach below the
        // hull remains walkable; these three boxes follow the central and side modules.
        AddHullCollider(root.transform, "Central_Pressurized_Hull_Collision",
            new Vector2(-0.69f, 3.12f), new Vector2(7.36f, 4.41f));
        AddHullCollider(root.transform, "Crew_Module_Collision",
            new Vector2(-4.79f, 3.01f), new Vector2(2.02f, 3.33f));
        AddHullCollider(root.transform, "Life_Support_Module_Collision",
            new Vector2(4.25f, 2.98f), new Vector2(2.47f, 3.28f));
        return root;
    }

    private static void AddHullCollider(Transform parent, string objectName, Vector2 offset, Vector2 size)
    {
        var colliderObject = new GameObject(objectName);
        colliderObject.transform.SetParent(parent, false);
        var collider = colliderObject.AddComponent<BoxCollider2D>();
        collider.offset = offset;
        collider.size = size;
        collider.isTrigger = false;
    }

    private static bool HasBlockingOverlap(GameObject station)
    {
        var stationColliders = station.GetComponentsInChildren<Collider2D>();
        var sceneColliders = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        foreach (var sceneCollider in sceneColliders)
        {
            if (sceneCollider == null || !sceneCollider.enabled || sceneCollider.isTrigger)
                continue;
            if (sceneCollider.transform == station.transform || sceneCollider.transform.IsChildOf(station.transform))
                continue;
            if (sceneCollider.attachedRigidbody != null)
                continue;

            foreach (var stationCollider in stationColliders)
            {
                if (stationCollider != null && stationCollider.enabled && stationCollider.bounds.Intersects(sceneCollider.bounds))
                    return true;
            }
        }
        return false;
    }
}

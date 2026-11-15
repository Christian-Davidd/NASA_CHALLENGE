using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneOverlapAudit
{
    private const string ScenePath = "Assets/Scenes/Mars_Natural_Landscape_01.unity";

    public static void RepositionBoulders()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var boulders = GameObject.Find("Buried_Boulders_North");
        if (boulders == null)
            throw new System.InvalidOperationException("Buried_Boulders_North is missing from the scene.");

        // Keep the full rock cluster; move it into the open northeast ground corridor.
        var position = boulders.transform.localPosition;
        position.x = 31f;
        position.y = 29.5f;
        boulders.transform.localPosition = position;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"REPOSITIONED_WITHOUT_REMOVAL | Buried_Boulders_North | localPosition={position}");
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var colliders = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        var structuralRoots = new HashSet<Transform>();

        foreach (var collider in colliders)
        {
            if (!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy)
                continue;

            structuralRoots.Add(collider.transform.root);
            Debug.Log($"SOLID_COLLIDER | {collider.transform.root.name}/{collider.name} | pos={collider.transform.position} | bounds={collider.bounds}");
        }

        foreach (var root in structuralRoots)
        {
            var bounds = CombinedBounds(root.GetComponentsInChildren<Collider2D>());
            Debug.Log($"WORLD_ROOT | {root.name} | pos={root.position} | bounds={bounds}");
        }

        int overlapCount = 0;
        for (int i = 0; i < colliders.Length; i++)
        {
            var a = colliders[i];
            if (!a.enabled || a.isTrigger || !a.gameObject.activeInHierarchy)
                continue;

            for (int j = i + 1; j < colliders.Length; j++)
            {
                var b = colliders[j];
                if (!b.enabled || b.isTrigger || !b.gameObject.activeInHierarchy)
                    continue;
                if (a.transform == b.transform || a.transform.IsChildOf(b.transform) || b.transform.IsChildOf(a.transform))
                    continue;

                var ab = a.bounds;
                var bb = b.bounds;
                if (!ab.Intersects(bb))
                    continue;

                float overlapX = Mathf.Min(ab.max.x, bb.max.x) - Mathf.Max(ab.min.x, bb.min.x);
                float overlapY = Mathf.Min(ab.max.y, bb.max.y) - Mathf.Max(ab.min.y, bb.min.y);
                float overlapArea = overlapX * overlapY;
                float smallerArea = Mathf.Min(ab.size.x * ab.size.y, bb.size.x * bb.size.y);
                if (overlapX < 0.08f || overlapY < 0.08f || overlapArea < 0.2f || smallerArea <= 0f || overlapArea / smallerArea < 0.2f)
                    continue;

                overlapCount++;
                Debug.LogWarning($"SOLID_OVERLAP | {a.transform.root.name}/{a.name} <-> {b.transform.root.name}/{b.name} | intersection={overlapX:F2}x{overlapY:F2} | centers={ab.center} ; {bb.center}");
            }
        }

        Debug.Log($"SCENE_OVERLAP_AUDIT_COMPLETE | solidColliders={colliders.Length} | meaningfulOverlaps={overlapCount}");
    }

    private static Bounds CombinedBounds(Collider2D[] colliders)
    {
        bool initialized = false;
        Bounds bounds = default;
        foreach (var collider in colliders)
        {
            if (!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy)
                continue;

            if (!initialized)
            {
                bounds = collider.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return bounds;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene doorway that carries the existing astronaut between the surface and a module.
/// It is invoked by the existing E interaction in AstronautController2D.
/// </summary>
[DisallowMultipleComponent]
public sealed class MarsScenePortal2D : MonoBehaviour
{
    [SerializeField] private string destinationScene;
    [SerializeField] private string spawnObjectName = "Cultivation_Interior_Spawn";
    [SerializeField] private bool returnToExterior;

    private static GameObject travelingPlayer;
    private static Vector3 exteriorReturnPosition;
    private static bool hasExteriorReturnPosition;
    private static bool awaitingSceneChange;
    private static string pendingSpawnObjectName;

    public static void SetDefaultExteriorReturnPosition(Vector3 position)
    {
        if (hasExteriorReturnPosition) return;
        exteriorReturnPosition = position;
        hasExteriorReturnPosition = true;
    }

    public string Interact(GameObject player)
    {
        if (player == null || string.IsNullOrWhiteSpace(destinationScene))
            return "La compuerta no está disponible.";
        if (awaitingSceneChange)
            return "La transición entre el exterior y el módulo ya está en curso.";

        if (returnToExterior)
        {
            if (!hasExteriorReturnPosition)
                return "No se encontró el punto de regreso al exterior.";
        }
        else
        {
            exteriorReturnPosition = player.transform.position;
            hasExteriorReturnPosition = true;
        }

        travelingPlayer = player;
        pendingSpawnObjectName = spawnObjectName;
        DontDestroyOnLoad(player);

        awaitingSceneChange = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(destinationScene, LoadSceneMode.Single);
        return returnToExterior ? "Regresando a la superficie marciana…" : "Entrando al módulo agrícola…";
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!awaitingSceneChange) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        awaitingSceneChange = false;

        if (travelingPlayer == null) return;

        RemoveDuplicateAstronauts(travelingPlayer);

        if (scene.name == "Mars_Natural_Landscape_01" && hasExteriorReturnPosition)
        {
            travelingPlayer.transform.position = exteriorReturnPosition;
        }
        else
        {
            var spawn = GameObject.Find(string.IsNullOrWhiteSpace(pendingSpawnObjectName)
                ? "Cultivation_Interior_Spawn"
                : pendingSpawnObjectName);
            if (spawn != null)
                travelingPlayer.transform.position = spawn.transform.position;
        }

        var camera = Camera.main;
        var follow = camera != null ? camera.GetComponent<MarsCameraFollow2D>() : null;
        if (follow != null)
            follow.SendMessage("FindTargetIfNeeded", SendMessageOptions.DontRequireReceiver);

        if (scene.name == "Cultivation_Module_Interior")
        {
            var airlock = Object.FindFirstObjectByType<MarsAirlockCycle2D>();
            if (airlock != null) airlock.BeginEntry(travelingPlayer);
        }
    }

    private static void RemoveDuplicateAstronauts(GameObject keep)
    {
        var astronauts = Object.FindObjectsByType<AstronautController2D>(FindObjectsSortMode.None);
        foreach (var astronaut in astronauts)
        {
            if (astronaut != null && astronaut.gameObject != keep)
                Object.Destroy(astronaut.gameObject);
        }
    }
}

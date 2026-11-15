using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene doorway that carries the existing astronaut between the surface and a module
/// (Cultivation_Module_Interior or Spaceship_Interior).
/// It is invoked by the existing E interaction in AstronautController2D.
/// </summary>
[DisallowMultipleComponent]
public sealed class MarsScenePortal2D : MonoBehaviour
{
    [SerializeField] private string destinationScene;
    [SerializeField] private string spawnObjectName = "Cultivation_Interior_Spawn";
    [SerializeField] private bool returnToExterior;

    private static GameObject travelingPlayer;
    private static Vector3 exteriorReturnPosition = new Vector3(0f, -3.5f, 0f);
    private static bool hasExteriorReturnPosition;
    private static bool awaitingSceneChange;
    private static string pendingSpawnObjectName;
    private static Texture2D beaconTex;
    private GUIStyle beaconStyle;

    public static bool IsTransitioningToAnyInterior =>
        awaitingSceneChange &&
        (pendingSpawnObjectName == "Cultivation_Interior_Spawn" || pendingSpawnObjectName == "Spaceship_Interior_Spawn");

    public static void SetDefaultExteriorReturnPosition(Vector3 position)
    {
        if (hasExteriorReturnPosition) return;
        exteriorReturnPosition = position;
        hasExteriorReturnPosition = true;
    }

    public static void CleanupOnMissionRestart()
    {
        if (awaitingSceneChange)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            awaitingSceneChange = false;
        }
        hasExteriorReturnPosition = false;
        pendingSpawnObjectName = null;
        if (travelingPlayer != null)
        {
            Object.Destroy(travelingPlayer);
            travelingPlayer = null;
        }
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
            {
                exteriorReturnPosition = new Vector3(0f, -3.5f, 0f);
                hasExteriorReturnPosition = true;
            }
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

        if (returnToExterior) return "Regresando a la superficie marciana…";
        return destinationScene == "Spaceship_Interior"
            ? "Entrando a la Nave Espacial…"
            : "Entrando al módulo agrícola…";
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
            string fallbackSpawn = scene.name == "Spaceship_Interior"
                ? "Spaceship_Interior_Spawn"
                : "Cultivation_Interior_Spawn";

            var spawn = GameObject.Find(string.IsNullOrWhiteSpace(pendingSpawnObjectName)
                ? fallbackSpawn
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

    private void OnGUI()
    {
        if (returnToExterior) return;
        if (MarsMonitorScreen2D.IsOpen || CommsTowerCMEEvent.IsMinigameActive || MarsMissionHUD.IsScienceLabOpen || MarsSpaceshipStation2D.IsMinigameActive)
            return;

        var flr = DONKIFLRManager.Instance;
        var cam = Camera.main;
        if (cam == null) return;

        bool esNave = destinationScene == "Spaceship_Interior";
        bool mostrarGuiaFLR = !esNave && flr != null && flr.HayTormentaFLRActiva;
        bool mostrarGuiaNave = esNave &&
            (MarsSampleNode2D.HasHeldSample ||
             (flr != null && (flr.OxigenoActual < 45f || flr.AguaActual < 45f || flr.RequiereMantenimientoTermico)));

        if (!mostrarGuiaFLR && !mostrarGuiaNave) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0f, 1.35f, 0f));
        if (screenPos.z <= 0f) return;

        if (beaconTex == null)
        {
            beaconTex = new Texture2D(1, 1);
            beaconTex.SetPixel(0, 0, Color.white);
            beaconTex.Apply();
        }

        if (beaconStyle == null)
        {
            beaconStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.94f, 0.55f, 1f) }
            };
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
        float guiY = Screen.height - screenPos.y;
        Rect rect = new Rect(screenPos.x - 165f, guiY - 22f, 330f, 32f);

        Color prev = GUI.color;
        if (mostrarGuiaFLR)
        {
            GUI.color = Color.Lerp(new Color(0.14f, 0.28f, 0.12f, 0.92f), new Color(0.75f, 0.42f, 0.06f, 0.96f), pulse);
            GUI.DrawTexture(rect, beaconTex);
            GUI.color = new Color(0.35f, 1f, 0.55f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), beaconTex);
            GUI.color = prev;
            GUI.Label(rect, "🛡️ REFUGIO MÓDULO AGRÍCOLA [E]", beaconStyle);
        }
        else
        {
            GUI.color = Color.Lerp(new Color(0.08f, 0.20f, 0.34f, 0.92f), new Color(0.14f, 0.42f, 0.56f, 0.96f), pulse);
            GUI.DrawTexture(rect, beaconTex);
            GUI.color = new Color(0.32f, 0.92f, 1.0f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), beaconTex);
            GUI.color = prev;
            GUI.Label(rect, "🚀 NAVE: MUESTRAS / O₂ / AGUA / TÉRMICO [E]", beaconStyle);
        }
    }
}

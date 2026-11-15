using System.Collections;
using UnityEngine;

/// <summary>
/// Runs the visible dust-control and pressure-equalization arrival before returning control to the astronaut.
/// Also creates a player from the saved project prefab when this scene is opened directly.
/// </summary>
public sealed class MarsAirlockCycle2D : MonoBehaviour
{
    [SerializeField] private GameObject fallbackAstronautPrefab;
    [SerializeField] private Transform entrySpawn;
    [SerializeField] private Transform vestibuleDestination;
    [SerializeField] private Transform fallbackInteriorSpawn;
    [SerializeField] private Transform entryPressureGate;
    [SerializeField] private Vector3 entryGateOpenPosition;
    [SerializeField] private SpriteRenderer[] cycleLamps;

    private bool entrySequenceStarted;

    private void Start()
    {
        // Reuse the astronaut brought through the scene portal even if its prefab tag differs.
        var controller = Object.FindFirstObjectByType<AstronautController2D>();
        var astronaut = controller != null ? controller.gameObject : GameObject.FindGameObjectWithTag("Player");
        if (astronaut == null && fallbackAstronautPrefab != null)
        {
            MarsScenePortal2D.SetDefaultExteriorReturnPosition(new Vector3(3.8f, -4.2f, 0f));
            astronaut = Instantiate(fallbackAstronautPrefab);
            astronaut.name = "Astronaut_Player";
            if (fallbackInteriorSpawn != null) astronaut.transform.position = fallbackInteriorSpawn.position;
            else if (vestibuleDestination != null) astronaut.transform.position = vestibuleDestination.position;
        }
    }

    public void BeginEntry(GameObject astronaut)
    {
        if (entrySequenceStarted || astronaut == null) return;
        entrySequenceStarted = true;
        StartCoroutine(RunEntryCycle(astronaut));
    }

    private IEnumerator RunEntryCycle(GameObject astronaut)
    {
        var controller = astronaut.GetComponent<AstronautController2D>();
        var sprite = astronaut.GetComponent<SpriteRenderer>();
        var body = astronaut.GetComponent<Rigidbody2D>();

        if (entrySpawn != null) astronaut.transform.position = entrySpawn.position;
        controller?.SetTransitionLocked(true);
        controller?.FinishTransitionFacing(Vector2.right);

        SetLamp(0);
        yield return new WaitForSeconds(0.55f); // Outer hatch sealed; dust stays in the vestibule.
        SetLamp(1);
        yield return new WaitForSeconds(1.05f); // Filtered purge and pressure equalization.
        SetLamp(2);
        yield return new WaitForSeconds(0.3f);

        Vector3 closedGatePosition = entryPressureGate != null ? entryPressureGate.position : Vector3.zero;
        var entryGateCollider = entryPressureGate != null ? entryPressureGate.GetComponent<BoxCollider2D>() : null;
        if (entryPressureGate != null)
        {
            if (entryGateCollider != null) entryGateCollider.enabled = false;
            yield return MoveTransform(entryPressureGate, closedGatePosition, entryGateOpenPosition, 0.48f);
        }

        if (vestibuleDestination != null)
        {
            Vector3 start = astronaut.transform.position;
            Vector3 finish = vestibuleDestination.position;
            float duration = 1.4f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                Vector3 position = Vector3.Lerp(start, finish, t);
                if (body != null) body.position = position;
                else astronaut.transform.position = position;

                if (sprite != null && controller != null)
                    sprite.sprite = controller.GetTransitionWalkSprite(true, (int)(elapsed / 0.18f) % 2);
                yield return null;
            }
            if (body != null) body.position = finish;
            else astronaut.transform.position = finish;
        }

        if (entryPressureGate != null)
        {
            yield return MoveTransform(entryPressureGate, entryGateOpenPosition, closedGatePosition, 0.42f);
            if (entryGateCollider != null) entryGateCollider.enabled = true;
        }

        controller?.FinishTransitionFacing(Vector2.right);
        controller?.SetTransitionLocked(false);
    }

    private IEnumerator MoveTransform(Transform target, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            target.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        target.position = to;
    }

    private void SetLamp(int activeIndex)
    {
        if (cycleLamps == null) return;
        for (int i = 0; i < cycleLamps.Length; i++)
        {
            if (cycleLamps[i] == null) continue;
            cycleLamps[i].color = i == activeIndex
                ? (i == 0 ? new Color(0.86f, 0.22f, 0.18f) : i == 1 ? new Color(0.98f, 0.65f, 0.19f) : new Color(0.32f, 0.76f, 0.55f))
                : new Color(0.27f, 0.29f, 0.28f);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public sealed class AstronautController2D : MonoBehaviour
{
    [Header("Z-2 sprites")]
    [SerializeField] private Sprite frontIdle;
    [SerializeField] private Sprite backIdle;
    [SerializeField] private Sprite leftIdle;
    [SerializeField] private Sprite rightIdle;
    [SerializeField] private Sprite leftWalkA;
    [SerializeField] private Sprite leftWalkB;
    [SerializeField] private Sprite rightWalkA;
    [SerializeField] private Sprite rightWalkB;
    [SerializeField] private Sprite frontWalkA;
    [SerializeField] private Sprite frontWalkB;
    [SerializeField] private Sprite backWalkA;
    [SerializeField] private Sprite backWalkB;
    [SerializeField] private Sprite leftInteract;
    [SerializeField] private Sprite rightInteract;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 3.2f;
    [SerializeField, Min(0.1f)] private float interactionRadius = 1.6f;
    [SerializeField, Min(0.1f)] private float interactionPoseDuration = 0.7f;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private Vector2 facing = Vector2.down;
    private bool lastFacingLeft;
    private MarsInteractable2D nearbyInteractable;
    private float poseUntil;
    private float nextWalkFrame;
    private int walkFrame;
    private string statusMessage = "";
    private float statusUntil;
    private GUIStyle hudStyle;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        spriteRenderer.sprite = frontIdle;
    }

    private void Update()
    {
        if (CommsTowerCMEEvent.IsMinigameActive)
        {
            moveInput = Vector2.zero;
            UpdateSprite();
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float x = Axis(keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed,
                       keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed);
        float y = Axis(keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed,
                       keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed);
        moveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);

        if (moveInput.sqrMagnitude > 0.01f)
            facing = moveInput;
        if (Mathf.Abs(moveInput.x) > 0.01f)
            lastFacingLeft = moveInput.x < 0f;

        FindNearbyInteractable();
        if (keyboard.eKey.wasPressedThisFrame)
            Interact();

        UpdateSprite();
    }

    private void FixedUpdate()
    {
        body.linearVelocity = moveInput * moveSpeed;
    }

    private static float Axis(bool negative, bool positive)
    {
        return (positive ? 1f : 0f) - (negative ? 1f : 0f);
    }

    private void FindNearbyInteractable()
    {
        nearbyInteractable = null;
        var hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        float bestDistance = float.MaxValue;
        foreach (var hit in hits)
        {
            var candidate = hit.GetComponent<MarsInteractable2D>();
            if (candidate == null || candidate.IsComplete) continue;
            float distance = Vector2.Distance(transform.position, hit.ClosestPoint(transform.position));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearbyInteractable = candidate;
            }
        }
    }

    private void Interact()
    {
        var rover = FindFirstObjectByType<RobotCompanionFollow2D>();
        if (rover != null && rover.TryDeliverSample(transform, out string roverMessage))
        {
            statusMessage = roverMessage;
            statusUntil = Time.time + 4f;
            poseUntil = Time.time + interactionPoseDuration;
            return;
        }

        float searchRadius = Mathf.Max(interactionRadius, 2.2f);
        var hits = Physics2D.OverlapCircleAll(transform.position, searchRadius);

        foreach (var hit in hits)
        {
            var tower = hit.GetComponent<CommsTowerCMEEvent>();
            if (tower != null)
            {
                statusMessage = tower.InteractFromPlayer();
                statusUntil = Time.time + 3f;
                poseUntil = Time.time + interactionPoseDuration;
                return;
            }
        }

        foreach (var hit in hits)
        {
            var crop = hit.GetComponent<MarsCropPlot2D>();
            if (crop != null)
            {
                statusMessage = crop.InteractFromPlayer();
                statusUntil = Time.time + 3f;
                poseUntil = Time.time + interactionPoseDuration;
                return;
            }
        }

        MarsSampleNode2D closestSample = null;
        float bestSampleDist = float.MaxValue;
        foreach (var hit in hits)
        {
            var sample = hit.GetComponent<MarsSampleNode2D>();
            if (sample == null || !sample.IsAvailable) continue;
            float d = Vector2.Distance(transform.position, hit.ClosestPoint(transform.position));
            if (d < bestSampleDist)
            {
                bestSampleDist = d;
                closestSample = sample;
            }
        }

        if (closestSample != null)
        {
            statusMessage = closestSample.CollectSample();
            statusUntil = Time.time + 3f;
            poseUntil = Time.time + interactionPoseDuration;
            return;
        }

        if (nearbyInteractable == null)
        {
            statusMessage = "Acércate a la Torre, Cultivos o una Muestra y presiona [E].";
            statusUntil = Time.time + 2f;
            return;
        }

        statusMessage = nearbyInteractable.Interact();
        statusUntil = Time.time + 3f;
        poseUntil = Time.time + interactionPoseDuration;
    }

    private void UpdateSprite()
    {
        spriteRenderer.sortingOrder = 10 + Mathf.RoundToInt(-transform.position.y * 3f);

        if (Time.time < poseUntil)
        {
            spriteRenderer.sprite = lastFacingLeft ? leftInteract : rightInteract;
            return;
        }

        if (moveInput.sqrMagnitude > 0.01f)
        {
            if (Time.time >= nextWalkFrame)
            {
                walkFrame = 1 - walkFrame;
                nextWalkFrame = Time.time + 0.18f;
            }

            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            {
                spriteRenderer.sprite = moveInput.x < 0
                    ? (walkFrame == 0 ? leftWalkA : leftWalkB)
                    : (walkFrame == 0 ? rightWalkA : rightWalkB);
            }
            else
            {
                spriteRenderer.sprite = moveInput.y > 0
                    ? (walkFrame == 0 ? backWalkA : backWalkB)
                    : (walkFrame == 0 ? frontWalkA : frontWalkB);
            }
            return;
        }

        if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
            spriteRenderer.sprite = facing.x < 0 ? leftIdle : rightIdle;
        else
            spriteRenderer.sprite = facing.y > 0 ? backIdle : frontIdle;
    }

    private void OnGUI()
    {
        if (Time.time >= statusUntil) return;

        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.Max(14, Screen.height / 54),
                padding = new RectOffset(16, 16, 10, 10),
                normal = { textColor = Color.white }
            };
        }

        GUI.Box(new Rect(18, Screen.height - 82, Mathf.Min(700, Screen.width - 36), 60),
            statusMessage, hudStyle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}

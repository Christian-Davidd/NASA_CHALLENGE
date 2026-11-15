using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

    private float ActiveInteractionRadius => SceneManager.GetActiveScene().name == "Cultivation_Module_Interior"
        ? 0.9f
        : Mathf.Max(interactionRadius, 2.2f);

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private Vector2 facing = Vector2.down;
    private bool lastFacingLeft;
    private bool transitionLocked;
    private MarsInteractable2D nearbyInteractable;
    private MarsPressureDoorPanel2D nearbyPressurePanel;
    private MarsDecontaminationPanel2D nearbyDecontaminationPanel;
    private MarsCropLifecycle2D nearbyCropBank;
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
        if (transitionLocked)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (MarsMonitorScreen2D.IsOpen)
        {
            moveInput = Vector2.zero;
            UpdateSprite();
            return;
        }

        if (CommsTowerCMEEvent.IsMinigameActive ||
            MarsMissionHUD.IsScienceLabOpen ||
            MarsSpaceshipStation2D.IsMinigameActive ||
            (DONKIFLRManager.Instance != null && DONKIFLRManager.Instance.MisionFallida))
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
        else if (keyboard.eKey.isPressed)
            HandleHoldInteract();

        UpdateSprite();
    }

    private void FixedUpdate()
    {
        if (transitionLocked)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }
        if (MarsMonitorScreen2D.IsOpen ||
            MarsSpaceshipStation2D.IsMinigameActive ||
            (DONKIFLRManager.Instance != null && DONKIFLRManager.Instance.MisionFallida))
        {
            body.linearVelocity = Vector2.zero;
            return;
        }
        float speedMul = (DONKIFLRManager.Instance != null && DONKIFLRManager.Instance.EscudoSobrecargadoSinControlTermico)
            ? 0.72f
            : 1f;
        body.linearVelocity = moveInput * (moveSpeed * speedMul);
    }

    public void SetTransitionLocked(bool locked)
    {
        transitionLocked = locked;
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.bodyType = locked ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        }
    }

    public Sprite GetTransitionWalkSprite(bool movingRight, int frame)
    {
        return movingRight
            ? (frame == 0 ? rightWalkA : rightWalkB)
            : (frame == 0 ? leftWalkA : leftWalkB);
    }

    public void FinishTransitionFacing(Vector2 direction)
    {
        facing = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
        {
            lastFacingLeft = facing.x < 0f;
            spriteRenderer.sprite = lastFacingLeft ? leftIdle : rightIdle;
        }
        else
        {
            spriteRenderer.sprite = facing.y > 0f ? backIdle : frontIdle;
        }
    }

    private static float Axis(bool negative, bool positive)
    {
        return (positive ? 1f : 0f) - (negative ? 1f : 0f);
    }

    private void FindNearbyInteractable()
    {
        nearbyInteractable = null;
        nearbyPressurePanel = null;
        nearbyDecontaminationPanel = null;
        nearbyCropBank = null;
        var hits = Physics2D.OverlapCircleAll(transform.position, ActiveInteractionRadius);
        float bestDistance = float.MaxValue;
        float bestPressureDistance = float.MaxValue;
        float bestDecontaminationDistance = float.MaxValue;
        float bestCropDistance = float.MaxValue;
        foreach (var hit in hits)
        {
            var decontaminationPanel = hit.GetComponent<MarsDecontaminationPanel2D>();
            if (decontaminationPanel != null && !decontaminationPanel.IsComplete)
            {
                float decontaminationDistance = Vector2.Distance(transform.position, hit.ClosestPoint(transform.position));
                if (decontaminationDistance < bestDecontaminationDistance)
                {
                    bestDecontaminationDistance = decontaminationDistance;
                    nearbyDecontaminationPanel = decontaminationPanel;
                }
            }

            var pressurePanel = hit.GetComponent<MarsPressureDoorPanel2D>();
            if (pressurePanel != null)
            {
                float pressureDistance = Vector2.Distance(transform.position, hit.ClosestPoint(transform.position));
                if (pressureDistance < bestPressureDistance)
                {
                    bestPressureDistance = pressureDistance;
                    nearbyPressurePanel = pressurePanel;
                }
            }

            var cropBank = hit.GetComponent<MarsCropLifecycle2D>();
            if (cropBank != null)
            {
                float cropDistance = Vector2.Distance(transform.position, hit.ClosestPoint(transform.position));
                if (cropDistance < bestCropDistance)
                {
                    bestCropDistance = cropDistance;
                    nearbyCropBank = cropBank;
                }
            }

            var candidate = hit.GetComponent<MarsInteractable2D>();
            if (candidate == null || candidate.IsComplete) continue;
            float candidateDistance = Vector2.Distance(transform.position, hit.ClosestPoint(transform.position));
            if (candidateDistance < bestDistance)
            {
                bestDistance = candidateDistance;
                nearbyInteractable = candidate;
            }
        }
    }

    private void Interact()
    {
        if (nearbyDecontaminationPanel != null)
        {
            statusMessage = nearbyDecontaminationPanel.Interact();
            statusUntil = Time.time + 4f;
            poseUntil = Time.time + interactionPoseDuration;
            return;
        }

        if (nearbyPressurePanel != null)
        {
            statusMessage = nearbyPressurePanel.Interact(gameObject);
            statusUntil = Time.time + 4f;
            poseUntil = Time.time + interactionPoseDuration;
            return;
        }

        if (nearbyCropBank != null)
        {
            MarsMonitorScreen2D.OpenCrop(nearbyCropBank);
            statusMessage = $"Inspección de {nearbyCropBank.CropName}: etapa {nearbyCropBank.StageLabel}.";
            statusUntil = Time.time + 4f;
            poseUntil = Time.time + interactionPoseDuration;
            return;
        }

        var rover = FindFirstObjectByType<RobotCompanionFollow2D>();
        if (rover != null && rover.TryDeliverSample(transform, out string roverMessage))
        {
            statusMessage = roverMessage;
            statusUntil = Time.time + 4f;
            poseUntil = Time.time + interactionPoseDuration;
            return;
        }

        float searchRadius = ActiveInteractionRadius;
        var hits = Physics2D.OverlapCircleAll(transform.position, searchRadius);

        foreach (var hit in hits)
        {
            var shipStation = hit.GetComponent<MarsSpaceshipStation2D>();
            if (shipStation != null)
            {
                statusMessage = shipStation.InteractTapFromPlayer();
                statusUntil = Time.time + 3.5f;
                poseUntil = Time.time + interactionPoseDuration;
                return;
            }
        }

        foreach (var hit in hits)
        {
            var portal = hit.GetComponent<MarsScenePortal2D>();
            if (portal == null) continue;

            portal.Interact(gameObject);
            return;
        }

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
            var solar = hit.GetComponent<MarsSolarPanelStation2D>();
            if (solar != null)
            {
                statusMessage = solar.InteractHoldFromPlayer(0.14f);
                statusUntil = Time.time + 2.2f;
                poseUntil = Time.time + interactionPoseDuration;
                return;
            }
        }

        foreach (var hit in hits)
        {
            var ice = hit.GetComponent<MarsIceDepositNode2D>();
            if (ice != null)
            {
                statusMessage = ice.InteractHoldFromPlayer(0.14f);
                statusUntil = Time.time + 2.2f;
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
            return;

        statusMessage = nearbyInteractable.Interact();
        statusUntil = Time.time + 6f;
        poseUntil = Time.time + interactionPoseDuration;
    }

    private void HandleHoldInteract()
    {
        float searchRadius = ActiveInteractionRadius;
        var hits = Physics2D.OverlapCircleAll(transform.position, searchRadius);

        foreach (var hit in hits)
        {
            var solar = hit.GetComponent<MarsSolarPanelStation2D>();
            if (solar != null)
            {
                statusMessage = solar.InteractHoldFromPlayer(Time.deltaTime);
                statusUntil = Time.time + 1.2f;
                poseUntil = Time.time + 0.15f;
                return;
            }
        }

        foreach (var hit in hits)
        {
            var ice = hit.GetComponent<MarsIceDepositNode2D>();
            if (ice != null)
            {
                statusMessage = ice.InteractHoldFromPlayer(Time.deltaTime);
                statusUntil = Time.time + 1.2f;
                poseUntil = Time.time + 0.15f;
                return;
            }
        }

        foreach (var hit in hits)
        {
            var shipStation = hit.GetComponent<MarsSpaceshipStation2D>();
            if (shipStation != null && shipStation.SupportsHoldInteraction)
            {
                statusMessage = shipStation.InteractHoldFromPlayer(Time.deltaTime);
                statusUntil = Time.time + 1.2f;
                poseUntil = Time.time + 0.15f;
                return;
            }
        }
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
        if (MarsMonitorScreen2D.IsOpen || MarsSpaceshipStation2D.IsMinigameActive || CommsTowerCMEEvent.IsMinigameActive || MarsMissionHUD.IsScienceLabOpen) return;
        string message = Time.time < statusUntil
            ? statusMessage
            : nearbyDecontaminationPanel != null
                ? nearbyDecontaminationPanel.InteractionPrompt
                : nearbyPressurePanel != null
                ? nearbyPressurePanel.InteractionPrompt
                : nearbyCropBank != null
                    ? nearbyCropBank.InteractionPrompt
                    : nearbyInteractable != null ? nearbyInteractable.InteractionPrompt : null;
        if (string.IsNullOrEmpty(message)) return;

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
            message, hudStyle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}

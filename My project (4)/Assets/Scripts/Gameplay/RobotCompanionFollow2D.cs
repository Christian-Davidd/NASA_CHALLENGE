using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PERSONA 3] Script de seguimiento suave del Amigo Robot detrás del astronauta.
/// Mantiene una distancia cómoda detrás de la dirección de avance del jugador.
/// </summary>
public sealed class RobotCompanionFollow2D : MonoBehaviour
{
    [Header("Configuración de Seguimiento Suave")]
    [SerializeField] private Transform targetAstronaut;
    [SerializeField, Min(0.5f)] private float followDistance = 1.65f;
    [SerializeField, Min(0.05f)] private float smoothTime = 0.24f;
    [SerializeField] private float maxFollowSpeed = 6.5f;

    [Header("Visual del rover")]
    [SerializeField] private SpriteRenderer roverRenderer;
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;

    [Header("Búsqueda de muestras")]
    [SerializeField, Min(0.1f)] private float sampleSearchSeconds = 35f;

    private double sampleSearchElapsed;
    private enum SampleTrip { Following, Searching, Returning }
    private SampleTrip sampleTrip;
    private MarsSampleNode2D mapSample;
    private bool carryingBonus;
    private readonly List<MarsSampleNode2D> candidateSamples = new();
    private readonly List<Vector2> sampleRoute = new();
    private int waypointIndex;
    private float nextRouteUpdate;
    private Vector2 returnRouteTarget;
    private RoverSampleNavigation navigation;

    private const float SampleReach = 0.55f;
    private const float DeliveryReach = 2f;
    private const string BonusFact = "El regolito marciano contiene polvo y fragmentos de roca.";
    private Rigidbody2D astronautBody;
    private Vector2? lastSearchPosition;
    private GUIStyle giftNoticeStyle;

    private const float BobAmplitude = 0.045f;
    private const float BobFrequency = 1.4f;
    private Vector3 roverRestPosition;

    private Vector3 currentVelocity;
    private Vector3 lastTargetPosition;
    private Vector2 trailingDirection = new Vector2(-1f, -0.25f).normalized;

    private void Start()
    {
        if (roverRenderer != null)
            roverRestPosition = roverRenderer.transform.localPosition;

        FindTargetIfNeeded();
        if (targetAstronaut != null)
        {
            lastTargetPosition = targetAstronaut.position;
            astronautBody = targetAstronaut.GetComponent<Rigidbody2D>();
        }
    }

    private void FixedUpdate()
    {
        if (sampleTrip != SampleTrip.Following) return;

        FindTargetIfNeeded();
        if (targetAstronaut == null) return;

        // Medir el desplazamiento en pasos de física, independientemente de los FPS.
        Vector2 position = astronautBody != null
            ? astronautBody.position
            : (Vector2)targetAstronaut.position;

        if (lastSearchPosition.HasValue && (position - lastSearchPosition.Value).sqrMagnitude > 0.000001f)
        {
            sampleSearchElapsed += Time.fixedDeltaTime;
            if (sampleSearchElapsed >= sampleSearchSeconds)
            {
                sampleSearchElapsed = 0;
                BeginSampleTrip();
            }
        }

        lastSearchPosition = position;
    }

    private void OnGUI()
    {
        string notice;
        if (sampleTrip == SampleTrip.Searching)
            notice = "Voy por una muestra…";
        else if (sampleTrip == SampleTrip.Returning)
            notice = CanDeliverTo(targetAstronaut) ? "¡Tengo una muestra! Pulsa [E]" : "¡Encontré una muestra!";
        else
            return;

        var camera = Camera.main;
        if (camera == null) return;

        Vector3 labelPosition = roverRenderer != null
            ? new Vector3(roverRenderer.bounds.center.x, roverRenderer.bounds.max.y, transform.position.z)
            : transform.position + Vector3.up;
        Vector3 anchor = camera.WorldToScreenPoint(labelPosition);
        if (anchor.z <= 0f || anchor.x < 0f || anchor.x > Screen.width ||
            anchor.y < 0f || anchor.y > Screen.height) return;

        if (giftNoticeStyle == null)
        {
            giftNoticeStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
        }

        const float width = 260f;
        const float height = 30f;
        float x = Mathf.Clamp(anchor.x - width * 0.5f, 0f, Mathf.Max(0f, Screen.width - width));
        float y = Mathf.Max(0f, Screen.height - anchor.y - height - 8f);
        GUI.Box(new Rect(x, y, width, height), notice, giftNoticeStyle);
    }

    private void LateUpdate()
    {
        FindTargetIfNeeded();
        if (targetAstronaut == null) return;

        Vector3 targetDelta = targetAstronaut.position - lastTargetPosition;
        if (targetDelta.sqrMagnitude > 0.0004f)
        {
            trailingDirection = ((Vector2)targetDelta).normalized;
        }
        lastTargetPosition = targetAstronaut.position;

        // Posición objetivo justo detrás del astronauta según su dirección de caminata
        Vector3 desiredPos = targetAstronaut.position - (Vector3)(trailingDirection * followDistance);
        desiredPos.z = 0f;

        if (sampleTrip == SampleTrip.Following)
        {
            if (targetDelta.sqrMagnitude > 0.0004f)
                UpdateFacingSprite(trailingDirection);
        }
        else
        {
            desiredPos = GetSampleTripDestination();
            Vector2 movement = desiredPos - transform.position;
            if (movement.sqrMagnitude > 0.0004f)
                UpdateFacingSprite(movement);
        }

        if (sampleTrip == SampleTrip.Following)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPos,
                ref currentVelocity,
                smoothTime,
                maxFollowSpeed,
                Time.deltaTime
            );
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, desiredPos, maxFollowSpeed * Time.deltaTime);
        }

        if (roverRenderer != null)
        {
            float bob = BobAmplitude * Mathf.Sin(Time.time * BobFrequency * 2f * Mathf.PI);
            roverRenderer.transform.localPosition = roverRestPosition + Vector3.up * bob;
            roverRenderer.sortingOrder = 12 + Mathf.RoundToInt(-transform.position.y * 3f);
        }
    }

    private void BeginSampleTrip()
    {
        MarsSampleNode2D.GetAvailableSamples(gameObject.scene, candidateSamples);
        Vector2 start = transform.position;
        candidateSamples.Sort((a, b) =>
            ((Vector2)a.transform.position - start).sqrMagnitude.CompareTo(
                ((Vector2)b.transform.position - start).sqrMagnitude));

        foreach (var sample in candidateSamples)
        {
            if (!PlanRoute(sample.transform.position, SampleReach)) continue;
            mapSample = sample;
            sampleTrip = SampleTrip.Searching;
            return;
        }

        if (MarsSampleNode2D.AllMapSamplesCollected)
        {
            carryingBonus = true;
            StartReturnTrip();
        }
    }

    private Vector3 GetSampleTripDestination()
    {
        if (sampleTrip == SampleTrip.Searching)
        {
            if (mapSample == null || !mapSample.IsAvailable)
            {
                ResetSampleTrip();
                BeginSampleTrip();
                return transform.position;
            }

            Vector3 samplePosition = mapSample.transform.position;
            if (((Vector2)(samplePosition - transform.position)).sqrMagnitude <= SampleReach * SampleReach &&
                mapSample.TryReserveForRover(this))
            {
                StartReturnTrip();
                return transform.position;
            }

            return NextWaypoint();
        }

        if (CanDeliverTo(targetAstronaut))
            return transform.position;

        Vector2 targetPosition = targetAstronaut.position;
        if (Time.time >= nextRouteUpdate &&
            (waypointIndex >= sampleRoute.Count || (targetPosition - returnRouteTarget).sqrMagnitude >= 1f))
        {
            nextRouteUpdate = Time.time + 0.6f;
            returnRouteTarget = targetPosition;
            PlanRoute(targetPosition, DeliveryReach * 0.8f);
        }

        return NextWaypoint();
    }

    private bool PlanRoute(Vector2 destination, float reach)
    {
        navigation ??= new RoverSampleNavigation();
        waypointIndex = 0;
        currentVelocity = Vector3.zero;
        return navigation.TryBuildRoute(transform.position, destination, reach, sampleRoute);
    }

    private Vector3 NextWaypoint()
    {
        while (waypointIndex < sampleRoute.Count &&
               ((Vector2)transform.position - sampleRoute[waypointIndex]).sqrMagnitude < 0.0001f)
        {
            waypointIndex++;
            currentVelocity = Vector3.zero;
        }

        return waypointIndex < sampleRoute.Count ? (Vector3)sampleRoute[waypointIndex] : transform.position;
    }

    private void StartReturnTrip()
    {
        sampleTrip = SampleTrip.Returning;
        sampleRoute.Clear();
        nextRouteUpdate = 0f;
    }

    private bool CanDeliverTo(Transform astronaut)
    {
        if (astronaut == null || astronaut != targetAstronaut ||
            ((Vector2)(astronaut.position - transform.position)).sqrMagnitude > DeliveryReach * DeliveryReach)
            return false;

        navigation ??= new RoverSampleNavigation();
        return navigation.HasClearPath(transform.position, astronaut.position);
    }

    public bool TryDeliverSample(Transform astronaut, out string message)
    {
        message = null;
        if (sampleTrip != SampleTrip.Returning || !CanDeliverTo(astronaut)) return false;

        if (carryingBonus)
        {
            MarsSampleNode2D.AddBonusSample();
            message = $"Muestra extra del rover ({MarsSampleNode2D.CollectedSamples}/{MarsSampleNode2D.TotalSamples}). {BonusFact}";
        }
        else if (mapSample == null || !mapSample.TryDeliverFromRover(this, out message))
        {
            ResetSampleTrip();
            return false;
        }

        ResetSampleTrip();
        return true;
    }

    private void OnDisable()
    {
        ResetSampleTrip();
    }

    private void ResetSampleTrip()
    {
        if (mapSample != null)
            mapSample.ReleaseRoverReservation(this);

        mapSample = null;
        carryingBonus = false;
        sampleRoute.Clear();
        sampleTrip = SampleTrip.Following;
        sampleSearchElapsed = 0;
        lastSearchPosition = null;
    }

    private void UpdateFacingSprite(Vector2 direction)
    {
        if (roverRenderer == null) return;

        Sprite facingSprite;
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            facingSprite = direction.x < 0f ? leftSprite : rightSprite;
        else
            facingSprite = direction.y > 0f ? backSprite : frontSprite;

        if (facingSprite != null)
            roverRenderer.sprite = facingSprite;
    }

    private void FindTargetIfNeeded()
    {
        if (targetAstronaut != null) return;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            targetAstronaut = playerObj.transform;
            astronautBody = playerObj.GetComponent<Rigidbody2D>();
            lastTargetPosition = targetAstronaut.position;
            lastSearchPosition = null;
        }
    }
}

// Busca un recorrido 2D con margen para el rover y evita los colliders sólidos del mapa.
internal sealed class RoverSampleNavigation
{
    private const float GridStep = 0.5f;
    private const float RoverClearance = 0.21f;
    private const int SearchLimit = 12000;
    private static readonly Vector2Int[] Neighbors =
    {
        Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down
    };

    private readonly List<RaycastHit2D> hits = new();
    private readonly Queue<Vector2Int> frontier = new();
    private readonly Dictionary<Vector2Int, Vector2Int> parents = new();
    private readonly ContactFilter2D solidOnly = new() { useTriggers = false };

    public bool HasClearPath(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.001f) return true;

        Physics2D.CircleCast(from, RoverClearance, delta / distance, solidOnly, hits, distance);
        foreach (var hit in hits)
        {
            if (hit.rigidbody == null || hit.rigidbody.bodyType == RigidbodyType2D.Static)
                return false;
        }

        return true;
    }

    public bool TryBuildRoute(Vector2 start, Vector2 destination, float reach, List<Vector2> route)
    {
        route.Clear();
        if (TryApproach(start, destination, reach, out Vector2 direct))
        {
            route.Add(direct);
            return true;
        }

        frontier.Clear();
        parents.Clear();
        frontier.Enqueue(Vector2Int.zero);
        parents.Add(Vector2Int.zero, Vector2Int.zero);
        while (frontier.Count > 0 && parents.Count < SearchLimit)
        {
            Vector2Int cell = frontier.Dequeue();
            Vector2 point = start + (Vector2)cell * GridStep;
            if (cell != Vector2Int.zero && TryApproach(point, destination, reach, out Vector2 approach))
            {
                route.Add(approach);
                while (cell != Vector2Int.zero)
                {
                    route.Add(start + (Vector2)cell * GridStep);
                    cell = parents[cell];
                }

                route.Reverse();
                return true;
            }

            foreach (var offset in Neighbors)
            {
                Vector2Int next = cell + offset;
                if (parents.ContainsKey(next) || !HasClearPath(point, start + (Vector2)next * GridStep))
                    continue;

                parents.Add(next, cell);
                frontier.Enqueue(next);
            }
        }

        return false;
    }

    private bool TryApproach(Vector2 from, Vector2 destination, float reach, out Vector2 approach)
    {
        Vector2 offset = from - destination;
        approach = offset.sqrMagnitude <= reach * reach
            ? from
            : destination + offset.normalized * (reach * 0.8f);
        return HasClearPath(from, approach) && HasClearPath(approach, destination);
    }
}

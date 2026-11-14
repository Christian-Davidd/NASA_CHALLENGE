using UnityEngine;

/// <summary>
/// [PERSONA 3] Script de seguimiento suave del Amigo Robot detrás del astronauta.
/// Mantiene una distancia cómoda detrás de la dirección de avance del jugador.
/// </summary>
[RequireComponent(typeof(MarsWorldPlaceholder2D))]
public sealed class RobotCompanionFollow2D : MonoBehaviour
{
    [Header("Configuración de Seguimiento Suave")]
    [SerializeField] private Transform targetAstronaut;
    [SerializeField, Min(0.5f)] private float followDistance = 1.65f;
    [SerializeField, Min(0.05f)] private float smoothTime = 0.24f;
    [SerializeField] private float maxFollowSpeed = 6.5f;

    private Vector3 currentVelocity;
    private Vector3 lastTargetPosition;
    private Vector2 trailingDirection = new Vector2(-1f, -0.25f).normalized;

    private void Start()
    {
        FindTargetIfNeeded();
        if (targetAstronaut != null)
        {
            lastTargetPosition = targetAstronaut.position;
        }
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

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref currentVelocity,
            smoothTime,
            maxFollowSpeed,
            Time.deltaTime
        );
    }

    private void FindTargetIfNeeded()
    {
        if (targetAstronaut != null) return;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            targetAstronaut = playerObj.transform;
        }
    }
}

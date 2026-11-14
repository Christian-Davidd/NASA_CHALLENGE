using UnityEngine;

public sealed class MarsCameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0.1f)] private float damping = 6f;
    [SerializeField] private Vector2 mapSize = new Vector2(108f, 72f);
    private Camera cameraComponent;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        FindTargetIfNeeded();
    }

    private void LateUpdate()
    {
        FindTargetIfNeeded();
        if (target == null) return;

        float halfHeight = cameraComponent.orthographicSize;
        float halfWidth = halfHeight * cameraComponent.aspect;
        float minX = -mapSize.x * .5f + halfWidth;
        float maxX = mapSize.x * .5f - halfWidth;
        float minY = -mapSize.y * .5f + halfHeight;
        float maxY = mapSize.y * .5f - halfHeight;
        float x = minX > maxX ? 0f : Mathf.Clamp(target.position.x, minX, maxX);
        float y = minY > maxY ? 0f : Mathf.Clamp(target.position.y, minY, maxY);
        var destination = new Vector3(x, y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, destination, 1f - Mathf.Exp(-damping * Time.deltaTime));
    }

    private void FindTargetIfNeeded()
    {
        if (target != null) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
    }
}

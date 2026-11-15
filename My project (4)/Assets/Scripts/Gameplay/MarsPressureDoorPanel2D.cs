using System.Collections;
using UnityEngine;

/// <summary>Wall-panel interaction for opening a pressure door from either side.</summary>
public sealed class MarsPressureDoorPanel2D : MonoBehaviour
{
    [SerializeField] private Transform pressureGate;
    [SerializeField] private Vector3 gateOpenPosition;
    [SerializeField] private BoxCollider2D gateCollider;
    [SerializeField] private SpriteRenderer panelIndicator;
    [SerializeField] private SpriteRenderer[] statusLamps;
    [SerializeField] private MarsDecontaminationPanel2D decontaminationCycle;
    [SerializeField] private MarsPressureDoorPanel2D sharedController;
    [SerializeField, Min(0f)] private float equalizationSeconds = 0.8f;
    [SerializeField, Min(0.1f)] private float slideSeconds = 0.45f;
    [SerializeField, Min(0f)] private float passThroughClearance = 1.2f;

    private Vector3 gateClosedPosition;
    private bool isBusy;
    private bool isOpen;

    public string InteractionPrompt => sharedController != null
        ? sharedController.InteractionPrompt
        : isBusy
            ? "Igualando la presión…"
            : isOpen ? "[E] Cerrar la compuerta" : "[E] Operar compuerta presurizada";

    private void Awake()
    {
        if (pressureGate != null)
            gateClosedPosition = pressureGate.position;
        SetIndicator(new Color(0.86f, 0.22f, 0.18f));
    }

    public string Interact(GameObject player)
    {
        if (sharedController != null)
            return sharedController.Interact(player);
        if (decontaminationCycle != null && !decontaminationCycle.IsComplete)
            return "Completa primero la purga filtrada en el panel de la esclusa.";
        if (pressureGate == null)
            return "El panel de la compuerta no responde.";
        if (isBusy)
            return "Igualando la presión. Espera a que el indicador cambie a verde.";

        if (isOpen)
        {
            if (player != null && Mathf.Abs(player.transform.position.x - pressureGate.position.x) < passThroughClearance)
                return "Aléjate del umbral antes de cerrar la compuerta.";
            StartCoroutine(CloseGate());
            return "Cerrando la compuerta presurizada.";
        }

        StartCoroutine(OpenForPassage(player));
        return "Iniciando el ciclo de presión para abrir el acceso a los cultivos.";
    }

    private IEnumerator OpenForPassage(GameObject player)
    {
        isBusy = true;
        SetIndicator(new Color(0.98f, 0.65f, 0.19f));
        yield return new WaitForSeconds(equalizationSeconds);

        if (gateCollider != null) gateCollider.enabled = false;
        yield return SlideGate(gateClosedPosition, gateOpenPosition);
        isOpen = true;
        isBusy = false;
        SetIndicator(new Color(0.32f, 0.76f, 0.55f));

        if (player == null) yield break;
        float startingSide = Mathf.Sign(player.transform.position.x - pressureGate.position.x);
        while (player != null && isOpen)
        {
            float relativeX = player.transform.position.x - pressureGate.position.x;
            bool crossed = Mathf.Sign(relativeX) != startingSide && Mathf.Abs(relativeX) >= passThroughClearance;
            if (crossed)
            {
                yield return new WaitForSeconds(0.55f);
                if (player != null && Mathf.Abs(player.transform.position.x - pressureGate.position.x) >= passThroughClearance)
                    break;
            }
            yield return null;
        }

        if (player != null && isOpen)
            yield return CloseGate();
    }

    private IEnumerator CloseGate()
    {
        isBusy = true;
        SetIndicator(new Color(0.98f, 0.65f, 0.19f));
        yield return SlideGate(pressureGate.position, gateClosedPosition);
        if (gateCollider != null) gateCollider.enabled = true;
        isOpen = false;
        isBusy = false;
        SetIndicator(new Color(0.86f, 0.22f, 0.18f));
    }

    private IEnumerator SlideGate(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < slideSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideSeconds));
            pressureGate.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        pressureGate.position = to;
    }

    private void SetIndicator(Color color)
    {
        if (statusLamps != null && statusLamps.Length > 0)
        {
            int activeIndex = color.r > 0.8f && color.g < 0.4f ? 0 : color.r > 0.8f ? 1 : 2;
            Color off = new Color(0.25f, 0.28f, 0.28f);
            for (int i = 0; i < statusLamps.Length; i++)
                if (statusLamps[i] != null) statusLamps[i].color = i == activeIndex ? color : off;
            return;
        }

        if (panelIndicator != null) panelIndicator.color = color;
    }
}

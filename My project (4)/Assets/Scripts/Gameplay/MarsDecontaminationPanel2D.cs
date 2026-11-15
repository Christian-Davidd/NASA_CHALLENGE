using System.Collections;
using UnityEngine;

/// <summary>Starts a brief filtered-air purge in the suit vestibule.</summary>
public sealed class MarsDecontaminationPanel2D : MonoBehaviour
{
    [SerializeField] private ParticleSystem purgeMist;
    [SerializeField, Min(0.5f)] private float purgeSeconds = 4f;

    private bool isRunning;
    public bool IsComplete { get; private set; }

    public string InteractionPrompt => isRunning
        ? "Purga de aire filtrado en curso…"
        : IsComplete ? "Purga completada · acceso habilitado" : "[E] Iniciar purga filtrada de la esclusa";

    public string Interact()
    {
        if (isRunning) return "La esclusa está realizando la purga. Espera a que termine.";
        if (IsComplete) return "Purga completada. Ya puedes abrir la compuerta de cultivos.";
        StartCoroutine(RunPurge());
        return "Iniciando la purga filtrada de polvo en la esclusa.";
    }

    private IEnumerator RunPurge()
    {
        isRunning = true;
        if (purgeMist != null)
        {
            purgeMist.Clear(true);
            purgeMist.Play(true);
        }

        yield return new WaitForSeconds(purgeSeconds);
        if (purgeMist != null) purgeMist.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        isRunning = false;
        IsComplete = true;
    }
}

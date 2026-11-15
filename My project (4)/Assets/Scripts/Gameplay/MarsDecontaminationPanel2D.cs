using System.Collections;
using UnityEngine;

/// <summary>Starts a brief filtered-air purge in the suit vestibule.</summary>
public sealed class MarsDecontaminationPanel2D : MonoBehaviour
{
    [SerializeField] private ParticleSystem purgeMist;
    [SerializeField, Min(0.5f)] private float purgeSeconds = 4f;

    private bool isRunning;
    public bool IsComplete { get; private set; }

    private void Awake()
    {
        if (purgeMist == null) return;

        // Keep the filtered mist inside the vestibule and only emit it when the panel is used.
        var main = purgeMist.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.035f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.18f);
        main.startColor = new Color(1f, 1f, 1f, 0.82f);
        main.maxParticles = 220;

        var emission = purgeMist.emission;
        emission.rateOverTime = 78f;

        var shape = purgeMist.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2.8f, 4.8f, 0.04f);

        var colorOverLifetime = purgeMist.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var mistGradient = new Gradient();
        mistGradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.68f, 0.18f),
                new GradientAlphaKey(0.58f, 0.72f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = mistGradient;

        purgeMist.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

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

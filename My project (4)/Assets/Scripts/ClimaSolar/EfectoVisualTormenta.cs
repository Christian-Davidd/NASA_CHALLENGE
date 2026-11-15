using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MarteSim.ClimaEspacial
{
    public class EfectoVisualTormenta : MonoBehaviour
    {
        [Header("1. Tinte de Pantalla (UI Image)")]
        [SerializeField] private Image overlayTormenta;
        [SerializeField] private Color colorClaseC = new Color(1f, 0.55f, 0.1f, 0.18f);  // Ámbar suave
        [SerializeField] private Color colorClaseM = new Color(1f, 0.30f, 0.05f, 0.28f); // Naranja intenso
        [SerializeField] private Color colorClaseX = new Color(0.95f, 0.08f, 0.05f, 0.40f); // Rojo crítico
        [SerializeField] private float velocidadPulso = 3.5f;
        [SerializeField] private float velocidadTransicion = 2.5f;

        [Header("2. Partículas de Polvo / Radiación (Opcional)")]
        [SerializeField] private ParticleSystem particulasTormenta;

        [Header("3. Vibración de Cámara por Viento (Opcional)")]
        [SerializeField] private Transform camaraJuego;
        [SerializeField] private float intensidadVibracion = 0.03f;

        private bool _efectoActivo = false;
        private Color _colorObjetivoActual;
        private float _alphaActual = 0f;
        private Vector3 _ultimoDesplazamientoCamara = Vector3.zero;
        private bool _suscrito = false;

        private void Awake()
        {
            if (overlayTormenta != null)
            {
                // Evita que el tinte rojo bloquee clics en los botones del HUD
                overlayTormenta.raycastTarget = false;
                Color c = overlayTormenta.color;
                c.a = 0f;
                overlayTormenta.color = c;
            }

            if (particulasTormenta != null)
            {
                particulasTormenta.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Start()
        {
            if (camaraJuego == null && Camera.main != null)
            {
                camaraJuego = Camera.main.transform;
            }

            // Acopla las partículas a la cámara de la escena actual conservando su posición/rotación local
            if (particulasTormenta != null && camaraJuego != null && particulasTormenta.transform.parent != camaraJuego)
            {
                particulasTormenta.transform.SetParent(camaraJuego, false);
            }

            SuscribirEventos();
        }

        private void OnEnable()
        {
            SuscribirEventos();
        }

        private void OnDisable()
        {
            if (_suscrito && TormentaSolarManager.Instance != null)
            {
                TormentaSolarManager.Instance.OnAlertaTormentaIniciada -= ActivarEfecto;
                TormentaSolarManager.Instance.OnAlertaTormentaFinalizada -= DesactivarEfecto;
                _suscrito = false;
            }
            RestaurarCamara();
        }

        private void SuscribirEventos()
        {
            if (_suscrito || TormentaSolarManager.Instance == null) return;
            TormentaSolarManager.Instance.OnAlertaTormentaIniciada += ActivarEfecto;
            TormentaSolarManager.Instance.OnAlertaTormentaFinalizada += DesactivarEfecto;
            _suscrito = true;
        }

        private void ActivarEfecto(DatosTormentaActiva datos)
        {
            _efectoActivo = true;

            switch (datos.claseSolar)
            {
                case "X": _colorObjetivoActual = colorClaseX; break;
                case "M": _colorObjetivoActual = colorClaseM; break;
                default: _colorObjetivoActual = colorClaseC; break;
            }

            if (particulasTormenta != null && !particulasTormenta.isPlaying)
            {
                particulasTormenta.Play();
            }
        }

        private void DesactivarEfecto()
        {
            _efectoActivo = false;

            if (particulasTormenta != null && particulasTormenta.isPlaying)
            {
                particulasTormenta.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void Update()
        {
            ActualizarTintePantalla();
            ActualizarVibracionCamara();
        }

        private void ActualizarTintePantalla()
        {
            if (overlayTormenta == null) return;

            float alphaDeseado = 0f;
            if (_efectoActivo)
            {
                // Oscila suavemente entre el 65% y el 100% de la opacidad del color según la severidad
                float pulso = Mathf.Lerp(0.65f, 1f, (Mathf.Sin(Time.time * velocidadPulso) + 1f) * 0.5f);
                alphaDeseado = _colorObjetivoActual.a * pulso;
            }

            _alphaActual = Mathf.MoveTowards(_alphaActual, alphaDeseado, velocidadTransicion * Time.deltaTime);

            Color colorFinal = _colorObjetivoActual;
            colorFinal.a = _alphaActual;
            overlayTormenta.color = colorFinal;
        }

        private void ActualizarVibracionCamara()
        {
            if (camaraJuego == null) return;

            // Revertimos el desplazamiento del frame anterior para no interferir con scripts de seguimiento
            RestaurarCamara();

            if (_efectoActivo && _alphaActual > 0.02f)
            {
                Vector2 ruido = Random.insideUnitCircle * intensidadVibracion;
                _ultimoDesplazamientoCamara = new Vector3(ruido.x, ruido.y, 0f);
                camaraJuego.localPosition += _ultimoDesplazamientoCamara;
            }
        }

        private void RestaurarCamara()
        {
            if (camaraJuego != null && _ultimoDesplazamientoCamara != Vector3.zero)
            {
                camaraJuego.localPosition -= _ultimoDesplazamientoCamara;
                _ultimoDesplazamientoCamara = Vector3.zero;
            }
        }

        [ContextMenu("Probar Efecto Tormenta (8 segundos)")]
        public void ProbarEfectoManual()
        {
            StopAllCoroutines();
            StartCoroutine(RutinaPrueba());
        }

        private IEnumerator RutinaPrueba()
        {
            ActivarEfecto(new DatosTormentaActiva { claseSolar = "X", nivelAlerta = "PRUEBA" });
            yield return new WaitForSeconds(8f);
            DesactivarEfecto();
        }
    }
}
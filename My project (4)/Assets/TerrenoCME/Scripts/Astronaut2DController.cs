using UnityEngine;
using UnityEngine.InputSystem;

namespace JuniorAstronaut.TerrenoCME
{
    public class Astronaut2DController : MonoBehaviour
    {
        [Header("=== MOVIMIENTO 2D ===")]
        public float velocidad = 5.5f;
        public float limiteIzquierdo = -7.5f;
        public float limiteDerecho = 7.5f;

        [Header("=== INTERACCION ===")]
        public float rangoInteraccion = 2.0f;
        public CommsTowerCMEEvent torreDeComunicaciones;

        [Header("=== SPRITES ASTRONAUTA Z2 (Derecha) ===")]
        public Sprite neutroDerecho;
        public Sprite caminarDerecho1;
        public Sprite caminarDerecho2;
        public Sprite alertaDerecho;
        public Sprite interactuarDerecho;
        public Sprite repararDerecho;

        [Header("=== SPRITES ASTRONAUTA Z2 (Izquierda) ===")]
        public Sprite neutroIzquierdo;
        public Sprite caminarIzquierdo1;
        public Sprite caminarIzquierdo2;
        public Sprite alertaIzquierdo;
        public Sprite interactuarIzquierdo;
        public Sprite repararIzquierdo;

        public bool EstaCercaDeLaTorre { get; private set; }

        private SpriteRenderer spriteRenderer;
        private bool mirandoDerecha = true;
        private float animTimer = 0f;
        private bool pasoCaminar = false;
        private float accionTimer = 0f;
        private bool accionEsReparar = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            float movX = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                movX = -1f;
                mirandoDerecha = false;
            }
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                movX = 1f;
                mirandoDerecha = true;
            }

            bool caminando = Mathf.Abs(movX) > 0.01f;
            if (caminando)
            {
                Vector3 pos = transform.position;
                pos.x = Mathf.Clamp(pos.x + movX * velocidad * Time.deltaTime, limiteIzquierdo, limiteDerecho);
                transform.position = pos;

                animTimer += Time.deltaTime;
                if (animTimer >= 0.18f)
                {
                    animTimer = 0f;
                    pasoCaminar = !pasoCaminar;
                }
            }
            else
            {
                animTimer = 0f;
            }

            if (accionTimer > 0f)
            {
                accionTimer -= Time.deltaTime;
            }

            // Verificar cercania fisica con la Torre de Comunicaciones
            if (torreDeComunicaciones != null)
            {
                float dist = Mathf.Abs(transform.position.x - torreDeComunicaciones.transform.position.x);
                EstaCercaDeLaTorre = dist <= rangoInteraccion;

                if (EstaCercaDeLaTorre && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    accionEsReparar = torreDeComunicaciones.antenaDanada;
                    accionTimer = 0.45f;
                    torreDeComunicaciones.InteractuarDesdeAstronauta();
                }
            }

            ActualizarSprite(caminando);
        }

        private void ActualizarSprite(bool caminando)
        {
            if (spriteRenderer == null) return;

            Sprite nuevoSprite = null;

            if (accionTimer > 0f)
            {
                if (accionEsReparar)
                    nuevoSprite = mirandoDerecha ? repararDerecho : repararIzquierdo;
                else
                    nuevoSprite = mirandoDerecha ? interactuarDerecho : interactuarIzquierdo;
            }
            else if (caminando)
            {
                if (mirandoDerecha)
                    nuevoSprite = pasoCaminar ? caminarDerecho1 : caminarDerecho2;
                else
                    nuevoSprite = pasoCaminar ? caminarIzquierdo1 : caminarIzquierdo2;
            }
            else if (torreDeComunicaciones != null && torreDeComunicaciones.alertaActiva)
            {
                nuevoSprite = mirandoDerecha ? alertaDerecho : alertaIzquierdo;
            }
            else
            {
                nuevoSprite = mirandoDerecha ? neutroDerecho : neutroIzquierdo;
            }

            if (nuevoSprite != null)
            {
                spriteRenderer.sprite = nuevoSprite;
            }
        }
    }
}

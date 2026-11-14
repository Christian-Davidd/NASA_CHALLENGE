using UnityEngine;

namespace JuniorAstronaut.TerrenoCME
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ProceduralBox2D : MonoBehaviour
    {
        public Color colorInicial = Color.white;
        private SpriteRenderer sr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            sr.color = colorInicial;
        }

        public void CambiarColor(Color nuevoColor)
        {
            if (sr != null) sr.color = nuevoColor;
        }
    }
}


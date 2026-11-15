using UnityEngine;

/// <summary>
/// Dibuja un cuadrado placeholder 2D con su etiqueta flotante en pantalla
/// para identificar dónde irá cada asset definitivo (Torre, Cultivos, Muestras, Robot).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MarsWorldPlaceholder2D : MonoBehaviour
{
    [Header("Etiqueta e Identificación")]
    [SerializeField] private string labelTitle = "[PLACEHOLDER]";
    [SerializeField] private string labelSubtitle = "Reemplazar con asset final";
    [SerializeField] private Color squareColor = new Color(0.2f, 0.8f, 0.95f, 0.92f);
    [SerializeField] private Color labelTextColor = Color.white;
    [SerializeField] private float labelHeightOffset = 1.35f;
    [SerializeField] private bool tintSpriteWithSquareColor = true;

    private SpriteRenderer spriteRenderer;
    private static Sprite fallbackSquareSprite;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private static Texture2D labelBgTex;

    public string LabelTitle
    {
        get => labelTitle;
        set => labelTitle = value;
    }

    public string LabelSubtitle
    {
        get => labelSubtitle;
        set => labelSubtitle = value;
    }

    private void Awake()
    {
        EnsureVisuals();
    }

    private void OnEnable()
    {
        EnsureVisuals();
    }

    public void SetVisualState(Color color, string title, string subtitle, bool tintSprite = true)
    {
        squareColor = color;
        labelTitle = title;
        labelSubtitle = subtitle;
        tintSpriteWithSquareColor = tintSprite;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = tintSpriteWithSquareColor ? squareColor : Color.white;
    }

    private void EnsureVisuals()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            if (spriteRenderer.sprite == null)
                spriteRenderer.sprite = GetOrCreateFallbackSquareSprite();

            spriteRenderer.color = tintSpriteWithSquareColor ? squareColor : Color.white;
            spriteRenderer.sortingOrder = 12 + Mathf.RoundToInt(-transform.position.y * 3f);
        }
    }

    private void LateUpdate()
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = 12 + Mathf.RoundToInt(-transform.position.y * 3f);
    }

    private static Sprite GetOrCreateFallbackSquareSprite()
    {
        if (fallbackSquareSprite != null)
            return fallbackSquareSprite;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x < 3 || x >= size - 3 || y < 3 || y >= size - 3;
                pixels[y * size + x] = border
                    ? Color.white
                    : new Color(0.92f, 0.92f, 0.92f, 0.95f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        fallbackSquareSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
        return fallbackSquareSprite;
    }

    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        if (CommsTowerCMEEvent.IsMinigameActive || MarsSpaceshipStation2D.IsMinigameActive || string.IsNullOrEmpty(labelTitle)) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = transform.position + new Vector3(0f, labelHeightOffset, 0f);
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        if (screenPos.z <= 0f) return;

        // Solo dibujar etiquetas que estén dentro o cerca de la vista de cámara
        if (screenPos.x < -120f || screenPos.x > Screen.width + 120f ||
            screenPos.y < -80f || screenPos.y > Screen.height + 80f)
            return;

        EnsureStyles();

        float guiX = screenPos.x;
        float guiY = Screen.height - screenPos.y;

        bool hasSubtitle = !string.IsNullOrEmpty(labelSubtitle);
        float boxWidth = Mathf.Max(165f, Mathf.Max(labelTitle.Length * 7.8f, hasSubtitle ? labelSubtitle.Length * 6.8f : 0f) + 22f);
        float boxHeight = hasSubtitle ? 42f : 26f;

        Rect boxRect = new Rect(guiX - boxWidth * 0.5f, guiY - boxHeight * 0.5f, boxWidth, boxHeight);

        Color prevColor = GUI.color;
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.86f);
        GUI.DrawTexture(boxRect, labelBgTex);

        // Borde superior del color del cuadrado
        GUI.color = new Color(squareColor.r, squareColor.g, squareColor.b, 0.95f);
        GUI.DrawTexture(new Rect(boxRect.x, boxRect.y, boxRect.width, 3f), labelBgTex);
        GUI.color = prevColor;

        titleStyle.normal.textColor = labelTextColor;
        GUI.Label(new Rect(boxRect.x + 4f, boxRect.y + 4f, boxRect.width - 8f, 20f), labelTitle, titleStyle);

        if (hasSubtitle)
        {
            GUI.Label(new Rect(boxRect.x + 4f, boxRect.y + 21f, boxRect.width - 8f, 18f), labelSubtitle, subtitleStyle);
        }
    }

    private void EnsureStyles()
    {
        if (labelBgTex == null)
        {
            labelBgTex = new Texture2D(1, 1);
            labelBgTex.SetPixel(0, 0, Color.white);
            labelBgTex.Apply();
        }

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (subtitleStyle == null)
        {
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.85f, 0.92f, 1f, 0.95f) }
            };
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = squareColor;
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}

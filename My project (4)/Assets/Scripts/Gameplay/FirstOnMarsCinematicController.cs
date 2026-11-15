using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Plays the bilingual launch cinematic, then opens the Mars gameplay scene.</summary>
[DisallowMultipleComponent]
public sealed class FirstOnMarsCinematicController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip spanishClip;
    [SerializeField] private VideoClip englishClip;
    [SerializeField] private string gameplayScene = "Mars_Natural_Landscape_01";

    private Button spanishButton;
    private Button englishButton;
    private Image spanishButtonImage;
    private Image englishButtonImage;
    private Text statusText;
    private bool isLoadingGameplay;

    private static readonly Color PanelColor = new(0.025f, 0.055f, 0.075f, 0.78f);
    private static readonly Color SelectedColor = new(0.67f, 0.30f, 0.17f, 0.98f);
    private static readonly Color IdleColor = new(0.12f, 0.19f, 0.22f, 0.94f);

    private void Start()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogError("No se encontró el reproductor de la cinemática.", this);
            LoadGameplay();
            return;
        }

        EnsureEventSystem();
        BuildLanguageOverlay();

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCamera = Camera.main;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.loopPointReached += OnVideoEnded;
        videoPlayer.errorReceived += OnVideoError;

        // Start in Spanish; the language controls can switch to the English clip.
        PlaySpanish();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            LoadGameplay();
    }

    private void OnDestroy()
    {
        if (videoPlayer == null) return;
        videoPlayer.loopPointReached -= OnVideoEnded;
        videoPlayer.errorReceived -= OnVideoError;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var eventSystem = new GameObject("Cinematic Event System", typeof(EventSystem));
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private void BuildLanguageOverlay()
    {
        var canvasObject = new GameObject("Cinematic Language Controls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var panelObject = new GameObject("Language and Skip", typeof(RectTransform), typeof(Image));
        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-28f, -28f);
        panelRect.sizeDelta = new Vector2(500f, 84f);
        panelObject.GetComponent<Image>().color = PanelColor;

        spanishButton = CreateButton(panelRect, font, "ESPAÑOL", new Vector2(-172f, -42f), 148f, PlaySpanish, out spanishButtonImage);
        englishButton = CreateButton(panelRect, font, "ENGLISH", new Vector2(-18f, -42f), 148f, PlayEnglish, out englishButtonImage);
        CreateButton(panelRect, font, "OMITIR / SKIP", new Vector2(165f, -42f), 168f, LoadGameplay, out _);

        var statusObject = new GameObject("Cinematic Status", typeof(RectTransform), typeof(Text));
        var statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.SetParent(canvas.transform, false);
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 28f);
        statusRect.sizeDelta = new Vector2(1300f, 54f);
        statusText = statusObject.GetComponent<Text>();
        statusText.font = font;
        statusText.fontSize = 28;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.white;
        statusText.raycastTarget = false;
    }

    private Button CreateButton(Transform parent, Font font, string label, Vector2 position, float width, UnityEngine.Events.UnityAction action, out Image image)
    {
        var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(width, 60f);

        image = buttonObject.GetComponent<Image>();
        image.color = IdleColor;
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = label;
        text.fontSize = 21;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.96f, 0.97f, 0.95f, 1f);
        text.raycastTarget = false;
        return button;
    }

    private void PlaySpanish()
    {
        SetSelectedLanguage(true);
        Play(spanishClip);
    }

    private void PlayEnglish()
    {
        SetSelectedLanguage(false);
        Play(englishClip);
    }

    private void Play(VideoClip clip)
    {
        CancelInvoke(nameof(LoadGameplay));
        if (clip == null)
        {
            ShowStatus("No se encontró esta versión de la cinemática.");
            return;
        }

        videoPlayer.Stop();
        videoPlayer.clip = clip;
        ShowStatus(string.Empty);
        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer player)
    {
        player.prepareCompleted -= OnPrepared;
        player.Play();
    }

    private void OnVideoEnded(VideoPlayer player) => LoadGameplay();

    private void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogWarning("No se pudo reproducir la cinemática: " + message, this);
        ShowStatus("La cinemática no se pudo reproducir. Abriendo Marte…");
        Invoke(nameof(LoadGameplay), 2f);
    }

    private void SetSelectedLanguage(bool spanish)
    {
        if (spanishButtonImage != null) spanishButtonImage.color = spanish ? SelectedColor : IdleColor;
        if (englishButtonImage != null) englishButtonImage.color = spanish ? IdleColor : SelectedColor;
    }

    private void ShowStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private void LoadGameplay()
    {
        if (isLoadingGameplay) return;
        if (videoPlayer != null) videoPlayer.Stop();
        if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
        {
            ShowStatus($"No se encuentra la escena {gameplayScene} en Build Settings.");
            Debug.LogError("La escena de Marte no está disponible en Build Settings.", this);
            return;
        }

        isLoadingGameplay = true;
        SceneManager.LoadScene(gameplayScene, LoadSceneMode.Single);
    }
}

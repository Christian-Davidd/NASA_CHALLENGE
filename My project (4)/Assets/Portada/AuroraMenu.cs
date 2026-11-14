using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class AuroraMenu : MonoBehaviour
{
    const int SignalCount = 5;
    const float MissionTime = 32;
    static readonly Color Ink = new(0.025f, 0.06f, 0.10f);
    static readonly Color White = new(0.92f, 0.96f, 0.97f);
    static readonly Color Cyan = new(1f, 0.68f, 0.45f);
    static readonly Color Fog = new(0.64f, 0.76f, 0.82f);
    static readonly Color Line = new(0.33f, 0.72f, 0.79f, 0.32f);
    static readonly Color Panel = new(0.036f, 0.10f, 0.15f, 0.97f);

    [SerializeField] string gameplayScene = "";
    [SerializeField] int missionStage;
    enum Page { Home, Options, Help, Credits, Confirm, Flight, Pause, Result, Loading }
    Page page;
    RectTransform root, screen, hero, orbit, pilot;
    RawImage heroGlow;
    RawImage pilotImage;
    CanvasGroup entrance;
    Font font;
    Texture field, halo, crew, crewBack, coverCrew, nasa;
    AudioSource audioSource;
    AudioClip click, signal;
    Text status, volumeLabel;
    Button firstButton;
    Slider volume;
    Toggle motion;
    float elapsed, remaining, movement;
    float hazardCooldown;
    Vector2 flightVelocity;
    bool animate, won;
    int collected;
    readonly List<RectTransform> beacons = new();
    readonly List<Graphic> glints = new();
    readonly List<Selectable> controls = new();
    readonly Vector2[] coordinates = {
        new(360, 323), new(783, 165), new(1247, 277), new(1144, 653), new(538, 679)
    };
    readonly Vector2[] relayCoordinates = {
        new(271, 273), new(693, 214), new(1301, 250), new(1316, 670), new(416, 668)
    };
    readonly Vector2[] hazards = { new(516, 404), new(897, 343), new(1085, 580) };
    Vector2 BeaconPosition(int index) => (missionStage == 2 ? relayCoordinates : coordinates)[index] + new Vector2(0, 34);

    void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        field = Resources.Load<Texture>("Aurora/DeepField");
        halo = Resources.Load<Texture>("Aurora/Halo");
        crew = Resources.Load<Texture>("Aurora/Crew");
        crewBack = Resources.Load<Texture>("Aurora/CrewBack");
        coverCrew = Resources.Load<Texture>("Aurora/CoverCrew");
        nasa = Resources.Load<Texture>("Aurora/NASA");
        click = Resources.Load<AudioClip>("Aurora/Click");
        signal = Resources.Load<AudioClip>("Aurora/Signal");
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = PlayerPrefs.GetFloat("Aurora.Volume", .65f);
        animate = PlayerPrefs.GetInt("Aurora.Motion", 1) == 1;

        if (EventSystem.current == null)
            new GameObject("Aurora EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvas = GetComponentInParent<Canvas>();
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var surface = Rect((RectTransform)transform, "Interfaz First on Mars", 0, 0, 0, 0);
        surface.anchorMin = Vector2.zero;
        surface.anchorMax = Vector2.one;
        surface.offsetMin = surface.offsetMax = Vector2.zero;
        var surround = Art(surface, "Horizonte de Marte", field, Color.white, 0, 0, 1600, 900);
        surround.rectTransform.pivot = new Vector2(.5f, .5f);
        var fit = surround.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 1600f / 900;
        root = Rect(surface, "Composición 16:9", 0, 0, 1600, 900);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
        root.anchoredPosition = Vector2.zero;
        if (missionStage == 0)
            Show(Page.Home);
        else
            StartDemo();
    }

    void Show(Page next)
    {
        if (screen)
        {
            screen.gameObject.SetActive(false);
            Destroy(screen.gameObject);
        }
        page = next;
        elapsed = 0;
        hero = null;
        glints.Clear();
        controls.Clear();
        firstButton = null;
        screen = Rect(root, next.ToString(), 0, 0, 1600, 900);
        entrance = screen.gameObject.AddComponent<CanvasGroup>();
        entrance.alpha = animate && next == Page.Home ? 0 : 1;
        switch (next)
        {
            case Page.Home: DrawHome(); break;
            case Page.Options: DrawDialog("Opciones", "Ajustes de la interfaz"); DrawOptions(); break;
            case Page.Help: DrawHelp(); break;
            case Page.Credits: DrawCredits(); break;
            case Page.Confirm: DrawConfirm(); break;
            case Page.Flight: DrawFlight(); break;
            case Page.Pause: DrawPause(); break;
            case Page.Result: DrawResult(); break;
            case Page.Loading: DrawDialog("Despegando…", "Cargando la misión"); break;
        }
        EventSystem.current.SetSelectedGameObject(firstButton ? firstButton.gameObject : null);
    }

    void DrawHome()
    {
        Bar(screen, Cyan, 111, 86, 23, 4);
        Label(screen, "CENTRO DE OPERACIONES", 147, 72, 480, 32, 19, Cyan, true);
        Bar(screen, Line, 111, 122, 1378, 1);
        if (nasa)
            Art(screen, "Insignia NASA", nasa, Color.white, 1374, 31, 94, 80);
        Bar(screen, Line, 111, 797, 1378, 1);
        Label(screen, "Tu gran salto empieza aquí.", 111, 180, 680, 39, 22, Fog, true);
        Label(screen, "First on", 102, 222, 765, 115, 110, White, true);
        Label(screen, "Mars", 102, 326, 765, 122, 116, Cyan, true);
        Label(screen, "Entrena hoy, explora mañana,\npara el beneficio de todos.",
            111, 452, 645, 66, 25, White);
        Action("Despegar", 111, 552, 407, StartDemo, true);
        Action("Opciones", 111, 625, 195, () => Show(Page.Options));
        Action("Cómo jugar", 323, 625, 195, () => Show(Page.Help));
        Action("Salir", 111, 698, 195, () => Show(Page.Confirm));
        Action("Créditos", 323, 698, 195, () => Show(Page.Credits));
        Label(screen, "Ratón  /  Tab y Enter", 111, 813, 500, 32, 19, Fog);

        heroGlow = Art(screen, "Luz de fondo", halo, new Color(1, 1, 1, .79f), 838, 140, 662, 662);
        orbit = Rect(screen, "Anillo orbital", 1174, 456, 0, 0);
        Orbit(orbit, Vector2.zero, 266);
        Corner(914, 181, 1, 1);
        Corner(1434, 181, -1, 1);
        Corner(914, 722, 1, -1);
        Corner(1434, 722, -1, -1);
        hero = Art(screen, "Astronauta", coverCrew, Color.white, 1174, 457, 396, 500).rectTransform;
        hero.pivot = new Vector2(.5f, .5f);
        Label(screen, "TRIPULACIÓN DE MARTE", 944, 738, 460, 29, 17, Cyan, true, TextAnchor.MiddleCenter);
        Label(screen, "ÓRBITA DE MARTE", 1120, 813, 367, 32, 19, Fog, true, TextAnchor.MiddleRight);
    }

    void Orbit(RectTransform parent, Vector2 center, float radius)
    {
        const int segments = 72;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2 * Mathf.PI / segments;
            Vector2 spot = center + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var tick = Bar(parent, i % 6 == 0 ? Cyan : Line, spot.x, spot.y, i % 6 == 0 ? 10 : 3, 2);
            tick.localEulerAngles = new Vector3(0, 0, -angle * Mathf.Rad2Deg);
        }
        for (int i = 0; i < 10; i++)
        {
            float x = -249 + (i * 103) % 505;
            float y = -225 + (i * 127) % 470;
            glints.Add(Bar(parent, Cyan, x, y, 4, 4).GetComponent<Graphic>());
        }
    }

    void Corner(float x, float y, int horizontal, int vertical)
    {
        Bar(screen, Line, horizontal > 0 ? x : x - 26, y, 26, 2);
        Bar(screen, Line, x, vertical > 0 ? y : y - 26, 2, 26);
    }

    void DrawDialog(string title, string subtitle)
    {
        Bar(screen, new Color(0, .02f, .04f, .75f), 0, 0, 1600, 900);
        Bar(screen, Line, 451, 185, 698, 530);
        Bar(screen, Panel, 452, 186, 696, 528);
        Bar(screen, Cyan, 488, 221, 38, 4);
        Label(screen, title, 488, 243, 622, 76, 58, White, true);
        Label(screen, subtitle, 491, 323, 620, 38, 23, Fog);
        Bar(screen, Line, 491, 378, 618, 1);
    }

    void DrawOptions()
    {
        volumeLabel = Label(screen, "", 491, 395, 540, 36, 24, White);
        volume = Rect(screen, "Volumen", 491, 444, 398, 31).gameObject.AddComponent<Slider>();
        Bar(volume.transform as RectTransform, Line, 0, 13, 398, 5);
        var track = Rect(volume.transform as RectTransform, "Recorrido", 0, 0, 377, 31);
        track.anchorMax = Vector2.one;
        track.offsetMin = new Vector2(11, 0);
        track.offsetMax = new Vector2(-11, 0);
        var handle = Bar(track, Cyan, 0, 0, 22, 31);
        handle.pivot = new Vector2(.5f, .5f);
        handle.sizeDelta = new Vector2(22, 0);
        handle.anchoredPosition = Vector2.zero;
        volume.handleRect = handle;
        volume.targetGraphic = handle.GetComponent<Graphic>();
        handle.GetComponent<Graphic>().raycastTarget = true;
        var inputArea = Bar((RectTransform)volume.transform, Color.clear, 0, 0, 398, 31);
        inputArea.SetAsFirstSibling();
        inputArea.GetComponent<Graphic>().raycastTarget = true;
        controls.Add(volume);
        volume.value = audioSource.volume;
        volumeLabel.text = $"Volumen de efectos    {Mathf.RoundToInt(volume.value * 100)}%";
        volume.onValueChanged.AddListener(value =>
        {
            audioSource.volume = value;
            volumeLabel.text = $"Volumen de efectos    {Mathf.RoundToInt(value * 100)}%";
            PlayerPrefs.SetFloat("Aurora.Volume", value);
        });

        Action("Probar sonido", 913, 427, 196, () => Play(signal), quiet: true);

        motion = Rect(screen, "Animación", 491, 510, 608, 43).gameObject.AddComponent<Toggle>();
        var box = Bar((RectTransform)motion.transform, Line, 0, 7, 29, 29);
        motion.targetGraphic = box.GetComponent<Graphic>();
        var toggleArea = Bar((RectTransform)motion.transform, Color.clear, 0, 0, 608, 43);
        toggleArea.SetAsFirstSibling();
        toggleArea.GetComponent<Graphic>().raycastTarget = true;
        controls.Add(motion);
        motion.graphic = Bar(box, Cyan, 5, 5, 19, 19).GetComponent<Graphic>();
        motion.isOn = animate;
        motion.onValueChanged.AddListener(value =>
        {
            animate = value;
            PlayerPrefs.SetInt("Aurora.Motion", value ? 1 : 0);
        });
        Label((RectTransform)motion.transform, "Animar la portada", 47, 0, 350, 43, 24, White);

        var back = Action("Volver", 805, 608, 304, ReturnHome, true);
        Link(volume, motion, back);
        firstButton = back;
    }

    void DrawCredits()
    {
        DrawDialog("Créditos", "First on Mars");
        Label(screen, "Desarrollo", 491, 410, 270, 38, 24, Cyan, true);
        Label(screen, "Deivis Quispe\nChristian Almache\nDavid Pilatasig\nEstefania Narváez\nJosue Pomasqui",
            755, 413, 354, 143, 24, White, alignment: TextAnchor.UpperLeft);
        Label(screen, "Equipo", 491, 558, 270, 38, 24, Cyan, true);
        Label(screen, "BAZINGA", 755, 558, 354, 38, 24, White);
        Action("Volver", 491, 608, 618, ReturnHome, true);
    }

    void DrawHelp()
    {
        DrawDialog("Cómo jugar", "Recoge cinco señales por misión antes de agotar el oxígeno.");
        Label(screen, "WASD o flechas", 491, 410, 290, 38, 24, Cyan, true);
        Label(screen, "Mover al astronauta", 780, 410, 310, 38, 24, White);
        Label(screen, "Balizas luminosas", 491, 465, 285, 38, 24, Cyan, true);
        Label(screen, "Recógelas. Evita las rojas.", 780, 465, 329, 38, 24, White);
        Label(screen, "Esc", 491, 520, 280, 38, 24, Cyan, true);
        Label(screen, "Pausar la misión", 780, 520, 310, 38, 24, White);
        Action("Volver", 491, 608, 618, ReturnHome, true);
    }

    void DrawConfirm()
    {
        DrawDialog("¿Salir del juego?", "Puedes volver a la portada o cerrar la aplicación.");
        Action("Volver", 491, 608, 297, ReturnHome, true);
        Action("Salir", 807, 608, 302, () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    void BeginMission()
    {
        if (missionStage == 0 && !string.IsNullOrWhiteSpace(gameplayScene) && Application.CanStreamedLevelBeLoaded(gameplayScene))
        {
            Show(Page.Loading);
            SceneManager.LoadSceneAsync(gameplayScene);
            return;
        }
        remaining = missionStage == 2 ? 26 : MissionTime;
        hazardCooldown = 0;
        collected = 0;
        movement = 0;
        Show(Page.Flight);
    }

    void DrawFlight()
    {
        Bar(screen, Panel, 91, 78, 1418, 72);
        string title = missionStage == 2 ? "RELEVO EN MARTE" : "ENTRENAMIENTO ORBITAL";
        Label(screen, title, 116, 94, 500, 39, 22, White, true);
        status = Label(screen, "", 657, 94, 557, 39, 24, Cyan, true, TextAnchor.MiddleRight);
        Action("Pausar", 1267, 84, 224, () => Show(Page.Pause));
        string instruction = missionStage == 2
            ? "Evita las zonas rojas: cada contacto consume 5 s de oxígeno."
            : "WASD  /  Flechas para moverte       Esc para pausar";
        Label(screen, instruction, 108, 800, 1110, 36, 22, White);
        if (missionStage == 2)
        {
            foreach (var hazard in hazards)
            {
                Art(screen, "Zona de riesgo", halo, new Color(1, .16f, .15f), hazard.x - 86, hazard.y - 86, 172, 172);
                Label(screen, "!", hazard.x - 20, hazard.y - 20, 40, 40, 34, new Color(1, .43f, .42f),
                    true, TextAnchor.MiddleCenter);
            }
        }
        var targets = Rect(screen, "Balizas", 0, 0, 1600, 900);
        beacons.Clear();
        for (int i = 0; i < SignalCount; i++)
        {
            Vector2 position = BeaconPosition(i);
            var beacon = Rect(targets, $"Baliza {i + 1}", position.x - 53, position.y - 53, 106, 106);
            Art(beacon, "Resplandor", halo, Color.white, 0, 0, 106, 106);
            var core = Bar(beacon, Cyan, 44, 44, 18, 18);
            core.localEulerAngles = new Vector3(0, 0, 45);
            beacons.Add(beacon);
            beacon.gameObject.SetActive((recoveredMask & (1 << i)) == 0);
        }
        pilotImage = Art(screen, "Astronauta", crew, Color.white, flightPosition.x - 27,
            flightPosition.y - 58, 55, 116);
        pilot = pilotImage.rectTransform;
        UpdateStatus();
    }

    Vector2 flightPosition = new(800, 510);
    int recoveredMask;
    void StartDemo()
    {
        flightPosition = new Vector2(800, 510);
        flightVelocity = Vector2.zero;
        recoveredMask = 0;
        BeginMission();
    }

    void DrawPause()
    {
        DrawDialog("Misión en pausa", "La cuenta de oxígeno está detenida.");
        Action("Continuar", 491, 420, 618, () => Show(Page.Flight), true);
        Action("Reiniciar", 491, 495, 618, StartDemo);
        Action("Volver a la portada", 491, 570, 618, ReturnHome);
    }

    void DrawResult()
    {
        DrawDialog(won ? "Misión cumplida" : "Oxígeno agotado", won && missionStage == 2
            ? "Completaste el entrenamiento y el relevo en Marte."
            : won ? "Las cinco señales volvieron a la estación." : $"Recuperaste {collected} de {SignalCount} señales.");
        if (won && missionStage == 1 && Application.CanStreamedLevelBeLoaded("RelevoMarte"))
        {
            Action("Siguiente misión", 491, 420, 618, () => SceneManager.LoadScene("RelevoMarte"), true);
            Action("Repetir entrenamiento", 491, 495, 618, StartDemo);
            Action("Volver a la portada", 491, 570, 618, ReturnHome);
            return;
        }
        Action("Jugar de nuevo", 491, 440, 618, StartDemo, true);
        Action("Volver a la portada", 491, 518, 618, ReturnHome);
    }

    void ReturnHome()
    {
        PlayerPrefs.Save();
        if (missionStage > 0 && Application.CanStreamedLevelBeLoaded("Portada"))
            SceneManager.LoadScene("Portada");
        else
            Show(Page.Home);
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        var keys = Keyboard.current;
        if (keys != null && keys.tabKey.wasPressedThisFrame && controls.Count > 0)
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            int index = controls.FindIndex(control => control.gameObject == selected);
            int step = keys.shiftKey.isPressed ? -1 : 1;
            EventSystem.current.SetSelectedGameObject(controls[(index + step + controls.Count) % controls.Count].gameObject);
        }
        if (keys != null && keys.escapeKey.wasPressedThisFrame)
        {
            switch (page)
            {
                case Page.Home: Show(Page.Confirm); break;
                case Page.Flight: Show(Page.Pause); break;
                case Page.Pause: Show(Page.Flight); break;
                case Page.Options:
                case Page.Help:
                case Page.Credits:
                case Page.Confirm:
                case Page.Result: ReturnHome(); break;
            }
            return;
        }
        if (page == Page.Home && animate)
        {
            entrance.alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed * 2.4f));
            hero.anchoredPosition = new Vector2(1174 + 12 * Mathf.Sin(elapsed * .83f),
                -457 + 20 * Mathf.Sin(elapsed * 1.16f));
            hero.localEulerAngles = new Vector3(0, 0, 4 * Mathf.Sin(elapsed * .93f));
            orbit.localEulerAngles = new Vector3(0, 0, -elapsed * 4);
            heroGlow.color = new Color(1, 1, 1, .75f + .14f * Mathf.Sin(elapsed * 1.16f));
            for (int i = 0; i < glints.Count; i++)
                glints[i].color = new Color(Cyan.r, Cyan.g, Cyan.b, .52f + .39f * Mathf.Sin(elapsed * 2.3f + i * 2));
        }
        if (page == Page.Flight)
            Fly(keys);
    }

    void Fly(Keyboard keys)
    {
        float deltaTime = Time.unscaledDeltaTime;
        remaining = Mathf.Max(0, remaining - deltaTime);
        if (remaining <= 0)
        {
            won = false;
            Show(Page.Result);
            return;
        }
        Vector2 direction = Vector2.zero;
        if (keys != null)
        {
            direction.x = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0)
                - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
            direction.y = (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0)
                - (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0);
        }
        flightVelocity = Vector2.MoveTowards(flightVelocity, direction.normalized * 260, 1750 * deltaTime);
        flightPosition += flightVelocity * deltaTime;
        flightPosition.x = Mathf.Clamp(flightPosition.x, 112, 1488);
        flightPosition.y = Mathf.Clamp(flightPosition.y, 209, 747);
        if (missionStage == 2)
        {
            hazardCooldown = Mathf.Max(0, hazardCooldown - deltaTime);
            if (hazardCooldown == 0)
            {
                foreach (var hazard in hazards)
                {
                    if (Vector2.Distance(flightPosition, hazard) >= 58)
                        continue;
                    remaining = Mathf.Max(0, remaining - 5);
                    hazardCooldown = 1.5f;
                    Play(click);
                    break;
                }
            }
        }
        float bob = 0;
        if (animate && direction != Vector2.zero)
        {
            movement += deltaTime;
            bob = 3 * Mathf.Sin(movement * 19);
        }
        pilot.anchoredPosition = new Vector2(flightPosition.x - 27, -(flightPosition.y - 58) + bob);
        if (direction.y != 0)
            pilotImage.texture = direction.y < 0 ? crewBack : crew;

        for (int i = 0; i < SignalCount; i++)
        {
            if ((recoveredMask & (1 << i)) != 0)
                continue;
            var beacon = beacons[i];
            beacon.localScale = Vector3.one * (animate ? 1 + .095f * Mathf.Sin(elapsed * 4 + i) : 1);
            if (Vector2.Distance(flightPosition, BeaconPosition(i)) > 52)
                continue;
            recoveredMask |= 1 << i;
            beacon.gameObject.SetActive(false);
            collected++;
            Play(signal);
        }
        if (collected == SignalCount)
        {
            won = true;
            Show(Page.Result);
            return;
        }
        UpdateStatus();
    }

    void UpdateStatus()
    {
        status.color = remaining <= 8 || hazardCooldown >= 1 ? new Color(1, .56f, .40f) : Cyan;
        status.text = $"Señales  {collected}/{SignalCount}       Oxígeno  {Mathf.CeilToInt(remaining):00} s";
    }

    void Play(AudioClip clip)
    {
        if (clip)
            audioSource.PlayOneShot(clip);
    }

    void OnApplicationQuit() => PlayerPrefs.Save();

    Button Action(string caption, float x, float y, float width, Action apply, bool bright = false, bool quiet = false)
    {
        var box = Bar(screen, bright ? Cyan : Panel, x, y, width, 57);
        var image = box.GetComponent<Image>();
        image.raycastTarget = true;
        var button = box.gameObject.AddComponent<Button>();
        box.name = caption;
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = bright ? new Color(1, .92f, .84f) : Cyan;
        colors.selectedColor = bright ? new Color(1, .97f, .91f) : Cyan;
        colors.pressedColor = new Color(.60f, .79f, .81f);
        colors.colorMultiplier = 1;
        button.colors = colors;
        if (!bright)
            Bar(box, Line, 0, 55, width, 2);
        Label(box, caption, 0, 0, width, 57, 24, bright ? Ink : White, true, TextAnchor.MiddleCenter);
        button.onClick.AddListener(() =>
        {
            if (!quiet)
                Play(click);
            apply();
        });
        if (!firstButton)
            firstButton = button;
        controls.Add(button);
        return button;
    }

    static void Link(params Selectable[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            var nav = items[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = items[(i + items.Length - 1) % items.Length];
            nav.selectOnDown = items[(i + 1) % items.Length];
            items[i].navigation = nav;
        }
    }

    static RectTransform Rect(RectTransform parent, string name, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    static RectTransform Bar(RectTransform parent, Color color, float x, float y, float width, float height)
    {
        var rect = Rect(parent, "Color", x, y, width, height);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    static RawImage Art(RectTransform parent, string name, Texture texture, Color tint,
        float x, float y, float width, float height)
    {
        var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.color = tint;
        image.raycastTarget = false;
        return image;
    }

    Text Label(RectTransform parent, string content, float x, float y, float width, float height,
        int size, Color color, bool bold = false, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var text = Rect(parent, content, x, y, width, height).gameObject.AddComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }
}

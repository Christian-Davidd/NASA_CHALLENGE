using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public static class FirstOnMarsCinematicBuilder
{
    private const string SpanishVideoPath = "Assets/Video/FirstOnMars/FirstOnMars_Spanish.mp4";
    private const string EnglishVideoPath = "Assets/Video/FirstOnMars/FirstOnMars_English.mp4";
    private const string IntroScenePath = "Assets/Scenes/FirstOnMars_Cinematic.unity";
    private const string CoverScenePath = "Assets/Scenes/Portada.unity";

    public static void BuildIntroScene()
    {
        if (!File.Exists(SpanishVideoPath) || !File.Exists(EnglishVideoPath))
            throw new FileNotFoundException("Faltan los dos videos de First on Mars en Assets/Video/FirstOnMars.");

        var spanishClip = AssetDatabase.LoadAssetAtPath<VideoClip>(SpanishVideoPath);
        var englishClip = AssetDatabase.LoadAssetAtPath<VideoClip>(EnglishVideoPath);
        if (spanishClip == null || englishClip == null)
            throw new FileLoadException("Unity no pudo importar los clips de First on Mars.");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("First on Mars Cinematic Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = false;

        var playerObject = new GameObject("First on Mars Intro Player", typeof(VideoPlayer));
        var videoPlayer = playerObject.GetComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCamera = camera;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        var controller = playerObject.AddComponent<FirstOnMarsCinematicController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("videoPlayer").objectReferenceValue = videoPlayer;
        serialized.FindProperty("spanishClip").objectReferenceValue = spanishClip;
        serialized.FindProperty("englishClip").objectReferenceValue = englishClip;
        serialized.FindProperty("coverScene").stringValue = "Portada";
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, IntroScenePath);
        PutIntroBeforeCoverInBuildSettings();
        Debug.Log($"Cinemática creada: ES {spanishClip.frameCount} fotogramas; EN {englishClip.frameCount} fotogramas.");
    }

    private static void PutIntroBeforeCoverInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene => scene.path == IntroScenePath);
        var coverIndex = scenes.FindIndex(scene => scene.path == CoverScenePath);
        var intro = new EditorBuildSettingsScene(IntroScenePath, true);
        scenes.Insert(coverIndex >= 0 ? coverIndex : 0, intro);
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

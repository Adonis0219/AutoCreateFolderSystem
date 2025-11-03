using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ToolbarEditorTest;

[InitializeOnLoad]
public class CustomToolbarButton
{
    static CustomToolbarButton()
    {
        ToolbarEditor.RightToolbarGUI.Add(OnToolbarGUI);
    }

    static void OnToolbarGUI()
    {
        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent("▷현재씬")))
        {
            EditorSceneManager.playModeStartScene = null;
            UnityEditor.EditorApplication.isPlaying = true;
        }

        if (GUILayout.Button(new GUIContent("▶메인씬")))
        {
            // TODO: 빌드 세팅에서 첫번째 씬을 불러오도록 변경
            var pathOfMainMenuScene = "Assets/1. Scenes/1. MainScene.unity"; // Main Menu Scene의 경로를 정확하게 입력해주세요.
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathOfMainMenuScene);
            EditorSceneManager.playModeStartScene = sceneAsset;
            UnityEditor.EditorApplication.isPlaying = true;
        }
    }
}
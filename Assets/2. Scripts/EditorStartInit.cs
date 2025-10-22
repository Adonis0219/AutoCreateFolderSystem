using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ToolbarEditorTest;

[InitializeOnLoad]
public class EditorStartInit
{
    static EditorStartInit()
    {
        // 툴바 버튼 추가
        ToolbarEditor.RightToolbarGUI.Add(OnToolbarGUI);
    }

    // 툴바에 버튼을 추가하는 메서드
    static void OnToolbarGUI()
    {
        // 오른쪽 끝으로 밀어내기
        GUILayout.FlexibleSpace();

        // new GUIContent(string name) : 버튼 생성 메서드
        if (GUILayout.Button(new GUIContent("▷현재씬")))
        {
            // 현재 열려있는 씬을 재생 모드에서 시작
            EditorSceneManager.playModeStartScene = null;
            // 재생 모드 시작
            UnityEditor.EditorApplication.isPlaying = true;
        }

        if (GUILayout.Button(new GUIContent("▶메인씬")))
        {
            // TODO: 빌드 세팅에서 첫번째 씬을 불러오도록 변경
            // 특정 씬을 재생 모드에서 시작
            var pathOfMainMenuScene = "Assets/1. Scenes/1. MainScene.unity"; // Main Menu Scene의 경로를 정확하게 입력해주세요.
            // 씬 에셋 로드
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathOfMainMenuScene);
            // 재생 모드 시작 씬으로 설정
            EditorSceneManager.playModeStartScene = sceneAsset;
            // 재생 모드 시작
            UnityEditor.EditorApplication.isPlaying = true;
        }
    }
}
using UnityEditor;
using UnityEngine;
using ToolbarEditorTest;

// 해당 클래스의 정적 생성자가 다음 두 가지 시점에 자동으로 호출됨
// 1. Unity Editor가 시작될 때
// 2. 스크립트가 재컴파일 될 때
[InitializeOnLoad]
public class MyToolbarButton
{
    static MyToolbarButton()
    {
        ToolbarEditor.RightToolbarGUI.Add(OnToolbarGUI);
    }

    static void OnToolbarGUI()
    {
        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent("Hello")))
        {
            Debug.Log("My Button Clicked!");
        }
    }
}

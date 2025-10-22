using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Based on 'unity toolbar extender' project by marijnz
// Original: https://github.com/marijnz/unity-toolbar-extender
// Modified for use in this project
namespace ToolbarEditorTest
{
    /// <summary>
    /// Unity 에디터 상단의 Toolbar 영역을 확장할 수 있도록 도와주는 클래스입니다.
    /// Unity 내부 Toolbar의 OnGUI를 Reflection을 통해 접근하여,
    /// 왼쪽과 오른쪽에 원하는 GUI 요소를 추가할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 자세한 구문 분석은
    /// <see href="https://www.notion.so/Toolbar-Extender-293b1f49203e80d8a8e8f0fbeccea6c6?source=copy_link">3번 항목</see>을 참고하세요.
    /// </remarks>
    [InitializeOnLoad]
    public static class ToolbarEditor
    {
        static int m_toolCount;                   // 에디터 툴 버튼 개수 (이동, 회전, 스케일 등)
        static GUIStyle m_commandStyle = null;    // Toolbar의 스타일 정보 (CommandLeft 등)

        // 외부에서 툴바에 그릴 함수를 등록할 수 있는 리스트
        public static readonly List<Action> LeftToolbarGUI = new List<Action>();
        public static readonly List<Action> RightToolbarGUI = new List<Action>();

        /// <summary>
        /// Unity 에디터 로드 시 자동으로 실행되는 정적 생성자입니다.
        /// Toolbar 내부 구조를 Reflection으로 가져와 각종 위치 계산에 필요한 값을 추출합니다.
        /// </summary>
        static ToolbarEditor()
        {
            // Unity 내부 Toolbar 타입 가져오기
            Type toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");

#if UNITY_2019_1_OR_NEWER
            string fieldName = "k_ToolCount";   // Unity 2019 이후 버전에서는 정수형
#else
            string fieldName = "s_ShownToolIcons"; // 이전 버전은 배열
#endif

            // 내부 필드 접근 (툴 버튼 개수)
            FieldInfo toolIcons = toolbarType.GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            // Unity 버전에 따라 필드 형태가 다름 → 적절히 파싱
#if UNITY_2019_3_OR_NEWER
            m_toolCount = toolIcons != null ? ((int)toolIcons.GetValue(null)) : 8;
#elif UNITY_2019_1_OR_NEWER
            m_toolCount = toolIcons != null ? ((int) toolIcons.GetValue(null)) : 7;
#elif UNITY_2018_1_OR_NEWER
            m_toolCount = toolIcons != null ? ((Array) toolIcons.GetValue(null)).Length : 6;
#else
            m_toolCount = toolIcons != null ? ((Array) toolIcons.GetValue(null)).Length : 5;
#endif

            // ToolbarCallback을 통해 OnGUI 시점을 훅킹하여 커스텀 GUI를 그릴 수 있게 설정
            ToolbarCallback.OnToolbarGUI = OnGUI;
            ToolbarCallback.OnToolbarGUILeft = GUILeft;
            ToolbarCallback.OnToolbarGUIRight = GUIRight;
        }

        // Toolbar 내에서 사용하는 레이아웃 상수들
#if UNITY_2019_3_OR_NEWER
        public const float space = 8;
#else
        public const float space = 10;
#endif
        public const float largeSpace = 20;
        public const float buttonWidth = 32;
        public const float dropdownWidth = 80;
#if UNITY_2019_1_OR_NEWER
        public const float playPauseStopWidth = 140;
#else
        public const float playPauseStopWidth = 100;
#endif

        /// <summary>
        /// 실제 Toolbar 상에서 커스텀 GUI를 그리는 메인 함수입니다.
        /// </summary>
        static void OnGUI()
        {
            // 초기 GUIStyle 생성
            if (m_commandStyle == null)
            {
                m_commandStyle = new GUIStyle("CommandLeft");
            }

            var screenWidth = EditorGUIUtility.currentViewWidth;

            // Unity 내부 Toolbar 레이아웃 계산과 동일한 방식으로 영역 분할
            float playButtonsPosition = Mathf.RoundToInt((screenWidth - playPauseStopWidth) / 2);

            // 왼쪽 영역 계산
            Rect leftRect = new Rect(0, 0, screenWidth, Screen.height);
            leftRect.xMin += space;
            leftRect.xMin += buttonWidth * m_toolCount; // 기본 툴 버튼 영역
#if UNITY_2019_3_OR_NEWER
            leftRect.xMin += space; // 툴과 Pivot 사이 간격
#else
            leftRect.xMin += largeSpace;
#endif
            leftRect.xMin += 64 * 2; // Pivot 버튼들
            leftRect.xMax = playButtonsPosition; // 중앙의 재생버튼 왼쪽까지만 사용

            // 오른쪽 영역 계산
            Rect rightRect = new Rect(0, 0, screenWidth, Screen.height);
            rightRect.xMin = playButtonsPosition;
            rightRect.xMin += m_commandStyle.fixedWidth * 3; // 재생 버튼 3개 (Play, Pause, Stop)
            rightRect.xMax = screenWidth;
            rightRect.xMax -= space;
            rightRect.xMax -= dropdownWidth; // Layout
            rightRect.xMax -= space;
            rightRect.xMax -= dropdownWidth; // Layers
#if UNITY_2019_3_OR_NEWER
            rightRect.xMax -= space;
#else
            rightRect.xMax -= largeSpace;
#endif
            rightRect.xMax -= dropdownWidth; // Account
            rightRect.xMax -= space;
            rightRect.xMax -= buttonWidth; // Cloud
            rightRect.xMax -= space;
            rightRect.xMax -= 78; // Collaborate 영역

            // 양쪽 여백 보정
            leftRect.xMin += space;
            leftRect.xMax -= space;
            rightRect.xMin += space;
            rightRect.xMax -= space;

            // Toolbar 높이 및 위치 설정
#if UNITY_2019_3_OR_NEWER
            leftRect.y = 4;
            leftRect.height = 22;
            rightRect.y = 4;
            rightRect.height = 22;
#else
            leftRect.y = 5;
            leftRect.height = 24;
            rightRect.y = 5;
            rightRect.height = 24;
#endif

            // 왼쪽 커스텀 GUI 영역
            if (leftRect.width > 0)
            {
                GUILayout.BeginArea(leftRect);
                GUILayout.BeginHorizontal();
                foreach (var handler in LeftToolbarGUI)
                {
                    handler(); // 등록된 GUI 함수 실행
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            // 오른쪽 커스텀 GUI 영역
            if (rightRect.width > 0)
            {
                GUILayout.BeginArea(rightRect);
                GUILayout.BeginHorizontal();
                foreach (var handler in RightToolbarGUI)
                {
                    handler();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

        /// <summary>
        /// 왼쪽 Toolbar 영역만 별도로 그릴 때 호출되는 함수
        /// </summary>
        public static void GUILeft()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in LeftToolbarGUI)
            {
                handler();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 오른쪽 Toolbar 영역만 별도로 그릴 때 호출되는 함수
        /// </summary>
        public static void GUIRight()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in RightToolbarGUI)
            {
                handler();
            }
            GUILayout.EndHorizontal();
        }
    }
}

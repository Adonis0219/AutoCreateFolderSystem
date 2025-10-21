using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

// Based on 'unity toolbar extender' project by marijnz
// Original: https://github.com/marijnz/unity-toolbar-extender
// Modified for use in this project
namespace ToolbarEditorTest
{
    /// <summary>
    /// Unity 에디터 상단 Toolbar의 OnGUI 시점을 감지하고
    /// 원하는 콜백 함수를 연결할 수 있도록 해주는 클래스입니다.
    /// 내부적으로 UnityEditor.Toolbar 및 GUIView의 Reflection을 활용합니다.
    /// </summary>
    public static class ToolbarCallback
    {
        // UnityEditor 어셈블리에 접근해 툴바 관련 타입 정보 가져오기
        static Type m_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        // UnityEditor.GUIView 타입 정보 가져오기
        static Type m_guiViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");

#if UNITY_2020_1_OR_NEWER
        // Unity 2020 이상에서는 Toolbar 구조가 변경되어 IWindowBackend을 통해 visualTree 접근
        static Type m_iWindowBackendType = typeof(Editor).Assembly.GetType("UnityEditor.IWindowBackend");
        static PropertyInfo m_windowBackend = m_guiViewType.GetProperty("windowBackend",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        static PropertyInfo m_viewVisualTree = m_iWindowBackendType.GetProperty("visualTree",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#else
        // 이전 버전에서는 GUIView에서 바로 visualTree 접근 가능
        static PropertyInfo m_viewVisualTree = m_guiViewType.GetProperty("visualTree",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif

        // IMGUIContainer의 내부 onGUIHandler 필드 정보
        static FieldInfo m_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // 현재 활성화된 툴바 인스턴스
        static ScriptableObject m_currentToolbar;

        /// <summary>
        /// 외부에서 툴바 OnGUI에 연결할 수 있는 델리게이트
        /// </summary>
        public static Action OnToolbarGUI;
        public static Action OnToolbarGUILeft;
        public static Action OnToolbarGUIRight;

        // 정적 생성자: 클래스가 처음 로드될 때 호출되어 초기화 작업 수행
        // EditorApplication.update 이벤트에 OnUpdate 메서드 등록
        // 제거 후 등록하는 이유 : 중복 등록 방지
        // 이미 OnUpdate가 등록돼있다면 제거 후 등록
        // OnUpdate가 등록돼있지 않다면 -= 연산자는 아무런 영향이 없음
        static ToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        /// <summary>
        /// 에디터가 갱신될 때마다 실행되며,
        /// 현재 Toolbar 객체를 찾고 OnGUI 이벤트를 연결하는 역할을 합니다.
        /// </summary>
        static void OnUpdate()
        {
            // 현재 Toolbar 인스턴스가 없는 경우 새로 검색
            if (m_currentToolbar == null)
            {
                // 모든 Toolbar 인스턴스를 찾아 첫 번째 것을 현재 툴바로 지정
                var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;

                if (m_currentToolbar != null)
                {
#if UNITY_2021_1_OR_NEWER
                    // Unity 2021 이후: Toolbar 내부의 m_Root 필드에서 UIElement 트리를 가져와 콜백 등록
                    var root = m_currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    var rawRoot = root.GetValue(m_currentToolbar);
                    var mRoot = rawRoot as VisualElement;

                    // Toolbar의 왼쪽 / 오른쪽 영역을 찾아 각각 콜백 등록
                    RegisterCallback("ToolbarZoneLeftAlign", OnToolbarGUILeft);
                    RegisterCallback("ToolbarZoneRightAlign", OnToolbarGUIRight);

                    void RegisterCallback(string root, Action cb)
                    {
                        var toolbarZone = mRoot.Q(root);

                        var parent = new VisualElement()
                        {
                            style = {
                                flexGrow = 1,
                                flexDirection = FlexDirection.Row,
                            }
                        };

                        var container = new IMGUIContainer();
                        container.style.flexGrow = 1;
                        container.onGUIHandler += () => {
                            cb?.Invoke(); // 등록된 콜백 실행
                        };

                        parent.Add(container);
                        toolbarZone.Add(parent);
                    }
#else
#if UNITY_2020_1_OR_NEWER
                    // Unity 2020: IWindowBackend을 통해 visualTree 접근
                    var windowBackend = m_windowBackend.GetValue(m_currentToolbar);
                    var visualTree = (VisualElement)m_viewVisualTree.GetValue(windowBackend, null);
#else
                    // Unity 2019 이하: Toolbar 객체에서 직접 visualTree 가져오기
                    var visualTree = (VisualElement)m_viewVisualTree.GetValue(m_currentToolbar, null);
#endif
                    // Toolbar 내부의 첫 번째 자식이 IMGUIContainer임 → 해당 핸들러를 교체하여 OnGUI 연결
                    var container = (IMGUIContainer)visualTree[0];

                    // 기존 핸들러를 제거 후 새 핸들러 등록
                    var handler = (Action)m_imguiContainerOnGui.GetValue(container);
                    handler -= OnGUI;
                    handler += OnGUI;
                    m_imguiContainerOnGui.SetValue(container, handler);
#endif
                }
            }
        }

        /// <summary>
        /// 실제 OnGUI 이벤트 발생 시, 등록된 핸들러를 호출합니다.
        /// </summary>
        static void OnGUI()
        {
            var handler = OnToolbarGUI;
            if (handler != null)
                handler();
        }
    }
}

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ToolbarEditorTest
{
    public static class ToolbarCallback
    {
        // UnityEditor 어셈블리에 접근해 툴바 관련 타입 정보 가져오기
        static Type m_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        // UnityEditor.GUIView 타입 정보 가져오기
        static Type m_guiViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");

        // UnityEditor.IWindowBackend 타입 정보 가져오기
        static Type m_iWindowBackendType = typeof(Editor).Assembly.GetType("UnityEditor.IWindowBackend");

        // BindingFlags(검색 조건) 열거형을 사용해 public, non-public, instance 멤버 모두 접근 가능하도록 설정
        // GUIView의 windowBackend 프로퍼티 정보 가져오기 
        static PropertyInfo m_windowBackend = m_guiViewType.GetProperty("windowBackend",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        // IWindowBackend의 visualTree 프로퍼티 정보 가져오기
        static PropertyInfo m_viewVisualTree = m_iWindowBackendType.GetProperty("visualTree",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // IMGUIContainer의 m_OnGUIHandler 필드 정보 가져오기
        static FieldInfo m_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        // 현재 활성화된 툴바 인스턴스
        static ScriptableObject m_currentToolbar;

        /// <summary>
        /// Toolbar OnGUI 메서드에 대한 콜백
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
        /// 에디터 업데이트 시 호출되는 메서드
        /// </summary>
        /// <remarks>
        static void OnUpdate()
        {
            if (m_currentToolbar == null)
            {
                // 모든 툴바 오브젝트를 찾아 첫 번째 것을 현재 툴바로 설정
                var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
                // 현재 활성화된 툴바가 없으면 첫 번째 툴바를 할당
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;

                if (m_currentToolbar != null)
                {
                    // m_currentToolbar 객체 내부에 숨겨져있는 m_Root 필드에 접근하여 VisualElement 타입으로 캐스팅
                    // m_Root 필드(비공개 인스턴스 필드) 가져오기
                    var root = m_currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    // m_Root 필드의 값 가져오기
                    var rawRoot = root.GetValue(m_currentToolbar);
                    // VisualElement로 캐스팅
                    var mRoot = rawRoot as VisualElement;

                    RegisterCallback("ToolbarZoneLeftAlign", OnToolbarGUILeft);
                    RegisterCallback("ToolbarZoneRightAlign", OnToolbarGUIRight);

                    void RegisterCallback(string root, Action cb)
                    {
                        var toolbarZone = mRoot.Q(root);

                        var parent = new VisualElement()
                        {
                            style =
                            {
                                flexGrow = 1,
                                flexDirection = FlexDirection.Row,
                            }
                        };

                        var container = new IMGUIContainer();
                        container.style.flexGrow = 1;
                        container.onGUIHandler += () => cb?.Invoke();
                        
                        parent.Add(container);
                        toolbarZone.Add(parent);
                    }

                    var windowBackend = m_windowBackend.GetValue(m_currentToolbar);

                    // Get it's visual tree
                    var visualTree = (VisualElement)m_viewVisualTree.GetValue(windowBackend, null);

                    // Get first child which 'happens' to be toolbar IMGUIContainer
                    var container = (IMGUIContainer)visualTree[0];

                    // (Re)attach handler
                    var handler = (Action)m_imguiContainerOnGui.GetValue(container);
                    handler -= OnGUI;
                    handler += OnGUI;
                    m_imguiContainerOnGui.SetValue(container, handler);
                }
            }
        }

        static void OnGUI()
        {
            var handler = OnToolbarGUI;
            if (handler != null) handler();
        }
    }
}
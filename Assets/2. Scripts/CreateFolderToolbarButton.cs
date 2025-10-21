using UnityEditor;
using UnityEngine;
using ToolbarEditorTest;
using System.IO;

// 해당 클래스의 정적 생성자가 다음 두 가지 시점에 자동으로 호출됨
// 1. Unity Editor가 시작될 때
// 2. 스크립트가 재컴파일 될 때
[InitializeOnLoad]
public class CreateFolderToolbarButton
{
    static CreateFolderToolbarButton()
    {
        ToolbarEditor.RightToolbarGUI.Add(OnToolbarGUI);
    }

    // 툴바에 버튼을 추가하는 메서드
    static void OnToolbarGUI()
    {
        // 오른쪽 끝으로 밀어내기
        GUILayout.FlexibleSpace();

        // new GUIContent(string name) : 버튼 생성 메서드
        // GUILayout.Button(GUIContent content) : 버튼이 클릭되었는지 여부 반환
        if (GUILayout.Button(new GUIContent("폴더 생성")))
        {
            CreateFolder();
        }
    }

    static string[] subfolderNames =
    {
        "1. Scenes",
        "2. Scripts",
        "3. Prefabs",
        "4. Materials",
        "5. Animations",
    };

    static string ASSET = "Assets";
    static string branchName = "";

    /// <summary>
    /// Assets 폴더 내에 새로운 폴더를 생성하는 메서드
    /// </summary>
    static void CreateFolder()
    {
        branchName = GitUtility.GetCurrentBranchName();

        string folderName = $"1. {branchName}";

        GameLogger.Log("폴더 생성을 시작합니다." +
            "\nCreating folder...");

        // 1. 상위 폴더(새 폴더) 생성
        CreateNew(folderName);

        // 2. 하위 폴더들 생성
        CreateSubs(folderName);

        // 3. 씬 복제
        DuplicateScenes(folderName);

        // 에셋 데이터베이스 갱신
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Assets 폴더 내에 새로운 폴더를 생성하는 메서드
    /// </summary>
    /// <param name="createFolder">새로 만들 폴더명</param>
    static void CreateNew(string createFolder)
    {
        string folderPath = $"{ASSET}/{createFolder}";

        // 폴더가 이미 존재하는지 확인
        // AssetDatabase : Unity 에디터에서 에셋과 폴더를 관리하는 클래스
        // IsValidFolder(string path) : 해당 경로에 폴더가 존재하는지 여부 반환
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            // 폴더가 존재하지 않으면 새 폴더 생성
            // CreateFolder(string parentFolder, string newFolderName) : 새로운 폴더 생성
            AssetDatabase.CreateFolder(ASSET, createFolder);

            GameLogger.Log($"Assets 폴더에 {createFolder} 폴더가 생성되었습니다." +
                $"\nFolder 'NewFolder' created in Assets.");
        }
        else
        {
            GameLogger.Log($"Assets 폴더에 {createFolder} 이름의 폴더가 이미 존재합니다." +
                $"\nFolder 'NewFolder' already exists in Assets.");
        }
    }

    /// <summary>
    /// 하위 폴더들 생성 메서드
    /// </summary>
    /// <param name="parentFolder">상위 폴더명(방금 새로 만든 폴더)</param>
    static void CreateSubs(string parentFolder)
    {
        string folderPath = $"{ASSET}/{parentFolder}";

        foreach (var subfolder in subfolderNames)
        {
            string subfolderPath = $"{folderPath}/{subfolder}";

            if (AssetDatabase.IsValidFolder(subfolderPath))
            {
                GameLogger.Log($"{parentFolder} 아래에 {subfolder}가 이미 존재합니다." +
                    $"\nSubfolder '{subfolder}' already exists in '{parentFolder}'.");
                continue;
            }

            AssetDatabase.CreateFolder(folderPath, subfolder);

            GameLogger.Log($"{parentFolder} 아래에 {subfolder} 폴더가 생성되었습니다." +
                $"\nSubfolder '{subfolder}' created in '{parentFolder}'.");
        }
    }

    
    static void DuplicateScenes(string targetFolderName)
    {
        // 씬만 찾기 위한 필터 설정
        string filter = $"t:Scene";

        // 원본 씬들이 위치한 폴더 경로
        string targetScenePath = $"Assets/1. Scenes";

        // 해당 경로에서 씬 에셋들의 GUID 배열을 가져옴
        string[] sceneGUIDs = AssetDatabase.FindAssets(filter, new[] { targetScenePath });

        if (sceneGUIDs.Length == 0)
        {
            GameLogger.Log("복제할 씬이 없습니다." +
                "\nNo scenes found to duplicate.");
            return;
        }

        // 찾은 모든 씬에 대해 복제 작업 수행
        foreach (var sceneGUID in sceneGUIDs)
        {
            GameLogger.Log($"씬 Path: {AssetDatabase.GUIDToAssetPath(sceneGUID)}");

            // GUID를 경로로 변환
            string originalScenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
            // 복제된 씬이 저장될 경로 생성
            string sceneFileName = Path.GetFileName(originalScenePath);
            string targetFolderPath = $"{ASSET}/{targetFolderName}/{subfolderNames[0]}/{sceneFileName}";

            if (AssetDatabase.CopyAsset(originalScenePath, targetFolderPath))
            {
                GameLogger.Log($"씬이 성공적으로 복제되었습니다: {originalScenePath} -> {targetFolderPath}" +
                    $"\nScene duplicated successfully: {originalScenePath} -> {targetFolderPath}");

                // ------ 씬 이름 변경 로직 ------
                // 1. 방금 복제된 파일의 경로(targetFolderPath)에서 파일 이름과 확장자 분리
                string originalName = Path.GetFileNameWithoutExtension(targetFolderPath);

                // 2. 브랜치 이름을 접미사로 추가한 새로운 이름 생성
                string newName = $"{originalName}_{branchName}";

                // 3. AssetDatabase.RenameAsset 메서드를 사용하여 파일 이름 변경
                // (성공 시 빈 문자열, 실패 시 에러 메세지 반환)
                string errorMessage = AssetDatabase.RenameAsset(targetFolderPath, newName);

                if (string.IsNullOrEmpty(errorMessage))
                {
                    GameLogger.Log($"씬 이름이 성공적으로 변경되었습니다: {originalName} -> {newName}" +
                        $"\nScene renamed successfully: {originalName} -> {newName}");
                }
                else
                {
                    GameLogger.LogError($"씬 이름 변경에 실패했습니다: {originalName} -> {newName}" +
                        $"\nFailed to rename scene: {originalName} -> {newName}");
                    GameLogger.LogError(errorMessage);
                }
            }
            else
            {
                GameLogger.LogError($"씬 복제에 실패했습니다: {originalScenePath} -> {targetFolderPath}" +
                    $"\nFailed to duplicate scene: {originalScenePath} -> {targetFolderPath}");

                GameLogger.Log(targetFolderPath);
            }
        }
    }
}
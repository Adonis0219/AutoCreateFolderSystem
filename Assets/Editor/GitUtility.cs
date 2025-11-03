// Process 클래스 사용을 위해 추가
using System.Diagnostics;
using System.Text;
using UnityEngine;

/// <summary>
/// 구문 분석 페이지
/// <see href="https://www.notion.so/Toolbar-Extender-293b1f49203e80d8a8e8f0fbeccea6c6?source=copy_link">1번 항목</see>을 참고하세요.
/// </summary>
public static class GitUtility
{
    /// <summary>
    /// 현재 Git 브랜치 이름을 반환합니다.
    /// </summary>
    /// <returns>현재 Git 브랜치명</returns>
    public static string GetCurrentBranchName()
    {
        try
        {
            // Git 명령어를 실행하기 위한 ProcessStartInfo 설정
            ProcessStartInfo startInfo = new ProcessStartInfo("git")
            {
                // Git 명령어 인자 설정
                Arguments = "rev-parse --abbrev-ref HEAD",
                // 표준 출력을 리다이렉션할지 여부
                // 리다이렉션 : 출력 스트림을 다른 곳으로 보내는 것
                RedirectStandardOutput = true,
                // 운영체제 셀을 통해 프로세스를 시작할지 여부 설정
                UseShellExecute = false,
                // 창을 표시하지 않도록 설정
                CreateNoWindow = true,
                // Git 명령어가 실행될 디렉토리 설정 -> 프로젝트 폴더에서 실행
                WorkingDirectory = Application.dataPath,

                // --- 한글 처리를 위한 인코딩 설정 추가 ---
                StandardOutputEncoding = Encoding.UTF8
            };

            using (Process process = Process.Start(startInfo))
            {
                // 명령어 실행 결과(표준 출력) 읽기
                string branchName = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                // Git 저장소가 아니거나 오류가 발생하면 빈 문자열이 반환되는 것을 방지
                if (string.IsNullOrEmpty(branchName))
                {
                    UnityEngine.Debug.LogWarning("Git 브랜치 이름을 가져오지 못했습니다. 'Unknown으로 대체합니다.");
                    return "Unknown";
                }

                // refs/head/main 같은 전체 경로 대신 최종 이름만 사용하도록 처리
                if (branchName.Contains("/"))
                {
                    branchName = branchName.Substring(branchName.LastIndexOf('/') + 1);
                }

                return branchName;
            }       
        }

        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Git 브랜치 이름을 가져오는 중 오류가 발생했습니다: {ex.Message}");
            return "Error";
        }
    }
}

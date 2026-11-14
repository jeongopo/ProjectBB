using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

/// <summary>
/// Data/Excel의 xlsx를 Tools/ExcelDataTool로 변환해 XML과 데이터 코드를 갱신한다.
/// 변환 도구를 터미널에서 따로 실행하지 않아도 되도록 메뉴로 감싼 것이다. dotnet SDK가 필요하다.
/// </summary>
public static class ExcelDataConvertMenu
{
    private const string ToolProjectPath = "Tools/ExcelDataTool";

    [MenuItem("Tools/Data/1. Convert Excel To XML")]
    public static void ConvertExcelToXml()
    {
        string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
        string toolPath = Path.Combine(projectRoot, ToolProjectPath);
        if (!Directory.Exists(toolPath))
        {
            Debug.LogError($"[ExcelData] 변환 도구 폴더를 찾을 수 없습니다: {toolPath}");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{toolPath}\" --nologo",
            WorkingDirectory = projectRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        var output = new StringBuilder();
        var error = new StringBuilder();

        try
        {
            EditorUtility.DisplayProgressBar("Excel 변환", "ExcelDataTool 실행 중...", 0.5f);

            using (var process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (_, e) => AppendLine(output, e.Data);
                process.ErrorDataReceived += (_, e) => AppendLine(error, e.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                ReportResult(process.ExitCode, output.ToString(), error.ToString());
            }
        }
        catch (Win32Exception)
        {
            Debug.LogError("[ExcelData] dotnet 실행에 실패했습니다. .NET SDK가 설치되어 있고 PATH에 등록되어 있는지 확인하세요.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
    }

    private static void AppendLine(StringBuilder builder, string line)
    {
        if (line != null)
        {
            builder.AppendLine(line);
        }
    }

    private static void ReportResult(int exitCode, string output, string error)
    {
        if (exitCode != 0)
        {
            Debug.LogError($"[ExcelData] 변환에 실패했습니다 (exit {exitCode}).\n{error}{output}");
            return;
        }

        if (error.Length > 0)
        {
            Debug.LogWarning($"[ExcelData] 변환은 완료됐지만 경고가 있습니다.\n{error}");
        }

        Debug.Log($"[ExcelData] 변환 완료.\n{output}");
    }
}

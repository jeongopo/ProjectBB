using System.Data;
using ExcelDataTool;

const string EnumDefinesFileName = "DataEnumDefines.xlsx";
const string EnumDefinesCodeFileName = "DataEnumDefines.cs";
const string StructCodeFileName = "DataDrivenDefines.cs";

string repoRoot = FindRepoRoot(Directory.GetCurrentDirectory());
string excelDir = GetArg(args, "--excel") ?? Path.Combine(repoRoot, "Data", "Excel");
string xmlDir = GetArg(args, "--xml") ?? Path.Combine(repoRoot, "Assets", "Resources", "XML");
string codeDir = GetArg(args, "--code") ?? Path.Combine(repoRoot, "Assets", "Scripts", "Data");

try
{
    if (!Directory.Exists(excelDir))
    {
        Console.Error.WriteLine($"엑셀 폴더가 존재하지 않습니다: {excelDir}");
        return 1;
    }

    var xlsxFiles = Directory.GetFiles(excelDir, "*.xlsx", SearchOption.AllDirectories)
        .Concat(Directory.GetFiles(excelDir, "*.xls", SearchOption.AllDirectories))
        .Where(f => !Path.GetFileName(f).StartsWith("~$")) // 엑셀이 파일을 열어둔 동안 생기는 임시 잠금 파일 제외
        .Where(f => !Path.GetFileName(f).Equals(EnumDefinesFileName, StringComparison.OrdinalIgnoreCase))
        .OrderBy(f => f, StringComparer.Ordinal)
        .ToList();

    if (xlsxFiles.Count == 0)
    {
        Console.Error.WriteLine($"엑셀 파일을 찾을 수 없습니다: {excelDir}");
        return 1;
    }

    // Pass 1: 모든 테이블을 미리 로드하고 타입/ID를 수집한다 (다른 테이블 참조 검증을 위해 전체가 먼저 필요).
    var tables = new Dictionary<string, DataTable>(StringComparer.OrdinalIgnoreCase);
    var columnTypesByTable = new Dictionary<string, List<ColumnType>>(StringComparer.OrdinalIgnoreCase);

    foreach (var file in xlsxFiles)
    {
        string tableName = Path.GetFileNameWithoutExtension(file);
        DataSet dataSet;
        try
        {
            dataSet = ExcelHelper.LoadAllSheets(file);
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"[{tableName}] 파일을 읽을 수 없습니다 (다른 프로그램에서 열려 있을 수 있습니다): {file}\n  {ex.Message}");
            return 1;
        }
        if (dataSet.Tables.Count == 0)
        {
            continue;
        }

        DataTable table;
        try
        {
            // 한 파일에 탭이 여러 개면(예: BBString.xlsx의 카테고리별 탭) 전부 하나의 테이블로 합친다.
            table = ExcelHelper.CombineSheets(dataSet);
        }
        catch (InvalidDataException ex)
        {
            Console.Error.WriteLine($"[{tableName}] {ex.Message}");
            return 1;
        }

        tables[tableName] = table;
        try
        {
            columnTypesByTable[tableName] = XmlConverter.ParseColumnTypes(table);
        }
        catch (InvalidDataException ex)
        {
            Console.Error.WriteLine($"[{tableName}] {ex.Message}");
            return 1;
        }
    }

    var idsByTable = ReferenceValidator.CollectIds(tables);

    // Pass 2: id/id[] 컬럼이 실제 존재하는 테이블의 ID를 가리키는지 검증.
    // 참조가 깨진 건 대부분 데이터 쪽 문제라 변환 자체를 막지는 않는다 — 경고만 남기고 그대로 진행.
    var warnings = new List<string>();
    foreach (var (tableName, table) in tables)
    {
        warnings.AddRange(ReferenceValidator.ValidateAndResolveReferences(tableName, table, columnTypesByTable[tableName], idsByTable));
    }

    if (warnings.Count > 0)
    {
        Console.Error.WriteLine($"[경고] 참조 검증에서 {warnings.Count}건의 문제가 발견됐습니다 (변환은 계속 진행합니다):");
        foreach (var warning in warnings)
        {
            Console.Error.WriteLine("  - " + warning);
        }
    }

    // XML과 코드 생성.
    Directory.CreateDirectory(xmlDir);
    Directory.CreateDirectory(codeDir);

    foreach (var (tableName, table) in tables)
    {
        string xmlPath = Path.Combine(xmlDir, tableName + ".xml");
        XmlConverter.SaveDataTableToXml(table, columnTypesByTable[tableName], xmlPath);
        Console.WriteLine($"XML 저장: {xmlPath}");
    }

    string structBody = XmlConverter.GenerateStructFromXml(xmlDir, out var structNames);

    string classHeaderCode = "public class DataStorage\n{\n";
    foreach (var name in structNames)
    {
        classHeaderCode += $"\tpublic Dictionary<string,{name}> {name}Data;\n";
    }
    classHeaderCode += "\tpublic void LoadData()\n\t{\n";
    foreach (var name in structNames)
    {
        classHeaderCode += $"\t\t{name}Data = DataManager.LoadDefineData<{name}>(\"{name}\");\n";
    }
    classHeaderCode += "\t}\n";

    string usingCode = "// This file is auto-generated from XML files.\n"
                    + "using System;\n"
                    + "using System.IO;\n"
                    + "using System.Xml.Serialization;\n"
                    + "using System.Collections.Generic;\n"
                    + "using UnityEngine;\n"
                    + "using DataEnumDefines;\n";

    string fullStructCode = usingCode + classHeaderCode + "\t// classDefine\n" + structBody + "}\n";
    string structFilePath = Path.Combine(codeDir, StructCodeFileName);
    File.WriteAllText(structFilePath, fullStructCode);
    Console.WriteLine($"구조체 코드 저장: {structFilePath}");

    string enumDefinesPath = Path.Combine(excelDir, EnumDefinesFileName);
    if (File.Exists(enumDefinesPath))
    {
        string enumCode = ExcelHelper.GenerateEnumFromExcel(enumDefinesPath);
        string enumFilePath = Path.Combine(codeDir, EnumDefinesCodeFileName);
        File.WriteAllText(enumFilePath, enumCode);
        Console.WriteLine($"Enum 코드 저장: {enumFilePath}");
    }
    else
    {
        Console.WriteLine($"{EnumDefinesFileName}을 찾을 수 없어 enum 코드 생성을 건너뜁니다: {enumDefinesPath}");
    }

    Console.WriteLine("변환 완료.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("변환 중 오류가 발생했습니다: " + ex.Message);
    return 1;
}

static string? GetArg(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == name)
        {
            return args[i + 1];
        }
    }
    return null;
}

static string FindRepoRoot(string startDir)
{
    var dir = new DirectoryInfo(startDir);
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "Data", "Excel")) &&
            Directory.Exists(Path.Combine(dir.FullName, "Assets")))
        {
            return dir.FullName;
        }
        dir = dir.Parent;
    }
    throw new DirectoryNotFoundException(
        "저장소 루트를 찾을 수 없습니다 (Data/Excel, Assets 폴더 기준). --excel/--xml/--code 인자로 경로를 직접 지정하세요.");
}

using System.Data;

namespace ExcelDataTool
{
    public class TableIdInfo
    {
        // enum 값과 마찬가지로 대소문자 차이로 인한 오탐을 피하기 위해 대소문자 무시 비교.
        public HashSet<string> Ids { get; } = new(StringComparer.OrdinalIgnoreCase);

        // ID_DEV 컬럼 값(기획자용 별칭) -> 실제 ID.
        public Dictionary<string, string> AliasToId { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public static class ReferenceValidator
    {
        /// <summary>
        /// Pass 1: 테이블명 -> (ID 집합, ID_DEV 별칭 -> ID 매핑).
        /// </summary>
        public static Dictionary<string, TableIdInfo> CollectIds(Dictionary<string, DataTable> tables)
        {
            var result = new Dictionary<string, TableIdInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var (tableName, table) in tables)
            {
                int idColIndex = FindColumnIndex(table, "ID");
                int aliasColIndex = FindColumnIndex(table, XmlConverter.AliasColumnName);
                var info = new TableIdInfo();

                if (idColIndex >= 0)
                {
                    for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex++)
                    {
                        string? idValue = table.Rows[rowIndex][idColIndex]?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(idValue))
                        {
                            continue;
                        }
                        info.Ids.Add(idValue);

                        if (aliasColIndex >= 0)
                        {
                            string? alias = table.Rows[rowIndex][aliasColIndex]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(alias))
                            {
                                info.AliasToId[alias] = idValue;
                            }
                        }
                    }
                }
                result[tableName] = info;
            }
            return result;
        }

        /// <summary>
        /// Pass 2: 한 테이블의 id/id[] 타입 컬럼들이 실제로 존재하는 참조 테이블의 ID(또는 ID_DEV 별칭)를
        /// 가리키는지 검증하고, 별칭으로 적힌 값은 실제 ID로 치환한다(테이블에 직접 반영됨).
        /// </summary>
        public static List<string> ValidateAndResolveReferences(
            string tableName,
            DataTable table,
            List<ColumnType> columnTypes,
            Dictionary<string, TableIdInfo> idsByTable)
        {
            var errors = new List<string>();
            int idColIndex = FindColumnIndex(table, "ID");

            for (int colIndex = 0; colIndex < columnTypes.Count; colIndex++)
            {
                var columnType = columnTypes[colIndex];
                if (columnType.Kind != FieldKind.Id && columnType.Kind != FieldKind.IdArray)
                {
                    continue;
                }

                string refTable = columnType.RefTable!;
                if (!idsByTable.TryGetValue(refTable, out var refInfo))
                {
                    errors.Add($"[{tableName}.{columnType.ColumnName}] 알 수 없는 참조 테이블 '{refTable}'");
                    continue;
                }

                for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex++)
                {
                    string rowId = idColIndex >= 0
                        ? (table.Rows[rowIndex][idColIndex]?.ToString()?.Trim() ?? "")
                        : $"row#{rowIndex}";
                    string rawValue = table.Rows[rowIndex][colIndex]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(rawValue) || IsNoReferenceSentinel(rawValue))
                    {
                        continue;
                    }

                    if (columnType.Kind == FieldKind.Id)
                    {
                        string? resolved = ResolveReference(rawValue, refInfo);
                        if (resolved == null)
                        {
                            errors.Add($"[{tableName}.{columnType.ColumnName}] 행 ID='{rowId}' 값 '{rawValue}'가 참조 테이블 '{refTable}'의 ID/{XmlConverter.AliasColumnName}에 존재하지 않습니다.");
                        }
                        else if (resolved != rawValue)
                        {
                            table.Rows[rowIndex][colIndex] = resolved;
                        }
                    }
                    else
                    {
                        var resolvedItems = new List<string>();
                        bool changed = false;
                        bool hasError = false;

                        foreach (var item in XmlConverter.ParseStringArray(rawValue))
                        {
                            if (IsNoReferenceSentinel(item))
                            {
                                resolvedItems.Add(item);
                                continue;
                            }

                            string? resolved = ResolveReference(item, refInfo);
                            if (resolved == null)
                            {
                                errors.Add($"[{tableName}.{columnType.ColumnName}] 행 ID='{rowId}' 값 '{item}'가 참조 테이블 '{refTable}'의 ID/{XmlConverter.AliasColumnName}에 존재하지 않습니다.");
                                hasError = true;
                                continue;
                            }
                            if (resolved != item)
                            {
                                changed = true;
                            }
                            resolvedItems.Add(resolved);
                        }

                        // 에러가 있으면 어차피 이번 실행은 실패 처리되므로 원본을 건드리지 않는다.
                        if (!hasError && changed)
                        {
                            table.Rows[rowIndex][colIndex] = "{" + string.Join(";", resolvedItems) + "}";
                        }
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// "참조 없음"을 뜻하는 약속값. id/id[] 컬럼에 이 값이 있으면 검증 없이 그대로 통과시킨다.
        /// </summary>
        private static bool IsNoReferenceSentinel(string value) => value.Equals("NONE", StringComparison.OrdinalIgnoreCase);

        private static string? ResolveReference(string value, TableIdInfo refInfo)
        {
            if (refInfo.Ids.Contains(value))
            {
                return value;
            }
            if (refInfo.AliasToId.TryGetValue(value, out var realId))
            {
                return realId;
            }
            return null;
        }

        private static int FindColumnIndex(DataTable table, string columnName)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (string.Equals(table.Columns[i].ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}

using System;
using UnityEngine;

/// <summary>
/// id:BBString 타입 필드(Item.DESC, Recipe.DESC 등)를 실제 텍스트로 조회하는 공통 로직.
/// DataDrivenDefines.cs는 엑셀에서 자동 생성되어 매번 덮어써지므로, 이 헬퍼는 별도 파일에 둔다.
/// </summary>
public static class BBStringExtensions
{
    private const string NoReferenceSentinel = "NONE"; // Tools/ExcelDataTool의 "참조 없음" 약속값과 동일

    public static string GetBBStringText(this DataStorage storage, string bbStringId)
    {
        if (string.IsNullOrEmpty(bbStringId) || bbStringId.Equals(NoReferenceSentinel, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (storage?.BBStringData != null && storage.BBStringData.TryGetValue(bbStringId, out var entry))
        {
            return entry.BODY;
        }

        Debug.LogWarning($"BBString ID '{bbStringId}'를 찾을 수 없습니다.");
        return bbStringId;
    }
}

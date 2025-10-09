using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>
/// 범용 파싱 유틸리티.
/// - {1;179} 형태의 배열 문자열 파싱 및 역직렬화 지원
/// - 다양한 구분자(;, , 공백 등) 입력도 유연히 처리
/// - 향후 다른 타입 파서 추가를 위한 확장 지향 구조
/// </summary>
public static class ParsingHelper
{
	/// <summary>
	/// 문자열에서 유효 payload 추출 및 토큰 분리.
	/// {a;b;c} → [a,b,c]
	/// a, b, c  → [a,b,c]
	/// </summary>
	private static string[] SplitArrayPayload(string input)
	{
		if (string.IsNullOrWhiteSpace(input)) return Array.Empty<string>();

		string trimmed = input.Trim();
		if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
		{
			trimmed = trimmed.Substring(1, Math.Max(0, trimmed.Length - 2));
			return trimmed.Split(';')
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.ToArray();
		}

		char[] separators = { ';', ',', ' ', '\t', '\n', '\r' };
		return trimmed.Split(separators, StringSplitOptions.RemoveEmptyEntries)
			.Select(s => s.Trim())
			.Where(s => s.Length > 0)
			.ToArray();
	}

	/// <summary>
	/// {1;179} 형태 또는 일반 구분자 문자열을 int 배열로 파싱.
	/// </summary>
	public static int[] ParseIntArray(string input)
	{
		try
		{
			var parts = SplitArrayPayload(input);
			int[] result = new int[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
				{
					Debug.LogError($"'{parts[i]}'을(를) int로 변환할 수 없습니다.");
					return Array.Empty<int>();
				}
				result[i] = value;
			}
			return result;
		}
		catch (Exception e)
		{
			Debug.LogError($"ParseIntArray 오류: {e.Message}");
			return Array.Empty<int>();
		}
	}

	/// <summary>
	/// {1.5;179.2} 형태 또는 일반 구분자 문자열을 float 배열로 파싱.
	/// </summary>
	public static float[] ParseFloatArray(string input, IFormatProvider culture = null)
	{
		culture ??= CultureInfo.InvariantCulture;
		try
		{
			var parts = SplitArrayPayload(input);
			float[] result = new float[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				if (!float.TryParse(parts[i], NumberStyles.Float | NumberStyles.AllowThousands, culture, out float value))
				{
					Debug.LogError($"'{parts[i]}'을(를) float로 변환할 수 없습니다.");
					return Array.Empty<float>();
				}
				result[i] = value;
			}
			return result;
		}
		catch (Exception e)
		{
			Debug.LogError($"ParseFloatArray 오류: {e.Message}");
			return Array.Empty<float>();
		}
	}

	/// <summary>
	/// {a;b;c} 형태 또는 일반 구분자 문자열을 string 배열로 파싱.
	/// </summary>
	public static string[] ParseStringArray(string input)
	{
		try
		{
			return SplitArrayPayload(input);
		}
		catch (Exception e)
		{
			Debug.LogError($"ParseStringArray 오류: {e.Message}");
			return Array.Empty<string>();
		}
	}

	/// <summary>
	/// 제네릭 배열 파싱. 변환기(converter)를 통해 임의 타입 배열을 생성.
	/// </summary>
	public static T[] ParseArray<T>(string input, Func<string, T> converter)
	{
		if (converter == null)
		{
			Debug.LogError("converter가 null 입니다.");
			return Array.Empty<T>();
		}

		try
		{
			var parts = SplitArrayPayload(input);
			return parts.Select(p => converter(p)).ToArray();
		}
		catch (Exception e)
		{
			Debug.LogError($"ParseArray<{typeof(T).Name}> 오류: {e.Message}");
			return Array.Empty<T>();
		}
	}
}




using System;
using System.Collections.Generic;
using UnityEngine;

public static class CurrencyIds
{
    public const string Gold = "gold";
}

[Serializable]
public struct CurrencyAmount
{
    [SerializeField] private string _currencyId;
    [SerializeField] private int _amount;

    public string CurrencyId => _currencyId;
    public int Amount => _amount;

    public CurrencyAmount(string currencyId, int amount)
    {
        _currencyId = currencyId;
        _amount = amount;
    }
}

/// <summary>
/// 골드, 포인트처럼 슬롯을 차지하지 않고 수량만 가지는 재화의 잔액을 관리한다. 재화 정의는 Currency 테이블의 ID를 쓴다.
/// </summary>
[AddComponentMenu("Inventory/CurrencyWallet")]
[DisallowMultipleComponent]
public class CurrencyWallet : MonoBehaviour
{
    [Header("Initial")]
    [SerializeField] private List<CurrencyAmount> _initialBalances = new List<CurrencyAmount>
    {
        new CurrencyAmount(CurrencyIds.Gold, 0),
    };

    private readonly Dictionary<string, int> _balances = new Dictionary<string, int>();

    public event Action<string, int> OnChanged;

    private bool _initialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    // UI의 OnEnable이 이 컴포넌트의 Awake보다 먼저 호출될 수 있어서, 읽는 시점에도 초기화를 보장한다.
    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        foreach (CurrencyAmount initial in _initialBalances)
        {
            if (!string.IsNullOrEmpty(initial.CurrencyId))
            {
                _balances[initial.CurrencyId] = Mathf.Max(0, initial.Amount);
            }
        }
    }

    public int Get(string currencyId)
    {
        EnsureInitialized();
        return _balances.TryGetValue(currencyId, out int amount) ? amount : 0;
    }

    public bool Has(string currencyId, int amount)
    {
        return Get(currencyId) >= amount;
    }

    public void Add(string currencyId, int amount)
    {
        if (string.IsNullOrEmpty(currencyId) || amount <= 0)
        {
            return;
        }

        Set(currencyId, Get(currencyId) + amount);
    }

    public bool TrySpend(string currencyId, int amount)
    {
        if (string.IsNullOrEmpty(currencyId) || amount <= 0 || !Has(currencyId, amount))
        {
            return false;
        }

        Set(currencyId, Get(currencyId) - amount);
        return true;
    }

    internal List<CurrencyAmount> CaptureBalances()
    {
        EnsureInitialized();
        List<CurrencyAmount> balances = new List<CurrencyAmount>(_balances.Count);
        foreach (KeyValuePair<string, int> balance in _balances)
        {
            balances.Add(new CurrencyAmount(balance.Key, balance.Value));
        }

        return balances;
    }

    internal void RestoreBalances(SaveData data)
    {
        EnsureInitialized();
        List<string> previousIds = new List<string>(_balances.Keys);
        _balances.Clear();
        if (data.Balances != null)
        {
            foreach (CurrencyAmount balance in data.Balances)
            {
                Set(balance.CurrencyId, balance.Amount);
            }
        }
        else
        {
            Set(CurrencyIds.Gold, data.coin);
        }

        foreach (string id in previousIds)
        {
            if (!_balances.ContainsKey(id))
            {
                OnChanged?.Invoke(id, 0);
            }
        }
    }

    public void Set(string currencyId, int amount)
    {
        if (string.IsNullOrEmpty(currencyId))
        {
            return;
        }

        EnsureInitialized();

        int clamped = Mathf.Max(0, amount);
        if (_balances.TryGetValue(currencyId, out int current) && current == clamped)
        {
            return;
        }

        _balances[currencyId] = clamped;
        OnChanged?.Invoke(currencyId, clamped);
    }
}

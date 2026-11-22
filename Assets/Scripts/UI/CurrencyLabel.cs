using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 지정한 재화의 잔액을 TMP 텍스트에 표시한다. 값이 늘면 살짝 튀어 오르는 연출을 준다.
/// </summary>
public class CurrencyLabel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CurrencyWallet _wallet = default;
    [SerializeField] private TMP_Text _text = default;

    [Header("Currency")]
    [SerializeField] private string _currencyId = CurrencyIds.Gold;

    [Header("Feedback")]
    [SerializeField] private float _punchScale = 0.2f;
    [SerializeField] private float _punchDuration = 0.25f;

    private Tween _punchTween;
    private int _displayedAmount = -1;

    private void OnEnable()
    {
        // 프리팹은 씬 오브젝트를 직접 참조할 수 없어서, 인스펙터에 비어 있으면 씬에서 찾는다.
        if (_wallet == null)
        {
            _wallet = FindFirstObjectByType<CurrencyWallet>();
        }

        if (_wallet == null)
        {
            return;
        }

        _wallet.OnChanged += HandleChanged;
        Refresh(_wallet.Get(_currencyId), false);
    }

    private void OnDisable()
    {
        if (_wallet != null)
        {
            _wallet.OnChanged -= HandleChanged;
        }

        _punchTween?.Kill();
        if (_text != null)
        {
            _text.transform.localScale = Vector3.one;
        }
    }

    private void HandleChanged(string currencyId, int amount)
    {
        if (currencyId != _currencyId)
        {
            return;
        }

        Refresh(amount, true);
    }

    private void Refresh(int amount, bool animate)
    {
        if (_text == null)
        {
            return;
        }

        bool increased = amount > _displayedAmount;
        _displayedAmount = amount;
        _text.SetText("{0}", amount);

        if (animate && increased)
        {
            PlayPunch();
        }
    }

    private void PlayPunch()
    {
        _punchTween?.Kill(true);
        _punchTween = _text.transform.DOPunchScale(Vector3.one * _punchScale, _punchDuration)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}

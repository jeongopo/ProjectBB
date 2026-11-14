using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 담기 미니게임의 칸/주문 행 하나. 프리팹에서 참조를 연결하고, 프리팹이 없으면 PlatingComponent 가 코드로 만들어 Setup 한다.
public class PlatingCellView : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private GameObject _cursor;
    [SerializeField] private GameObject _check;
    [SerializeField] private Image _wrongEffect;

    private Tween _wrongTween;

    public Image Background => _background;
    public Image Icon => _icon;
    public TMP_Text Label => _label;
    public bool HasCursorFrame => _cursor != null;

    public void Setup(Image background, Image icon, TMP_Text label)
    {
        _background = background;
        _icon = icon;
        _label = label;
    }

    public void SetCursor(bool visible)
    {
        if (_cursor != null)
        {
            _cursor.SetActive(visible);
        }
    }

    public void SetCheck(bool visible)
    {
        if (_check != null)
        {
            _check.SetActive(visible);
        }
    }

    // 오답 이펙트가 있으면 번쩍였다가 사라진다
    public void PlayWrong(float duration)
    {
        if (_wrongEffect == null)
        {
            return;
        }

        KillWrong();
        Color color = _wrongEffect.color;
        color.a = 1f;
        _wrongEffect.color = color;
        _wrongEffect.gameObject.SetActive(true);
        _wrongTween = _wrongEffect.DOFade(0f, duration)
            .SetLink(gameObject)
            .OnComplete(() => _wrongEffect.gameObject.SetActive(false));
    }

    private void KillWrong()
    {
        if (_wrongTween != null && _wrongTween.IsActive())
        {
            _wrongTween.Kill();
        }
        _wrongTween = null;
    }

    private void OnDisable()
    {
        KillWrong();
        if (_wrongEffect != null)
        {
            _wrongEffect.gameObject.SetActive(false);
        }
    }
}

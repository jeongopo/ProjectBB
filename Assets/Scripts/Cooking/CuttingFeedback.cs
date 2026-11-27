using DataEnumDefines;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

internal sealed class CuttingFeedback : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float _bounceDuration = 0.17f;
    [SerializeField, Min(0.01f)] private float _fragmentDuration = 0.27f;
    [SerializeField] private Ease _fragmentEase = Ease.OutQuad;

    private readonly Image[] _fragments = new Image[5];
    private RectTransform _ingredient;
    private Vector2 _restPosition;
    private Sequence _sequence;

    internal void Initialize(Image ingredient)
    {
        _ingredient = ingredient.rectTransform;
        _restPosition = _ingredient.anchoredPosition;
        for (int i = 0; i < _fragments.Length; i++)
        {
            var fragment = new GameObject($"CuttingFragment_{i + 1:00}", typeof(RectTransform), typeof(Image));
            fragment.layer = ingredient.gameObject.layer;
            Image image = fragment.GetComponent<Image>();
            image.raycastTarget = false;
            image.rectTransform.SetParent(_ingredient, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.enabled = false;
            _fragments[i] = image;
        }
    }

    internal void Play(ENUMGRADE grade, bool hit)
    {
        if (_ingredient == null)
        {
            return;
        }

        Clear();
        _sequence = DOTween.Sequence().SetLink(gameObject);
        float height = grade == ENUMGRADE.GREAT ? 7f : hit ? 4f : 2f;
        Vector2 recoil = hit ? Vector2.up * height : Vector2.right * height;
        _sequence.Join(_ingredient.DOPunchAnchorPos(recoil, _bounceDuration, 4, 0.35f));

        int fragmentCount = grade == ENUMGRADE.GREAT ? 5 : hit ? 2 : 0;
        for (int i = 0; i < fragmentCount; i++)
        {
            Image fragment = _fragments[i];
            RectTransform rect = fragment.rectTransform;
            float direction = (i - (fragmentCount - 1) * 0.5f) * 10f;
            Vector2 destination = new Vector2(direction * 1.4f, 12f + 5f * (i % 2));
            rect.anchoredPosition = new Vector2(direction * 0.16f, 0f);
            rect.sizeDelta = Vector2.one * (i % 2 == 0 ? 3f : 4f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            fragment.color = i % 2 == 0 ? new Color32(246, 213, 137, 255) : new Color32(221, 143, 76, 255);
            fragment.enabled = true;

            _sequence.Join(rect.DOAnchorPos(destination, _fragmentDuration).SetEase(_fragmentEase));
            _sequence.Join(rect.DOLocalRotate(new Vector3(0f, 0f, direction + 65f), _fragmentDuration));
            _sequence.Insert(_fragmentDuration * 0.35f, fragment.DOFade(0f, _fragmentDuration * 0.65f));
        }

        _sequence.OnComplete(() =>
        {
            _sequence = null;
            ResetVisuals();
        });
    }

    private void OnDisable() => Clear();

    private void Clear()
    {
        _sequence?.Kill();
        _sequence = null;
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        if (_ingredient != null)
        {
            _ingredient.anchoredPosition = _restPosition;
        }
        foreach (Image fragment in _fragments)
        {
            if (fragment != null)
            {
                fragment.enabled = false;
            }
        }
    }
}

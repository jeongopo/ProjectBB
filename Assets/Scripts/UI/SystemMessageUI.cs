using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

// 게임 전체에서 공용으로 쓰는 한 줄 시스템 메시지. 글자 길이에 맞춰 폭이 늘어나고 상한을 넘으면 줄바꿈된다.
public class SystemMessageUI : MonoBehaviour
{
    public static SystemMessageUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform _box;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private TMP_Text _label;

    [Header("Layout")]
    [SerializeField] private Vector2 _padding = new Vector2(12f, 6f);
    [SerializeField] private float _minHeight = 24f;
    [SerializeField] private float _maxWidth = 360f;

    [Header("Animation")]
    [SerializeField] private float _displayDuration = 1.5f;
    [SerializeField] private float _fadeDuration = 0.2f;
    [SerializeField] private int _maxQueueCount = 3;

    private readonly Queue<string> _queue = new Queue<string>();
    private Sequence _sequence;

    public static void Show(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (Instance == null)
        {
            Debug.LogWarning($"SystemMessageUI: 씬에 인스턴스가 없어 메시지를 표시하지 못했습니다. ({message})");
            return;
        }

        Instance.Enqueue(message);
    }

    private void Awake()
    {
        Instance = this;
        _group.alpha = 0f;
    }

    private void OnDestroy()
    {
        KillSequence();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Enqueue(string message)
    {
        while (_queue.Count >= _maxQueueCount)
        {
            _queue.Dequeue();
        }

        _queue.Enqueue(message);
        if (_sequence == null)
        {
            PlayNext();
        }
    }

    private void PlayNext()
    {
        if (_queue.Count == 0)
        {
            _sequence = null;
            return;
        }

        ApplyText(_queue.Dequeue());

        _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        _sequence.Append(_group.DOFade(1f, _fadeDuration));
        _sequence.AppendInterval(_displayDuration);
        _sequence.Append(_group.DOFade(0f, _fadeDuration));
        _sequence.OnComplete(PlayNext);
    }

    private void ApplyText(string message)
    {
        _label.text = message;

        float maxLabelWidth = _maxWidth - _padding.x * 2f;
        Vector2 preferred = _label.GetPreferredValues(message, maxLabelWidth, 0f);
        float width = Mathf.Ceil(Mathf.Min(preferred.x, maxLabelWidth) + _padding.x * 2f);
        float height = Mathf.Max(_minHeight, Mathf.Ceil(preferred.y + _padding.y * 2f));
        _box.sizeDelta = new Vector2(width, height);
    }

    private void KillSequence()
    {
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Kill();
        }
        _sequence = null;
    }
}

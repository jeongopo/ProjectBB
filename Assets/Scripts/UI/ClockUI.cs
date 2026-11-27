using DG.Tweening;
using TMPro;
using UnityEngine;
using GamePlay;
using UnityEngine.UI;

public class ClockUI : MonoBehaviour
{
    private const int MinutesPerDay = 24 * 60;

    [Header("References")]
    [SerializeField] private GameClock _clock = default;
    [SerializeField] private TMP_Text _timeText = default;
    [SerializeField] private RectTransform _arrow = default;
    [SerializeField] private Button _advanceHourButton;

    [Header("Arrow")]
    [SerializeField] private float _arrowTweenDuration = 0.3f;
    [SerializeField] private Ease _arrowEase = Ease.OutQuad;

    private Tween _arrowTween;

    private void Awake()
    {
        if (_advanceHourButton == null)
        {
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _advanceHourButton.gameObject.SetActive(true);
        _advanceHourButton.onClick.AddListener(AdvanceOneHour);
#else
        _advanceHourButton.gameObject.SetActive(false);
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void AdvanceOneHour()
    {
        if (_clock != null)
        {
            _clock.AdvanceOneHourForTesting();
        }
    }
#endif

    private void OnEnable()
    {
        // 프리팹은 씬 오브젝트를 직접 참조할 수 없어서, 인스펙터에 비어 있으면 씬에서 찾는다.
        if (_clock == null)
        {
            _clock = FindFirstObjectByType<GameClock>();
        }

        if (_clock == null)
        {
            return;
        }

        _clock.OnMinuteChanged += Refresh;
        Refresh(_clock.TotalMinutes, false);
    }

    private void OnDisable()
    {
        if (_clock != null)
        {
            _clock.OnMinuteChanged -= Refresh;
        }

        _arrowTween?.Kill();
    }

    private void Refresh(int totalMinutes)
    {
        Refresh(totalMinutes, true);
    }

    private void Refresh(int totalMinutes, bool animate)
    {
        if (_timeText != null)
        {
            _timeText.SetText("{0:00}:{1:00}", totalMinutes / 60, totalMinutes % 60);
        }

        RotateArrow(totalMinutes, animate);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_advanceHourButton != null)
        {
            _advanceHourButton.interactable = !_clock.IsDayEnded;
        }
#endif
    }

    // 0시가 12시 방향이고 시계 방향으로 하루에 한 바퀴 돈다.
    private void RotateArrow(int totalMinutes, bool animate)
    {
        if (_arrow == null)
        {
            return;
        }

        _arrowTween?.Kill();

        float angle = -360f * totalMinutes / MinutesPerDay;
        if (!animate)
        {
            _arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
            return;
        }

        _arrowTween = _arrow.DOLocalRotate(new Vector3(0f, 0f, angle), _arrowTweenDuration)
            .SetEase(_arrowEase)
            .SetLink(gameObject);
    }
}

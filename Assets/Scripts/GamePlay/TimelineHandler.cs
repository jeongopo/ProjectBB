using System.Collections.Generic;
using UnityEngine;
using GamePlay;
using GameEnumDefines;

// <summary>
// 시간대가 바뀌거나, 세팅하는 것들은 여기서 처리
// </summary>
[AddComponentMenu("GamePlay/TimelineSystem")]
[DisallowMultipleComponent]
public class TimelineHandler : MonoBehaviour
{
    [SerializeField] private GameClock _clock = default;
    [SerializeField] private List<TimelineBase> _timelineContents = default;

    private void OnEnable()
    {
        if (_clock != null)
        {
            _clock.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    private void OnDisable()
    {
        if (_clock != null)
        {
            _clock.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void HandlePhaseChanged(DayPhase phase)
    {
        if (_timelineContents == null)
        {
            return;
        }

        foreach (TimelineBase content in _timelineContents)
        {
            if (content == null)
            {
                continue;
            }

            if (content.Phase == phase)
            {
                content.Enter();
            }
            else
            {
                content.Exit();
            }
        }
    }
}

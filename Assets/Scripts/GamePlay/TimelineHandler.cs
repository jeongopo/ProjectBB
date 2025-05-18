using System.Collections;
using System.Collections.Generic;
using UnityEngine;


enum TimelineType
{
    Afternoon,
}

// <summary>
// 시간대가 바뀌거나, 세팅하는 것들은 여기서 처리
// </summary>
[AddComponentMenu("GamePlay/TimelineSystem")]
[DisallowMultipleComponent]
public class TimelineHandler : MonoBehaviour
{
    [SerializeField] private List<TimelineBase> _timelineContents = default;
}
using UnityEngine;
using GameEnumDefines;

// <summary>
// 시간단위의 콘텐츠들이 공통으로 가질 요소들 정의
// </summary>
public class TimelineBase : MonoBehaviour
{
    [SerializeField] private DayPhase _phase = default;

    public DayPhase Phase => _phase;

    public void Enter()
    {
        gameObject.SetActive(true);
        OnEnter();
    }

    public void Exit()
    {
        OnExit();
        gameObject.SetActive(false);
    }

    protected virtual void OnEnter()
    {
    }

    protected virtual void OnExit()
    {
    }
}

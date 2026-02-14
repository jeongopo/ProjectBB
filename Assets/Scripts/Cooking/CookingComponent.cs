using UnityEngine;
using UnityEngine.UI;
using GamePlay;
using System;

/// <summary>
/// 모든 미니게임의 기본이 되는 추상 클래스
/// 하위 미니게임 컴포넌트는 이 클래스를 상속받아 필요한 메서드를 구현
/// </summary>
public abstract class CookingComponent : MonoBehaviour
{
    [SerializeField] protected CookingTimerComponent cookingTimer;
    [SerializeField] protected Button cuttingStartButton;

    protected bool isPlaying = false;

    protected virtual void Start()
    {
        if (cuttingStartButton != null)
        {
            cuttingStartButton.onClick.AddListener(StartMiniGame);
        }

        if (cookingTimer == null)
        {
            cookingTimer = FindFirstObjectByType<CookingTimerComponent>();
        }

        if (cookingTimer != null)
        {
            cookingTimer.gameObject.SetActive(false);
        }
    }

    protected abstract void InitCooking();

    public virtual void StartMiniGame()
    {
        if (cookingTimer != null)
        {
            cookingTimer.StartCountdown(OnTimerComplete);
        }
        else
        {
            OnTimerComplete();
        }
    }

    protected virtual void OnTimerComplete()
    {
        InitCooking();
        isPlaying = true;

        FindFirstObjectByType<InputManager>().OnInteractPressed += Interact;
    }

    protected abstract void Interact();

    protected abstract void JudgeResult();

    /// <summary>
    /// 미니게임 종료 및 결과 처리
    /// </summary>
    protected abstract void EndMiniGame();

    /// <summary>
    /// 미니게임 종료 시 호출되는 공통 정리 로직
    /// </summary>
    protected virtual void OnGameEnd()
    {
        FindFirstObjectByType<InputManager>().OnInteractPressed -= Interact;
        isPlaying = false;
    }
}

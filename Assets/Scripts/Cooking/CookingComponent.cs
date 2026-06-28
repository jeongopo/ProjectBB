using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using GamePlay;
using GameEnumDefines;
using System;

public abstract class CookingComponent : MonoBehaviour
{
    public event Action OnMiniGameEnd;

    [SerializeField] protected CookingTimerComponent cookingTimer;
    [SerializeField] protected Button CookingStartButton;

    protected bool isPlaying = false;
    protected InputManager inputManager;
    protected InputAction moveAction;
    protected InputAction interactAction;

    protected virtual void Start()
    {
        if (CookingStartButton != null)
        {
            CookingStartButton.onClick.AddListener(PreStartMiniGame);
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

    //처음 요리 UI 열리자마자 생기는 이벤트
    public void InitCooking()
    {
        inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager != null)
        {
            inputManager.SwitchInputState(InputState.Minigame);
        }

        SetupMoveAction();
        SetupInteractAction();
        
        InitMiniGameData();
    }

    public virtual void InitMiniGameData()
    {
        //게임 여러번 실행할때 계속 불릴 함수
    }

    //미니게임 실행 직전
    public virtual void PreStartMiniGame()
    {
        if (cookingTimer != null)
        {
            cookingTimer.StartCountdown(StartMiniGame);
        }
        else
        {
            StartMiniGame();
        }
    }

    //실제 미니게임 시작
    protected virtual void StartMiniGame()
    {
        isPlaying = true;
    }

    private void SetupMoveAction()
    {
        if (inputManager == null) return;

        var actionMap = inputManager.GetCurrentActionMap();
        if (actionMap != null)
        {
            moveAction = actionMap.FindAction("Game_Move");
        }
    }

    private void SetupInteractAction()
    {
        if (inputManager == null) return;

        var actionMap = inputManager.GetCurrentActionMap();
        if (actionMap != null)
        {
            interactAction = actionMap.FindAction("Game_Interact");
            if (interactAction != null)
            {
                interactAction.started += OnInteractStarted;
                interactAction.canceled += OnInteractCanceled;
            }
        }
    }

    private void OnInteractStarted(InputAction.CallbackContext context)
    {
        if(!isPlaying || interactAction == null || !interactAction.enabled)
            return;
        Interact();
    }

    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        // 필요시 취소 로직
    }

    protected virtual void Update()
    {
        if (!isPlaying || moveAction == null || !moveAction.enabled)
            return;

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        OnMove(moveInput);
    }

    protected virtual void Interact() {}

    protected virtual void OnMove(Vector2 moveInput) {}

    protected abstract void JudgeResult();

    protected abstract void EndMiniGame();

    protected virtual void OnGameEnd()
    {
        if (interactAction != null)
        {
            interactAction.started -= OnInteractStarted;
            interactAction.canceled -= OnInteractCanceled;
        }

        if (inputManager != null)
        {
            inputManager.SwitchInputState(InputState.Default);
        }

        isPlaying = false;
        OnMiniGameEnd?.Invoke();
    }
}

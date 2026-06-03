using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using GamePlay;
using GameEnumDefines;
using System;

public abstract class CookingComponent : MonoBehaviour
{
    [SerializeField] protected CookingTimerComponent cookingTimer;
    [SerializeField] protected Button cuttingStartButton;

    protected bool isPlaying = false;
    protected InputManager inputManager;
    protected InputAction moveAction;
    protected InputAction interactAction;

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
        inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager != null)
        {
            inputManager.SwitchInputState(InputState.Minigame);
        }

        InitCooking();
        SetupMoveAction();
        SetupInteractAction();
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
    }
}

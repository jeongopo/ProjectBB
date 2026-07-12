using UnityEngine;
using UnityEngine.UI;
using GameEnumDefines;
using DataEnumDefines;
using System;
using System.Collections;

public class CookingManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject cookingPanel;
    [SerializeField] private Button closeButton;

    [Header("Cooking Components")]
    [SerializeField] private BoilingComponent boilingComponent;
    [SerializeField] private CuttingComponent cuttingComponent;

    [Header("Shared Result/Timer UI")]
    [SerializeField] private CookingTimerComponent cookingTimer;
    [SerializeField] private Image resultImage;
    [SerializeField] private Sprite[] resultSprites; // 인덱스 = (int)ENUMGRADE - 1 : NORMAL(0), GOOD(1), GREAT(2)
    [SerializeField] private float resultDisplayDuration = 2f;

    private CookingComponent currentComponent;
    private GamePlay.InputManager inputManager;

    private void Awake()
    {
        inputManager = FindFirstObjectByType<GamePlay.InputManager>();
        if (cookingPanel != null)
            cookingPanel.SetActive(false);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseCooking);

        if (cookingTimer != null)
            cookingTimer.gameObject.SetActive(false);
        if (resultImage != null)
            resultImage.gameObject.SetActive(false);
    }

    public void PlayCountdown(Action onComplete)
    {
        if (cookingTimer != null)
        {
            cookingTimer.StartCountdown(onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    public void ShowResult(ENUMGRADE grade, Action onHidden)
    {
        int spriteIndex = (int)grade - 1;
        Sprite sprite = (resultSprites != null && spriteIndex >= 0 && spriteIndex < resultSprites.Length)
            ? resultSprites[spriteIndex]
            : null;

        if (resultImage != null && sprite != null)
        {
            resultImage.sprite = sprite;
            resultImage.gameObject.SetActive(true);
            StartCoroutine(HideResultThenInvoke(onHidden));
        }
        else
        {
            onHidden?.Invoke();
        }
    }

    private IEnumerator HideResultThenInvoke(Action onHidden)
    {
        yield return new WaitForSeconds(resultDisplayDuration);

        if (resultImage != null)
        {
            resultImage.gameObject.SetActive(false);
        }

        onHidden?.Invoke();
    }

    public void OpenCooking(CookingType type)
    {
        currentComponent = GetCookingComponent(type);
        if (currentComponent == null)
        {
            Debug.LogWarning($"CookingManager: No component assigned for type {type}");
            return;
        }

        SetAllSubPanelsActive(false);
        currentComponent.gameObject.SetActive(true);
        cookingPanel.SetActive(true);

        currentComponent.OnMiniGameEnd += OnMiniGameEnded;
        currentComponent.InitCooking();
    }

    private void OnMiniGameEnded()
    {
        // 보상 처리
    }

    public void CloseCooking()
    {
        if (currentComponent != null)
        {
            currentComponent.OnCookingEnd();
            currentComponent.OnMiniGameEnd -= OnMiniGameEnded;
            currentComponent = null;
        }

        SetAllSubPanelsActive(false);
        if (cookingPanel != null)
            cookingPanel.SetActive(false);

        if (inputManager != null)
            inputManager.SwitchInputState(InputState.Default);
    }

    private void SetAllSubPanelsActive(bool active)
    {
        if (boilingComponent != null) boilingComponent.gameObject.SetActive(active);
        if (cuttingComponent != null) cuttingComponent.gameObject.SetActive(active);
    }

    private CookingComponent GetCookingComponent(CookingType type) => type switch
    {
        CookingType.Boiling => boilingComponent,
        CookingType.Cutting => cuttingComponent,
        _ => null
    };
}

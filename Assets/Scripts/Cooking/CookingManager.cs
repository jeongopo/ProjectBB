using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
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

    [Header("Shared Timer UI")]
    [SerializeField] private CookingTimerComponent cookingTimer;

    [Header("Result UI")]
    [FormerlySerializedAs("resultImage")]
    [SerializeField] private Image resultImage;
    [SerializeField] private Sprite[] roundResultSprites; // 인덱스 = (int)ENUMGRADE - 1 : NORMAL(0), GOOD(1), GREAT(2)
    [SerializeField] private float roundResultDisplayDuration = 1f;
    [SerializeField] private Sprite[] finalResultSprites; // 인덱스 = (int)ENUMGRADE - 1 : NORMAL(0), GOOD(1), GREAT(2)
    [SerializeField] private float resultDisplayDuration = 2f;

    private CookingComponent currentComponent;
    private GamePlay.InputManager inputManager;
    private Coroutine resultCoroutine;

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

    //1회 판정마다 짧게 보여주는 이미지
    public void ShowRoundResult(ENUMGRADE grade)
    {
        int spriteIndex = (int)grade - 1;
        Sprite sprite = (roundResultSprites != null && spriteIndex >= 0 && spriteIndex < roundResultSprites.Length)
            ? roundResultSprites[spriteIndex]
            : null;

        if (resultImage == null || sprite == null)
            return;

        if (resultCoroutine != null)
            StopCoroutine(resultCoroutine);

        resultImage.sprite = sprite;
        resultImage.gameObject.SetActive(true);
        resultCoroutine = StartCoroutine(HideRoundResultAfterDelay());
    }

    private IEnumerator HideRoundResultAfterDelay()
    {
        yield return new WaitForSeconds(roundResultDisplayDuration);

        resultImage.gameObject.SetActive(false);
        resultCoroutine = null;
    }

    //미니게임 최종 결과 표시 후 onHidden 콜백 호출
    public void ShowResult(ENUMGRADE grade, Action onHidden)
    {
        int spriteIndex = (int)grade - 1;
        Sprite sprite = (finalResultSprites != null && spriteIndex >= 0 && spriteIndex < finalResultSprites.Length)
            ? finalResultSprites[spriteIndex]
            : null;

        if (resultImage == null || sprite == null)
        {
            onHidden?.Invoke();
            return;
        }

        if (resultCoroutine != null)
            StopCoroutine(resultCoroutine);

        resultImage.sprite = sprite;
        resultImage.gameObject.SetActive(true);
        resultCoroutine = StartCoroutine(HideResultThenInvoke(onHidden));
    }

    private IEnumerator HideResultThenInvoke(Action onHidden)
    {
        yield return new WaitForSeconds(resultDisplayDuration);

        resultImage.gameObject.SetActive(false);
        resultCoroutine = null;

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

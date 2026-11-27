using UnityEngine;
using GamePlay;
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
    [SerializeField] private PlatingComponent platingComponent;
    [SerializeField] private float _platingHorizontalOffset = -12f;

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
    private IngredientPreparation _preparation;
    private CookingType _openType;
    private InventoryManager _inventory;
    private SavedItemStack? _ingredient;
    private bool _requiresSelection;
    private CookingResultUI _completedItem;
    private DataManager _dataManager;

    private void Awake()
    {
        inputManager = FindFirstObjectByType<GamePlay.InputManager>();
        _dataManager = FindFirstObjectByType<DataManager>();
        if (cookingPanel != null)
        {
            _completedItem = cookingPanel.GetComponentInChildren<CookingResultUI>(true);
            cookingPanel.SetActive(false);
        }
        if (platingComponent != null && platingComponent.transform is RectTransform platingRect)
        {
            platingRect.anchoredPosition += Vector2.right * _platingHorizontalOffset;
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                GameAudio.Play(SoundCue.PanelClose);
                CloseCooking();
            });
        }

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
    public void ShowRoundResult(ENUMGRADE grade, bool playSound = true)
    {
        if (playSound)
        {
            GameAudio.Play(grade >= ENUMGRADE.GOOD ? SoundCue.CookingSuccess : SoundCue.CookingMiss, 0.45f);
        }
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
    public void ShowResult(ENUMGRADE grade, Action onHidden, string outputItemId = null)
    {
        GameAudio.Play(grade >= ENUMGRADE.GOOD ? SoundCue.CookingSuccess : SoundCue.CookingMiss);
        int spriteIndex = (int)grade - 1;
        Sprite sprite = (finalResultSprites != null && spriteIndex >= 0 && spriteIndex < finalResultSprites.Length)
            ? finalResultSprites[spriteIndex]
            : null;

        bool showCompleted = !string.IsNullOrEmpty(outputItemId) && ShowCompletedItem(outputItemId, grade: sprite);
        if (!showCompleted && (resultImage == null || sprite == null))
        {
            onHidden?.Invoke();
            return;
        }

        if (resultCoroutine != null)
            StopCoroutine(resultCoroutine);

        if (resultImage != null)
        {
            resultImage.sprite = sprite;
            resultImage.gameObject.SetActive(!showCompleted && sprite != null);
        }
        resultCoroutine = StartCoroutine(HideResultThenInvoke(onHidden));
    }

    private IEnumerator HideResultThenInvoke(Action onHidden)
    {
        yield return new WaitForSeconds(resultDisplayDuration);

        if (resultImage != null)
        {
            resultImage.gameObject.SetActive(false);
        }
        _completedItem?.Hide();
        resultCoroutine = null;

        onHidden?.Invoke();
    }

    public void OpenCooking(CookingType type)
    {
        if (inputManager != null && !inputManager.CanOpenGameplayPanel)
        {
            return;
        }

        _openType = type;
        currentComponent = GetCookingComponent(type);
        if (currentComponent == null)
        {
            Debug.LogWarning($"CookingManager: No component assigned for type {type}");
            return;
        }

        SetAllSubPanelsActive(false);
        currentComponent.gameObject.SetActive(true);
        cookingPanel.SetActive(true);
        GameAudio.Play(SoundCue.PanelOpen);

        currentComponent.InitCooking();
        OpenIngredientInventory(type);
    }

    private void OpenIngredientInventory(CookingType type)
    {
        if (_inventory == null)
        {
            _inventory = FindFirstObjectByType<InventoryManager>();
        }

        _ingredient = null;
        _requiresSelection = type == CookingType.Cutting || type == CookingType.Boiling;
        if (_requiresSelection && _preparation == null)
        {
            DataManager data = FindFirstObjectByType<DataManager>();
            if (data != null && _inventory != null)
            {
                _preparation = new IngredientPreparation(data.dataStorage, _inventory);
            }
        }

        currentComponent.SetIngredientReady(!_requiresSelection);
        _inventory?.OpenForCooking(_requiresSelection ? IsValidIngredient : null, OnIngredientChanged);
    }

    private bool IsValidIngredient(SavedItemStack item) => _preparation?.CanPrepare(_openType, item) == true;

    private void OnIngredientChanged(SavedItemStack? item)
    {
        _ingredient = item;
        if (currentComponent != null)
        {
            currentComponent.SetIngredientReady(!_requiresSelection || item.HasValue);
            Sprite ingredient = item.HasValue
                ? ItemIcon.ResolveIcon(_dataManager != null ? _dataManager.dataStorage : null, item.Value.ItemId)
                : null;
            currentComponent.SetIngredientSprite(ingredient);
        }
    }

    internal bool TryBeginPreparation()
    {
        if (inputManager != null && inputManager.CurrentInputState == InputState.Dialogue)
        {
            return false;
        }

        if (!_requiresSelection)
        {
            return true;
        }
        if (!_ingredient.HasValue || _preparation?.TryStart(_openType, _ingredient.Value) != true)
        {
            return false;
        }

        _inventory.SetCookingSelectionLocked(true);
        return true;
    }

    internal string CompletePreparation(ENUMGRADE grade) => _preparation?.Complete(grade);

    internal bool ShowCompletedItem(string itemId, string fallbackName = null, Sprite grade = null)
    {
        if (_completedItem == null || _dataManager == null)
        {
            return false;
        }

        _completedItem.Show(_dataManager.dataStorage, itemId, fallbackName, grade);
        return true;
    }

    internal void FinishPreparationRound()
    {
        _inventory?.SetCookingSelectionLocked(false);
    }

    public void CloseCooking()
    {
        _completedItem?.Hide();
        _preparation?.Cancel();
        _inventory?.CloseAfterCooking();
        _ingredient = null;
        _requiresSelection = false;
        if (cookingTimer != null)
        {
            cookingTimer.StopCountdown();
        }
        if (resultCoroutine != null)
        {
            StopCoroutine(resultCoroutine);
            resultCoroutine = null;
        }
        if (resultImage != null)
        {
            resultImage.gameObject.SetActive(false);
        }
        if (currentComponent != null)
        {
            currentComponent.OnCookingEnd();
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
        if (platingComponent != null) platingComponent.gameObject.SetActive(active);
    }

    private CookingComponent GetCookingComponent(CookingType type) => type switch
    {
        CookingType.Boiling => boilingComponent,
        CookingType.Cutting => cuttingComponent,
        CookingType.Plating => platingComponent,
        _ => null
    };
}

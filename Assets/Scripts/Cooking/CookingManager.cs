using UnityEngine;
using UnityEngine.UI;
using GameEnumDefines;

public class CookingManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject cookingPanel;
    [SerializeField] private Button closeButton;

    [Header("Cooking Components")]
    [SerializeField] private BoilingComponent boilingComponent;
    [SerializeField] private CuttingComponent cuttingComponent;

    private CookingComponent currentComponent;
    private GamePlay.InputManager inputManager;

    private void Awake()
    {
        inputManager = FindFirstObjectByType<GamePlay.InputManager>();
        if (cookingPanel != null)
            cookingPanel.SetActive(false);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseCooking);
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
        if (currentComponent != null)
        {
            currentComponent.OnMiniGameEnd -= OnMiniGameEnded;
            currentComponent = null;
        }
    }

    public void CloseCooking()
    {
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

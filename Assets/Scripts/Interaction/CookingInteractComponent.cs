using UnityEngine;
using Interaction;
using GameEnumDefines;

public class CookingInteractComponent : InteractComponent
{
    [SerializeField] private CookingType cookingType;

    private CookingManager cookingManager;
    public override bool CanInteract => base.CanInteract && cookingManager != null;
    public override string PromptKey => cookingType switch
    {
        CookingType.Boiling => "STR_Interact_Boil",
        CookingType.Cutting => "STR_Interact_Cut",
        _ => "STR_Interact_Plate",
    };

    protected override void Start()
    {
        base.Start();
        cookingManager = FindFirstObjectByType<CookingManager>();
        if (cookingManager == null)
            Debug.LogWarning("CookingInteractComponent: CookingManager not found in scene.");
    }

    protected override void OnInteract()
    {
        if (cookingManager == null) return;
        cookingManager.OpenCooking(cookingType);
    }
}

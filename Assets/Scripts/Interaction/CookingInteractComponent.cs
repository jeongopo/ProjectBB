using UnityEngine;
using Interaction;
using GameEnumDefines;

public class CookingInteractComponent : InteractComponent
{
    [SerializeField] private CookingType cookingType;

    private CookingManager cookingManager;

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

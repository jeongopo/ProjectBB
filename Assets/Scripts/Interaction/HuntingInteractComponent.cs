using UnityEngine;
using Interaction;

public class HuntingInteractComponent : InteractComponent
{
    private HuntingManager huntingManager;

    protected override void Start()
    {
        base.Start();
        huntingManager = FindFirstObjectByType<HuntingManager>();
        if (huntingManager == null)
            Debug.LogWarning("HuntingInteractComponent: HuntingManager not found in scene.");
    }

    protected override void OnInteract()
    {
        if (huntingManager == null) return;
        huntingManager.OpenHunting();
    }
}

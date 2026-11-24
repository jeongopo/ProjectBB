using UnityEngine;
using Interaction;

public class HuntingInteractComponent : InteractComponent
{
    private HuntingManager huntingManager;
    public override bool CanInteract => base.CanInteract && huntingManager != null;
    public override string PromptKey => "STR_Interact_Hunt";

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

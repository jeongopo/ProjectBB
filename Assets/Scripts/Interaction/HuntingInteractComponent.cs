using UnityEngine;
using Interaction;

public class HuntingInteractComponent : InteractComponent
{
    [SerializeField] private string huntingID = "Hunting1";

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
        huntingManager.OpenHunting(huntingID);
    }
}

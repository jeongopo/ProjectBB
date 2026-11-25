using UnityEngine;

namespace Interaction
{
    public sealed class DialogueInteractComponent : InteractComponent
    {
        [SerializeField] private string _speakerKey;
        [SerializeField] private Sprite _portrait;
        [SerializeField] private string[] _lineKeys;

        private DialoguePanel _panel;

        public override string PromptKey => "STR_Interact_Talk";
        public override bool CanInteract => base.CanInteract && _panel != null && !_panel.IsOpen && _lineKeys?.Length > 0;

        protected override void Start()
        {
            _panel = FindFirstObjectByType<DialoguePanel>(FindObjectsInactive.Include);
        }

        protected override void OnInteract()
        {
            _panel.Open(_speakerKey, _portrait, _lineKeys);
        }
    }
}

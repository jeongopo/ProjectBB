using GamePlay;
using UnityEngine;

namespace Interaction
{
    public sealed class EventTrigger : InteractComponent
    {
        [SerializeField] private EventDefinition _event;
        private EventRunner _runner;

        public override bool CanInteract => base.CanInteract && _event != null && _runner != null && _runner.CanTrigger(_event.Id);

        protected override void Start()
        {
            _runner = FindFirstObjectByType<EventRunner>();
        }

        protected override void OnInteract()
        {
            _runner.TryStart(_event.Id);
        }
    }
}

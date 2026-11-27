using GamePlay;
using UnityEngine;

namespace Interaction
{
    public sealed class TableServingPoint : InteractComponent
    {
        private CustomerSeat[] _seats;
        private BoxCollider2D _table;
        private Transform _player;

        public override string PromptKey => "STR_Interact_Serve";
        public override bool CanInteract => base.CanInteract && FindGuest() != null;
        protected override Vector2 InteractionRangeSize => GetComponent<BoxCollider2D>().size;

        internal static void Install(Furniture table, CustomerSeat[] seats, Transform player)
        {
            if (!table.TryGetComponent(out BoxCollider2D collider))
            {
                return;
            }

            Bounds bounds = collider.bounds;
            GameObject point = new GameObject("TableServingPoint");
            point.transform.SetParent(table.transform, false);
            point.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.85f, table.transform.position.z);
            BoxCollider2D range = point.AddComponent<BoxCollider2D>();
            range.isTrigger = true;
            range.size = new Vector2(bounds.size.x + 1f, 1.7f);
            point.AddComponent<TableServingPoint>().Configure(collider, seats, player);
        }

        private void Configure(BoxCollider2D table, CustomerSeat[] seats, Transform player)
        {
            _table = table;
            _seats = seats;
            _player = player;
        }

        protected override void OnInteract()
        {
            FindGuest()?.Interact();
        }

        private CustomerController FindGuest()
        {
            if (_seats == null || _player == null || _table == null)
            {
                return null;
            }

            CustomerController best = null;
            float bestDistance = float.PositiveInfinity;
            Bounds table = _table.bounds;
            foreach (CustomerSeat seat in _seats)
            {
                CustomerController guest = seat != null ? seat.Occupant : null;
                if (guest == null || guest.State != GameEnumDefines.CustomerState.Seated || guest.Order?.PlatingOrder.servable != true)
                {
                    continue;
                }

                Vector3 position = seat.SitPosition;
                if (position.x < table.min.x - 0.25f || position.x > table.max.x + 0.25f ||
                    position.y < table.min.y - 1.5f || position.y > table.max.y + 1.75f)
                {
                    continue;
                }

                float distance = (position - _player.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = guest;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}

using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// 손님이 앉는 자리. 의자 가구에 붙이고, 앉는 위치는 Sit Point로 지정한다.
    /// </summary>
    public class CustomerSeat : MonoBehaviour
    {
        [SerializeField] private Transform _sitPoint = default;
        [SerializeField] private bool _reservedForRegulars;

        public bool ReservedForRegulars => _reservedForRegulars;
        public CustomerController Occupant { get; private set; }
        public bool IsOccupied => Occupant != null;
        public Vector3 SitPosition => _sitPoint != null ? _sitPoint.position : transform.position;

        public bool TryReserve(CustomerController customer)
        {
            if (IsOccupied)
            {
                return false;
            }

            Occupant = customer;
            return true;
        }

        public void Release(CustomerController customer)
        {
            if (Occupant == customer)
            {
                Occupant = null;
            }
        }
    }
}

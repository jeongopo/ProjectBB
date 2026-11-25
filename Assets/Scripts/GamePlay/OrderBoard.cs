using System;
using System.Collections.Generic;
using UnityEngine;
using GameEnumDefines;
using Interaction;

namespace GamePlay
{
    public class CustomerOrder
    {
        public CustomerController Customer { get; }
        public DataStorage.Recipe_Combine Recipe { get; }
        public PlatingOrder PlatingOrder { get; }

        public CustomerOrder(CustomerController customer, DataStorage.Recipe_Combine recipe, PlatingOrder platingOrder)
        {
            Customer = customer;
            Recipe = recipe;
            PlatingOrder = platingOrder;
        }
    }

    /// <summary>
    /// 손님이 발행한 주문 목록. 담기 미니게임(PlatingComponent)과 손님 사이를 잇고, 서빙과 결제를 처리한다.
    /// </summary>
    [AddComponentMenu("GamePlay/OrderBoard")]
    [DisallowMultipleComponent]
    public class OrderBoard : MonoBehaviour
    {
        [Header("Plating")]
        [SerializeField] private float _timeLimit = 30f;
        [SerializeField] private float _mistakePenalty = 3f;

        [Header("References (비워두면 씬에서 찾는다)")]
        [SerializeField] private CurrencyWallet _wallet = default;
        [SerializeField] private DayStats _dayStats = default;

        private readonly List<CustomerOrder> _orders = new List<CustomerOrder>();
        private readonly List<PlatingOrder> _platingOrders = new List<PlatingOrder>();
        private readonly Dictionary<string, Sprite> _iconCache = new Dictionary<string, Sprite>();
        private DataManager _dataManager;
        private InventoryManager _inventory;
        private MainCharacter _player;
        private ServeFeedback _serveFeedback;
        private SupplyShelf _supplyShelf;
        private List<DataStorage.Recipe_Combine> _recipePool;

        public IReadOnlyList<PlatingOrder> PlatingOrders => _platingOrders;
        public IReadOnlyList<CustomerOrder> Orders => _orders;

        public event Action<PlatingOrder> OnOrderAdded;
        public event Action<PlatingOrder> OnOrderRemoved;

        private void Start()
        {
            _dataManager = FindFirstObjectByType<DataManager>();
            _inventory = FindFirstObjectByType<InventoryManager>();
            _player = FindFirstObjectByType<MainCharacter>();
            _supplyShelf = FindFirstObjectByType<SupplyShelf>();
            if (!TryGetComponent(out _serveFeedback))
            {
                _serveFeedback = gameObject.AddComponent<ServeFeedback>();
            }

            if (_wallet == null)
            {
                _wallet = FindFirstObjectByType<CurrencyWallet>();
            }

            if (_dayStats == null)
            {
                _dayStats = FindFirstObjectByType<DayStats>();
            }
        }

        public CustomerOrder IssueOrder(CustomerController customer)
        {
            DataStorage.Recipe_Combine recipe = PickRecipe();
            if (recipe == null)
            {
                return null;
            }

            PlatingRecipe platingRecipe = PlatingRecipe.FromRecipeRow(recipe, _timeLimit, _mistakePenalty);
            CustomerOrder order = new CustomerOrder(customer, recipe, new PlatingOrder(platingRecipe));

            _orders.Add(order);
            _platingOrders.Add(order.PlatingOrder);
            if (_dayStats != null)
            {
                _dayStats.RecordOrdered();
            }

            OnOrderAdded?.Invoke(order.PlatingOrder);
            return order;
        }

        public bool TryGetOrder(CustomerController customer, out CustomerOrder order)
        {
            order = _orders.Find(o => o.Customer == customer);
            return order != null;
        }

        /// <summary>
        /// 완성된 요리를 인벤토리에서 꺼내 손님에게 서빙하고 요리 가격을 골드로 받는다.
        /// </summary>
        public ServeResult TryServe(CustomerController customer)
        {
            if (!TryGetOrder(customer, out CustomerOrder order))
            {
                return ServeResult.NoOrder;
            }

            if (!order.PlatingOrder.servable)
            {
                return ServeResult.NotCooked;
            }

            // 담기가 요리를 등급 0으로 인벤토리에 넣는다.
            if (_inventory == null || !_inventory.RemoveItem(order.Recipe.PRODUCT_DISH_ID, 0, 1))
            {
                return ServeResult.NoDish;
            }

            int paid = 0;
            if (_wallet != null)
            {
                int previousGold = _wallet.Get(CurrencyIds.Gold);
                _wallet.Add(CurrencyIds.Gold, order.Recipe.PRICE);
                paid = _wallet.Get(CurrencyIds.Gold) - previousGold;
            }

            if (_dayStats != null)
            {
                _dayStats.RecordServed(paid);
            }

            RemoveOrder(order);
            Vector3 destination = customer.transform.position;
            Vector3 origin = _player != null ? _player.transform.position : destination;
            _serveFeedback.Play(origin, destination, paid);
            return ServeResult.Success;
        }

        /// <summary>
        /// 서빙하지 못한 채 손님이 떠날 때 주문을 없애고 놓친 손님으로 집계한다.
        /// </summary>
        public void CancelOrder(CustomerController customer)
        {
            if (!TryGetOrder(customer, out CustomerOrder order))
            {
                return;
            }

            RemoveOrder(order);
            if (_dayStats != null)
            {
                _dayStats.RecordMissed();
            }
        }

        public Sprite GetOrderIcon(CustomerOrder order)
        {
            string itemId = order.Recipe.PRODUCT_DISH_ID;
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            if (_iconCache.TryGetValue(itemId, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = ItemIcon.ResolveIcon(_dataManager != null ? _dataManager.dataStorage : null, itemId);
            if (sprite != null)
            {
                _iconCache[itemId] = sprite;
            }

            return sprite;
        }

        private void RemoveOrder(CustomerOrder order)
        {
            _orders.Remove(order);
            _platingOrders.Remove(order.PlatingOrder);
            OnOrderRemoved?.Invoke(order.PlatingOrder);
        }

        private DataStorage.Recipe_Combine PickRecipe()
        {
            if (_supplyShelf != null && _supplyShelf.isActiveAndEnabled)
            {
                IReadOnlyList<DataStorage.Recipe_Combine> starter = _supplyShelf.GetStarterRecipes();
                if (starter.Count > 0)
                {
                    return starter[UnityEngine.Random.Range(0, starter.Count)];
                }
            }

            if (_recipePool == null)
            {
                Dictionary<string, DataStorage.Recipe_Combine> table = _dataManager != null ? _dataManager.dataStorage?.Recipe_CombineData : null;
                if (table == null || table.Count == 0)
                {
                    return null;
                }

                _recipePool = new List<DataStorage.Recipe_Combine>(table.Values);
            }

            return _recipePool[UnityEngine.Random.Range(0, _recipePool.Count)];
        }
    }
}

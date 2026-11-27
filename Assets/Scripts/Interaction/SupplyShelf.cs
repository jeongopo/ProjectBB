using System;
using System.Collections.Generic;
using GamePlay;
using UnityEngine;

namespace Interaction
{
    [DisallowMultipleComponent]
    public sealed class SupplyShelf : InteractComponent
    {
        [Header("Daily Supplies")]
        [SerializeField] private string[] _starterRecipeIds = { "210800201", "210800301" };
        [SerializeField, Min(1)] private int _portionsPerRecipe = 4;

        private readonly List<DataStorage.Recipe_Combine> _recipes = new List<DataStorage.Recipe_Combine>();
        private readonly Dictionary<string, int> _dailyStock = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _remainingStock = new Dictionary<string, int>();
        private InventoryManager _inventory;
        private DataStorage _data;
        private GameClock _clock;
        private int _stockDay;
        internal event Action SuppliesReceived;

        public override bool CanInteract => base.CanInteract && _inventory != null && _data?.Recipe_CombineData != null;
        public override string PromptKey => "STR_SupplyShelf_Prompt";

        protected override void Start()
        {
            _inventory = FindFirstObjectByType<InventoryManager>();
            _data = FindFirstObjectByType<DataManager>()?.dataStorage;
            _clock = FindFirstObjectByType<GameClock>();
        }

        internal IReadOnlyList<DataStorage.Recipe_Combine> GetStarterRecipes()
        {
            BuildStockIfReady();
            return _recipes;
        }

        protected override void OnInteract()
        {
            BuildStockIfReady();
            int today = _clock != null ? _clock.Day : 1;
            if (_stockDay != today)
            {
                _stockDay = today;
                _remainingStock.Clear();
                foreach (KeyValuePair<string, int> entry in _dailyStock)
                {
                    _remainingStock.Add(entry.Key, entry.Value);
                }
            }

            int received = 0;
            foreach (KeyValuePair<string, int> entry in _dailyStock)
            {
                int remaining = _remainingStock[entry.Key];
                if (remaining > 0 && _inventory.TryAddItem(entry.Key, remaining))
                {
                    _remainingStock[entry.Key] = 0;
                    received += remaining;
                }
            }

            if (received > 0)
            {
                SuppliesReceived?.Invoke();
                SystemMessageUI.Show(string.Format(_data.GetBBStringText("STR_SupplyShelf_Received"), received));
                GameAudio.Play(SoundCue.Select);
            }
            else
            {
                bool hasStock = false;
                foreach (int amount in _remainingStock.Values)
                {
                    hasStock |= amount > 0;
                }
                SystemMessageUI.Show(_data.GetBBStringText(hasStock ? "STR_SupplyShelf_Full" : "STR_SupplyShelf_Empty"));
            }
        }

        private void BuildStockIfReady()
        {
            if (_recipes.Count > 0 || _data?.Recipe_CombineData == null)
            {
                return;
            }

            int portions = Mathf.Max(1, _portionsPerRecipe);
            foreach (string id in _starterRecipeIds)
            {
                if (string.IsNullOrEmpty(id) || !_data.Recipe_CombineData.TryGetValue(id, out DataStorage.Recipe_Combine row) ||
                    _recipes.Contains(row))
                {
                    continue;
                }

                PlatingRecipe recipe = PlatingRecipe.FromRecipeRow(row, 0f, 0f);
                if (recipe.ingredients.Count == 0)
                {
                    continue;
                }

                _recipes.Add(row);
                foreach (PlatingIngredient ingredient in recipe.ingredients)
                {
                    string stockId = IngredientPreparation.TryGetRecipe(_data, ingredient.itemID, out DataStorage.Recipe_Interaction prep)
                        ? prep.INPUT_ITEM_ID
                        : ingredient.itemID;
                    _dailyStock.TryGetValue(stockId, out int count);
                    _dailyStock[stockId] = count + ingredient.count * portions;
                }
            }
        }
    }
}

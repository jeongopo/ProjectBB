using System.Collections.Generic;
using DataEnumDefines;
using GameEnumDefines;
using GamePlay;
using Interaction;

internal sealed class IngredientPreparation
{
    private readonly DataStorage _data;
    private readonly InventoryManager _inventory;
    private readonly OrderBoard _orders;
    private readonly SupplyShelf _shelf;
    private DataStorage.Recipe_Interaction _active;

    internal IngredientPreparation(DataStorage data, InventoryManager inventory, OrderBoard orders, SupplyShelf shelf)
    {
        _data = data;
        _inventory = inventory;
        _orders = orders;
        _shelf = shelf;
    }

    internal bool TryStart(CookingType type)
    {
        Cancel();
        ENUMINTERACTIONTYPE method = type == CookingType.Cutting ? ENUMINTERACTIONTYPE.CHOP : ENUMINTERACTIONTYPE.BOIL;
        bool hasPendingOrder = false;
        if (_orders != null)
        {
            foreach (PlatingOrder order in _orders.PlatingOrders)
            {
                if (order.servable)
                {
                    continue;
                }
                hasPendingOrder = true;
                _active = Find(order.recipe, method);
                if (_active != null)
                {
                    break;
                }
            }
        }

        if (_active == null && !hasPendingOrder)
        {
            foreach (DataStorage.Recipe_Combine row in _shelf.GetStarterRecipes())
            {
                _active = Find(PlatingRecipe.FromRecipeRow(row, 0f, 0f), method);
                if (_active != null)
                {
                    break;
                }
            }
        }

        if (_active == null)
        {
            SystemMessageUI.Show(_data.GetBBStringText("STR_Prep_Unavailable"));
            return false;
        }

        SystemMessageUI.Show(string.Format(_data.GetBBStringText("STR_Prep_Target"), _data.GetBBStringText(_active.DESC)));
        return true;
    }

    internal void Complete(ENUMGRADE grade)
    {
        DataStorage.Recipe_Interaction recipe = _active;
        Cancel();
        if (recipe == null || grade < ENUMGRADE.GOOD)
        {
            return;
        }

        if (_inventory.TryConvertOne(recipe.INPUT_ITEM_ID, recipe.OUTPUT_ITEM_ID, (int)grade))
        {
            SystemMessageUI.Show(string.Format(_data.GetBBStringText("STR_Prep_Completed"), _data.GetBBStringText(recipe.DESC)));
        }
        else
        {
            SystemMessageUI.Show(_data.GetBBStringText("STR_Prep_CannotStore"));
        }
    }

    internal void Cancel() => _active = null;

    private DataStorage.Recipe_Interaction Find(PlatingRecipe recipe, ENUMINTERACTIONTYPE method)
    {
        foreach (PlatingIngredient ingredient in recipe.ingredients)
        {
            if (_inventory.GetCount(ingredient.itemID) < ingredient.count &&
                TryGetRecipe(_data, ingredient.itemID, out DataStorage.Recipe_Interaction preparation) &&
                preparation.INTERACTION_TYPE == method && _inventory.GetCount(preparation.INPUT_ITEM_ID) > 0)
            {
                return preparation;
            }
        }

        return null;
    }

    internal static bool TryGetRecipe(DataStorage data, string outputId, out DataStorage.Recipe_Interaction recipe)
    {
        recipe = null;
        Dictionary<string, DataStorage.Recipe_Interaction> table = data?.Recipe_InteractionData;
        return table != null && table.TryGetValue(outputId, out recipe) &&
            recipe.OUTPUT_ITEM_ID == outputId && !string.IsNullOrEmpty(recipe.INPUT_ITEM_ID) &&
            recipe.INPUT_ITEM_ID != "NONE";
    }
}

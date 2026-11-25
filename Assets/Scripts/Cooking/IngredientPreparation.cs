using System.Collections.Generic;
using DataEnumDefines;
using GameEnumDefines;

internal sealed class IngredientPreparation
{
    private readonly DataStorage _data;
    private readonly InventoryManager _inventory;
    private readonly Dictionary<(string ItemId, ENUMINTERACTIONTYPE Method), DataStorage.Recipe_Interaction> _recipes =
        new Dictionary<(string, ENUMINTERACTIONTYPE), DataStorage.Recipe_Interaction>();
    private DataStorage.Recipe_Interaction _active;
    private int _inputGrade;

    internal IngredientPreparation(DataStorage data, InventoryManager inventory)
    {
        _data = data;
        _inventory = inventory;
        if (_data?.Recipe_InteractionData == null)
        {
            return;
        }

        foreach (DataStorage.Recipe_Interaction recipe in _data.Recipe_InteractionData.Values)
        {
            if (IsIngredient(recipe.INPUT_ITEM_ID) && IsIngredient(recipe.OUTPUT_ITEM_ID))
            {
                _recipes.TryAdd((recipe.INPUT_ITEM_ID, recipe.INTERACTION_TYPE), recipe);
            }
        }
    }

    internal bool CanPrepare(CookingType type, SavedItemStack item) => Find(type, item.ItemId) != null;

    internal bool TryStart(CookingType type, SavedItemStack item)
    {
        Cancel();
        DataStorage.Recipe_Interaction recipe = Find(type, item.ItemId);
        if (recipe == null || _inventory.GetCount(item.ItemId, item.Grade) <= 0)
        {
            SystemMessageUI.Show(_data.GetBBStringText("STR_Prep_Unavailable"));
            return false;
        }

        _active = recipe;
        _inputGrade = item.Grade;
        SystemMessageUI.Show(string.Format(_data.GetBBStringText("STR_Prep_Target"), _data.GetBBStringText(recipe.DESC)));
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

        if (_inventory.TryConvertOne(recipe.INPUT_ITEM_ID, _inputGrade, recipe.OUTPUT_ITEM_ID, (int)grade))
        {
            SystemMessageUI.Show(string.Format(_data.GetBBStringText("STR_Prep_Completed"), _data.GetBBStringText(recipe.DESC)));
        }
        else
        {
            SystemMessageUI.Show(_data.GetBBStringText("STR_Prep_CannotStore"));
        }
    }

    internal void Cancel() => _active = null;

    private DataStorage.Recipe_Interaction Find(CookingType type, string itemId)
    {
        ENUMINTERACTIONTYPE method = type == CookingType.Cutting ? ENUMINTERACTIONTYPE.CHOP : ENUMINTERACTIONTYPE.BOIL;
        return _recipes.TryGetValue((itemId, method), out DataStorage.Recipe_Interaction recipe) ? recipe : null;
    }

    internal static bool TryGetRecipe(DataStorage data, string outputId, out DataStorage.Recipe_Interaction recipe)
    {
        recipe = null;
        Dictionary<string, DataStorage.Recipe_Interaction> table = data?.Recipe_InteractionData;
        return table != null && table.TryGetValue(outputId, out recipe) &&
            recipe.OUTPUT_ITEM_ID == outputId && IsIngredient(recipe.INPUT_ITEM_ID);
    }

    private static bool IsIngredient(string id) => !string.IsNullOrEmpty(id) && id != "NONE";
}

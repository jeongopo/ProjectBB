using System;
using System.Collections.Generic;
using System.Linq;

// 담기(Plating) 미니게임에서 쓰는 데이터 모델.
// minigame_plating.txt([plating] 시트) + recipe_combine 이 엑셀 파이프라인에 올라오면 DataStorage 로 대체될 임시 구조.
[Serializable]
public class PlatingIngredient
{
    public string itemID;
    public int count = 1;
}

[Serializable]
public class PlatingRecipe
{
    private const int MaxTotalIngredients = 5;
    private const string NoneId = "NONE";

    public string name;
    public string displayName; // 주문서 등에 보이는 이름. 비어 있으면 name 을 쓴다
    public string outputItemID; // 완성된 요리 아이템 ID. 비어 있으면 인벤토리에 넣지 않음
    public List<PlatingIngredient> ingredients = new List<PlatingIngredient>();

    [UnityEngine.Header("minigame_plating.txt")]
    public float timeLimit = 30f;        // 0이면 시간 제한 없음. 시간이 다 되면 실패
    public float mistakePenalty = 3f;    // 오답 1회당 줄어드는 제한 시간(초)

    public string DisplayText => string.IsNullOrEmpty(displayName) ? name : displayName;

    // time_limit / mistake_penalty 는 데이터에 아직 없어서 호출하는 쪽이 값을 넘긴다
    public static PlatingRecipe FromRecipeRow(DataStorage.Recipe_Combine row, float timeLimit, float mistakePenalty)
    {
        PlatingRecipe recipe = new PlatingRecipe
        {
            name = row.NAME,
            displayName = row.DISPLAY_NAME,
            outputItemID = row.PRODUCT_DISH_ID,
            timeLimit = timeLimit,
            mistakePenalty = mistakePenalty,
        };

        string[] components = { row.COMPONENT_1, row.COMPONENT_2, row.COMPONENT_3, row.COMPONENT_4, row.COMPONENT_5 };
        foreach (string itemID in components)
        {
            if (string.IsNullOrEmpty(itemID) || itemID == NoneId)
                continue;

            PlatingIngredient existing = recipe.ingredients.Find(i => i.itemID == itemID);
            if (existing != null)
                existing.count++;
            else
                recipe.ingredients.Add(new PlatingIngredient { itemID = itemID, count = 1 });
        }
        return recipe;
    }

    public int TotalIngredientCount => ingredients.Sum(i => i.count);

    public int GetRequiredCount(string itemID)
    {
        int total = 0;
        foreach (PlatingIngredient ingredient in ingredients)
        {
            if (ingredient.itemID == itemID)
                total += ingredient.count;
        }
        return total;
    }

    public IEnumerable<string> DistinctItemIDs => ingredients.Select(i => i.itemID).Distinct();

    public bool Validate(int gridCapacity, out string error)
    {
        int total = TotalIngredientCount;

        if (DistinctItemIDs.Count() == 0 || ingredients.Any(i => string.IsNullOrEmpty(i.itemID) || i.count <= 0))
            error = "재료가 비어 있거나 수량이 0 이하입니다.";
        else if (total > MaxTotalIngredients)
            error = $"재료 총 개수({total})가 최대치({MaxTotalIngredients})를 넘습니다.";
        else if (total > gridCapacity)
            error = $"재료 총 개수({total})가 격자 칸 수({gridCapacity})보다 많습니다.";
        else if (timeLimit < 0f || mistakePenalty < 0f)
            error = "time_limit 과 mistake_penalty 는 0 이상이어야 합니다.";
        else
            error = null;

        return error == null;
    }
}

// 식당 운영이 발행한 주문서. 담기는 이미 발행된 주문서만 사용한다.
public class PlatingOrder
{
    public PlatingRecipe recipe;
    public bool servable; // 완성 후 서빙 전

    public PlatingOrder(PlatingRecipe recipe)
    {
        this.recipe = recipe;
    }
}

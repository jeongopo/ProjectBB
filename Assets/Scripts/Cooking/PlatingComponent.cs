using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DataEnumDefines;
using GamePlay;

// 담기(Plating) 미니게임.
// ② 주문서 선택 → ③ 격자에서 주문에 필요한 재료를 제한 시간 안에 모두 선택 → 성공/실패
public class PlatingComponent : CookingComponent
{
    private enum Phase
    {
        None,
        OrderSelect, // ② 주문서 선택
        Select,      // ③ 격자에서 재료 선택
        Ending,      // 성공/실패 연출 중 (입력 차단)
    }

    private class GridSlot
    {
        public string itemID;
        public bool isAnswer;
        public bool picked;
    }

    private static readonly Color CellColor = Color.white;
    private static readonly Color CursorColor = new Color(1f, 0.85f, 0.2f, 0.9f);
    private static readonly Color MistakeColor = new Color(1f, 0.35f, 0.35f, 1f);
    private static readonly Color RowColor = Color.white;
    private static readonly Color RowReadyColor = new Color(0.7f, 1f, 0.7f, 1f);
    private static readonly Color RowTextColor = new Color(0.25f, 0.15f, 0.08f, 1f);
    private static readonly Color RowAimedTextColor = new Color(0.8f, 0.2f, 0.1f, 1f);

    private const float PickedIconAlpha = 0.6f;
    private const float HoldMinScale = 0.85f;
    private const float ReadyRefreshInterval = 0.2f;

    [Header("Settings")]
    [SerializeField] private int gridSize = 5;
    [SerializeField] private float holdTime = 0.5f;           // 재료를 고르기 위해 스페이스를 누르고 있어야 하는 시간
    [SerializeField] private float resultDisplayDuration = 1.5f;
    [SerializeField] private float cellSize = 60f;
    [SerializeField] private string waterItemID = "WATER";   // 미끼가 모자랄 때 칸을 채우는 물 아이템
    [SerializeField] private Sprite waterSprite;             // 물 아이템은 Item 데이터에 없으므로 아이콘을 직접 지정
    [SerializeField] private Vector4 orderListPadding = new Vector4(14f, 34f, 14f, 12f); // 좌, 상, 우, 하

    [Header("Wrong Feedback")]
    [SerializeField] private float wrongFlashDuration = 0.35f;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 10f;
    [SerializeField] private int shakeVibrato = 20;

    [Header("Prefabs (비워두면 코드로 기본 모양을 생성)")]
    [SerializeField] private PlatingCellView cellPrefab;
    [SerializeField] private PlatingCellView orderRowPrefab;

    [Header("UI")]
    [SerializeField] private RectTransform orderListRoot;
    [SerializeField] private RectTransform plateGridRoot;    // 격자 판. Image 가 있으면 오답 때 붉게 깜빡인다
    [SerializeField] private Image timeGaugeImage;           // Image Type = Filled

    [Header("Test (식당 운영/[plating] 데이터 연동 전 임시)")]
    [SerializeField] private List<PlatingRecipe> testOrders = new List<PlatingRecipe>();
    [SerializeField] private bool useDataOrdersIfEmpty = true;
    [SerializeField] private int dataOrderCount = 3;
    [SerializeField] private float defaultTimeLimit = 30f;
    [SerializeField] private float defaultMistakePenalty = 3f;
    [SerializeField] private bool grantTestIngredientsOnOpen = false;

    private DataManager dataManager;
    private InventoryManager inventory;
    private GamePlay.OrderBoard orderBoard;

    private Phase phase = Phase.None;
    private readonly List<PlatingOrder> orders = new List<PlatingOrder>();
    private readonly List<PlatingCellView> orderRows = new List<PlatingCellView>();
    private int aimIndex;
    private float readyRefreshTimer;

    private PlatingOrder nowOrder;
    private PlatingRecipe nowRecipe; // 시작한 레시피. 미니게임이 끝날 때까지 고정

    private GridSlot[] slots;
    private readonly List<PlatingCellView> cells = new List<PlatingCellView>();
    private int cursor;
    private int pickedAnswerCount;

    private bool holding;
    private float holdTimer;

    private float timeRemaining;
    private Vector2Int lastMoveDir;

    private Image boardImage;
    private Tween shakeTween;
    private Sequence flashSequence;

    private Coroutine endingRoutine;

    private readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

    // ───────────────────────── 외부 연동 ─────────────────────────

    // 식당 운영이 발행한 주문서를 넘겨준다. 미니게임을 열기 전에 호출해야 한다.
    public void SetOrders(IEnumerable<PlatingOrder> newOrders)
    {
        orders.Clear();
        if (newOrders != null)
            orders.AddRange(newOrders);
    }

    // 손님의 인내심이 바닥나 떠났을 때 식당 운영이 호출한다.
    public void NotifyCustomerLeft(PlatingOrder order)
    {
        int index = orders.IndexOf(order);
        if (index < 0)
            return;

        bool wasPlaying = order == nowOrder && phase == Phase.Select;
        orders.RemoveAt(index);

        if (wasPlaying)
        {
            SystemMessageUI.Show(GetText("STR_UI_Plating_CustomerLeft"));
            ResetPlayState();
            if (orders.Count > 0)
                EnterOrderSelect();
            else
                cookingManager?.CloseCooking();
            return;
        }

        if (phase != Phase.OrderSelect)
            return;

        if (orders.Count == 0)
        {
            cookingManager?.CloseCooking();
            return;
        }

        if (index == aimIndex)
            aimIndex = 0;
        else if (index < aimIndex)
            aimIndex--;

        RebuildOrderRows();
    }

    // 미니게임이 열려 있는 동안 새로 발행된 주문을 목록에 추가한다.
    private void HandleOrderAdded(PlatingOrder order)
    {
        if (orders.Contains(order))
            return;

        orders.Add(order);
        if (phase == Phase.OrderSelect)
            RebuildOrderRows();
    }

    private void UnsubscribeOrderBoard()
    {
        if (orderBoard == null)
            return;

        orderBoard.OnOrderAdded -= HandleOrderAdded;
        orderBoard.OnOrderRemoved -= NotifyCustomerLeft;
        orderBoard = null;
    }

    // ───────────────────────── 생명주기 ─────────────────────────

    public override void InitCooking()
    {
        base.InitCooking();

        if (cookingManager == null)
            cookingManager = FindFirstObjectByType<CookingManager>();
        dataManager = FindFirstObjectByType<DataManager>();
        inventory = FindFirstObjectByType<InventoryManager>();
        if (plateGridRoot != null)
            boardImage = plateGridRoot.GetComponent<Image>();

        // 식당 운영(OrderBoard)이 있으면 손님 주문을 쓰고, 없을 때만 임시 테스트 주문을 쓴다
        UnsubscribeOrderBoard();
        orderBoard = FindFirstObjectByType<GamePlay.OrderBoard>();
        if (orderBoard != null)
        {
            SetOrders(orderBoard.PlatingOrders);
            orderBoard.OnOrderAdded += HandleOrderAdded;
            orderBoard.OnOrderRemoved += NotifyCustomerLeft;
        }
        else if (orders.Count == 0)
        {
            LoadTestOrders();
        }

        if (grantTestIngredientsOnOpen)
            GrantTestIngredients();

        isPlaying = true;
        lastMoveDir = Vector2Int.zero;
        ResetPlayState();
        EnterOrderSelect();

        if (orders.Count == 0)
            SystemMessageUI.Show(GetText("STR_UI_Plating_NoOrder"));
    }

    public override void OnCookingEnd()
    {
        UnsubscribeOrderBoard();
        ResetPlayState();
        StopAllCoroutines();
        endingRoutine = null;
        isPlaying = false;
        phase = Phase.None;

        base.OnCookingEnd();
    }

    private void OnDisable()
    {
        KillFeedbackTweens();
    }

    protected override ENUMGRADE JudgeResult()
    {
        return ENUMGRADE.NORMAL;
    }

    protected override void EndMiniGame()
    {
        Succeed();
    }

    // ───────────────────────── 입력 ─────────────────────────

    protected override void Update()
    {
        if (!isPlaying)
            return;

        if (WasCancelPressed())
        {
            HandleCancel();
            if (!isPlaying)
                return;
        }

        base.Update(); // OnMove 호출

        switch (phase)
        {
            case Phase.OrderSelect:
                readyRefreshTimer -= Time.deltaTime;
                if (readyRefreshTimer <= 0f)
                {
                    readyRefreshTimer = ReadyRefreshInterval;
                    RefreshOrderRows();
                }
                break;

            case Phase.Select:
                UpdateHold();
                TickTime();
                break;
        }
    }

    protected override void OnMove(Vector2 moveInput)
    {
        // 방향키 한 번 누를 때마다 한 칸 이동 (눌린 채로 유지해도 반복하지 않음)
        Vector2Int dir = Vector2Int.zero;
        if (Mathf.Abs(moveInput.x) >= Mathf.Abs(moveInput.y))
        {
            if (moveInput.x > 0.5f) dir = Vector2Int.right;
            else if (moveInput.x < -0.5f) dir = Vector2Int.left;
        }
        else
        {
            if (moveInput.y > 0.5f) dir = Vector2Int.up;
            else if (moveInput.y < -0.5f) dir = Vector2Int.down;
        }

        bool pressedNow = dir != Vector2Int.zero && lastMoveDir == Vector2Int.zero;
        lastMoveDir = dir;
        if (!pressedNow)
            return;

        switch (phase)
        {
            case Phase.OrderSelect:
                MoveAim(dir);
                break;

            case Phase.Select:
                if (holding)
                    break;
                cursor = MoveCursor(cursor, gridSize, dir);
                RefreshGridUI();
                break;
        }
    }

    protected override void Interact()
    {
        switch (phase)
        {
            case Phase.OrderSelect:
                TryFire();
                break;

            case Phase.Select:
                if (slots[cursor].picked)
                    break;
                holding = true;
                holdTimer = 0f;
                break;
        }
    }

    private static bool WasCancelPressed()
    {
        return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
    }

    private void HandleCancel()
    {
        switch (phase)
        {
            case Phase.OrderSelect:
                cookingManager?.CloseCooking();
                break;

            case Phase.Select:
                ResetPlayState();
                EnterOrderSelect();
                break;
        }
    }

    // ───────────────────────── ② 주문서 선택 ─────────────────────────

    private void EnterOrderSelect()
    {
        phase = Phase.OrderSelect;
        aimIndex = Mathf.Clamp(aimIndex, 0, Mathf.Max(0, orders.Count - 1));
        readyRefreshTimer = ReadyRefreshInterval;

        SetVisible(plateGridRoot, false);
        if (timeGaugeImage != null)
            timeGaugeImage.gameObject.SetActive(false);

        RebuildOrderRows();
    }

    private void MoveAim(Vector2Int dir)
    {
        if (orders.Count <= 1)
            return;

        int step = (dir == Vector2Int.right || dir == Vector2Int.down) ? 1 : -1;
        aimIndex = (aimIndex + step + orders.Count) % orders.Count;
        RefreshOrderRows();
    }

    private void TryFire()
    {
        if (orders.Count == 0)
        {
            SystemMessageUI.Show(GetText("STR_UI_Plating_NoOrder"));
            return;
        }

        PlatingOrder order = orders[aimIndex];
        if (order.servable)
            return;

        if (!order.recipe.Validate(gridSize * gridSize, out string error))
        {
            Debug.LogError($"PlatingComponent: '{order.recipe.name}' 설정 오류 - {error}");
            SystemMessageUI.Show(GetText("STR_UI_Plating_RecipeError"));
            return;
        }

        Dictionary<string, int> shortages = GetShortages(order.recipe);
        if (shortages.Count > 0)
        {
            KeyValuePair<string, int> first = shortages.First();
            string textId = shortages.Count > 1 ? "STR_UI_Plating_ShortageMulti" : "STR_UI_Plating_Shortage";
            SystemMessageUI.Show(string.Format(GetText(textId), GetItemName(first.Key), first.Value, shortages.Count - 1));
            return;
        }

        StartPlating(order);
    }

    // 보유 수량이 필요 수량보다 적은 재료 → 부족한 개수
    private Dictionary<string, int> GetShortages(PlatingRecipe recipe)
    {
        Dictionary<string, int> shortages = new Dictionary<string, int>();
        foreach (string itemID in recipe.DistinctItemIDs)
        {
            int owned = inventory != null ? inventory.GetCount(itemID) : 0;
            int required = recipe.GetRequiredCount(itemID);
            if (owned < required)
                shortages[itemID] = required - owned;
        }
        return shortages;
    }

    private void RebuildOrderRows()
    {
        foreach (PlatingCellView row in orderRows)
            Destroy(row.gameObject);
        orderRows.Clear();

        if (orderListRoot == null)
            return;

        for (int i = 0; i < orders.Count; i++)
        {
            PlatingCellView row = CreateCell(orderListRoot, orderRowPrefab);
            if (orderRowPrefab == null)
            {
                row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 32f);
                row.Icon.gameObject.SetActive(false);
                row.Label.alignment = TextAlignmentOptions.MidlineLeft;
                row.Label.fontSize = 18f;
                RectTransform labelRect = row.Label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8f, 0f);
                labelRect.offsetMax = new Vector2(-8f, 0f);
            }
            orderRows.Add(row);
        }

        EnsureVerticalLayout(orderListRoot);
        RefreshOrderRows();
    }

    private void RefreshOrderRows()
    {
        for (int i = 0; i < orderRows.Count && i < orders.Count; i++)
        {
            PlatingOrder order = orders[i];
            bool aimed = i == aimIndex;
            bool ready = !order.servable && GetShortages(order.recipe).Count == 0;

            string text = order.recipe.DisplayText;
            if (order.servable) text += " [서빙 가능]";
            if (aimed) text = "▶ " + text;

            orderRows[i].Label.text = text;
            orderRows[i].Background.color = ready ? RowReadyColor : RowColor;
            orderRows[i].Label.color = aimed ? RowAimedTextColor : RowTextColor;
        }
    }

    // ───────────────────────── ③ 재료 선택 ─────────────────────────

    private void StartPlating(PlatingOrder order)
    {
        ResetPlayState();

        nowOrder = order;
        nowRecipe = order.recipe;

        slots = BuildSlots();
        BuildGrid(plateGridRoot, gridSize, cells);
        cursor = slots.Length / 2;
        pickedAnswerCount = 0;

        timeRemaining = nowRecipe.timeLimit;
        holding = false;

        SetVisible(plateGridRoot, true);
        if (timeGaugeImage != null)
            timeGaugeImage.gameObject.SetActive(nowRecipe.timeLimit > 0f);

        phase = Phase.Select;
        RefreshGridUI();
        RefreshTimeUI();
    }

    // 필요 재료(수량만큼) + 미끼(레시피에 없는 재료, 중복 없음) + 모자라면 물. 위치는 무작위
    private GridSlot[] BuildSlots()
    {
        int capacity = gridSize * gridSize;
        List<GridSlot> list = new List<GridSlot>();

        foreach (PlatingIngredient ingredient in nowRecipe.ingredients)
        {
            for (int i = 0; i < ingredient.count; i++)
                list.Add(new GridSlot { itemID = ingredient.itemID, isAnswer = true });
        }

        HashSet<string> neededIDs = new HashSet<string>(nowRecipe.DistinctItemIDs);
        List<string> baitPool = new List<string>();
        Dictionary<string, DataStorage.Item> itemRows = dataManager != null ? dataManager.dataStorage?.ItemData : null;
        if (itemRows != null)
        {
            HashSet<string> answerIcons = new HashSet<string>();
            foreach (string itemId in neededIDs)
            {
                if (itemRows.TryGetValue(itemId, out DataStorage.Item item) && !string.IsNullOrEmpty(item.ICONPATH))
                {
                    answerIcons.Add(item.ICONPATH);
                }
            }

            // 원재료 아이콘을 재사용하는 손질 재료와 원재료를 한 격자에 내면 구분할 수 없다.
            foreach (KeyValuePair<string, DataStorage.Item> row in itemRows)
            {
                if (!neededIDs.Contains(row.Key) && row.Key != waterItemID && !answerIcons.Contains(row.Value.ICONPATH))
                {
                    baitPool.Add(row.Key);
                }
            }
        }
        Shuffle(baitPool);

        foreach (string baitID in baitPool)
        {
            if (list.Count >= capacity)
                break;
            list.Add(new GridSlot { itemID = baitID });
        }

        while (list.Count < capacity)
            list.Add(new GridSlot { itemID = waterItemID });

        Shuffle(list);
        return list.ToArray();
    }

    private void UpdateHold()
    {
        if (!holding)
            return;

        if (interactAction == null || !interactAction.IsPressed())
        {
            CancelHold();
            return;
        }

        holdTimer += Time.deltaTime;
        float t = Mathf.Clamp01(holdTimer / holdTime);
        SetCellScale(cursor, Mathf.Lerp(1f, HoldMinScale, t));

        if (holdTimer >= holdTime)
        {
            CancelHold();
            ResolvePick();
        }
    }

    private void CancelHold()
    {
        holding = false;
        holdTimer = 0f;
        SetCellScale(cursor, 1f);
    }

    private void SetCellScale(int index, float scale)
    {
        if (index >= 0 && index < cells.Count)
            cells[index].transform.localScale = Vector3.one * scale;
    }

    private void ResolvePick()
    {
        GridSlot slot = slots[cursor];
        if (slot.picked)
            return;

        if (!slot.isAnswer)
        {
            RegisterMistake();
            return;
        }

        slot.picked = true;
        pickedAnswerCount++;
        RefreshGridUI();

        if (pickedAnswerCount >= nowRecipe.TotalIngredientCount)
            Succeed();
    }

    // 오답: 제한 시간만 줄어들고 화면이 흔들리며 붉게 깜빡인다
    private void RegisterMistake()
    {
        if (nowRecipe.timeLimit > 0f)
            timeRemaining = Mathf.Max(0f, timeRemaining - nowRecipe.mistakePenalty);

        PlayWrongFeedback(cursor);
        RefreshTimeUI();

        if (nowRecipe.timeLimit > 0f && timeRemaining <= 0f)
            Fail();
    }

    private void TickTime()
    {
        if (nowRecipe.timeLimit <= 0f)
            return;

        timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
        RefreshTimeUI();

        if (timeRemaining <= 0f)
            Fail();
    }

    // ───────────────────────── 결과 ─────────────────────────

    private void Succeed()
    {
        if (inventory == null || !inventory.TryCraft(nowRecipe))
        {
            GameAudio.Play(SoundCue.CookingMiss);
            BeginEnding();
            SystemMessageUI.Show(GetText("STR_Prep_CannotStore"));
            endingRoutine = StartCoroutine(EndingRoutine());
            return;
        }

        GameAudio.Play(SoundCue.CookingSuccess);
        BeginEnding();

        nowOrder.servable = true;
        SystemMessageUI.Show(GetText("STR_UI_Plating_Success"));
        endingRoutine = StartCoroutine(EndingRoutine());
    }

    private void Fail()
    {
        GameAudio.Play(SoundCue.CookingMiss);
        BeginEnding();

        SystemMessageUI.Show(GetText("STR_UI_Plating_Fail"));
        endingRoutine = StartCoroutine(EndingRoutine());
    }

    private void BeginEnding()
    {
        phase = Phase.Ending;
        isPlaying = false;
        CancelHold();
        RefreshGridUI();

        if (endingRoutine != null)
            StopCoroutine(endingRoutine);
    }

    private IEnumerator EndingRoutine()
    {
        yield return new WaitForSeconds(resultDisplayDuration);
        endingRoutine = null;

        ResetPlayState();
        cookingManager?.CloseCooking();
    }

    private void ResetPlayState()
    {
        KillFeedbackTweens();

        nowOrder = null;
        nowRecipe = null;
        slots = null;
        holding = false;
        holdTimer = 0f;
        timeRemaining = 0f;
        pickedAnswerCount = 0;

        if (boardImage != null)
            boardImage.color = Color.white;
    }

    // ───────────────────────── UI ─────────────────────────

    private void RefreshGridUI()
    {
        if (slots == null)
            return;

        for (int i = 0; i < cells.Count && i < slots.Length; i++)
        {
            GridSlot slot = slots[i];
            PlatingCellView cell = cells[i];

            SetIcon(cell, slot.itemID, slot.picked ? PickedIconAlpha : 1f);
            cell.Label.text = string.Empty;
            cell.SetCheck(slot.picked);
            ApplyCursor(cell, phase == Phase.Select && i == cursor);
        }
    }

    private void RefreshTimeUI()
    {
        if (timeGaugeImage == null || nowRecipe == null)
            return;

        timeGaugeImage.fillAmount = nowRecipe.timeLimit > 0f ? timeRemaining / nowRecipe.timeLimit : 1f;
    }

    // 커서 프레임 이미지가 있으면 프레임을 켜고, 없으면 배경색으로 표시한다
    private static void ApplyCursor(PlatingCellView cell, bool isCursor)
    {
        cell.SetCursor(isCursor);
        cell.Background.color = isCursor && !cell.HasCursorFrame ? CursorColor : CellColor;
    }

    private void PlayWrongFeedback(int cellIndex)
    {
        KillFeedbackTweens();

        if (plateGridRoot != null)
        {
            shakeTween = plateGridRoot.DOShakeAnchorPos(shakeDuration, shakeStrength, shakeVibrato).SetLink(gameObject);
        }

        flashSequence = DOTween.Sequence().SetLink(gameObject);
        if (boardImage != null)
        {
            boardImage.color = MistakeColor;
            flashSequence.Join(boardImage.DOColor(Color.white, wrongFlashDuration));
        }
        if (cellIndex >= 0 && cellIndex < cells.Count)
        {
            Image cellBackground = cells[cellIndex].Background;
            cellBackground.color = MistakeColor;
            cells[cellIndex].PlayWrong(wrongFlashDuration);
            flashSequence.Join(cellBackground.DOColor(CellColor, wrongFlashDuration));
        }
    }

    // 흔들림 도중에 끝나도 격자가 제자리로 돌아오도록 완료 처리(complete)로 정리한다
    private void KillFeedbackTweens()
    {
        if (shakeTween != null && shakeTween.IsActive())
            shakeTween.Kill(true);
        shakeTween = null;

        if (flashSequence != null && flashSequence.IsActive())
            flashSequence.Kill();
        flashSequence = null;
    }

    private void SetIcon(PlatingCellView cell, string itemID, float alpha)
    {
        Sprite sprite = GetIconSprite(itemID);
        cell.Icon.sprite = sprite;
        cell.Icon.color = new Color(1f, 1f, 1f, alpha);
        cell.Icon.gameObject.SetActive(sprite != null);
    }

    private Sprite GetIconSprite(string itemID)
    {
        if (iconCache.TryGetValue(itemID, out Sprite cached))
            return cached;

        Sprite sprite = null;
        if (itemID == waterItemID)
        {
            sprite = waterSprite;
        }
        else if (dataManager != null && dataManager.dataStorage?.ItemData != null
            && dataManager.dataStorage.ItemData.TryGetValue(itemID, out DataStorage.Item item))
        {
            sprite = ItemIcon.LoadIconSprite(item.ICONPATH);
        }

        iconCache[itemID] = sprite;
        return sprite;
    }

    private string GetText(string bbStringId)
    {
        return dataManager != null ? dataManager.dataStorage.GetBBStringText(bbStringId) : bbStringId;
    }

    private string GetItemName(string itemID)
    {
        if (itemID == waterItemID)
            return "물";

        if (IngredientPreparation.TryGetRecipe(dataManager != null ? dataManager.dataStorage : null, itemID,
            out DataStorage.Recipe_Interaction preparation))
        {
            return GetText(preparation.DESC);
        }

        if (dataManager != null && dataManager.dataStorage?.ItemData != null
            && dataManager.dataStorage.ItemData.TryGetValue(itemID, out DataStorage.Item item))
        {
            return item.NAME;
        }

        return itemID;
    }

    // ───────────────────────── UI 생성 헬퍼 ─────────────────────────

    private static void SetVisible(RectTransform root, bool visible)
    {
        if (root != null)
            root.gameObject.SetActive(visible);
    }

    private void BuildGrid(RectTransform root, int size, List<PlatingCellView> targetCells)
    {
        foreach (PlatingCellView cell in targetCells)
            Destroy(cell.gameObject);
        targetCells.Clear();

        if (root == null)
            return;

        GridLayoutGroup grid = root.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = root.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = new Vector2(4f, 4f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = size;

        for (int i = 0; i < size * size; i++)
            targetCells.Add(CreateCell(root, cellPrefab));
    }

    private void EnsureVerticalLayout(RectTransform root)
    {
        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset((int)orderListPadding.x, (int)orderListPadding.z, (int)orderListPadding.y, (int)orderListPadding.w);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private PlatingCellView CreateCell(RectTransform parent, PlatingCellView prefab)
    {
        if (prefab != null)
        {
            return Instantiate(prefab, parent, false);
        }

        GameObject root = new GameObject("PlatingCell", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);

        Image background = root.GetComponent<Image>();
        background.color = CellColor;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(root.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.gameObject.SetActive(false);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(root.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = 12f;
        label.alignment = TextAlignmentOptions.Bottom;
        label.color = RowTextColor;
        label.raycastTarget = false;
        label.text = string.Empty;

        PlatingCellView view = root.AddComponent<PlatingCellView>();
        view.Setup(background, icon, label);
        return view;
    }

    // ───────────────────────── 유틸 ─────────────────────────

    // up/down 은 같은 열, left/right 는 같은 행 안에서 반대편 끝으로 순환 (다음 행으로 넘어가지 않음)
    private static int MoveCursor(int current, int size, Vector2Int dir)
    {
        int col = current % size;
        int row = current / size;
        col = (col + dir.x + size) % size;
        row = (row - dir.y + size) % size;
        return row * size + col;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ───────────────────────── 임시 테스트 데이터 ─────────────────────────

    private void LoadTestOrders()
    {
        orders.Clear();

        List<PlatingRecipe> source = testOrders;
        if ((source == null || source.Count == 0) && useDataOrdersIfEmpty)
            source = BuildRecipesFromData();

        if (source == null)
            return;

        foreach (PlatingRecipe recipe in source)
            orders.Add(new PlatingOrder(recipe));
    }

    // 식당 운영이 주문을 발행하기 전까지 Recipe_Combine 에서 무작위로 몇 개를 주문서로 쓴다
    private List<PlatingRecipe> BuildRecipesFromData()
    {
        Dictionary<string, DataStorage.Recipe_Combine> table = dataManager != null ? dataManager.dataStorage?.Recipe_CombineData : null;
        if (table == null || table.Count == 0)
        {
            Debug.LogWarning("PlatingComponent: Recipe_Combine 데이터가 없어 주문을 만들 수 없습니다. testOrders 를 직접 채워주세요.");
            return null;
        }

        List<DataStorage.Recipe_Combine> rows = table.Values.ToList();
        Shuffle(rows);

        List<PlatingRecipe> recipes = new List<PlatingRecipe>();
        foreach (DataStorage.Recipe_Combine row in rows.Take(dataOrderCount))
            recipes.Add(ToPlatingRecipe(row));
        return recipes;
    }

    private PlatingRecipe ToPlatingRecipe(DataStorage.Recipe_Combine row)
    {
        return PlatingRecipe.FromRecipeRow(row, defaultTimeLimit, defaultMistakePenalty);
    }

    // 테스트용: 주문에 필요한 재료를 인벤토리에 지급
    private void GrantTestIngredients()
    {
        if (inventory == null)
            return;

        foreach (PlatingOrder order in orders)
        {
            foreach (string itemID in order.recipe.DistinctItemIDs)
            {
                inventory.AddItem(itemID, 3);
            }
        }
    }
}

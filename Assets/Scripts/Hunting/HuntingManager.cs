using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameEnumDefines;

public class HuntingManager : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject huntingPanel;
    [SerializeField] private Button closeButton;

    [Header("Select Panel")]
    [SerializeField] private GameObject selectPanel;
    [SerializeField] private Transform selectListParent;
    [SerializeField] private HuntingSelectItem selectItemPrefab;

    [Header("Progress Panel")]
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Transform itemListParent;
    [SerializeField] private ItemIcon itemIconPrefab;
    [SerializeField] private Image progressImage;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Transform resultGridParent;
    [SerializeField] private Button returnButton;

    private const string ProgressHitSpritePath = "Art/Hunting/Hunting_SceneHit";
    private const string ProgressNormalSpritePath = "Art/Hunting/Hunting_SceneNormal";
    private const float ProgressHitDisplayDuration = 0.3f;

    private DataManager dataManager;
    private GamePlay.InputManager inputManager;
    private readonly List<ItemIcon> spawnedIcons = new List<ItemIcon>();
    private readonly List<HuntingSelectItem> spawnedSelectItems = new List<HuntingSelectItem>();
    private readonly List<ItemIcon> spawnedResultIcons = new List<ItemIcon>();
    private Coroutine battlePhaseCoroutine;
    private Coroutine progressImageCoroutine;
    private Sprite progressHitSprite;
    private Sprite progressNormalSprite;
    private string currentHuntingID;

    private void Awake()
    {
        dataManager = FindFirstObjectByType<DataManager>();
        inputManager = FindFirstObjectByType<GamePlay.InputManager>();

        if (huntingPanel != null)
            huntingPanel.SetActive(false);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseHunting);
        if (returnButton != null)
            returnButton.onClick.AddListener(ReturnToSelect);

        progressHitSprite = Resources.Load<Sprite>(ProgressHitSpritePath);
        progressNormalSprite = Resources.Load<Sprite>(ProgressNormalSpritePath);
        if (progressImage != null)
            progressImage.sprite = progressNormalSprite;
    }

    public void OpenHunting()
    {
        StopBattlePhase();
        ShowSelectPanel();

        if (huntingPanel != null)
            huntingPanel.SetActive(true);

        if (inputManager != null)
            inputManager.SwitchInputState(InputState.UI);
    }

    public void CloseHunting()
    {
        StopBattlePhase();

        if (huntingPanel != null)
            huntingPanel.SetActive(false);

        if (inputManager != null)
            inputManager.SwitchInputState(InputState.Default);
    }

    private void ShowSelectPanel()
    {
        if (progressPanel != null)
            progressPanel.SetActive(false);
        if (resultPanel != null)
            resultPanel.SetActive(false);
        if (selectPanel != null)
            selectPanel.SetActive(true);

        PopulateSelectList();
    }

    private void ReturnToSelect()
    {
        StopBattlePhase();
        ShowSelectPanel();
    }

    private void PopulateSelectList()
    {
        foreach (var item in spawnedSelectItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        spawnedSelectItems.Clear();

        if (selectListParent == null || selectItemPrefab == null || dataManager?.dataStorage.HuntingData == null)
            return;

        foreach (var huntingRow in dataManager.dataStorage.HuntingData.Values)
        {
            HuntingSelectItem item = Instantiate(selectItemPrefab, selectListParent);
            item.SetData(huntingRow.ID, huntingRow.NAME_K, SelectHunting);
            spawnedSelectItems.Add(item);
        }
    }

    private void SelectHunting(string huntingID)
    {
        currentHuntingID = huntingID;

        if (!TryGetCurrentHuntingRow(out var huntingRow))
        {
            Debug.LogWarning($"HuntingManager: Hunting data not found for ID {currentHuntingID}");
            return;
        }

        if (selectPanel != null)
            selectPanel.SetActive(false);
        if (progressPanel != null)
            progressPanel.SetActive(true);
        if (progressSlider != null)
            progressSlider.value = 0f;

        PrepareItemIcons(huntingRow.TOTAL_BATTLE_PHASE);

        battlePhaseCoroutine = StartCoroutine(BattlePhaseRoutine(huntingRow));
    }

    private void PrepareItemIcons(int count)
    {
        foreach (var icon in spawnedIcons)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }
        spawnedIcons.Clear();

        if (itemListParent == null || itemIconPrefab == null)
            return;

        for (int i = 0; i < count; i++)
        {
            ItemIcon icon = Instantiate(itemIconPrefab, itemListParent);
            icon.Hide();
            spawnedIcons.Add(icon);
        }
    }

    private IEnumerator BattlePhaseRoutine(DataStorage.Hunting huntingRow)
    {
        for (int i = 0; i < spawnedIcons.Count; i++)
        {
            yield return new WaitForSeconds(1f);

            if (TryRollDrop(huntingRow.DROP_ID, out string itemID, out int dropCount))
            {
                Debug.Log($"HuntingManager: Phase {i + 1}/{spawnedIcons.Count} drop -> {itemID} x{dropCount}");
                spawnedIcons[i].SetItem(itemID, dropCount);
                spawnedIcons[i].Show();
                PlayProgressHitEffect();
            }

            if (progressSlider != null)
                progressSlider.value = (float)(i + 1) / spawnedIcons.Count;
        }

        battlePhaseCoroutine = null;
        ShowResultPanel();
    }

    private void ShowResultPanel()
    {
        if (progressPanel != null)
            progressPanel.SetActive(false);
        if (resultPanel != null)
            resultPanel.SetActive(true);

        PopulateResultGrid();
    }

    private void PopulateResultGrid()
    {
        foreach (var icon in spawnedResultIcons)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }
        spawnedResultIcons.Clear();

        if (resultGridParent == null || itemIconPrefab == null)
            return;

        var aggregatedCounts = new Dictionary<string, int>();
        var orderedItemIDs = new List<string>();

        foreach (var icon in spawnedIcons)
        {
            if (icon == null || !icon.gameObject.activeSelf)
                continue;

            if (!aggregatedCounts.ContainsKey(icon.ItemID))
            {
                aggregatedCounts[icon.ItemID] = 0;
                orderedItemIDs.Add(icon.ItemID);
            }
            aggregatedCounts[icon.ItemID] += icon.Count;
        }

        foreach (var itemID in orderedItemIDs)
        {
            ItemIcon resultIcon = Instantiate(itemIconPrefab, resultGridParent);
            resultIcon.SetItem(itemID, aggregatedCounts[itemID]);
            resultIcon.Show();
            spawnedResultIcons.Add(resultIcon);
        }
    }

    private void StopBattlePhase()
    {
        if (battlePhaseCoroutine != null)
        {
            StopCoroutine(battlePhaseCoroutine);
            battlePhaseCoroutine = null;
        }

        if (progressImageCoroutine != null)
        {
            StopCoroutine(progressImageCoroutine);
            progressImageCoroutine = null;
        }

        if (progressImage != null)
            progressImage.sprite = progressNormalSprite;
    }

    private void PlayProgressHitEffect()
    {
        if (progressImage == null || progressHitSprite == null)
            return;

        if (progressImageCoroutine != null)
            StopCoroutine(progressImageCoroutine);

        progressImageCoroutine = StartCoroutine(ProgressHitRoutine());
    }

    private IEnumerator ProgressHitRoutine()
    {
        progressImage.sprite = progressHitSprite;
        yield return new WaitForSeconds(ProgressHitDisplayDuration);
        progressImage.sprite = progressNormalSprite;
        progressImageCoroutine = null;
    }

    private bool TryGetCurrentHuntingRow(out DataStorage.Hunting huntingRow)
    {
        huntingRow = null;
        return dataManager != null
            && dataManager.dataStorage.HuntingData != null
            && dataManager.dataStorage.HuntingData.TryGetValue(currentHuntingID, out huntingRow);
    }

    // dropIDs에 해당하는 Drop 데이터들의 RATE 비율로 하나를 뽑고, MIN~MAX 사이 개수를 정함
    private bool TryRollDrop(string[] dropIDs, out string itemID, out int count)
    {
        itemID = null;
        count = 0;

        if (dropIDs == null || dropIDs.Length == 0 || dataManager?.dataStorage.DropData == null)
            return false;

        var candidates = new List<DataStorage.Drop>();
        float totalRate = 0f;
        foreach (var dropID in dropIDs)
        {
            if (dataManager.dataStorage.DropData.TryGetValue(dropID, out var dropRow))
            {
                candidates.Add(dropRow);
                totalRate += dropRow.RATE;
            }
        }

        if (candidates.Count == 0 || totalRate <= 0f)
            return false;

        float roll = Random.Range(0f, totalRate);
        float cumulative = 0f;
        DataStorage.Drop chosen = candidates[candidates.Count - 1];
        foreach (var candidate in candidates)
        {
            cumulative += candidate.RATE;
            if (roll <= cumulative)
            {
                chosen = candidate;
                break;
            }
        }

        itemID = chosen.ITEMID;
        count = Random.Range(chosen.MIN, chosen.MAX + 1);

        string candidateLog = string.Join(", ", candidates.ConvertAll(c => $"{c.ID}({c.RATE}/{totalRate})"));
        Debug.Log($"HuntingManager: RollDrop candidates=[{candidateLog}] roll={roll:F2} -> chosen={chosen.ID}({chosen.ITEMID})");

        return true;
    }
}

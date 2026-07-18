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
    [SerializeField] private Button startButton;
    [SerializeField] private Slider progressSlider;

    [Header("Item Icons")]
    [SerializeField] private Transform itemListParent;
    [SerializeField] private ItemIcon itemIconPrefab;

    [Header("Test Data")]
    [SerializeField] private string testHuntingID = "Hunting1";

    private DataManager dataManager;
    private GamePlay.InputManager inputManager;
    private readonly List<ItemIcon> spawnedIcons = new List<ItemIcon>();
    private Coroutine battlePhaseCoroutine;
    private string currentHuntingID;

    private void Awake()
    {
        dataManager = FindFirstObjectByType<DataManager>();
        inputManager = FindFirstObjectByType<GamePlay.InputManager>();

        if (huntingPanel != null)
            huntingPanel.SetActive(false);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseHunting);
        if (startButton != null)
            startButton.onClick.AddListener(StartHunting);
    }

    public void OpenHunting(string huntingID = null)
    {
        currentHuntingID = string.IsNullOrEmpty(huntingID) ? testHuntingID : huntingID;

        if (!TryGetCurrentHuntingRow(out var huntingRow))
        {
            Debug.LogWarning($"HuntingManager: Hunting data not found for ID {currentHuntingID}");
            return;
        }

        StopBattlePhase();
        PrepareItemIcons(huntingRow.TOTAL_BATTLE_PHASE);

        if (startButton != null)
            startButton.gameObject.SetActive(true);
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

    private void StartHunting()
    {
        if (!TryGetCurrentHuntingRow(out var huntingRow))
            return;

        if (startButton != null)
            startButton.gameObject.SetActive(false);
        if (progressSlider != null)
            progressSlider.value = 0f;

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
            }

            if (progressSlider != null)
                progressSlider.value = (float)(i + 1) / spawnedIcons.Count;
        }

        battlePhaseCoroutine = null;

        if (startButton != null)
            startButton.gameObject.SetActive(true);
    }

    private void StopBattlePhase()
    {
        if (battlePhaseCoroutine != null)
        {
            StopCoroutine(battlePhaseCoroutine);
            battlePhaseCoroutine = null;
        }
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

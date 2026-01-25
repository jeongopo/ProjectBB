using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using System;

public class CuttingComponent : MonoBehaviour
{
    public Image[] cuttingParams; // CuttingParam1, 2, 3 배열
    public Image cuttingParamBar;
    public Button cuttingStartButton;

    private int[] cuttingRange;

    private RectTransform componentRect;
    private RectTransform barRect;
    private float baseWidth;
    private float baseValue;
    private float speed = 200f; // 바 이동 속도 (조정 가능)
    private bool movingRight = true;
    private bool isPlaying = false;
    
    [SerializeField] private string TestDataID = "Boiling_easy_01";

    void Start()
    {
        componentRect = GetComponent<RectTransform>();
        barRect = cuttingParamBar.GetComponent<RectTransform>();
        baseWidth = componentRect.rect.width;
        baseValue = baseWidth / 100;

        cuttingStartButton.onClick.AddListener(StartMiniGame);       
    }

    void InitCooking()
    {
        if(FindFirstObjectByType<DataManager>().dataStorage.Minigame_CuttingData.ContainsKey(TestDataID))
        {
            Debug.Log("Found TestDataID in DataManager");
        }

        cuttingRange = FindFirstObjectByType<DataManager>().dataStorage.Minigame_CuttingData[TestDataID].CUTTING_RANGE;

        for (int i = 0; i < cuttingParams.Length; i++)
        {
            cuttingParams[i].rectTransform.sizeDelta = new Vector2(cuttingRange[i] * baseValue, cuttingParams[i].rectTransform.sizeDelta.y);
        }
    }

    public void StartMiniGame()
    {
        InitCooking();

        isPlaying = true;
        barRect.localPosition = new Vector3(-baseWidth / 2, barRect.localPosition.y, 0);

        FindFirstObjectByType<InputManager>().OnInteractPressed += Interact;
    }

    public void EndMiniGame()
    {   
        FindFirstObjectByType<InputManager>().OnInteractPressed -= Interact;
        isPlaying = false;
    }

    void Interact()
    {
        if (isPlaying)
        {
            JudgeResult(); 
        }
    }

    void Update()
    {
        if (isPlaying)
        {
            // 바 좌우 이동
            float move = speed * Time.deltaTime * (movingRight ? 1 : -1);
            barRect.localPosition += new Vector3(move, 0, 0);

            // 방향 전환: CuttingComponent 너비 내에서
            if (barRect.localPosition.x >= baseWidth / 2)
            {
                movingRight = false;
            }
            else if (barRect.localPosition.x <= -baseWidth / 2)
            {
                movingRight = true;
            }
        }
    }

    // 결과 판정
    private void JudgeResult()
    {
        float barX = barRect.localPosition.x;
        float currentRange = 0f;

        for (int i = 0; i < cuttingRange.Length; i++)
        {
            currentRange = cuttingRange[i] * baseValue / 2;
            if (Math.Abs(barX) <= currentRange)
            {
                Debug.Log($"결과: CuttingParam{i+1} 범위");
                break;
            }
        }
        
        EndMiniGame();
    }
}
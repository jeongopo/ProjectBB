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


    [SerializeField] private float SliderSpeed = 1.0f; // 바 이동 속도 조절 변수

    private RectTransform componentRect;
    private RectTransform barRect;
    private float baseWidth;
    private float baseValue;
    private float speed = 200f; // 바 이동 속도 (조정 가능)
    private bool movingRight = true;
    private bool isPlaying = false;
    private int currentCycle = 0;

    //Data
    private int maxCuttingCycle = 10;
    private int[] cuttingRange;
    DataStorage.Minigame_Cutting currentCuttingData;
    
    
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

        currentCuttingData = FindFirstObjectByType<DataManager>().dataStorage.Minigame_CuttingData[TestDataID];
        cuttingRange = currentCuttingData.CUTTING_RANGE;
        maxCuttingCycle = currentCuttingData.CUTTING_CYCLES;

        for (int i = 0; i < cuttingParams.Length; i++)
        {
            cuttingParams[i].rectTransform.sizeDelta = new Vector2(cuttingRange[i] * baseValue, cuttingParams[i].rectTransform.sizeDelta.y);
        }

        barRect.localPosition = new Vector3(0,0, 0);
        movingRight = false; // 좌측부터 시작
        currentCycle = 0;
    }

    public void StartMiniGame()
    {
        InitCooking();

        isPlaying = true;

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
            float t = Mathf.Abs(barRect.localPosition.x) / (baseWidth / 2);
            float multiplier = Mathf.Sin(t * Mathf.PI);
            float move = speed * Time.deltaTime * (movingRight ? 1 : -1);
            barRect.localPosition += new Vector3(move, 0, 0);

            if (barRect.localPosition.x >= baseWidth / 2)
            {
                movingRight = false;
            }
            else if (barRect.localPosition.x <= -baseWidth / 2)
            {
                if(!movingRight)
                {
                    movingRight = true;
                    currentCycle++;

                    if( currentCycle >= maxCuttingCycle )
                    {
                        Debug.Log("최대 사이클 도달 - 미니게임 종료");
                        //@TODO 실패처리
                        EndMiniGame();
                    }
                }
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
                //@TODO 결과처리
                break;
            }
        }
        
        EndMiniGame();
    }
}
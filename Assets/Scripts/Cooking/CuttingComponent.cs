using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using System;
using Unity.VisualScripting;
using System.Data.Common;
using DataEnumDefines;

public class CuttingComponent : CookingComponent
{
    public Image[] cuttingParams; // CuttingParam1, 2, 3 배열
    public Image cuttingParamBar;

    [SerializeField] private float SliderSpeed = 1.0f; // 바 이동 속도 조절 변수

    private RectTransform componentRect;
    private RectTransform barRect;
    private float baseWidth;
    private float baseValue;
    private float speed = 200f; // 바 이동 속도 (조정 가능)
    private bool movingRight = true;
    private int currentCycle = 0;

    //Data
    private int maxCuttingCycle = 10;
    private int[] cuttingRange;
    DataStorage.Minigame_Cutting currentCuttingData;

    private int[] results;
    int totalAttempts = 0;
    
    
    [SerializeField] private string TestDataID = "Boiling_easy_01";

    protected override void Start()
    {
        componentRect = GetComponent<RectTransform>();
        barRect = cuttingParamBar.GetComponent<RectTransform>();
        baseWidth = componentRect.rect.width;
        baseValue = baseWidth / 100;

        base.Start();
        
        results = new int[(int)ENUMGRADE.GREAT + 1];
    }

    protected override void InitCooking()
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

        barRect.localPosition = new Vector3(-baseWidth / 2, barRect.localPosition.y, barRect.localPosition.z);
        movingRight = false; // 좌측부터 시작
        currentCycle = 0;

        Array.Clear(results, 0, results.Length);
        totalAttempts = 0;
    }

    protected override void EndMiniGame()
    {   
        OnGameEnd();
    }

    protected override void Interact()
    {
        if (isPlaying)
        {
            float barX = barRect.localPosition.x;
            float currentRange = 0f;

            for (int i = 0; i < cuttingRange.Length; i++)
            {
                currentRange = cuttingRange[i] * baseValue / 2;
                if (Math.Abs(barX) <= currentRange)
                {
                    Debug.Log($"결과: CuttingParam{i+1} 범위");
                    results[i]++;
                    totalAttempts++;

                    // 바 초기 위치로 리셋 
                    barRect.localPosition = new Vector3(-baseWidth / 2, barRect.localPosition.y, barRect.localPosition.z);
                    movingRight = false;
                    break;
                }
            }
        
            if(totalAttempts >= currentCuttingData.CUTTING_COUNTS)
            {
                Debug.Log("모든 시도 완료 - 미니게임 종료");
                //@TODO 결과에 따른 성공/실패 처리
                JudgeResult();
            }
        }
    }

    void Update()
    {
        if (isPlaying)
        {
            float t = (float) Mathf.Abs(barRect.localPosition.x) / (float) (baseWidth / 2);
            float addValue = Mathf.Cos(t * Mathf.PI * 0.5f); 
            float move = (SliderSpeed + addValue) * speed * Time.deltaTime * (movingRight ? 1 : -1);
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

    protected override void JudgeResult()
    {
        if(totalAttempts < currentCuttingData.CUTTING_COUNTS)
        {
            //실패 처리
        }
        else
        {
            int highestResultIndex = 0;
            for(int i=0; i<results.Length; i++)
            {
                if( i != highestResultIndex && results[i] > results[highestResultIndex])
                {
                            highestResultIndex = i;
                }
            }

            Debug.Log($"최종 결과: CuttingParam{highestResultIndex+1} 범위에서 가장 많은 성공 횟수 기록");
            //@todo 보상 획득 처리
        }
        EndMiniGame();
    }
}
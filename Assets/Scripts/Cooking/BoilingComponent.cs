using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using System;
using DataEnumDefines;
using System.Collections;

public class BoilingComponent : CookingComponent
{
    public Image arrowImage;
    private RectTransform arrowRect;

    [SerializeField] private float baseAngle = 90f; // 이미지 설정으로 일정수치 돌려놓아야 함
    private float currentAngle = 0f; // 실제 각도
    private const float MAX_ANGLE = 180f;
    private const float MIN_ANGLE = 0f;
    [SerializeField] private float coolDownSpeed = 45f; // 초당 감소 속도 (도/초)
    [SerializeField] private float interactIncrease = 15f; // Interact 시 증가 각도 (도)

    private int[] sweetSpotRange; // SWEET_SPOT 범위 [min, max]
    private int boilingTime;
    private int boilingDifficulty;

    private int[] results;
    private float elapsedTime = 0f;
    private float nextCheckTime = 1f; // 1초마다 체크
    private bool failed = false;

    [SerializeField] private string TestDataID = "1"; // XML의 ID와 일치

    protected override void Start()
    {
        if (arrowImage == null)
        {
            arrowImage = GetComponentInChildren<Image>();
        }

        if (arrowImage != null)
        {
            arrowRect = arrowImage.GetComponent<RectTransform>();
        }

        base.Start();

        currentAngle = (MIN_ANGLE + MAX_ANGLE) / 2f; // 중앙에서 시작

        results = new int[(int)ENUMGRADE.GREAT + 1];
    }

    protected override void InitCooking()
    {
        var dataManager = FindFirstObjectByType<DataManager>();
        if (dataManager == null)
        {
            Debug.LogError("DataManager not found in scene.");
            return;
        }

        if (!dataManager.dataStorage.Minigame_BoilingData.ContainsKey(TestDataID))
        {
            Debug.LogError($"Boiling data with ID {TestDataID} not found.");
            return;
        }

        var boilingData = dataManager.dataStorage.Minigame_BoilingData[TestDataID];

        sweetSpotRange = boilingData.SWEET_SPOT;
        boilingTime = boilingData.BOILING_TIME;
        boilingDifficulty = boilingData.BOILING_DIFFICULTY;

        currentAngle = (MIN_ANGLE + MAX_ANGLE) / 2f; // 중앙에서 시작
        elapsedTime = 0f;
        nextCheckTime = 1f;
        failed = false;

        Array.Clear(results, 0, results.Length);
        UpdateArrowRotation();
    }

    protected override void Interact()
    {
        if (isPlaying)
        {
            // 상호작용 시 각도 증가 (시계방향)
            currentAngle = Mathf.Min(currentAngle + interactIncrease, 180.0f);
            // 즉시 판정으로 저장하지 않고, 1초 체크 루틴에서 결과를 쌓음.
        }
    }

    private void CheckAndStoreResult()
    {
        // 각도가 SWEET_SPOT 범위 내인지 확인
        // 내부 각도는 0..180이므로 XML의 SWEET_SPOT(0..180)과 바로 비교
        int roundedAngle = Mathf.RoundToInt(currentAngle);

        if (sweetSpotRange == null || sweetSpotRange.Length < 2)
        {
            // 안전하게 기본값 처리
            sweetSpotRange = new int[] { 0, 0 };
        }

        Debug.Log($"Checking angle: {roundedAngle}, Sweet Spot Range: [{sweetSpotRange[0]}, {sweetSpotRange[1]}]");
        if (roundedAngle >= sweetSpotRange[0] && roundedAngle <= sweetSpotRange[1])
        {
            results[(int)ENUMGRADE.GREAT]++;
        }
        else
        {
            results[(int)ENUMGRADE.NORMAL]++;
        }
    }

    protected override void JudgeResult()
    {
        if(failed)
        {
            Debug.Log("실패");
            return;
        }
        else if (results[(int)ENUMGRADE.GREAT] >= results[(int)ENUMGRADE.NORMAL])
        {
            Debug.Log("대성공");
        }
        else
        {
            Debug.Log("성공");
        }
    }

    protected override void EndMiniGame()
    {
        JudgeResult();
        OnGameEnd();
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        // 시간 경과에 따라 각도를 0도 방향으로 감소
        currentAngle = Mathf.Max(currentAngle - (coolDownSpeed * Time.deltaTime), MIN_ANGLE);

        // 0도에 도달하면 실패로 간주하고 즉시 종료
        if (currentAngle <= MIN_ANGLE && !failed)
        {
            failed = true;
            isPlaying = false;
            EndMiniGame();
            return;
        }

        // 화살표 회전 업데이트
        UpdateArrowRotation();

        // 1초마다 결과 체크
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= nextCheckTime)
        {
            CheckAndStoreResult();
            nextCheckTime += 1f;
        }

        // 게임 종료 조건 확인 (시간 초과)
        if (elapsedTime >= boilingTime)
        {
            isPlaying = false;
            EndMiniGame();
        }
    }

    private void UpdateArrowRotation()
    {
        if (arrowRect == null) return;

        arrowRect.rotation = Quaternion.Euler(0, 0, -(currentAngle + baseAngle)); // Z축 음수로 회전
    }
}

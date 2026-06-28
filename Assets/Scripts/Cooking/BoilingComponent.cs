using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using System;
using DataEnumDefines;
using System.Collections;
using UnityEngine.InputSystem;

public class BoilingComponent : CookingComponent
{
    public Image arrowImage;
    private RectTransform arrowRect;

    [SerializeField] private float baseAngle = 90f;
    private float currentAngle = 0f;
    private const float MAX_ANGLE = 180f;
    private const float MIN_ANGLE = 0f;
    [SerializeField] private float coolDownSpeed = 45f;
    [SerializeField] private float interactIncrease = 15f;
    [SerializeField] private float targetMoveSpeed = 90f;

    private bool isMovingToTarget = false;
    private float targetAngle = 90f;

    private int[] sweetSpotRange;
    private int boilingTime;

    private int[] results;
    private float elapsedTime = 0f;
    private float nextCheckTime = 1f;
    private bool failed = false;

    [SerializeField] private string TestDataID = "1";

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
    }

    public override void InitMiniGameData()
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

        targetAngle = currentAngle;
        isMovingToTarget = false;
        elapsedTime = 0f;
        nextCheckTime = 1f;
        failed = false;

        currentAngle = (MIN_ANGLE + MAX_ANGLE) / 2f;
        results = new int[(int)ENUMGRADE.GREAT + 1];
        UpdateArrowRotation();
    }

    protected override void OnMove(Vector2 moveInput)
    {
        if (!isPlaying) return;
        if (isMovingToTarget) return;
        if (moveInput.x == 0) return;

        // 끝점(0, 180)에 가까울수록 변화량 감소
        float distanceToEndpoint = Mathf.Min(currentAngle, MAX_ANGLE - currentAngle);
        float scale = distanceToEndpoint / 90f;
        float delta = interactIncrease * scale;

        if (moveInput.x > 0)
            targetAngle = Mathf.Min(currentAngle + delta, MAX_ANGLE);
        else
            targetAngle = Mathf.Max(currentAngle - delta, MIN_ANGLE);

        if (Mathf.Abs(targetAngle - currentAngle) > 0.01f)
            isMovingToTarget = true;
    }

    private void CheckAndStoreResult()
    {
        int roundedAngle = Mathf.RoundToInt(currentAngle);

        if (sweetSpotRange == null || sweetSpotRange.Length < 2)
        {
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

    protected override void Update()
    {
        base.Update();

        if (!isPlaying) return;

        float distanceToEndpoint = Mathf.Min(currentAngle, MAX_ANGLE - currentAngle);
        float speedMultiplier = 1f + (1f - distanceToEndpoint / 90f);
        if (isMovingToTarget)
        {
            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, targetMoveSpeed * Time.deltaTime);
            if (currentAngle == targetAngle)
                isMovingToTarget = false;
        }
        else
        {
            float deltaAngle = coolDownSpeed * Time.deltaTime * speedMultiplier;
            if (currentAngle <= 90f)
            {                
                currentAngle = Mathf.Max(currentAngle - deltaAngle, MIN_ANGLE);
            }
            else
            {
                currentAngle = Mathf.Min(currentAngle + deltaAngle, MAX_ANGLE);
            }
        
        }

        if ((currentAngle <= MIN_ANGLE || currentAngle >= MAX_ANGLE) && !failed)
        {
            failed = true;
            isPlaying = false;
            EndMiniGame();
            return;
        }

        UpdateArrowRotation();

        elapsedTime += Time.deltaTime;
        if (elapsedTime >= nextCheckTime)
        {
            CheckAndStoreResult();
            nextCheckTime += 1f;
        }

        if (elapsedTime >= boilingTime)
        {
            isPlaying = false;
            EndMiniGame();
        }
    }

    private void UpdateArrowRotation()
    {
        if (arrowRect == null) return;

        arrowRect.rotation = Quaternion.Euler(0, 0, -(currentAngle + baseAngle));
    }
}

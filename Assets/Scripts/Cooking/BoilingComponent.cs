using UnityEngine;
using UnityEngine.UI;
using DataEnumDefines;
using UnityEngine.Serialization;

public class BoilingComponent : CookingComponent
{
    [FormerlySerializedAs("arrowImage")]
    [SerializeField] private Image _arrowImage;
    [SerializeField] private Image _gaugeImage;
    private RectTransform arrowRect;
    private Sprite _gaugeArtwork;
    private Sprite _gaugePreview;
    private Texture2D _gaugeTexture;
    private Vector2Int _shownRange = new Vector2Int(-1, -1);

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
    private DataManager _dataManager;

    protected override void Start()
    {
        if (_arrowImage != null)
        {
            arrowRect = _arrowImage.rectTransform;
        }

        base.Start();
    }

    public override void InitCooking()
    {
        base.InitCooking();
        if (TryGetBoilingData(out DataStorage.Minigame_Boiling data))
        {
            UpdateGaugeRange(data.SWEET_SPOT);
        }
        currentAngle = (MIN_ANGLE + MAX_ANGLE) / 2f;
        UpdateArrowRotation();
    }

    public override void InitMiniGameData()
    {
        if (!TryGetBoilingData(out DataStorage.Minigame_Boiling boilingData))
        {
            Debug.LogError($"Boiling data with ID {TestDataID} not found.", this);
            return;
        }

        sweetSpotRange = boilingData.SWEET_SPOT;
        UpdateGaugeRange(sweetSpotRange);
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

    private bool TryGetBoilingData(out DataStorage.Minigame_Boiling data)
    {
        if (_dataManager == null)
        {
            _dataManager = FindFirstObjectByType<DataManager>();
        }
        data = null;
        return _dataManager != null && _dataManager.dataStorage.Minigame_BoilingData != null &&
            _dataManager.dataStorage.Minigame_BoilingData.TryGetValue(TestDataID, out data);
    }

    private void UpdateGaugeRange(int[] range)
    {
        if (_gaugeImage == null || range == null || range.Length < 2 ||
            _shownRange == new Vector2Int(range[0], range[1]))
        {
            return;
        }

        if (_gaugeArtwork == null)
        {
            _gaugeArtwork = _gaugeImage.sprite;
        }
        if (_gaugeArtwork == null || !_gaugeArtwork.texture.isReadable)
        {
            return;
        }

        Texture2D source = _gaugeArtwork.texture;
        if (_gaugeTexture == null)
        {
            _gaugeTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            _gaugeTexture.filterMode = FilterMode.Point;
            _gaugeTexture.wrapMode = TextureWrapMode.Clamp;
            _gaugePreview = Sprite.Create(_gaugeTexture, _gaugeArtwork.rect, new Vector2(0.5f, 0.5f), _gaugeArtwork.pixelsPerUnit);
            _gaugeImage.sprite = _gaugePreview;
        }

        Vector2 hub = new Vector2(source.width * 0.5f, source.height * 0.11f);
        Color32[] pixels = source.GetPixels32();
        for (int y = Mathf.CeilToInt(source.height * 0.28f); y < source.height; y++)
        {
            for (int x = 0; x < source.width; x++)
            {
                int index = y * source.width + x;
                Color32 color = pixels[index];
                bool green = color.g > color.r + 10 && color.g > 130 && color.b < 120;
                bool amber = color.r > 210 && color.g >= 120 && color.g <= 158 && color.b < 15;
                if (color.a < 128 || (!green && !amber))
                {
                    continue;
                }

                int angle = Mathf.RoundToInt(180f - Mathf.Atan2(y - hub.y, x - hub.x) * Mathf.Rad2Deg);
                bool inRange = angle >= range[0] && angle <= range[1];
                if (inRange != green)
                {
                    pixels[index] = inRange
                        ? new Color32(133, 155, 86, color.a)
                        : new Color32(239, 141, 2, color.a);
                }
            }
        }
        _gaugeTexture.SetPixels32(pixels);
        _gaugeTexture.Apply();
        _shownRange = new Vector2Int(range[0], range[1]);
    }

    private void OnDestroy()
    {
        if (_gaugePreview != null)
        {
            Destroy(_gaugePreview);
        }
        if (_gaugeTexture != null)
        {
            Destroy(_gaugeTexture);
        }
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

    protected override void CheckAndStoreResult()
    {
        int roundedAngle = Mathf.RoundToInt(currentAngle);

        if (sweetSpotRange == null || sweetSpotRange.Length < 2)
        {
            sweetSpotRange = new int[] { 0, 0 };
        }

        Debug.Log($"Checking angle: {roundedAngle}, Sweet Spot Range: [{sweetSpotRange[0]}, {sweetSpotRange[1]}]");

        ENUMGRADE roundGrade;
        if (roundedAngle >= sweetSpotRange[0] && roundedAngle <= sweetSpotRange[1])
        {
            roundGrade = ENUMGRADE.GREAT;
            results[(int)ENUMGRADE.GREAT]++;
        }
        else
        {
            roundGrade = ENUMGRADE.NORMAL;
            results[(int)ENUMGRADE.NORMAL]++;
        }

        if (cookingManager != null)
        {
            cookingManager.ShowRoundResult(roundGrade);
        }
    }

    protected override ENUMGRADE JudgeResult()
    {
        if(failed)
        {
            Debug.Log("실패");
            return ENUMGRADE.NORMAL;
        }

        if (results[(int)ENUMGRADE.GREAT] >= results[(int)ENUMGRADE.NORMAL])
        {
            Debug.Log("대성공");
            return ENUMGRADE.GREAT;
        }

        Debug.Log("성공");
        return ENUMGRADE.GOOD;
    }

    protected override void EndMiniGame()
    {
        ShowResultThenEndGame(JudgeResult());
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
        if (arrowRect == null && _arrowImage != null)
        {
            arrowRect = _arrowImage.rectTransform;
        }
        if (arrowRect != null)
        {
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, baseAngle - currentAngle);
        }
    }
}

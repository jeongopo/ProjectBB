using DG.Tweening;
using UnityEngine;
using GameEnumDefines;

namespace GamePlay
{
    /// <summary>
    /// 손님 머리 위에 인내심 막대와 주문한 요리 아이콘을 보여준다. 스프라이트는 코드로 만들어서 프리팹에 따로 넣을 에셋이 없다.
    /// </summary>
    [RequireComponent(typeof(CustomerController))]
    public class CustomerStatusView : MonoBehaviour
    {
        private const float IconBackPadding = 0.12f;

        private static Sprite _squareSprite;
        private static Sprite _barSprite;

        [Header("Layout")]
        [SerializeField] private Vector2 _offset = new Vector2(0f, 1.8f);
        [SerializeField] private Vector2 _barSize = new Vector2(0.9f, 0.12f);
        [SerializeField] private float _iconOffsetY = 0.55f;
        [SerializeField] private float _iconSize = 0.6f;

        [Header("Sorting")]
        [SerializeField] private string _sortingLayerName = "Object";
        [SerializeField] private int _sortingOrder = 5000;

        [Header("Colors")]
        [SerializeField] private Color _barBackColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color _barFullColor = new Color(0.45f, 0.9f, 0.4f, 1f);
        [SerializeField] private Color _barLowColor = new Color(0.95f, 0.3f, 0.25f, 1f);
        [SerializeField] private Color _iconBackColor = new Color(1f, 1f, 1f, 0.9f);

        [Header("Feedback")]
        [SerializeField] private float _barTweenDuration = 0.3f;
        [SerializeField] private float _popDuration = 0.25f;
        [SerializeField] private Ease _popEase = Ease.OutBack;

        private CustomerController _customer;
        private Transform _statusRoot;
        private SpriteRenderer _barFill;
        private SpriteRenderer _icon;
        private SpriteRenderer _iconBack;
        private Tween _barTween;
        private Tween _popTween;

        private void Awake()
        {
            _customer = GetComponent<CustomerController>();
        }

        private void OnEnable()
        {
            _customer.OnStateChanged += HandleStateChanged;
            _customer.OnPatienceChanged += HandlePatienceChanged;
        }

        private void OnDisable()
        {
            _customer.OnStateChanged -= HandleStateChanged;
            _customer.OnPatienceChanged -= HandlePatienceChanged;
            _barTween?.Kill();
            _popTween?.Kill();
        }

        private void HandleStateChanged(CustomerState state)
        {
            // YSorter가 Awake 때 자식 렌더러를 모으므로, 그보다 늦은 이 시점에 만들어야 정렬 순서를 덮어쓰지 않는다.
            EnsureBuilt();

            if (state != CustomerState.Seated)
            {
                _statusRoot.gameObject.SetActive(false);
                return;
            }

            ApplyOrderIcon();
            ShowWithPop();
        }

        private void HandlePatienceChanged(float ratio)
        {
            EnsureBuilt();
            ratio = Mathf.Clamp01(ratio);
            _barFill.color = Color.Lerp(_barLowColor, _barFullColor, ratio);

            _barTween?.Kill();
            _barTween = _barFill.transform.DOScaleX(_barSize.x * ratio, _barTweenDuration)
                .SetLink(gameObject);
        }

        private void ApplyOrderIcon()
        {
            Sprite sprite = _customer.OrderIcon;
            _icon.sprite = sprite;
            _icon.gameObject.SetActive(sprite != null);
            _iconBack.gameObject.SetActive(sprite != null);
            if (sprite == null)
            {
                return;
            }

            float scale = _iconSize / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            _icon.transform.localScale = Vector3.one * scale;
            _iconBack.transform.localScale = Vector3.one * (_iconSize + IconBackPadding);
        }

        private void ShowWithPop()
        {
            _statusRoot.gameObject.SetActive(true);
            _statusRoot.localScale = Vector3.zero;

            _popTween?.Kill();
            _popTween = _statusRoot.DOScale(Vector3.one, _popDuration)
                .SetEase(_popEase)
                .SetLink(gameObject);
        }

        private void EnsureBuilt()
        {
            if (_statusRoot != null)
            {
                return;
            }

            _statusRoot = new GameObject("Status").transform;
            _statusRoot.SetParent(transform, false);
            _statusRoot.localPosition = _offset;

            float barLeft = -_barSize.x * 0.5f;
            CreateRenderer("BarBack", new Vector3(barLeft, 0f, 0f), _barSize, _barBackColor, 0, true);
            _barFill = CreateRenderer("BarFill", new Vector3(barLeft, 0f, 0f), _barSize, _barFullColor, 1, true);

            _iconBack = CreateRenderer("IconBack", new Vector3(0f, _iconOffsetY, 0f), Vector2.one, _iconBackColor, 0, false);
            _iconBack.sprite = GetSquareSprite();
            _icon = CreateRenderer("Icon", new Vector3(0f, _iconOffsetY, 0f), Vector2.one, Color.white, 1, false);

            _statusRoot.gameObject.SetActive(false);
        }

        // 막대는 왼쪽 끝이 기준이라 X 스케일만 바꾸면 왼쪽에서부터 줄어든다.
        private SpriteRenderer CreateRenderer(string objectName, Vector3 localPosition, Vector2 scale, Color color, int orderOffset, bool leftPivot)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(_statusRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = leftPivot ? GetBarSprite() : GetSquareSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingLayerName = _sortingLayerName;
            spriteRenderer.sortingOrder = _sortingOrder + orderOffset;
            return spriteRenderer;
        }

        private static Sprite GetBarSprite()
        {
            if (_barSprite == null)
            {
                Texture2D texture = Texture2D.whiteTexture;
                _barSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0f, 0.5f), texture.width);
            }

            return _barSprite;
        }

        private static Sprite GetSquareSprite()
        {
            if (_squareSprite == null)
            {
                Texture2D texture = Texture2D.whiteTexture;
                _squareSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            }

            return _squareSprite;
        }
    }
}

using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GamePlay
{
    /// <summary>
    /// 설치 모드. 고스트가 마우스를 따라다니며 설치 가능 여부를 색으로 보여 주고, 클릭하면 가구를 설치한다.
    /// 좌클릭: 설치 / 우클릭, Esc: 취소
    /// </summary>
    public class FurniturePlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlacementGrid _placementGrid;
        [SerializeField] private SpriteRenderer _ghost;

        [Header("Ghost")]
        [SerializeField] private Color _validColor = new Color(0.4f, 1f, 0.5f, 0.7f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0.35f, 0.35f, 0.7f);
        [SerializeField] private float _colorDuration = 0.1f;
        [SerializeField] private int _ghostSortingOrder = 1000;

        [Header("Place Feedback")]
        [SerializeField] private float _punchScale = 0.15f;
        [SerializeField] private float _punchDuration = 0.25f;

        private Furniture _prefab;
        private Camera _camera;
        private Tween _colorTween;
        private bool _lastCanPlace;
        private bool _hasGhostState;
        private Vector3 _ghostOffset;

        public bool IsPlacing => _prefab != null;

        public event Action<Furniture> Placed;
        public event Action Cancelled;

        private void Awake()
        {
            _camera = Camera.main;
            if (_ghost != null)
            {
                _ghost.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            _colorTween?.Kill();
        }

        private void OnDestroy()
        {
            _colorTween?.Kill();
        }

        public void BeginPlacement(Furniture prefab)
        {
            SpriteRenderer source = prefab.GetComponentInChildren<SpriteRenderer>();
            if (source == null)
            {
                Debug.LogError($"[FurniturePlacer] {prefab.name}에 SpriteRenderer가 없습니다.", prefab);
                return;
            }

            _prefab = prefab;
            _ghost.sprite = source.sprite;
            _ghost.sortingLayerID = source.sortingLayerID;
            _ghost.sortingOrder = _ghostSortingOrder;
            _ghost.transform.localScale = Vector3.Scale(prefab.transform.localScale, source.transform.localScale);
            _ghostOffset = Vector3.Scale(source.transform.localPosition, prefab.transform.localScale);
            _ghost.gameObject.SetActive(true);
            _hasGhostState = false;
        }

        public void CancelPlacement()
        {
            if (!IsPlacing)
            {
                return;
            }

            EndPlacement();
            Cancelled?.Invoke();
        }

        private void Update()
        {
            if (!IsPlacing)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.rightButton.wasPressedThisFrame || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                CancelPlacement();
                return;
            }

            Vector3 world = _camera.ScreenToWorldPoint(mouse.position.ReadValue());
            world.z = 0f;

            Vector3Int origin = _placementGrid.PositionToOrigin(world, _prefab.Size);
            Vector3 snapped = _placementGrid.OriginToPosition(origin, _prefab.Size);
            _ghost.transform.position = snapped + _ghostOffset;

            bool canPlace = _placementGrid.CanPlace(origin, _prefab.Size);
            if (!_hasGhostState || canPlace != _lastCanPlace)
            {
                SetGhostColor(canPlace ? _validColor : _invalidColor);
            }

            _hasGhostState = true;
            _lastCanPlace = canPlace;

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (canPlace && !overUi && mouse.leftButton.wasPressedThisFrame)
            {
                Place(origin);
            }
        }

        private void Place(Vector3Int origin)
        {
            Furniture instance = Instantiate(_prefab);
            if (!instance.PlaceAt(_placementGrid, origin))
            {
                Destroy(instance.gameObject);
                return;
            }

            Transform instanceTransform = instance.transform;
            Vector3 baseScale = instanceTransform.localScale;
            instanceTransform.DOPunchScale(baseScale * _punchScale, _punchDuration)
                .SetLink(instance.gameObject);

            Placed?.Invoke(instance);
            EndPlacement();
        }

        private void EndPlacement()
        {
            _prefab = null;
            _colorTween?.Kill();
            _ghost.gameObject.SetActive(false);
        }

        private void SetGhostColor(Color color)
        {
            _colorTween?.Kill();
            _colorTween = _ghost.DOColor(color, _colorDuration).SetLink(_ghost.gameObject);
        }
    }
}

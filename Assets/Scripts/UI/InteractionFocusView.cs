using System.Collections.Generic;
using Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay
{
    public sealed class InteractionFocusView : MonoBehaviour
    {
        private static readonly Vector3[] OutlineDirections =
        {
            Vector3.left, Vector3.right, Vector3.up, Vector3.down,
        };

        private readonly List<SpriteRenderer> _outline = new List<SpriteRenderer>();
        private Dictionary<string, DataStorage.BBString> _strings;
        private Camera _camera;
        private InteractComponent _target;
        private Material _outlineMaterial;
        private RectTransform _canvas;
        private RectTransform _prompt;
        private TMP_Text _label;
        private string _shownAction;

        private void Awake()
        {
            _camera = GetComponentInChildren<Camera>(true);
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _strings = DataManager.LoadDefineData<DataStorage.BBString>("BBString");
            Shader shader = Resources.Load<Shader>("CharacterOcclusionSilhouette");
            if (shader != null && shader.isSupported)
            {
                _outlineMaterial = new Material(shader);
                _outlineMaterial.SetColor("_SilhouetteColor", new Color32(255, 216, 137, 255));
            }

            BuildPrompt();
        }

        internal void Show(InteractComponent target)
        {
            if (_target != target)
            {
                ClearOutline();
                _target = target;
            }

            if (_target == null || _camera == null)
            {
                ClearOutline();
                _prompt.gameObject.SetActive(false);
                return;
            }

            Bounds bounds = new Bounds();
            bool hasVisual = false;
            int used = 0;
            foreach (SpriteRenderer source in _target.InteractionVisuals)
            {
                if (source == null || !source.enabled || !source.gameObject.activeInHierarchy ||
                    source.forceRenderingOff || source.sprite == null || source.color.a <= 0f)
                {
                    continue;
                }

                if (!hasVisual)
                {
                    bounds = source.bounds;
                    hasVisual = true;
                }
                else
                {
                    bounds.Encapsulate(source.bounds);
                }

                if (_outlineMaterial != null)
                {
                    DrawOutline(source, ref used);
                }
            }

            for (int i = used; i < _outline.Count; i++)
            {
                if (_outline[i] != null)
                {
                    _outline[i].enabled = false;
                }
            }

            Vector3 position = hasVisual ? bounds.center : _target.transform.position;
            if (hasVisual)
            {
                position.y = bounds.max.y;
            }
            position.y += _target is CustomerController ? 0.75f : 0.2f;
            Vector3 screen = _camera.WorldToScreenPoint(position);
            bool onScreen = screen.z > 0f && _camera.pixelRect.Contains(screen);
            _prompt.gameObject.SetActive(onScreen);
            if (!onScreen)
            {
                return;
            }

            string action = _target.PromptKey;
            if (action != _shownAction)
            {
                _shownAction = action;
                _label.text = _strings.TryGetValue(action, out DataStorage.BBString row) ? row.BODY : string.Empty;
                _prompt.sizeDelta = new Vector2(Mathf.Ceil(_label.preferredWidth + 14f), 22f);
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, screen, null, out Vector2 local);
            Rect area = _canvas.rect;
            float halfWidth = _prompt.sizeDelta.x * 0.5f;
            local.x = Mathf.Clamp(local.x, area.xMin + halfWidth, area.xMax - halfWidth);
            local.y = Mathf.Clamp(local.y, area.yMin + 11f, area.yMax - 11f);
            _prompt.anchoredPosition = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));
        }

        private void DrawOutline(SpriteRenderer source, ref int used)
        {
            for (int i = 0; i < OutlineDirections.Length; i++)
            {
                int index = used++;
                if (index == _outline.Count)
                {
                    _outline.Add(null);
                }

                SpriteRenderer copy = _outline[index];
                if (copy == null)
                {
                    GameObject outline = new GameObject("Interaction Outline");
                    copy = outline.AddComponent<SpriteRenderer>();
                    copy.sharedMaterial = _outlineMaterial;
                    _outline[index] = copy;
                }

                if (copy.transform.parent != source.transform)
                {
                    copy.gameObject.layer = source.gameObject.layer;
                    copy.transform.SetParent(source.transform, false);
                }

                copy.transform.localPosition = source.transform.InverseTransformVector(OutlineDirections[i] / 16f);
                copy.sprite = source.sprite;
                copy.flipX = source.flipX;
                copy.flipY = source.flipY;
                copy.sortingLayerID = source.sortingLayerID;
                copy.sortingOrder = Mathf.Max(short.MinValue, source.sortingOrder - 1);
                copy.enabled = true;
            }
        }

        private void ClearOutline()
        {
            // 트리거가 비활성화되는 도중에는 계층을 바꿀 수 없으므로 다음 외곽선을 그릴 때 옮긴다.
            foreach (SpriteRenderer copy in _outline)
            {
                if (copy != null)
                {
                    copy.enabled = false;
                }
            }
        }

        private void BuildPrompt()
        {
            GameObject root = new GameObject("Interaction Prompt Canvas", typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            _canvas = (RectTransform)root.transform;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 450);

            _prompt = (RectTransform)new GameObject("Interaction Prompt", typeof(RectTransform)).transform;
            _prompt.SetParent(_canvas, false);
            Image background = _prompt.gameObject.AddComponent<Image>();
            background.color = new Color32(49, 30, 23, 241);
            background.raycastTarget = false;

            RectTransform text = (RectTransform)new GameObject("Action", typeof(RectTransform)).transform;
            text.SetParent(_prompt, false);
            text.anchorMin = Vector2.zero;
            text.anchorMax = Vector2.one;
            text.offsetMin = text.offsetMax = Vector2.zero;
            _label = text.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Art/Font/PFStartDust 3");
            if (font != null)
            {
                _label.font = font;
            }
            _label.fontSize = 13f;
            _label.color = new Color32(255, 227, 178, 255);
            _label.alignment = TextAlignmentOptions.Center;
            _label.raycastTarget = false;
            _prompt.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            ClearOutline();
            if (_prompt != null)
            {
                _prompt.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            foreach (SpriteRenderer copy in _outline)
            {
                if (copy != null)
                {
                    Destroy(copy.gameObject);
                }
            }
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
            }
            if (_outlineMaterial != null)
            {
                Destroy(_outlineMaterial);
            }
        }
    }
}

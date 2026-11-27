using System.Collections.Generic;
using UnityEngine;

namespace GamePlay
{
    public enum CharacterPartStyle
    {
        Original,
        Alternate,
    }

    [DefaultExecutionOrder(50)]
    public sealed class CharacterAppearance : MonoBehaviour
    {
        private const string ResourceRoot = "Art/Character/";
        private static readonly Vector2 AtlasOrigin = new Vector2(256f, 312f);

        [SerializeField] private CharacterPartStyle _headStyle;
        [SerializeField] private CharacterPartStyle _outfitStyle;

        private readonly Dictionary<Sprite, Sprite[]> _frames = new Dictionary<Sprite, Sprite[]>();
        private Texture2D[] _atlases;
        private SpriteRenderer _source;
        private SpriteRenderer _head;
        private SpriteRenderer _outfit;
        private Sprite _lastSprite;
        private Sprite[] _currentParts;
        private bool _sourceWasHidden;

        internal bool IsLayered { get; private set; }

        internal void Initialize(SpriteRenderer source, CharacterPartStyle head, CharacterPartStyle outfit)
        {
            _source = source;
            _sourceWasHidden = source.forceRenderingOff;
            _headStyle = head;
            _outfitStyle = outfit;
            _atlases = new[]
            {
                Resources.Load<Texture2D>(ResourceRoot + "PlayerHead"),
                Resources.Load<Texture2D>(ResourceRoot + "PlayerHeadAlternate"),
                Resources.Load<Texture2D>(ResourceRoot + "PlayerOutfit"),
                Resources.Load<Texture2D>(ResourceRoot + "PlayerOutfitAlternate"),
            };
            foreach (Texture2D atlas in _atlases)
            {
                if (atlas == null)
                {
                    enabled = false;
                    return;
                }
            }

            _head = CreateRenderer("CharacterHead");
            _outfit = CreateRenderer("CharacterOutfit");
        }

        public void SetHeadStyle(CharacterPartStyle style)
        {
            _headStyle = style;
        }

        public void SetOutfitStyle(CharacterPartStyle style)
        {
            _outfitStyle = style;
        }

        private SpriteRenderer CreateRenderer(string objectName)
        {
            GameObject part = new GameObject(objectName);
            part.layer = _source.gameObject.layer;
            part.transform.SetParent(_source.transform, false);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = _source.sharedMaterial;
            renderer.enabled = false;
            return renderer;
        }

        private void LateUpdate()
        {
            if (_source == null || _head == null || _outfit == null)
            {
                return;
            }
            if (_lastSprite != _source.sprite)
            {
                _lastSprite = _source.sprite;
                _currentParts = GetFrameParts(_lastSprite);
            }
            IsLayered = _currentParts != null && _source.enabled && !_sourceWasHidden;
            _source.forceRenderingOff = _sourceWasHidden || IsLayered;
            _head.enabled = IsLayered;
            _outfit.enabled = IsLayered;
            if (!IsLayered)
            {
                return;
            }

            ApplyFrame(_head, _currentParts[_headStyle == CharacterPartStyle.Alternate ? 1 : 0]);
            ApplyFrame(_outfit, _currentParts[_outfitStyle == CharacterPartStyle.Alternate ? 3 : 2]);
        }

        private Sprite[] GetFrameParts(Sprite frame)
        {
            if (frame == null || !frame.name.StartsWith("Characters_", System.StringComparison.Ordinal))
            {
                return null;
            }

            if (_frames.TryGetValue(frame, out Sprite[] parts))
            {
                return parts;
            }

            Rect rect = frame.rect;
            rect.position -= AtlasOrigin;
            if (rect.xMin < 0f || rect.yMin < 0f || rect.xMax > _atlases[0].width || rect.yMax > _atlases[0].height)
            {
                return null;
            }

            Vector2 pivot = new Vector2(frame.pivot.x / rect.width, frame.pivot.y / rect.height);
            parts = new Sprite[_atlases.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = Sprite.Create(_atlases[i], rect, pivot, frame.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
            _frames.Add(frame, parts);
            return parts;
        }

        private void ApplyFrame(SpriteRenderer renderer, Sprite frame)
        {
            renderer.sprite = frame;
            renderer.flipX = _source.flipX;
            renderer.flipY = _source.flipY;
            renderer.color = _source.color;
            renderer.sortingLayerID = _source.sortingLayerID;
            renderer.sortingOrder = _source.sortingOrder;
            renderer.maskInteraction = _source.maskInteraction;
        }

        private void OnDisable()
        {
            IsLayered = false;
            if (_source != null)
            {
                _source.forceRenderingOff = _sourceWasHidden;
            }
            if (_head != null)
            {
                _head.enabled = false;
            }
            if (_outfit != null)
            {
                _outfit.enabled = false;
            }
        }

        private void OnDestroy()
        {
            foreach (Sprite[] parts in _frames.Values)
            {
                foreach (Sprite sprite in parts)
                {
                    Destroy(sprite);
                }
            }
            if (_head != null)
            {
                Destroy(_head.gameObject);
            }
            if (_outfit != null)
            {
                Destroy(_outfit.gameObject);
            }
        }
    }
}

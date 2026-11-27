using System.Collections.Generic;
using UnityEngine;

namespace GamePlay
{
    [DefaultExecutionOrder(100)]
    public sealed class CharacterOcclusionSilhouette : MonoBehaviour
    {
        private const int OverlayOrder = 32766;
        private readonly Dictionary<SpriteRenderer, SpriteMask> _masks = new Dictionary<SpriteRenderer, SpriteMask>();
        private readonly List<SpriteRenderer> _destroyedSources = new List<SpriteRenderer>();
        private SpriteRenderer _source;
        private SpriteRenderer _overlay;
        private Material _material;
        private CharacterAppearance _appearance;

        internal void Initialize(SpriteRenderer source, Color tint, CharacterAppearance appearance)
        {
            Shader shader = Resources.Load<Shader>("CharacterOcclusionSilhouette");
            if (shader == null || !shader.isSupported)
            {
                enabled = false;
                return;
            }

            _source = source;
            _appearance = appearance;
            _material = new Material(shader);
            _material.SetColor("_SilhouetteColor", tint);
            GameObject overlayObject = new GameObject("Occlusion Silhouette");
            overlayObject.layer = source.gameObject.layer;
            overlayObject.transform.SetParent(source.transform, false);
            _overlay = overlayObject.AddComponent<SpriteRenderer>();
            _overlay.sharedMaterial = _material;
            SortingLayer[] sortingLayers = SortingLayer.layers;
            _overlay.sortingLayerID = sortingLayers[sortingLayers.Length - 1].id;
            _overlay.sortingOrder = OverlayOrder;
            _overlay.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            _overlay.enabled = false;
        }

        private void LateUpdate()
        {
            foreach (KeyValuePair<SpriteRenderer, SpriteMask> entry in _masks)
            {
                if (entry.Key == null || entry.Value == null)
                {
                    _destroyedSources.Add(entry.Key);
                    if (entry.Value != null)
                    {
                        entry.Value.enabled = false;
                        Destroy(entry.Value.gameObject);
                    }
                }
                else
                {
                    entry.Value.enabled = false;
                }
            }

            foreach (SpriteRenderer source in _destroyedSources)
            {
                _masks.Remove(source);
            }
            _destroyedSources.Clear();

            if (_overlay == null)
            {
                return;
            }

            _overlay.enabled = false;
            if (!IsVisibleSprite(_source, _appearance != null && _appearance.IsLayered))
            {
                return;
            }

            Bounds characterBounds = _source.bounds;
            int layerId = _source.sortingLayerID;
            int layerValue = SortingLayer.GetLayerValueFromID(layerId);
            foreach (Furniture furniture in Furniture.ActiveFurniture)
            {
                foreach (SpriteRenderer renderer in furniture.Renderers)
                {
                    if (!IsVisibleSprite(renderer) || !DrawsInFront(renderer, layerValue) ||
                        !OverlapsInXY(characterBounds, renderer.bounds))
                    {
                        continue;
                    }

                    EnableMask(renderer, _overlay.sortingLayerID);
                    _overlay.enabled = true;
                }
            }

            _overlay.sprite = _source.sprite;
            _overlay.flipX = _source.flipX;
            _overlay.flipY = _source.flipY;
        }

        private bool DrawsInFront(SpriteRenderer renderer, int characterLayerValue)
        {
            int layerValue = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
            return layerValue > characterLayerValue ||
                (layerValue == characterLayerValue && renderer.sortingOrder > _source.sortingOrder);
        }

        private static bool IsVisibleSprite(SpriteRenderer renderer, bool showLayeredSource = false)
        {
            return renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                (showLayeredSource || !renderer.forceRenderingOff) && renderer.sprite != null && renderer.color.a > 0f;
        }

        private static bool OverlapsInXY(Bounds a, Bounds b)
        {
            return a.min.x < b.max.x && a.max.x > b.min.x &&
                a.min.y < b.max.y && a.max.y > b.min.y;
        }

        private void EnableMask(SpriteRenderer renderer, int layerId)
        {
            if (!_masks.TryGetValue(renderer, out SpriteMask mask))
            {
                GameObject maskObject = new GameObject("Furniture Occlusion Mask");
                maskObject.layer = renderer.gameObject.layer;
                maskObject.transform.SetParent(renderer.transform, false);
                mask = maskObject.AddComponent<SpriteMask>();
                mask.isCustomRangeActive = true;
                mask.backSortingOrder = OverlayOrder - 1;
                mask.frontSortingOrder = OverlayOrder + 1;
                mask.alphaCutoff = 0.1f;
                _masks.Add(renderer, mask);
            }

            mask.sprite = renderer.sprite;
            mask.transform.localScale = new Vector3(renderer.flipX ? -1f : 1f, renderer.flipY ? -1f : 1f, 1f);
            mask.backSortingLayerID = layerId;
            mask.frontSortingLayerID = layerId;
            mask.enabled = true;
        }

        private void OnDisable()
        {
            if (_overlay != null)
            {
                _overlay.enabled = false;
            }

            foreach (SpriteMask mask in _masks.Values)
            {
                if (mask != null)
                {
                    mask.enabled = false;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (SpriteMask mask in _masks.Values)
            {
                if (mask != null)
                {
                    Destroy(mask.gameObject);
                }
            }

            if (_overlay != null)
            {
                Destroy(_overlay.gameObject);
            }

            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}

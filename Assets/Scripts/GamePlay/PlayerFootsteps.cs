using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GamePlay
{
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        private enum Surface
        {
            Wood,
            Stone,
            Dirt,
        }

        [Serializable]
        private struct SurfaceTiles
        {
            [SerializeField] private Surface _surface;
            [SerializeField] private TileBase[] _tiles;

            public Surface Material => _surface;
            public TileBase[] Tiles => _tiles;
        }

        [Header("Footsteps")]
        [SerializeField, Min(0.1f)] private float _stepDistance = 1.1f;
        [SerializeField, Range(0f, 1f)] private float _gain = 0.65f;
        [SerializeField] private Surface _defaultSurface = Surface.Wood;
        [SerializeField] private SurfaceTiles[] _surfaces = Array.Empty<SurfaceTiles>();

        private readonly Dictionary<TileBase, Surface> _tileSurfaces = new Dictionary<TileBase, Surface>();
        private Tilemap _ground;
        private Rigidbody2D _body;
        private Collider2D _footCollider;
        private Vector2 _lastPosition;
        private float _distance;

        internal void Initialize(Tilemap ground, Rigidbody2D body)
        {
            _ground = ground;
            _body = body;
            _lastPosition = body.position;
            TryGetComponent(out _footCollider);
            _tileSurfaces.Clear();
            foreach (SurfaceTiles group in _surfaces)
            {
                if (group.Tiles == null)
                {
                    continue;
                }

                foreach (TileBase tile in group.Tiles)
                {
                    if (tile != null)
                    {
                        _tileSurfaces[tile] = group.Material;
                    }
                }
            }
        }

        private void OnEnable()
        {
            _distance = 0f;
            if (_body != null)
            {
                _lastPosition = _body.position;
            }
        }

        private void FixedUpdate()
        {
            if (_body == null)
            {
                return;
            }

            Vector2 position = _body.position;
            float traveled = Vector2.Distance(position, _lastPosition);
            _lastPosition = position;
            float stepDistance = Mathf.Max(0.1f, _stepDistance);
            // 순간 이동이나 정지 후에는 걸음 간격을 새로 센다.
            if (traveled < 0.002f || traveled > stepDistance)
            {
                _distance = 0f;
                return;
            }

            _distance += traveled;
            if (_distance < stepDistance)
            {
                return;
            }

            _distance -= stepDistance;
            SoundCue cue = GetSurface() switch
            {
                Surface.Stone => SoundCue.FootstepStone,
                Surface.Dirt => SoundCue.FootstepDirt,
                _ => SoundCue.FootstepWood,
            };
            GameAudio.Play(cue, _gain);
        }

        private Surface GetSurface()
        {
            if (_ground != null)
            {
                Vector3 point = transform.position;
                if (_footCollider != null && _footCollider.enabled)
                {
                    Bounds bounds = _footCollider.bounds;
                    point = new Vector3(bounds.center.x, bounds.min.y, point.z);
                }

                TileBase tile = _ground.GetTile(_ground.WorldToCell(point));
                if (tile != null && _tileSurfaces.TryGetValue(tile, out Surface material))
                {
                    return material;
                }
            }

            return _defaultSurface;
        }
    }
}

using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GamePlay
{
    public sealed class ServeFeedback : MonoBehaviour
    {
        private const int PlateSortingOrder = 6000;
        private const int GoldSortingOrder = PlateSortingOrder + 1;

        [Header("Plate")]
        [SerializeField, Min(0.01f)] private float _flightDuration = 0.24f;
        [SerializeField, Min(0f)] private float _arcHeight = 0.35f;
        [SerializeField, Min(0.05f)] private float _plateWidth = 0.55f;

        [Header("Gold")]
        [SerializeField, Min(0.01f)] private float _popupDuration = 0.75f;
        [SerializeField, Min(0f)] private float _popupRise = 0.55f;
        [SerializeField] private Vector3 _popupOffset = new Vector3(0f, 2.15f, 0f);

        private Sprite _plate;
        private TMP_FontAsset _font;
        private int _sortingLayer;

        private void Awake()
        {
            _plate = Resources.Load<Sprite>("Art/UI/Cooking/Plating/Plating_Plate");
            _font = Resources.Load<TMP_FontAsset>("Art/Font/PFStartDust 3");
            SortingLayer[] layers = SortingLayer.layers;
            _sortingLayer = layers[layers.Length - 1].id;
        }

        internal void Play(Vector3 from, Vector3 customerPosition, int gold)
        {
            Vector3 destination = customerPosition + Vector3.up * 0.85f;
            if (_plate == null)
            {
                ShowPayment(customerPosition, gold);
                return;
            }

            GameObject plate = new GameObject("Served Plate");
            plate.transform.SetParent(transform, false);
            plate.transform.position = from + Vector3.up * 0.85f;
            plate.transform.localScale = Vector3.one * (_plateWidth / _plate.bounds.size.x);
            SpriteRenderer renderer = plate.AddComponent<SpriteRenderer>();
            renderer.sprite = _plate;
            renderer.sortingLayerID = _sortingLayer;
            renderer.sortingOrder = PlateSortingOrder;

            plate.transform.DOJump(destination, _arcHeight, 1, _flightDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(plate)
                .OnComplete(() =>
                {
                    Destroy(plate);
                    ShowPayment(customerPosition, gold);
                });
        }

        private void ShowPayment(Vector3 position, int gold)
        {
            if (gold <= 0)
            {
                return;
            }

            GameAudio.Play(SoundCue.Served);
            GameObject popup = new GameObject("Serving Gold", typeof(RectTransform));
            popup.transform.SetParent(transform, false);
            Vector3 start = position + _popupOffset;
            popup.transform.position = start;
            popup.transform.localScale = Vector3.one * 0.16f;

            TextMeshPro label = popup.AddComponent<TextMeshPro>();
            if (_font != null)
            {
                label.font = _font;
            }
            label.rectTransform.sizeDelta = new Vector2(18f, 5f);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 5f;
            label.color = new Color32(255, 219, 124, 255);
            label.sortingLayerID = _sortingLayer;
            label.sortingOrder = GoldSortingOrder;
            label.text = $"+{gold}";

            Sequence sequence = DOTween.Sequence().SetLink(popup);
            sequence.Append(popup.transform.DOMoveY(start.y + _popupRise, _popupDuration).SetEase(Ease.OutQuad));
            sequence.Insert(_popupDuration * 0.55f,
                DOTween.To(() => label.alpha, alpha => label.alpha = alpha, 0f, _popupDuration * 0.45f));
            sequence.OnComplete(() => Destroy(popup));
        }
    }
}

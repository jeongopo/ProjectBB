using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RegularGuestCard : MonoBehaviour
{
    [SerializeField] private Image _portrait;
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _affinity;
    [SerializeField] private Image _order;

    internal void Show(string guestName, Sprite portrait, string affinity, Sprite order)
    {
        _name.text = guestName;
        _portrait.sprite = portrait;
        _portrait.enabled = portrait != null;
        _affinity.text = affinity;
        _order.sprite = order;
        _order.enabled = order != null;
    }
}

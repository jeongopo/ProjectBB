using System.Collections.Generic;
using UnityEngine;

public sealed class RegularGuestPanel : MonoBehaviour
{
    [SerializeField] private RegularGuestCard _cardTemplate;

    private readonly Dictionary<string, RegularGuestCard> _cards = new Dictionary<string, RegularGuestCard>();

    internal void ShowGuest(string id, string guestName, Sprite portrait, string affinity, Sprite order)
    {
        if (!_cards.TryGetValue(id, out RegularGuestCard card))
        {
            card = Instantiate(_cardTemplate, transform);
            card.name = id + "GuestCard";
            card.gameObject.SetActive(true);
            _cards.Add(id, card);
        }

        card.Show(guestName, portrait, affinity, order);
    }

    internal void RemoveGuest(string id)
    {
        if (_cards.TryGetValue(id, out RegularGuestCard card))
        {
            _cards.Remove(id);
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
    }
}

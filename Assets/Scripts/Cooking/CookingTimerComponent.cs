using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class CookingTimerComponent : MonoBehaviour
{
    [SerializeField] private Image timerImage;
    [SerializeField] private Sprite[] countdownSprites; // 순서: 3, 2, 1
    [SerializeField] private Sprite startSprite; // "Start" 이미지

    private Action onTimerComplete;

    private void Start()
    {
        if (timerImage == null)
        {
            timerImage = GetComponentInChildren<Image>();
        }
    }

    public void StartCountdown(Action onComplete = null)
    {
        gameObject.SetActive(true);
        onTimerComplete = onComplete;
        StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        for (int i = 0; i < countdownSprites.Length; i++)
        {
            if (timerImage != null)
            {
                timerImage.sprite = countdownSprites[i];
            }
            yield return new WaitForSeconds(1f);
        }

        if (timerImage != null)
        {
            timerImage.sprite = startSprite;
        }
        yield return new WaitForSeconds(1f);

        gameObject.SetActive(false);
        onTimerComplete?.Invoke();
    }

    public void StopCountdown()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}

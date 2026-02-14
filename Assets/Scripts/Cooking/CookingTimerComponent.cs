using UnityEngine;
using TMPro;
using System;
using System.Collections;
using Microsoft.Unity.VisualStudio.Editor;

public class CookingTimerComponent : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    
    private Action onTimerComplete;

    private void Start()
    {
        if (timerText == null)
        {
            timerText = GetComponentInChildren<TextMeshProUGUI>();
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
        for (int i = 3; i > 0; i--)
        {
            if (timerText != null)
            {
                timerText.text = i.ToString();
            }
            yield return new WaitForSeconds(1f);
        }

        gameObject.SetActive(false);
        onTimerComplete?.Invoke();
    }

    public void StopCountdown()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}

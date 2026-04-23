using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Shows a brief on-screen toast for new chain events.
/// Attach to a world-space or screen-space canvas element.
/// </summary>
public class EventToast : MonoBehaviour
{
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeDuration = 0.5f;

    private int _lastEventId = -1;

    void Start()
    {
        canvasGroup.alpha = 0f;
        ApiClient.Instance.OnEventsReceived += OnEvents;
    }

    void OnDestroy()
    {
        if (ApiClient.Instance != null)
            ApiClient.Instance.OnEventsReceived -= OnEvents;
    }

    void OnEvents(GameEvent[] events)
    {
        if (events == null || events.Length == 0) return;
        var latest = events[0];
        if (latest.id == _lastEventId) return;
        _lastEventId = latest.id;

        string icon = latest.eventType switch
        {
            "TILE_CLAIMED"      => "🏕 Land claimed on-chain!",
            "CURRENCY_CREATED"  => "💰 New currency created!",
            "DEATH"             => "💀 A player died!",
            "TREASON_ASSIGNED"  => "🗡 Treason mission assigned...",
            "TREASON_COMPLETED" => "🤑 Traitor cashed out!",
            _                   => $"📡 {latest.eventType}",
        };

        StopAllCoroutines();
        StartCoroutine(ShowToast(icon));
    }

    IEnumerator ShowToast(string message)
    {
        toastText.text = message;

        // Fade in
        float t = 0f;
        while (t < fadeDuration) { canvasGroup.alpha = t / fadeDuration; t += Time.deltaTime; yield return null; }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        // Fade out
        t = 0f;
        while (t < fadeDuration) { canvasGroup.alpha = 1f - t / fadeDuration; t += Time.deltaTime; yield return null; }
        canvasGroup.alpha = 0f;
    }
}

using UnityEngine;
using UnityEngine.Events;

public class InvincibilityController : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float defaultDurationSeconds = 2f;

    private bool isInvincible;
    public bool IsInvincible => isInvincible;

    // ★ 追加：開始/終了イベント
    public UnityEvent onInvincibilityStart;
    public UnityEvent onInvincibilityEnd;

    public void Begin(float? seconds = null)
    {
        float duration = Mathf.Max(0f, seconds ?? defaultDurationSeconds);
        StopAllCoroutines();                // 再入を安全に
        if (gameObject.activeInHierarchy)
            StartCoroutine(Run(duration));
        else
            isInvincible = true;
    }

    public void EndNow()
    {
        StopAllCoroutines();
        if (isInvincible)
        {
            isInvincible = false;
            onInvincibilityEnd?.Invoke();   // ★ 終了通知
        }
    }

    private System.Collections.IEnumerator Run(float duration)
    {
        isInvincible = true;
        onInvincibilityStart?.Invoke();     // ★ 開始通知

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        isInvincible = false;
        onInvincibilityEnd?.Invoke();       // ★ 終了通知
    }
}

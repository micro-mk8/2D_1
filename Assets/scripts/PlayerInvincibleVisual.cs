using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PlayerInvincibleVisual : MonoBehaviour
{
    [Header("�Q��")]
    [SerializeField] private InvincibilityController invincibility; // PlayerRoot�̓��R���|
    [SerializeField] private CanvasGroup canvasGroup;               // �����⊮�p

    [Header("�_�Őݒ�")]
    [SerializeField, Min(0f)] private float blinkIntervalSeconds = 0.125f; // ��8Hz
    [SerializeField, Range(0f, 1f)] private float lowAlpha = 0.25f;

    private float timer;
    private bool blinking;
    private float highAlpha = 1f;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (invincibility)
        {
            invincibility.onInvincibilityStart.AddListener(BeginBlink);
            invincibility.onInvincibilityEnd.AddListener(EndBlink);
        }
    }

    private void OnDisable()
    {
        if (invincibility)
        {
            invincibility.onInvincibilityStart.RemoveListener(BeginBlink);
            invincibility.onInvincibilityEnd.RemoveListener(EndBlink);
        }
        // �t�F�C���Z�[�t
        if (canvasGroup) canvasGroup.alpha = highAlpha;
        blinking = false;
        timer = 0f;
    }

    private void Update()
    {
        if (!blinking || !canvasGroup) return;

        timer += Time.deltaTime;
        if (timer >= blinkIntervalSeconds)
        {
            timer = 0f;
            canvasGroup.alpha = (canvasGroup.alpha >= 0.99f) ? lowAlpha : highAlpha;
        }
    }

    private void BeginBlink()
    {
        blinking = true;
        timer = 0f;
        if (canvasGroup) canvasGroup.alpha = lowAlpha;
    }

    private void EndBlink()
    {
        blinking = false;
        if (canvasGroup) canvasGroup.alpha = highAlpha;
    }
}

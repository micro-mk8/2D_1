using UnityEngine;

public class HurtboxToggleOnInvincible : MonoBehaviour
{
    [SerializeField] private InvincibilityController invincibility; // PlayerRoot�̓��R���|
    [SerializeField] private UIHitbox2D hurtbox;                    // Player��Hurtbox

    private bool originalEnabled = true;

    private void OnEnable()
    {
        if (invincibility)
        {
            invincibility.onInvincibilityStart.AddListener(DisableHurtbox);
            invincibility.onInvincibilityEnd.AddListener(EnableHurtbox);
        }
        if (hurtbox) originalEnabled = hurtbox.enabled;
    }

    private void OnDisable()
    {
        if (invincibility)
        {
            invincibility.onInvincibilityStart.RemoveListener(DisableHurtbox);
            invincibility.onInvincibilityEnd.RemoveListener(EnableHurtbox);
        }
        // �t�F�C���Z�[�t�F�������̂܂܎c��Ȃ��悤�ɖ߂�
        if (hurtbox) hurtbox.enabled = originalEnabled;
    }

    private void DisableHurtbox()
    {
        if (hurtbox) hurtbox.enabled = false;
    }

    private void EnableHurtbox()
    {
        if (hurtbox) hurtbox.enabled = true;
    }
}

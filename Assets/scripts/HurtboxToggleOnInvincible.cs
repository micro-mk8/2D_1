using UnityEngine;

public class HurtboxToggleOnInvincible : MonoBehaviour
{
    [SerializeField] private InvincibilityController invincibility; // PlayerRootの同コンポ
    [SerializeField] private UIHitbox2D hurtbox;                    // PlayerのHurtbox

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
        // フェイルセーフ：無効化のまま残らないように戻す
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

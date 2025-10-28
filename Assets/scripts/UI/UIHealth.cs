using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(UIHitbox2D))]
public class UIHealth : MonoBehaviour, IUIHurtTarget
{
    [Header("HP")]
    [Min(1)] public int maxHP = 3;
    [SerializeField] private int currentHP = -1; // �������� maxHP �ɑ�����
    public int CurrentHP => currentHP;            // �Q�Ɨp�i�ǂݎ���p�j

    [Header("���S���̋���")]
    [Tooltip("���S�����u�Ԃɂ��� GameObject ��j������")]
    public bool destroyOnDeath = false;
    [Tooltip("���S�����u�Ԃ� Hitbox �𖳌����i���d�q�b�g�h�~�j")]
    public bool disableHitboxOnDeath = true;

    [Header("�C�x���g�i�C�ӂ�UI�≉�o�֐ڑ��j")]
    public UnityEvent<int, int> onDamaged; // (currentHP, maxHP)
    public UnityEvent onDead;

    private UIHitbox2D myHitbox;
    private bool isDead;

    void Awake()
    {
        myHitbox = GetComponent<UIHitbox2D>();
    }

    void OnEnable()
    {
        // �������i�ĊJ���������j
        isDead = false;
        currentHP = Mathf.Clamp(currentHP < 0 ? maxHP : currentHP, 0, maxHP);
        if (myHitbox) myHitbox.enabled = true;


        //Hud�ʒm�𔭉΂��Ă��Ȃ�����Hud���̕\�L�����Z�b�g����Ă��Ȃ�
        onDamaged?.Invoke(currentHP, maxHP);

    }

    // ==== ��e�R�[���o�b�N�iUICollisionManager ����Ă΂��j ====
    public void OnHitBy(UIHitbox2D bulletHitbox)
    {
        if (isDead || bulletHitbox == null || myHitbox == null) return;

        // �w�c�������Ȃ疳���i�t�����h���[�t�@�C�A�����j
        if (bulletHitbox.faction == myHitbox.faction) return;

        // �_���[�W�l�i�f�t�H���g1�j
        int dmg = 1;
        var dmgComp = bulletHitbox.GetComponent<UIBulletDamage>();
        if (dmgComp) dmg = Mathf.Max(1, dmgComp.damage);

        // HP �����炷
        currentHP = Mathf.Max(0, currentHP - dmg);
        onDamaged?.Invoke(currentHP, maxHP);

        // ���ǉ��F�X�R�A�֕񍐁i�^�_���[�W�ƌ��j�j��
        ScoringManager.Instance?.ReportDamage(bulletHitbox, this, dmg, currentHP <= 0);


        // ���S����
        if (currentHP <= 0)
        {
            isDead = true;
            if (disableHitboxOnDeath && myHitbox) myHitbox.enabled = false;
            onDead?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }

    // �C�ӁF�O�������/���Z�b�g����������
    public void ResetHP() {
        
    currentHP = maxHP; isDead = false; if (myHitbox) myHitbox.enabled = true; 
    
    onDamaged?.Invoke(currentHP, maxHP);

    }
}


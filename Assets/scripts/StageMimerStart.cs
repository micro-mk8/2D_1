using UnityEngine;

/// <summary>
/// �V�[���J�n���� ScoringManager �̃^�C�}�[�����Z�b�g�i�X�R�A�͈ێ��j
/// </summary>
public class StageTimerStart : MonoBehaviour
{
    [SerializeField] private bool callOnStart = true;
    void Start()
    {
        if (callOnStart) ScoringManager.Instance?.StartNewLevel();
    }
}

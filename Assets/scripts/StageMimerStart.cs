using UnityEngine;

/// <summary>
/// シーン開始時に ScoringManager のタイマーをリセット（スコアは維持）
/// </summary>
public class StageTimerStart : MonoBehaviour
{
    [SerializeField] private bool callOnStart = true;
    void Start()
    {
        if (callOnStart) ScoringManager.Instance?.StartNewLevel();
    }
}

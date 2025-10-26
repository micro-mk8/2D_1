using UnityEngine;

/// <summary>
/// Y軸のみ直進（UIローカル）。上(+Y)向きが既定。負にすれば下向き。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BulletStraightUI : MonoBehaviour
{
    [SerializeField] private float speedPxPerSec = 900f;
    [SerializeField] private float directionSign = +1f; // +1:上, -1:下

    private RectTransform rect;

    void Awake() => rect = GetComponent<RectTransform>();

    public void SetSpeed(float s) => speedPxPerSec = s;
    public void SetUpwards(bool up) => directionSign = up ? +1f : -1f;

    void Update()
    {
        rect.anchoredPosition += new Vector2(0f, directionSign * speedPxPerSec * Time.deltaTime);
    }
}

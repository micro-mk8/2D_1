using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PlayerMoverUI : MonoBehaviour
{
    [Header("移動速度（ピクセル/秒）")]
    [SerializeField] private float moveSpeed = 400f;

    private RectTransform rectTr;

    void Awake()
    {
        rectTr = GetComponent<RectTransform>();
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal"); // ←→ / A D
        float y = Input.GetAxisRaw("Vertical");   // ↑↓ / W S

        Vector2 dir = new Vector2(x, y);
        if (dir.sqrMagnitude > 1f) dir.Normalize(); // 斜めの等速化

        rectTr.anchoredPosition += dir * moveSpeed * Time.deltaTime;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }
}

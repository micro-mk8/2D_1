using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PlayerMoverUI : MonoBehaviour
{
    [Header("�ړ����x�i�s�N�Z��/�b�j")]
    [SerializeField] private float moveSpeed = 400f;

    private RectTransform rectTr;

    void Awake()
    {
        rectTr = GetComponent<RectTransform>();
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal"); // ���� / A D
        float y = Input.GetAxisRaw("Vertical");   // ���� / W S

        Vector2 dir = new Vector2(x, y);
        if (dir.sqrMagnitude > 1f) dir.Normalize(); // �΂߂̓�����

        rectTr.anchoredPosition += dir * moveSpeed * Time.deltaTime;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }
}

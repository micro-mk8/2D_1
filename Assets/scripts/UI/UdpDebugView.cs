using UnityEngine;
using TMPro;

public class UdpDebugView : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    void Update()
    {
        var r = UdpReceiver.Instance;
        if (r == null || text == null) return;

        text.text =
            $"UDP Port: {r.listenPort}\n" +
            $"Raw: {r.latestRaw}\n" +
            $"Accel: {r.latestAccel.x:F2}, {r.latestAccel.y:F2}, {r.latestAccel.z:F2}";
    }
}

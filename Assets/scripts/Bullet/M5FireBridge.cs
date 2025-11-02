using UnityEngine;
using UnityEngine.Events;

public class M5FireBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AllyBulletController controller;  // player bullet shooter
    [SerializeField] private UdpReceiver udp;                   // UDP input from M5

    [Header("Start/Retry trigger")]
    [Tooltip("If true, when FIRE is received (and we're on title/gameover/etc.), raise start/retry event.")]
    [SerializeField] private bool triggerStartRetryOnFire = true;

    // GameFlowController will hook this in the Inspector
    // and call StartOrRetry() when this event fires.
    [SerializeField] private UnityEvent onFirePressedForStartRetry = new UnityEvent();

    private string lastRaw = null; // remember last packet so we don't spam every frame

    void Reset()
    {
        if (controller == null)
        {
            controller = GetComponent<AllyBulletController>();
        }

        if (udp == null)
        {
            udp = FindObjectOfType<UdpReceiver>();
        }
    }

    void Update()
    {
        if (udp == null) return;

        string raw = udp.latestRaw;
        if (string.IsNullOrEmpty(raw)) return;

        // skip duplicate packets
        if (raw == lastRaw) return;
        lastRaw = raw;

        // handle FIRE message
        if (raw.StartsWith("FIRE"))
        {
            // 1. normal shooting
            if (controller != null)
            {
                controller.enableM5Fire = true;
                controller.FireStraightOnce_FromM5();
            }

            // 2. request start/retry
            if (triggerStartRetryOnFire)
            {
                onFirePressedForStartRetry.Invoke();
            }
        }
    }

    // GameFlowController can turn this on/off
    public void SetStartRetryTriggerEnabled(bool enable)
    {
        triggerStartRetryOnFire = enable;
    }

    public UnityEvent GetStartRetryEvent()
    {
        return onFirePressedForStartRetry;
    }
}

// Scripts/Net/UdpReceiver.cs
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using UnityEngine;

public class UdpReceiver : MonoBehaviour
{
    public static UdpReceiver Instance { get; private set; }

    [Header("UDP Settings")]
    public int listenPort = 55001;
    public bool autoStart = true;

    [Header("Latest Payload (Debug)")]
    [TextArea(1, 5)] public string latestRaw = "";
    public Vector3 latestAccel = Vector3.zero; // ax, ay, az

    UdpClient _client;
    bool _running;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent != null) transform.SetParent(null); // ルート化
        DontDestroyOnLoad(gameObject);
    }



    void Start()
    {
        if (autoStart) StartReceive();
    }

    public void StartReceive()
    {
        if (_running) return;
        try
        {
            _client = new UdpClient(listenPort);
            _running = true;
            _ = ReceiveLoop(); // fire & forget
            Debug.Log($"[UDP] Listening on {listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UDP] Start failed: {e}");
            StopReceive();
        }
    }

    public void StopReceive()
    {
        _running = false;
        try { _client?.Close(); } catch { }
        _client = null;
    }

    async Task ReceiveLoop()
    {
        while (_running)
        {
            UdpReceiveResult res;
            try
            {
                res = await _client.ReceiveAsync();
            }
            catch (ObjectDisposedException) { break; }
            catch (SocketException se)
            {
                if (!_running) break;
                Debug.LogWarning($"[UDP] Socket error: {se.Message}");
                continue;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UDP] Receive error: {e.Message}");
                continue;
            }

            string s = "";
            try
            {
                s = Encoding.UTF8.GetString(res.Buffer);
                latestRaw = s.Trim();

                // 推奨: CSV "ax,ay,az" （小数点はピリオド）
                if (latestRaw.Contains(","))
                {
                    var sp = latestRaw.Split(',');
                    if (sp.Length >= 3)
                    {
                        var c = CultureInfo.InvariantCulture;
                        float ax = float.Parse(sp[0], c);
                        float ay = float.Parse(sp[1], c);
                        float az = float.Parse(sp[2], c);
                        latestAccel = new Vector3(ax, ay, az);
                    }
                }
                else if (latestRaw.Contains("{"))
                {
                    latestAccel = ParseAccelJson(latestRaw);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UDP] Parse error: {e.Message} raw={s}");
            }
        }
    }

    Vector3 ParseAccelJson(string json)
    {
        float ax = Extract(json, "\"ax\"");
        float ay = Extract(json, "\"ay\"");
        float az = Extract(json, "\"az\"");
        return new Vector3(ax, ay, az);
    }

    float Extract(string json, string key)
    {
        int i = json.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return 0f;
        i = json.IndexOf(':', i);
        if (i < 0) return 0f;
        int j = i + 1;
        while (j < json.Length && char.IsWhiteSpace(json[j])) j++;
        int k = j;
        while (k < json.Length && "-+0123456789.eE".IndexOf(json[k]) >= 0) k++;
        var c = CultureInfo.InvariantCulture;
        if (float.TryParse(json.Substring(j, k - j), NumberStyles.Float, c, out var v)) return v;
        return 0f;
    }

    void OnDestroy() => StopReceive();
}

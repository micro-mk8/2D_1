using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;

public class DifficultySelectController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameFlowController gameFlow;
    [SerializeField] private TMP_Text label;
    [SerializeField] private RectTransform enemyRoot;
    [SerializeField] private UdpReceiver udp;

    [Header("操作設定")]
    [SerializeField, Min(0.1f)] private float longPressSec = 0.6f;
    [SerializeField, Min(0.05f)] private float repeatBlockSec = 0.18f;

    [Header("難易度一覧")]
    public List<DifficultyOption> options = new List<DifficultyOption>();

    [Serializable]
    public struct DifficultyOption
    {
        [Header("◆ 難易度名")]
        public string label;

        [Header("▶ 基本（全パターン共通）")]
        [Tooltip("発射間隔（EnemyDanmakuController.intervalSec）")]
        public float intervalSec;
        [Tooltip("（EnemyDanmakuController.burstCount）")]
        public int burstCount;

        [Header("▶ 追尾弾（Pattern 0")]
        public bool useAimedTwin;
        [Tooltip("弾速（aimedSpeed）")] public float aimedSpeed;
        [Tooltip("左右オフセット(px)（twinOffsetX）")] public float twinOffsetX;

        [Header("▶ 放射弾（Pattern 1: 半円ばら撒き）")]
        public bool useSemicircle;
        [Tooltip("弾速（radialSpeed）")] public float radialSpeed;
        [Tooltip("角度刻み（度）（radialStepDeg）")] public float radialStepDeg;

        [Header("▶ 分裂弾（Pattern 2　分裂）")]
        public bool useSplit;
        [Tooltip("本体弾速（splitMainSpeed）")] public float splitMainSpeed;
        [Tooltip("分裂までの時間（秒）（splitAfterSec）")] public float splitAfterSec;
        [Tooltip("分裂後の弾数（splitShards）")] public int splitShards;
        [Tooltip("分裂後の弾速（shardSpeed）")] public float shardSpeed;

        [Header("▶ 係数")]
        [Tooltip("雑魚HP倍率")] public float enemyHpMultiplier;
        [Tooltip("ボスHP倍率")] public float bossHpMultiplier;
        [Tooltip("スコア倍率")] public float scoreMultiplier;
    }

    public static DifficultyOption Active { get; private set; }

    private int index = 0;
    private bool intercepting = false;
    private float keyRepeatBlockUntil = 0f;

    private bool m5Down = false;
    private float m5DownTime = 0f;
    private string lastRaw = null;
    
    void Start()
    {
        if (gameFlow != null)
        {
            intercepting = true;
            gameFlow.enabled = false; // タイトル中の即スタートを遅延
        }
        UpdateLabel();

        
        if (options.Count == 0)
        {
            options.Add(new DifficultyOption
            {
                label = "NORMAL",
                intervalSec = 0.50f,
                burstCount = 5,
                useAimedTwin = true,
                aimedSpeed = 420f,
                twinOffsetX = 12f,
                useSemicircle = true,
                radialSpeed = 350f,
                radialStepDeg = 15f,
                useSplit = true,
                splitMainSpeed = 260f,
                splitAfterSec = 0.8f,
                splitShards = 12,
                shardSpeed = 320f,
                enemyHpMultiplier = 1.0f,
                bossHpMultiplier = 1.0f,
                scoreMultiplier = 1.0f
            });
        }
    }

    void Update()
    {
        if (!intercepting) return;

        float now = Time.unscaledTime;

        // Space 短押し = 切替
        if (now >= keyRepeatBlockUntil)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { Prev(); keyRepeatBlockUntil = now + repeatBlockSec; }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { Next(); keyRepeatBlockUntil = now + repeatBlockSec; }
        }
        // Space 長押し = 決定
        if (Input.GetKey(KeyCode.Space))
        {
            m5DownTime += Time.unscaledDeltaTime;
            if (m5DownTime >= longPressSec) { ConfirmAndStart(); return; }
        }
        if (Input.GetKeyUp(KeyCode.Space)) m5DownTime = 0f;

        // M5 FIRE（短押し=切替 / 長押し=決定）
        if (udp != null)
        {
            var raw = udp.latestRaw;
            bool isFire = !string.IsNullOrEmpty(raw) && raw.StartsWith("FIRE");

            if (isFire && !m5Down)
            {
                if (raw != lastRaw) Next();
                m5Down = true;
                m5DownTime = 0f;
            }
            if (isFire && m5Down)
            {
                m5DownTime += Time.unscaledDeltaTime;
                if (m5DownTime >= longPressSec) { ConfirmAndStart(); return; }
            }
            if (!isFire && m5Down) { m5Down = false; m5DownTime = 0f; }

            lastRaw = raw;
        }
    }

    private void Prev()
    {
        if (options.Count == 0) return;
        index = (index - 1 + options.Count) % options.Count;
        UpdateLabel();
    }
    private void Next()
    {
        if (options.Count == 0) return;
        index = (index + 1) % options.Count;
        UpdateLabel();
    }
    private void UpdateLabel()
    {
        if (label != null && options.Count > 0)
            label.text = $"DIFFICULTY : {options[index].label}\n短押し=切替 / 長押し=決定";
    }

    private void ConfirmAndStart()
    {
        if (options.Count == 0 || gameFlow == null) return;

        Active = options[index];
        ApplyDifficultyToScene(Active);

        gameFlow.enabled = true;      // 即スタートを再有効化
        intercepting = false;
        gameFlow.StartOrRetry();      // 既存の開始経路で遷移
    }

    // ---- 適用処理 ----
    private readonly Dictionary<UIHealth, int> _originalMaxHp = new Dictionary<UIHealth, int>();

    private void ApplyDifficultyToScene(DifficultyOption d)
    {
        if (!enemyRoot) return;

        var shooters = enemyRoot.GetComponentsInChildren<EnemyDanmakuController>(true);
        foreach (var s in shooters)
        {
            // パターンON/OFF
            s.enablePattern0_AimedTwin = d.useAimedTwin;
            s.enablePattern1_Semicircle = d.useSemicircle;
            s.enablePattern2_Split = d.useSplit;

            SetField(s, "intervalSec", d.intervalSec);
            SetField(s, "burstCount", d.burstCount);

            SetField(s, "aimedSpeed", d.aimedSpeed);
            SetField(s, "twinOffsetX", d.twinOffsetX);

            SetField(s, "radialSpeed", d.radialSpeed);
            SetField(s, "radialStepDeg", d.radialStepDeg);

            SetField(s, "splitMainSpeed", d.splitMainSpeed);
            SetField(s, "splitAfterSec", d.splitAfterSec);
            SetField(s, "splitShards", d.splitShards);
            SetField(s, "shardSpeed", d.shardSpeed);
        }

        // HP倍率
        var healths = enemyRoot.GetComponentsInChildren<UIHealth>(true);
        foreach (var h in healths)
        {
            if (!_originalMaxHp.TryGetValue(h, out int baseHp))
            {
                baseHp = Mathf.Max(1, h.maxHP);
                _originalMaxHp[h] = baseHp;
            }
            h.maxHP = Mathf.Max(1, Mathf.RoundToInt(baseHp * d.enemyHpMultiplier));
        }

        // スコア倍率（必要な場合のみ）
        if (ScoringManager.Instance != null)
        {
            var sm = ScoringManager.Instance;
            sm.pointsPerKill = Mathf.RoundToInt(sm.pointsPerKill * d.scoreMultiplier);
            sm.pointsPerDamage = Mathf.RoundToInt(sm.pointsPerDamage * d.scoreMultiplier);
        }
    }

  
    private static void SetField<T>(UnityEngine.Object obj, string fieldName, T value)
    {
        var f = obj.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f != null && f.FieldType == typeof(T))
        {
            f.SetValue(obj, value);
        }
    }
}

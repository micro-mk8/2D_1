using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;  // BGM用、loopオン想定
    [SerializeField] private AudioSource sfxSource;  // 効果音用、one-shot再生

    [Header("Clips")]
    [SerializeField] private AudioClip bgmClip;          // プレイ中ずっと流すBGM
    [SerializeField] private AudioClip shootClip;        // 弾を撃つときのSE
    [SerializeField] private AudioClip gameOverClip;     // GameOver時に鳴らすジングル
    [SerializeField] private AudioClip gameClearClip;    // GameClear時に鳴らすジングル

    // ==== BGM系 ====

    // プレイ開始のときに呼ぶ
    public void PlayBGM()
    {
        if (bgmSource == null) return;
        if (bgmClip == null) return;

        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // クリア・ゲームオーバー時に呼ぶ（止める）
    public void StopBGM()
    {
        if (bgmSource == null) return;
        if (bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    // ==== 効果音系 ====

    // プレイヤーが弾を撃った瞬間に呼ぶ
    public void PlayShoot()
    {
        PlayOneShotSafe(shootClip);
    }

    // GameOver画面に入った瞬間に呼ぶ
    public void PlayGameOverJingle()
    {
        PlayOneShotSafe(gameOverClip);
    }

    // GameClear画面に入った瞬間に呼ぶ
    public void PlayGameClearJingle()
    {
        PlayOneShotSafe(gameClearClip);
    }

    // 汎用ワンショット
    private void PlayOneShotSafe(AudioClip clip)
    {
        if (sfxSource == null) return;
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }
}

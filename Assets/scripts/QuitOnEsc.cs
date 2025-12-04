using UnityEngine;

public class QuitOnEsc : MonoBehaviour
{
    void Update()
    {
        // Escキーが押されたら終了
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            // エディタ上では再生を止める
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // ビルドしたアプリでは終了
            Application.Quit();
#endif
        }
    }
}

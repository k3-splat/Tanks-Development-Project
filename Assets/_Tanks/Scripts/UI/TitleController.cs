using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleController : MonoBehaviour
{
    // ボタンに割り当て
    public void OnTapToStart()
    {
        // 必要ならフェード演出を挟む
        SceneManager.LoadScene("Home");
    }

    // 画面タップ全域で開始したい場合（Updateで検知）
    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            SceneManager.LoadScene("Home");
        }
    }
}

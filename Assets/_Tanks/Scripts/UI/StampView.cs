using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StampView : MonoBehaviour
{
    [SerializeField] private Image target;
    [SerializeField] private Sprite[] stamps; // 0..5
    private Coroutine _co;

    public void ShowStamp(int stampId)
    {
        if (stampId < 0 || stampId >= stamps.Length) return;

        if (_co != null) StopCoroutine(_co);

        target.sprite = stamps[stampId];
        target.color = new Color(target.color.r, target.color.g, target.color.b, 1f);
        target.gameObject.SetActive(true);

        _co = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(5f); // 5秒後にフェード開始（仕様）:contentReference[oaicite:10]{index=10}

        float t = 0f;
        float dur = 0.5f;
        var c = target.color;

        while (t < dur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / dur);
            target.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }

        target.gameObject.SetActive(false);
        _co = null;
    }
}

public class ReadyView : MonoBehaviour
{
    [SerializeField] private GameObject readyOn;  // 例：READYランプ
    [SerializeField] private GameObject readyOff;

    public void SetReady(bool ready)
    {
        if (readyOn) readyOn.SetActive(ready);
        if (readyOff) readyOff.SetActive(!ready);
    }
}

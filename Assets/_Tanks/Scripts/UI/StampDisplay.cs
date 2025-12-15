using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StampDisplay : MonoBehaviour
{
    [SerializeField] private Image stampImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float visibleSeconds = 3f;
    [SerializeField] private float fadeSeconds = 2f;

    private Coroutine routine;

    public void Show(Sprite sprite)
    {
        if (sprite == null) return;

        stampImage.sprite = sprite;
        stampImage.enabled = true;

        if (canvasGroup != null) canvasGroup.alpha = 1f;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(visibleSeconds);

        if (canvasGroup != null)
        {
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeSeconds);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        stampImage.enabled = false;
        routine = null;
    }
}

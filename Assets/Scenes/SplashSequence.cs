using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup logoCanvasGroup;

    [Header("Timing Settings")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float holdDuration = 1.3f;
    [SerializeField] private float fadeOutDuration = 1.0f;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "titlescreen";

    private void Start()
    {
        StartCoroutine(PlaySplashRoutine());
    }

    private IEnumerator PlaySplashRoutine()
    {
        // Ensure starting opacity is 0
        logoCanvasGroup.alpha = 0f;

        // 1. Fade from 0 to 100% opacity
        yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeInDuration));

        // 2. Hold at 100% opacity for 1.3 seconds
        yield return new WaitForSeconds(holdDuration);

        // 3. Fade from 100% to 0% opacity
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeOutDuration));

        // 4. Load the titlescreen scene
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            logoCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        logoCanvasGroup.alpha = endAlpha;
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleScreenDirector : MonoBehaviour
{
    [Header("Fade Overlay")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 0.5f;

    [Header("Grid Spatial Zoom-Back")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform gridRect;
    [SerializeField] private Image gridImage;
    [SerializeField] private float gridStartScale = 1.6f;
    [SerializeField] private float gridZoomDuration = 1.0f;

    [Header("Title Card Sequence")]
    [SerializeField] private RectTransform titleCardRect;
    [SerializeField] private CanvasGroup titleCardGroup;
    [SerializeField] private RectTransform titleCornerTarget;
    [SerializeField] private float titleFlickerDuration = 1.0f;
    [SerializeField] private float titlePauseDuration = 0.5f;
    [SerializeField] private float titleShrinkDuration = 0.6f;

    [Header("Map Sequence")]
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private CanvasGroup mapGroup;
    [SerializeField] private InteractiveMapParallax mapParallax;
    [SerializeField] private float mapFlickerDuration = 1.0f;

    [Header("Flicker Tuning")]
    [SerializeField] private float flickerStepInterval = 0.045f;
    [SerializeField] private Vector2 flickerPositionJitter = new Vector2(18f, 10f);
    [SerializeField] private float flickerScaleJitter = 0.06f;

    private void Start()
    {
        SetupInitialStates();
        StartCoroutine(RunTitleSequence());
    }

    private void SetupInitialStates()
    {
        // 1. Ensure FadeOverlay starts fully opaque black
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            Color c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }

        // 2. Size GridImage so scale 1.0 equals exact screen/canvas width, then start larger
        if (gridRect != null && canvasRect != null)
        {
            float targetWidth = canvasRect.rect.width;
            float aspect = 16f / 9f;
            if (gridImage != null && gridImage.sprite != null)
            {
                aspect = gridImage.sprite.rect.width / gridImage.sprite.rect.height;
            }
            gridRect.sizeDelta = new Vector2(targetWidth, targetWidth / aspect);
            gridRect.localScale = Vector3.one * gridStartScale;
        }

        // 3. Hide TitleCard and Map initially
        if (titleCardGroup != null) titleCardGroup.alpha = 0f;
        if (mapGroup != null) mapGroup.alpha = 0f;
        if (mapParallax != null) mapParallax.SetInteractive(false);
    }

    private IEnumerator RunTitleSequence()
    {
        // T = 0.0s: Start 0.5s fade-in AND 1.0s grid spatial zoom-back simultaneously
        StartCoroutine(FadeInRoutine());
        StartCoroutine(GridZoomBackRoutine());

        // Wait for the 0.5s fade-in to complete (halfway through the 1.0s grid zoom-back)
        yield return new WaitForSeconds(fadeInDuration);

        // T = 0.5s: Flicker titlecard to life for 1.0s
        yield return StartCoroutine(FlickerInRoutine(titleCardRect, titleCardGroup, titleFlickerDuration));

        // T = 1.5s: Hold titlecard steady for 0.5s
        yield return new WaitForSeconds(titlePauseDuration);

        // T = 2.0s: Shrink titlecard to top-left corner AND flicker map to life simultaneously
        StartCoroutine(MoveAndScaleTitleRoutine());
        yield return StartCoroutine(FlickerInRoutine(mapRect, mapGroup, mapFlickerDuration));

        // Enable interactive hover parallax once the map has settled
        if (mapParallax != null)
        {
            if (mapRect != null) mapParallax.UpdateBasePosition(mapRect.anchoredPosition);
            mapParallax.SetInteractive(true);
        }
    }

    private IEnumerator FadeInRoutine()
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        Color c = fadeOverlay.color;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeOverlay.color = c;
        fadeOverlay.gameObject.SetActive(false);
    }

    private IEnumerator GridZoomBackRoutine()
    {
        if (gridRect == null) yield break;

        float elapsed = 0f;
        while (elapsed < gridZoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / gridZoomDuration);

            // Cubic ease-out: starts fast, decelerates smoothly to a stop at scale 1.0
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            float currentScale = Mathf.Lerp(gridStartScale, 1f, easedT);
            gridRect.localScale = Vector3.one * currentScale;

            yield return null;
        }

        gridRect.localScale = Vector3.one;
    }

    private IEnumerator FlickerInRoutine(RectTransform targetRect, CanvasGroup targetGroup, float duration)
    {
        if (targetRect == null || targetGroup == null) yield break;

        Vector2 restingPos = targetRect.anchoredPosition;
        Vector3 restingScale = targetRect.localScale;

        float elapsed = 0f;
        float stepTimer = 0f;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            stepTimer += dt;

            if (stepTimer >= flickerStepInterval)
            {
                stepTimer = 0f;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Gradually stabilize opacity as the flicker nears completion
                float minAlpha = Mathf.Lerp(0.05f, 0.75f, progress);
                targetGroup.alpha = Random.value < 0.2f ? 0f : Random.Range(minAlpha, 1f);

                // Jitter position and scale, tapering off near the end
                float jitterDamp = 1f - (progress * 0.6f);
                Vector2 offset = new Vector2(
                    Random.Range(-flickerPositionJitter.x, flickerPositionJitter.x) * jitterDamp,
                    Random.Range(-flickerPositionJitter.y, flickerPositionJitter.y) * jitterDamp
                );
                targetRect.anchoredPosition = restingPos + offset;

                float scaleDelta = Random.Range(-flickerScaleJitter, flickerScaleJitter) * jitterDamp;
                targetRect.localScale = restingScale + new Vector3(scaleDelta, -scaleDelta, 0f);
            }

            yield return null;
        }

        // Snap cleanly to exact resting state
        targetGroup.alpha = 1f;
        targetRect.anchoredPosition = restingPos;
        targetRect.localScale = restingScale;
    }

    private IEnumerator MoveAndScaleTitleRoutine()
    {
        if (titleCardRect == null || titleCornerTarget == null) yield break;

        Vector2 startPos = titleCardRect.anchoredPosition;
        Vector3 startScale = titleCardRect.localScale;
        Vector2 endPos = titleCornerTarget.anchoredPosition;
        Vector3 endScale = titleCornerTarget.localScale;

        float elapsed = 0f;
        while (elapsed < titleShrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / titleShrinkDuration));

            titleCardRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            titleCardRect.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        titleCardRect.anchoredPosition = endPos;
        titleCardRect.localScale = endScale;
    }
}
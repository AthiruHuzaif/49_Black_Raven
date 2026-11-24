using UnityEngine;
using UnityEngine.EventSystems;

public class InteractiveMapParallax : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("References")]
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private RectTransform mapRect;

    [Header("Parallax Settings")]
    [Tooltip("Max pixels the map can shift away from center. Keep smaller than your bleed margin (e.g., 80 if bleed is 120).")]
    [SerializeField] private Vector2 maxPanOffset = new Vector2(80f, 80f);
    [SerializeField] private float smoothSpeed = 6f;

    private Vector2 baseAnchoredPosition;
    private Vector2 targetOffset;
    private bool isHovering;
    private bool isInteractive;

    private void Awake()
    {
        if (viewportRect == null)
            viewportRect = GetComponent<RectTransform>();

        if (mapRect != null)
            baseAnchoredPosition = mapRect.anchoredPosition;
    }

    public void SetInteractive(bool state)
    {
        isInteractive = state;
        if (!state)
        {
            isHovering = false;
            targetOffset = Vector2.zero;
        }
    }

    public void UpdateBasePosition(Vector2 newBasePos)
    {
        baseAnchoredPosition = newBasePos;
    }

    private void Update()
    {
        if (mapRect == null) return;

        Vector2 desiredPosition = baseAnchoredPosition + (isHovering && isInteractive ? targetOffset : Vector2.zero);
        mapRect.anchoredPosition = Vector2.Lerp(mapRect.anchoredPosition, desiredPosition, Time.deltaTime * smoothSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractive) return;
        isHovering = true;
        UpdateTargetOffset(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!isInteractive || !isHovering) return;
        UpdateTargetOffset(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        targetOffset = Vector2.zero;
    }

    private void UpdateTargetOffset(PointerEventData eventData)
    {
        if (viewportRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewportRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            Rect rect = viewportRect.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            // Normalize cursor position relative to viewport center (-1 to +1)
            float normX = Mathf.Clamp(localPoint.x / (rect.width * 0.5f), -1f, 1f);
            float normY = Mathf.Clamp(localPoint.y / (rect.height * 0.5f), -1f, 1f);

            // Invert direction so the map shifts opposite to the mouse cursor
            targetOffset = new Vector2(-normX * maxPanOffset.x, -normY * maxPanOffset.y);
        }
    }
}
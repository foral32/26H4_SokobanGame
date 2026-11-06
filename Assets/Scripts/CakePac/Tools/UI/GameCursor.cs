using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Pixelplacement;
using Sirenix.OdinInspector;
using Unity.VisualScripting;

public class GameCursor : ConoBehavior
{
    public enum CursorMode
    {
        Sprite,
        UIImage
    }

    [Title("Mode")]
    [SerializeField] private CursorMode cursorMode = CursorMode.UIImage;

    public ReferenceSetToggle DisableMovement = new ReferenceSetToggle();
    public ReferenceSetToggle DisableInput = new ReferenceSetToggle();

    [ShowIf("cursorMode", CursorMode.Sprite)]
    [SerializeField] private SpriteRenderer cursorRenderer = default;

    [ShowIf("cursorMode", CursorMode.UIImage)]
    [SerializeField] private Image cursorImage = default;
    // [SerializeField] private Texture2D defaultCursorTexture = default;
    // [SerializeField] private Texture2D pressedCursorTexture = default;

    private Vector3 _originalScale;
    [SerializeField] private float pressedCursorScale = .8f;

    [Title("硬参数")]

    [SerializeField, Tooltip("cursor热点相对于中心点的偏移")]
    private Vector2 spriteHotspotOffset;
    public Vector3 CursorPosition => transform.position + (Vector3)spriteHotspotOffset;


    [Title("Debug")]
    [SerializeField] private bool dragDebug = false;
    [SerializeField] private bool hideCursor = true;

    void Start()
    {
        _originalScale = (cursorMode == CursorMode.Sprite ? cursorRenderer.transform : cursorImage.transform).localScale;
        Cursor.visible = !hideCursor; // 隐藏系统光标

        // Ensure correct setup based on mode
        if (cursorMode == CursorMode.UIImage)
        {
            // Disable sprite renderer if it exists
            if (cursorRenderer != null) cursorRenderer.enabled = false;
            if (cursorImage != null) cursorImage.enabled = true;
        }
        else
        {
            // Disable image if it exists
            if (cursorImage != null) cursorImage.enabled = false;
            if (cursorRenderer != null) cursorRenderer.enabled = true;
        }
    }
    void OnValidate()
    {
        if (cursorMode == CursorMode.Sprite && cursorRenderer == null)
        {
            cursorRenderer = GetComponent<SpriteRenderer>();
        }
        else if (cursorMode == CursorMode.UIImage && cursorImage == null)
        {
            cursorImage = GetComponent<Image>();
        }
    }

    public void Update()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Update position based on mode
        if (cursorMode == CursorMode.Sprite)
        {
            Vector3 screenPos = Input.mousePosition;
            // 防止无效坐标
            if (float.IsNaN(screenPos.x) || float.IsNaN(screenPos.y))
                return;
            Vector3 worldPos = Camera.main != null
                ? Camera.main.ScreenToWorldPoint(screenPos).OverrideZ(transform.position.z)
                : transform.position;
            if (float.IsNaN(worldPos.x) || float.IsNaN(worldPos.y))
                return;
            transform.position = worldPos;
        }
        else
        {
            Vector3 screenPos = Input.mousePosition;
            // 防止无效坐标
            if (float.IsNaN(screenPos.x) || float.IsNaN(screenPos.y))
                return;
            transform.position = screenPos;
        }

        // Update scale based on input
        Transform targetTransform = this.transform;

        if (Input.GetMouseButtonUp(0))
        {
            Tween.LocalScale(targetTransform, _originalScale, 0.05f, 0f, Tween.EaseInOut);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Tween.LocalScale(targetTransform, _originalScale * pressedCursorScale, 0.05f, 0f, Tween.EaseInOut);
        }
    }

    void OnDrawGizmos()
    {
        if (!dragDebug) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(CursorPosition, 0.2f);
    }
}

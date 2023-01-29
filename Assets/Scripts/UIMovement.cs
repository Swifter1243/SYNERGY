using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Script to add movement to the UI based on the cursor position. </summary>
public class UIMovement : MonoBehaviour
{
    /// <summary> The amount of seconds to approach the mouse offset. </summary>
    [Range(0, 3f)]
    public float lerpSpeed = 0.5f;
    /// <summary> The strength of the mouse offset. </summary>
    [Range(0, 0.1f)]
    public float offsetStrength = 0.07f;
    /// <summary> The normalized (0-1) screenspace starting position of the object on the scene being opened. </summary>
    Vector3 originalPos;
    /// <summary> The rect transform of the object. </summary>
    RectTransform rect;
    /// <summary> The interpolator for the mouse position. </summary>
    Utils.Vector3Interpolator mouseLerp;

    /// <summary> Turns an offset based on the center of the screen into a position settable into the transform. </summary>
    /// <param name="offset"> The offset to convert. </param>
    Vector3 OffsetToPosition(Vector3 offset) {
        var originalPosX = originalPos.x * Screen.width;
        var originalPosY = originalPos.y * Screen.height;
        return new Vector3(originalPosX + offset.x, originalPosY + offset.y, 0);
    }

    /// <summary> Gets the offset from a position to the center of the screen. </summary>
    /// <param name="position"> The position to calculate the offset of. </param>
    /// <param name="strength"> The intensity of the offset. </param>
    public static Vector3 GetCenterOffset(Vector3 position, float strength)
    {
        float mouseOffsetX = -(position.x - (Screen.width / 2));
        float mouseOffsetY = -(position.y - (Screen.height / 2));
        return new Vector3(mouseOffsetX * strength, mouseOffsetY * strength, 0);
    }

    void Start()
    {
        mouseLerp = new Utils.Vector3Interpolator(Input.mousePosition, lerpSpeed);
        rect = GetComponent<RectTransform>();
        originalPos = new Vector3(rect.position.x / Screen.width, rect.position.y / Screen.height);
    }

    void Update()
    {
        mouseLerp.lerpSpeed = lerpSpeed;
        var mousePos = mouseLerp.Approach(Input.mousePosition);
        var offset = GetCenterOffset(mousePos, offsetStrength);
        rect.position = OffsetToPosition(offset);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMovement : MonoBehaviour
{
    [Range(0, 3f)]
    public float lerpSpeed = 0.5f;
    [Range(0, 0.1f)]
    public float offsetStrength = 0.07f;
    Vector3 originalPos;
    RectTransform rect;
    Utils.Vector3Interpolator mouseLerp;

    // Turns an offset based on the center of the screen into a position settable into the transform
    Vector3 offsetToPosition(Vector3 offset) {
        var originalPosX = originalPos.x * Screen.width;
        var originalPosY = originalPos.y * Screen.height;
        return new Vector3(originalPosX + offset.x, originalPosY + offset.y, 0);
    }


    // Gets the offset based on the cursor's proximity to the center of the screen
    public static Vector3 getMouseOffset(Vector3 mousePos, float strength)
    {
        float mouseOffsetX = -(mousePos.x - (Screen.width / 2));
        float mouseOffsetY = -(mousePos.y - (Screen.height / 2));
        return new Vector3(mouseOffsetX * strength, mouseOffsetY * strength, 0);
    }

    // Start is called before the first frame update
    void Start()
    {
        mouseLerp = new Utils.Vector3Interpolator(Input.mousePosition, lerpSpeed);
        rect = GetComponent<RectTransform>();
        originalPos = new Vector3(rect.position.x / Screen.width, rect.position.y / Screen.height);
    }

    // Update is called once per frame
    void Update()
    {
        var mousePos = mouseLerp.approach(Input.mousePosition);
        var offset = getMouseOffset(mousePos, offsetStrength);
        rect.position = offsetToPosition(offset);
    }
}

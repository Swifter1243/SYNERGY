using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> The script that runs on the background object. </summary>
public class Background : MonoBehaviour
{
    /// <summary> The multiplier for the offset the mouse has on the background. </summary>
    static float offsetStrength = 0.1f;
    /// <summary> The position which is approaching the mouse position. </summary>
    Vector2 currentPos;
    /// <summary> The material for the background. </summary>
    public Material mat;

    void Start()
    {
        currentPos = getOffset();
    }

    /// <summary> Approaches the cursor position every frame. </summary>
    void Update()
    {
        var mousePos = getOffset();
        currentPos = new Vector2(
            Utils.Approach(currentPos.x, mousePos.x, 2),
            Utils.Approach(currentPos.y, mousePos.y, 2)
        );
        mat.SetVector("_Offset", new Vector2(currentPos.x / Screen.width, currentPos.y / Screen.height));
    }

    /// <summary> Gets the offset position based on the mouse's position. </summary>
    static Vector2 getOffset() => UIMovement.GetCenterOffset(Input.mousePosition, offsetStrength);
}
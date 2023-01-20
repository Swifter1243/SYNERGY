using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    static float offsetStrength = 0.1f;
    Vector2 currentPos;
    public Material mat;

    void Start() {
        currentPos = getMousePos();
    }

    void Update()
    {
        var mousePos = getMousePos();
        currentPos = new Vector2(
            Utils.Approach(currentPos.x, mousePos.x, 2),
            Utils.Approach(currentPos.y, mousePos.y, 2)
        );
        mat.SetVector("_Offset", new Vector2(currentPos.x / Screen.width, currentPos.y / Screen.height));
    }

    static Vector2 getMousePos() => UIMovement.getMouseOffset(Input.mousePosition, offsetStrength);
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextMenuHide : MonoBehaviour
{
    public GameObject target;

    void Update()
    {
        if (target.activeSelf && (Input.GetMouseButtonDown(0) &&
            !RectTransformUtility.RectangleContainsScreenPoint(
                this.gameObject.GetComponent<RectTransform>(),
                Input.mousePosition,
                null))
            || Input.GetKeyDown(KeyCode.Escape))
        {
            target.SetActive(false);
        }
    }
}
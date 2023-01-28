using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Code that runs on context menus. 
/// For example the menu that asks if you'd like to exit the info screen without saving. </summary>
public class ContextMenuHide : MonoBehaviour
{
    /// <summary> The object to hide when the context menu is closed. </summary>
    public GameObject target;

    // Determines when to hide the context menu.
    void Update()
    {
        // Hides if target is visible and:
        // - Escape key is pressed OR
        // - Mouse is clicking outside of gameobject bounds.
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary> The editor variant of DisplayedObject. </summary>
public class EditorDisplayedObject : MonoBehaviour, IPointerDownHandler
{
    /// <summary> The DisplayedObject component of this object. </summary>
    DisplayedObject displayedObject;
    /// <summary> Whether this object is copy selected. </summary>
    public bool copySelected = false;

    // Initialize
    void Start()
    {
        displayedObject = GetComponent<DisplayedObject>();
        if (EditorHandler.copySelected.Contains(this.referenceObj)) {
            copySelected = true;
            displayedObject.ChangeOutline(DisplayedObject.OutlineType.COPYING);
        }
    }

    /// <summary> The object this visual is referring to in the difficulty. </summary>
    public Beatmap.GameplayObject referenceObj
    {
        get => displayedObject.referenceObj;
        set => displayedObject.referenceObj = value;
    }

    // Runs when this object is clicked.
    public void OnPointerDown(PointerEventData eventData)
    {
        // Check if note is being copy selected/deselected
        if (Input.GetKey(KeyCode.C))
        {
            if (TransformGizmo.selectedObj == this.gameObject) return;
            if (copySelected) CopyDeselect();
            else CopySelect();
        }
        // Otherwise register to transform system
        else
        {
            if (EditorHandler.editorMode == EditorHandler.EditorMode.TRANSFORM && !copySelected)
            {
                if (!displayedObject.IsSelectable(EditorHandler.scrollBeat)) return;
                if (TransformGizmo.selectedObj != null) TransformGizmo.objDisplay.ChangeOutline(DisplayedObject.OutlineType.OFF);

                TransformGizmo.avoidDeselect = true;
                TransformGizmo.clickedSelf = true;
                TransformGizmo.Select(this.gameObject);
            }
            else if (EditorHandler.editorMode == EditorHandler.EditorMode.DELETE) EditorHandler.DeleteObject(referenceObj);
        }
    }

    /// <summary> Copy select this object. </summary>
    public void CopySelect()
    {
        if (!copySelected) EditorHandler.copySelected.Add(this.referenceObj);
        copySelected = true;
        displayedObject.ChangeOutline(DisplayedObject.OutlineType.COPYING);
    }

    /// <summary> Copy deselect this object. </summary>
    public void CopyDeselect()
    {
        if (copySelected) EditorHandler.copySelected.Remove(this.referenceObj);
        copySelected = false;
        displayedObject.ChangeOutline(DisplayedObject.OutlineType.OFF);
    }
}

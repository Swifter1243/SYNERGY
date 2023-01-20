using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EditorDisplayedObject : MonoBehaviour, IPointerDownHandler
{
    DisplayedObject displayedObject;
    public bool copySelected = false;
    void Start()
    {
        displayedObject = GetComponent<DisplayedObject>();
        if (EditorHandler.copySelected.Contains(this.referenceObj)) {
            copySelected = true;
            displayedObject.ChangeOutline(DisplayedObject.OutlineType.COPYING);
        }
    }

    public Beatmap.GameplayObject referenceObj
    {
        get => displayedObject.referenceObj;
        set => displayedObject.referenceObj = value;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetKey(KeyCode.C))
        {
            if (TransformGizmo.selectedObj == this.gameObject) return;
            if (copySelected) CopyDeselect();
            else CopySelect();
        }
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
            else if (EditorHandler.editorMode == EditorHandler.EditorMode.DELETE) EditorHandler.Delete(referenceObj);
        }
    }

    public void CopySelect()
    {
        if (!copySelected) EditorHandler.copySelected.Add(this.referenceObj);
        copySelected = true;
        displayedObject.ChangeOutline(DisplayedObject.OutlineType.COPYING);
    }

    public void CopyDeselect()
    {
        if (copySelected) EditorHandler.copySelected.Remove(this.referenceObj);
        copySelected = false;
        displayedObject.ChangeOutline(DisplayedObject.OutlineType.OFF);
    }
}

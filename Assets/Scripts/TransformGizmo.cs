using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransformGizmo : MonoBehaviour
{
    /// <summary> All the modes that the transform gizmo can be in. </summary>
    public enum TransformMode
    {
        IDLE,
        ROTATING,
        TRANSLATING
    }

    /// <summary> The currently selected displayed object. </summary>
    public static GameObject selectedObj;
    /// <summary> The reference object in the difficulty of the selected object. </summary>
    public static Beatmap.GameplayObject referenceObj;
    /// <summary> The rect transform of the selected object. </summary>
    public static RectTransform objRect;
    /// <summary> The DisplayedObject class of the selected object. </summary>
    public static DisplayedObject objDisplay;
    /// <summary> Whether the selected object in a note. </summary>
    public static bool isNote;
    /// <summary> If the selected object is vertical assuming it's a swap. </summary>
    public static bool isVertical = false;
    /// <summary> The game object of the transform gizmo. </summary>
    static GameObject self;
    /// <summary> The canvas in the editor scene. </summary>
    public static Canvas canvas;
    /// <summary> The EditorHandler component of the editor scene. </summary>
    public EditorHandler editorHandler;

    /// <summary> The current mode of the transform gizmo. </summary>
    static TransformMode transformMode = TransformMode.IDLE;
    /// <summary> A value used to queue an action after clicking. </summary>
    float clickBuffer = 100;
    /// <summary> Avoid deselecting the selected object on the next frame if this is true. </summary>
    public static bool avoidDeselect = false;
    /// <summary> Whether the user is currently dragging the selected object. </summary>
    bool dragging = false;
    /// <summary> True when the user clicks on the selected object.  </summary>
    public static bool clickedSelf = false;

    // Translation
    /// <summary> The selected object position before a translation. </summary>
    static Vector3 startPos;
    /// <summary> The cursor position before a translation. </summary>
    static Vector3 startCursorPos;
    /// <summary> Whether the X axis is locked during a translation. </summary>
    static bool lockedX = false;
    /// <summary> Whether the Y axis is locked during a translation. </summary>
    static bool lockedY = false;
    /// <summary> Whether a swap started as vertical before a translation. </summary>
    static bool startVertical = false;

    // Rotation
    /// <summary> The selected object rotation before a rotation. </summary>
    static float startRot;
    /// <summary> The angle from the selected object to the mouse before a rotation. </summary>
    static float startMouseRot;

    /// <summary> Whether a swap started on the x axis before a rotation. </summary>
    public static bool startAxisIsX;

    /// <summary> If the selected object is currently a duplicated object. </summary>
    static bool duplicating;

    /// <summary> A class used for math with guides while transforming. </summary>
    class TransformGuide
    {
        public TransformGuide(float position, bool vertical)
        {
            this.position = position;
            this.vertical = vertical;
        }

        /// <summary> The X or Y position of this guide's axis. </summary>
        public float position;
        /// <summary> Whether this guide's axis is vertical. </summary>
        public bool vertical;

        /// <summary> The distance from an X or Y point to this guide's axis. </summary>
        public float DistanceTo(float position) => Mathf.Abs(this.position - position);
    }

    /// <summary> All active transform guides. </summary>
    List<TransformGuide> guides = new List<TransformGuide>();
    /// <summary> A reference to the editor's visuals. </summary>
    public MapVisuals mapVisuals;

    /// <summary> Action to run after click buffer empties. </summary>
    void ClickBuffer()
    {
        if (!avoidDeselect && selectedObj != null) Deselect();

        if (clickedSelf && Input.GetMouseButton(0))
        {
            dragging = true;
            StartTranslation();
            clickedSelf = false;
        }

        clickBuffer = 100;
        avoidDeselect = false;
    }

    /// <summary> The current X axis guide the selected object will snap to. </summary>
    TransformGuide xGuide;
    /// <summary> The current Y axis guide the selected object will snap to. </summary>
    TransformGuide yGuide;
    /// <summary> The distance the selected object has to be within to snap to a guide. </summary>
    public float snapDist = 20;
    /// <summary> The distance the selected object has to be away from a guide to leave it's snapping. </summary>
    public float leaveDist = 80;

    /// <summary> Clear current guides the selected object can snap to. </summary>
    void ClearGuides()
    {
        guides.Clear();
        xGuide = null;
        yGuide = null;
    }

    void Awake()
    {
        self = this.gameObject;
        canvas = GetComponentInParent<Canvas>();
    }

    /// <summary> If the selected object is being dragged on the timeline. </summary>
    static bool timeDragging = false;
    /// <summary> The time before the selected object was being dragged on the timeline. </summary>
    static float timeDragStart = 0;

    /// <summary> Run process of the transform gizmo. </summary>
    public void DoProcess()
    {
        // Object selection
        if (Input.GetMouseButtonDown(0) && EditorHandler.IsInVisual(Input.mousePosition) && !Input.GetKey(KeyCode.C)) clickBuffer = 0.02f;
        if (clickBuffer < 100) clickBuffer -= Time.deltaTime;
        if (clickBuffer <= 0) ClickBuffer();

        if (selectedObj == null)
        {
            CancelAll();
            this.gameObject.SetActive(false);
            return;
        };

        if (this.gameObject.activeSelf == false) this.gameObject.SetActive(true);
        this.transform.position = selectedObj.transform.position;

        // Deleting with key
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            EditorHandler.DeleteObject(referenceObj);
            Deselect();
            return;
        }

        // Duplicate object
        if (Input.GetKeyDown(KeyCode.D) && !duplicating)
        {
            var newObj = Beatmap.Copy(referenceObj);

            editorHandler.AddObject(newObj);
            Select(mapVisuals.onScreenObjs[newObj]);
            StartTranslation();
            duplicating = true;
        }

        // Mirror object
        if (Input.GetKeyDown(KeyCode.T) && referenceObj is Beatmap.Note note)
        {
            var horizontalMirror = mapVisuals.horizontalMirror;
            var verticalMirror = mapVisuals.verticalMirror;

            var horizontalPressed = Input.GetKey(KeyCode.F1);
            var verticalPressed = Input.GetKey(KeyCode.F2);

            if (horizontalPressed || verticalPressed)
            {
                horizontalMirror = horizontalPressed;
                verticalMirror = verticalPressed;
            }

            if (!horizontalMirror && !verticalMirror) return;

            var newNote = Beatmap.Copy(note);

            newNote.primary = !newNote.primary;
            if (horizontalMirror) newNote.y = 1 - newNote.y;
            if (verticalMirror) newNote.x = 1 - newNote.x;
            if (horizontalMirror != verticalMirror) newNote.direction = Utils.MirrorAngleX(newNote.direction);

            editorHandler.AddObject(newNote, true);
        }

        // Time dragging (press)
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            timeDragging = true;
            timeDragStart = referenceObj.time;
        }

        // Time dragging
        if (Input.GetKey(KeyCode.LeftShift) && timeDragging)
        {
            referenceObj.time = EditorHandler.scrollBeat;
            editorHandler.DrawGrid();
        }

        // Time dragging (release)
        if (Input.GetKeyUp(KeyCode.LeftShift)) FinishTimeDrag();

        // Translation
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (transformMode == TransformMode.TRANSLATING) FinishTranslation();
            else
            {
                CancelAll();
                StartTranslation();
            }
        }

        // Locking X axis
        if (Input.GetKeyDown(KeyCode.X))
        {
            lockedY = false;
            lockedX = !lockedX;
        }

        // Locking Y axis
        if (Input.GetKeyDown(KeyCode.Z))
        {
            lockedX = false;
            lockedY = !lockedY;
        }

        // Finishing translation from click
        if (Input.GetMouseButtonDown(0) && transformMode == TransformMode.TRANSLATING && !dragging && !Input.GetKey(KeyCode.C)) FinishTranslation();

        // Translating position
        if (transformMode == TransformMode.TRANSLATING)
        {
            if (isNote)
            {
                var delta = Input.mousePosition - startCursorPos;
                if (lockedX) delta.y = 0;
                if (lockedY) delta.x = 0;
                var pos = startPos + delta;

                // Guide snapping
                if (guides.Count > 0)
                {
                    TransformGuide closestX = null;
                    TransformGuide closestY = null;

                    guides.ForEach(g =>
                    {
                        if (g.vertical)
                        {
                            if (closestX == null) closestX = g;
                            if (closestX.DistanceTo(pos.x) > g.DistanceTo(pos.x)) closestX = g;
                        }
                        else
                        {
                            if (closestY == null) closestY = g;
                            if (closestY.DistanceTo(pos.y) > g.DistanceTo(pos.y)) closestY = g;
                        }
                    });

                    if (xGuide == null && closestX.DistanceTo(pos.x) <= snapDist) xGuide = closestX;
                    if (yGuide == null && closestY.DistanceTo(pos.y) <= snapDist) yGuide = closestY;

                    if (xGuide != null && closestX.DistanceTo(pos.x) < xGuide.DistanceTo(pos.x)) xGuide = closestX;
                    if (yGuide != null && closestY.DistanceTo(pos.y) < yGuide.DistanceTo(pos.y)) yGuide = closestY;

                    if (xGuide != null && xGuide.DistanceTo(pos.x) >= leaveDist) xGuide = null;
                    if (yGuide != null && yGuide.DistanceTo(pos.y) >= leaveDist) yGuide = null;

                    if (xGuide != null) pos.x = xGuide.position;
                    if (yGuide != null) pos.y = yGuide.position;
                }

                selectedObj.transform.position = pos;
                ForceInside(selectedObj);
            }
            else
            {
                var pos = EditorHandler.VisualToScreen(Input.mousePosition);
                pos.x /= Screen.width;
                pos.y /= Screen.height;
                pos.x -= 0.5f;
                pos.y -= 0.5f;

                var angle = GetMouseAngle();

                if (
                    (angle >= 45 && angle <= 135) ||
                    (angle <= -45 && angle >= -135)
                ) MakeSwapHorizontal();
                else MakeSwapVertical();
            }
        }

        // Rotation
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (transformMode == TransformMode.ROTATING) FinishRotation();
            else
            {
                CancelAll();
                StartRotation();
            }
        }

        // Finish rotation from click
        if (Input.GetMouseButtonDown(0) && transformMode == TransformMode.ROTATING && !dragging) FinishRotation();

        // Translating rotation
        if (transformMode == TransformMode.ROTATING)
        {
            var angle = startMouseRot + startRot - GetMouseAngle();
            if (isNote)
            {
                if (Input.GetKey(KeyCode.LeftControl)) angle = Mathf.Round(angle / 10) * 10;
                selectedObj.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                var mouseRot = GetMouseAngle();
                var difference = mouseRot - startMouseRot;
                difference = Mathf.Abs((difference + 180) % 360 - 180);

                if (difference >= 45 && difference <= 135)
                {
                    if (startVertical) MakeSwapHorizontal();
                    else MakeSwapVertical();
                }
                else
                {
                    if (startVertical) MakeSwapVertical();
                    else MakeSwapHorizontal();
                }
            }
        }

        // General
        if (Input.GetKeyDown(KeyCode.Escape)) CancelAll();

        if (dragging && !Input.GetMouseButton(0))
        {
            FinishAll();
            dragging = false;
        }

        // Refreshing guides
        if (Input.GetKeyDown(KeyCode.LeftControl) && transformMode == TransformMode.TRANSLATING && isNote)
        {
            ClearGuides();

            foreach (var val in mapVisuals.onScreenObjs.Values)
            {
                if (val == selectedObj) continue;
                guides.Add(new TransformGuide(val.transform.position.x, true));
                guides.Add(new TransformGuide(val.transform.position.y, false));
            }
        }

        // Clearing guides
        if (!Input.GetKey(KeyCode.LeftControl) && guides.Count > 0) ClearGuides();
    }

    /// <summary> Select an object with the transform gizmo. </summary>
    /// <param name="obj"> The object to select. </param>
    public static void Select(GameObject obj)
    {
        var displayedObj = obj.GetComponent<DisplayedObject>();
        if (EditorHandler.editorMode != EditorHandler.EditorMode.TRANSFORM) return;
        if (EditorHandler.copySelected.Contains(displayedObj.referenceObj)) return;
        if (selectedObj != null) Deselect();
        objDisplay = displayedObj;
        selectedObj = obj;
        referenceObj = objDisplay.referenceObj;
        objRect = obj.GetComponent<RectTransform>();
        isNote = EditorHandler.isNote(referenceObj);
        objDisplay.ChangeOutline(DisplayedObject.OutlineType.TRANSFORMING);
        if (!isNote) isVertical = (referenceObj as Beatmap.Swap).type == Beatmap.SwapType.Vertical;
    }

    /// <summary> Force an object inside of the bounds of the map visuals in the editor. </summary>
    /// <param name="obj"> The object to force inside. </param>
    public static void ForceInside(GameObject obj)
    {
        var screenPos = EditorHandler.VisualToScreen(obj.transform.position);
        var rect = obj.GetComponent<RectTransform>();
        var bounds = RectTransformUtility.PixelAdjustRect(rect, canvas);
        var width = bounds.width * EditorHandler.widthScalar;
        var height = bounds.height * EditorHandler.heightScalar;
        width /= 1.5f;
        height /= 1.5f;

        if (screenPos.x > Screen.width - width) screenPos.x = Screen.width - width;
        if (screenPos.x < width) screenPos.x = width;
        if (screenPos.y > Screen.height - height) screenPos.y = Screen.height - height;
        if (screenPos.y < height) screenPos.y = height;

        obj.transform.position = EditorHandler.ScreenToVisual(screenPos);
    }

    /// <summary> Deselect the selected object. </summary>
    public static void Deselect()
    {
        if (selectedObj == null) return;
        objDisplay.ChangeOutline(DisplayedObject.OutlineType.OFF);
        FinishTimeDrag();
        selectedObj = null;
        referenceObj = null;
        self.SetActive(false);
    }

    /// <summary> Start a translation with the transform gizmo. </summary>
    void StartTranslation()
    {
        if (isNote)
        {
            lockedX = false;
            lockedY = false;
            startPos = selectedObj.transform.position;
            startCursorPos = Input.mousePosition;
        }
        else startVertical = isVertical;
        transformMode = TransformMode.TRANSLATING;
        duplicating = false;
    }

    /// <summary> Cancel a translation with the transform gizmo. </summary>
    void CancelTranslation()
    {
        if (selectedObj == null) return;

        if (duplicating)
        {
            var obj = referenceObj;
            Deselect();
            EditorHandler.RemoveObject(obj);
            mapVisuals.Redraw();
            editorHandler.DrawGrid();
            return;
        }

        if (isNote) selectedObj.transform.position = startPos;
        else
        {
            if (startVertical) MakeSwapVertical();
            else MakeSwapHorizontal();
        }

        transformMode = TransformMode.IDLE;
    }

    /// <summary> Finish a translation with the transform gizmo. </summary>
    void FinishTranslation()
    {
        // Resetting transform gizmo state
        transformMode = TransformMode.IDLE;
        if (Input.GetMouseButtonDown(0)) avoidDeselect = true;

        // Register duplication action
        if (duplicating)
        {
            var placeAction = new EditorHandler.PlaceAction();
            placeAction.obj = referenceObj;
            EditorHandler.AddAction(placeAction);
        }

        if (isNote)
        {
            // Get normalized (0-1) screen coordinates of note
            Vector2 notePos = EditorHandler.VisualToScreen(selectedObj.transform.position);
            notePos.x /= Screen.width;
            notePos.y /= Screen.height;

            // Start creating action
            var action = new EditorHandler.TranslateNoteAction();
            action.obj = referenceObj;

            // Store information into difficulty note
            var referenceNote = referenceObj as Beatmap.Note;
            action.startPos = new Vector2(referenceNote.x, referenceNote.y);
            referenceNote.x = notePos.x;
            referenceNote.y = notePos.y;
            action.endPos = new Vector2(referenceNote.x, referenceNote.y);

            // Finalize
            if (duplicating) return;
            if (action.startPos.x != action.endPos.x || action.startPos.y != action.endPos.y)
                EditorHandler.AddAction(action);
        }
        else
        {
            // Store information into difficulty swap
            (referenceObj as Beatmap.Swap).type = isVertical ? Beatmap.SwapType.Vertical : Beatmap.SwapType.Horizontal;

            // Register action
            var action = new EditorHandler.MoveSwapAction();
            action.obj = referenceObj;
            action.startVertical = startVertical;
            action.endVertical = isVertical;
            if (duplicating) return;
            if (startVertical != isVertical) EditorHandler.AddAction(action);
        }
    }

    /// <summary> Get the angle from the selected object to the mouse. </summary>
    float GetMouseAngle() => Utils.GetAngleFromPos2D(Input.mousePosition, selectedObj.transform.position);

    /// <summary> Start a rotation with the transform gizmo. </summary>
    void StartRotation()
    {
        startMouseRot = GetMouseAngle();
        if (isNote) startRot = selectedObj.transform.rotation.eulerAngles.z;
        else startVertical = isVertical;
        transformMode = TransformMode.ROTATING;
        duplicating = false;
    }

    /// <summary> Cancel a rotation with the transform gizmo. </summary>
    void CancelRotation()
    {
        if (selectedObj == null) return;
        if (isNote) selectedObj.transform.rotation = Quaternion.Euler(0, 0, startRot);
        else
        {
            if (startVertical) MakeSwapVertical();
            else MakeSwapHorizontal();
        }
        transformMode = TransformMode.IDLE;
    }

    /// <summary> Finish a rotation with the transform gizmo. </summary>
    void FinishRotation()
    {
        mapVisuals.CheckSwaps();
        transformMode = TransformMode.IDLE;
        if (Input.GetMouseButtonDown(0)) avoidDeselect = true;

        if (isNote)
        {
            var noteObj = referenceObj as Beatmap.Note;

            var action = new EditorHandler.RotateNoteAction();
            action.obj = noteObj;
            action.startAngle = noteObj.direction;
            noteObj.direction = selectedObj.transform.eulerAngles.z;
            action.endAngle = noteObj.direction;
            if (duplicating) return;
            if (action.startAngle != action.endAngle) EditorHandler.AddAction(action);
        }
        else
        {
            (referenceObj as Beatmap.Swap).type = isVertical ? Beatmap.SwapType.Vertical : Beatmap.SwapType.Horizontal;

            var action = new EditorHandler.MoveSwapAction();
            action.obj = referenceObj;
            action.startVertical = startVertical;
            action.endVertical = isVertical;
            if (duplicating) return;
            if (startVertical != isVertical) EditorHandler.AddAction(action);
        }
    }

    /// <summary> Cancel any ongoing actions with the transform gizmo. </summary>
    void CancelAll()
    {
        if (transformMode == TransformMode.TRANSLATING) CancelTranslation();
        if (transformMode == TransformMode.ROTATING) CancelRotation();
    }

    /// <summary> Finish any ongoing actions with the transform gizmo. </summary>
    void FinishAll()
    {
        if (transformMode == TransformMode.TRANSLATING) FinishTranslation();
        if (transformMode == TransformMode.ROTATING) FinishRotation();
    }

    /// <summary> Finish dragging the selected object through the timeline. </summary>
    static void FinishTimeDrag()
    {
        if (!timeDragging) return;
        timeDragging = false;
        var action = new EditorHandler.TimeAction();
        action.obj = referenceObj;
        action.startTime = timeDragStart;
        action.endTime = EditorHandler.scrollBeat;
        EditorHandler.AddAction(action);
    }

    /// <summary> Start dragging the selected object on the X axis. </summary>
    public void DragX()
    {
        dragging = true;
        avoidDeselect = true;
        StartTranslation();
        lockedX = true;
    }
    
    /// <summary> Start dragging the selected object on the Y axis. </summary>
    public void DragY()
    {
        dragging = true;
        avoidDeselect = true;
        StartTranslation();
        lockedY = true;
    }

    /// <summary> Start drag rotating the selected object. </summary>
    public void DragRotate()
    {
        dragging = true;
        avoidDeselect = true;
        StartRotation();
    }

    /// <summary> Update the selected swap to be horizontal. </summary>
    public void MakeSwapHorizontal()
    {
        if (isNote || !isVertical) return;
        isVertical = false;
        var rect = selectedObj.GetComponent<RectTransform>();
        selectedObj.transform.rotation = Quaternion.Euler(0, 0, 0);
        var sizeDelta = rect.sizeDelta;
        sizeDelta.x = mapVisuals.GetBounds().width;
        rect.sizeDelta = sizeDelta;
    }

    /// <summary> Update the selected swap to be vertical. </summary>
    public void MakeSwapVertical()
    {
        if (isNote || isVertical) return;
        isVertical = true;
        var rect = selectedObj.GetComponent<RectTransform>();
        selectedObj.transform.rotation = Quaternion.Euler(0, 0, 90);
        var sizeDelta = rect.sizeDelta;
        sizeDelta.x = mapVisuals.GetBounds().height;
        rect.sizeDelta = sizeDelta;
    }

    /// <summary> Redraw the map visuals, while re-selecting the selected object afterward. </summary>
    public void RedrawVisuals()
    {
        var oldSelectedObj = referenceObj;
        mapVisuals.Redraw();
        if (oldSelectedObj != null && mapVisuals.onScreenObjs.ContainsKey(oldSelectedObj))
            Select(mapVisuals.onScreenObjs[referenceObj]);
    }
}

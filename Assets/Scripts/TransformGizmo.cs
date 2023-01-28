using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransformGizmo : MonoBehaviour
{
    public enum TransformMode
    {
        IDLE,
        ROTATING,
        TRANSLATING
    }

    public static GameObject selectedObj;
    public static Beatmap.GameplayObject referenceObj;
    public static RectTransform objRect;
    public static DisplayedObject objDisplay;
    public static bool isNote;
    public static bool isVertical = false;
    static GameObject self;
    public static Canvas canvas;
    public EditorHandler editorHandler;

    static TransformMode transformMode = TransformMode.IDLE;
    float clickBuffer = 100;
    public static bool avoidDeselect = false;
    bool dragging = false;
    public static bool clickedSelf = false;

    // Translation
    static Vector3 startPos;
    static Vector3 startCursorPos;
    static bool lockedX = false;
    static bool lockedY = false;
    static bool startVertical = false;

    // Rotation
    static float startRot;
    static float startMouseRot;

    public static bool startAxisIsX;
    static bool duplicating;

    class TransformGuide
    {
        public TransformGuide(float position, bool vertical)
        {
            this.position = position;
            this.vertical = vertical;
        }

        public float position;
        public bool vertical;

        public float DistanceTo(float position) => Mathf.Abs(this.position - position);
    }

    List<TransformGuide> guides = new List<TransformGuide>();
    public MapVisuals mapVisuals;

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

    TransformGuide xGuide;
    TransformGuide yGuide;
    public float snapDist = 20;
    public float leaveDist = 80;

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

    static bool timeDragging = false;
    static float timeDragStart = 0;

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

        // Deleting With Key
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            EditorHandler.DeleteObject(referenceObj);
            Deselect();
            return;
        }

        // Duplicate Object
        if (Input.GetKeyDown(KeyCode.D) && !duplicating)
        {
            var newObj = Beatmap.Copy(referenceObj);

            editorHandler.AddObject(newObj);
            Select(mapVisuals.onScreenObjs[newObj]);
            StartTranslation();
            duplicating = true;
        }

        // Mirror Object
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

        // Time Dragging (Press)
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            timeDragging = true;
            timeDragStart = referenceObj.time;
        }

        // Time Dragging
        if (Input.GetKey(KeyCode.LeftShift) && timeDragging)
        {
            referenceObj.time = EditorHandler.scrollBeat;
            editorHandler.DrawGrid();
        }

        // Time Dragging (Release)
        if (Input.GetKeyUp(KeyCode.LeftShift)) FinishDrag();

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

        // Locking X Axis
        if (Input.GetKeyDown(KeyCode.X))
        {
            lockedY = false;
            lockedX = !lockedX;
        }

        // Locking Y Axis
        if (Input.GetKeyDown(KeyCode.Z))
        {
            lockedX = false;
            lockedY = !lockedY;
        }

        // Finishing Translation From Click
        if (Input.GetMouseButtonDown(0) && transformMode == TransformMode.TRANSLATING && !dragging && !Input.GetKey(KeyCode.C)) FinishTranslation();

        // Translating Position
        if (transformMode == TransformMode.TRANSLATING)
        {
            if (isNote)
            {
                var delta = Input.mousePosition - startCursorPos;
                if (lockedX) delta.y = 0;
                if (lockedY) delta.x = 0;
                var pos = startPos + delta;

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

        if (Input.GetMouseButtonDown(0) && transformMode == TransformMode.ROTATING && !dragging) FinishRotation();

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

        // Guides
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

        if (!Input.GetKey(KeyCode.LeftControl) && guides.Count > 0) ClearGuides();
    }

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

    public static void Deselect()
    {
        if (selectedObj == null) return;
        objDisplay.ChangeOutline(DisplayedObject.OutlineType.OFF);
        FinishDrag();
        selectedObj = null;
        referenceObj = null;
        self.SetActive(false);
    }

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

    void FinishTranslation()
    {
        transformMode = TransformMode.IDLE;
        if (Input.GetMouseButtonDown(0)) avoidDeselect = true;

        if (duplicating)
        {
            var placeAction = new EditorHandler.PlaceAction();
            placeAction.obj = referenceObj;
            EditorHandler.AddAction(placeAction);
        }

        if (isNote)
        {
            Vector2 notePos = EditorHandler.VisualToScreen(selectedObj.transform.position);
            notePos.x /= Screen.width;
            notePos.y /= Screen.height;

            var action = new EditorHandler.TranslateNoteAction();
            action.obj = referenceObj;

            var referenceNote = referenceObj as Beatmap.Note;
            action.startPos = new Vector2(referenceNote.x, referenceNote.y);
            referenceNote.x = notePos.x;
            referenceNote.y = notePos.y;
            action.endPos = new Vector2(referenceNote.x, referenceNote.y);

            if (duplicating) return;
            if (action.startPos.x != action.endPos.x || action.startPos.y != action.endPos.y)
                EditorHandler.AddAction(action);
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

    float GetMouseAngle() => Utils.GetAngleFromPos(Input.mousePosition, selectedObj.transform.position);

    void StartRotation()
    {
        startMouseRot = GetMouseAngle();
        if (isNote) startRot = selectedObj.transform.rotation.eulerAngles.z;
        else startVertical = isVertical;
        transformMode = TransformMode.ROTATING;
        duplicating = false;
    }

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

    void CancelAll()
    {
        if (transformMode == TransformMode.TRANSLATING) CancelTranslation();
        if (transformMode == TransformMode.ROTATING) CancelRotation();
    }

    void FinishAll()
    {
        if (transformMode == TransformMode.TRANSLATING) FinishTranslation();
        if (transformMode == TransformMode.ROTATING) FinishRotation();
    }

    static void FinishDrag()
    {
        if (!timeDragging) return;
        timeDragging = false;
        var action = new EditorHandler.TimeAction();
        action.obj = referenceObj;
        action.startTime = timeDragStart;
        action.endTime = EditorHandler.scrollBeat;
        EditorHandler.AddAction(action);
    }

    public void DragX()
    {
        dragging = true;
        avoidDeselect = true;
        StartTranslation();
        lockedX = true;
    }

    public void DragY()
    {
        dragging = true;
        avoidDeselect = true;
        StartTranslation();
        lockedY = true;
    }

    public void DragRotate()
    {
        dragging = true;
        avoidDeselect = true;
        StartRotation();
    }

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

    public void RedrawVisuals()
    {
        var oldSelectedObj = referenceObj;
        mapVisuals.Redraw();
        if (oldSelectedObj != null && mapVisuals.onScreenObjs.ContainsKey(oldSelectedObj))
            Select(mapVisuals.onScreenObjs[referenceObj]);
    }
}

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

public class EditorHandler : MonoBehaviour
{
    public static float scrollBeat = 0;
    [Range(1f / 64f, 1)]
    public float scrollPrecision = 1f / 2f; // Scroll pecision
    [Range(0.2f, 3)]
    public float editorScale = 1;
    public Slider editorScaleSlider;
    float songBeats = 100000; // The amount of base beats that the song extends
    public float beatScale = 1f / 8f; // The width of the screen that each base beat will take up
    public float despawnDist = 0.1f;
    public float gridWidth = 0.2f; // Percentage of the screen the grid should take up
    public float iconSize = 0.3f; // Percentage of the grid width that the icons will be scaled to
    public GameObject beatGuide;
    List<GameObject> gridElements = new List<GameObject>();
    Utils.NumberWatcher watcher = new Utils.NumberWatcher();
    Utils.NumberWatcher screenWatcher = new Utils.NumberWatcher();
    public Button noteGridImage;
    public Button swapGridImage;
    public Texture2D noteIcon;
    public static Beatmap.Difficulty diff;
    Beatmap.Info info;
    public MapVisuals mapVisualRef;
    public static MapVisuals mapVisuals;
    public static bool avoidDeselect = false;
    public TransformGizmo transformGizmo;
    bool playing = false;
    public static AudioSource audioSource;
    public Text timeText;
    public Scrollbar scrollbar;
    public InputField precision1;
    public Text precision2;
    float precisionMin = 1;
    float precisionMax = 64;
    public static bool refreshGrid = false;
    public GameObject deleteOnIcon;
    public GameObject transformOnIcon;
    public GameObject noteOnIcon;
    public GameObject swapOnIcon;
    public Text statusText;
    public static List<Beatmap.GameplayObject> copySelected = new List<Beatmap.GameplayObject>();
    List<Beatmap.GameplayObject> clipboard = new List<Beatmap.GameplayObject>();
    public GameObject grid;

    public enum EditorMode
    {
        DELETE,
        TRANSFORM,
        NOTE,
        SWAP
    }

    public static EditorMode editorMode = EditorMode.TRANSFORM;
    public static bool axisMode = false;
    public static bool primaryMode = true;

    public void SetMode(EditorMode mode)
    {
        if (mode != editorMode)
        {
            if (editorMode == EditorMode.DELETE) deleteOnIcon.SetActive(false);
            if (editorMode == EditorMode.TRANSFORM) transformOnIcon.SetActive(false);
            if (editorMode == EditorMode.NOTE) noteOnIcon.SetActive(false);
            if (editorMode == EditorMode.SWAP) swapOnIcon.SetActive(false);

            if (mode == EditorMode.DELETE) deleteOnIcon.SetActive(true);
            if (mode == EditorMode.TRANSFORM) transformOnIcon.SetActive(true);
            if (mode == EditorMode.NOTE) noteOnIcon.SetActive(true);
            if (mode == EditorMode.SWAP) swapOnIcon.SetActive(true);
        }

        editorMode = mode;
        if (mode != EditorMode.TRANSFORM) TransformGizmo.Deselect();
    }

    public RawImage noteButtonIcon;
    public GameObject axisOnIcon;
    public Image primaryIcon;

    void UpdateNoteButton()
    {
        var note = new Beatmap.Note();
        note.axis = axisMode;
        note.primary = primaryMode;
        mapVisuals.NoteGraphic(note, noteButtonIcon);
        axisOnIcon.SetActive(axisMode);
        if (primaryMode) primaryIcon.color = Beatmap.PrimaryColor;
        else primaryIcon.color = Beatmap.SecondaryColor;
    }

    public void ToggleAxisMode()
    {
        axisMode = !axisMode;
        UpdateNoteButton();
    }

    public void TogglePrimaryMode()
    {
        primaryMode = !primaryMode;
        UpdateNoteButton();
    }

    public void ModeDelete() => SetMode(EditorMode.DELETE);
    public void ModeTransform() => SetMode(EditorMode.TRANSFORM);
    public void ModeNote() => SetMode(EditorMode.NOTE);
    public void ModeSwap() => SetMode(EditorMode.SWAP);

    void Start()
    {
        if (Beatmap.Active.diffName == null)
        {
            var songPath = new DirectoryInfo(SongHandler.songsFolder).GetDirectories()[0].ToString();
            var diffName = "/hard.dat";
            var diffPath = songPath + diffName;
            var audioPath = songPath + "/song.ogg";
            var songInfo = SongHandler.getInfoFromPath(songPath);

            Beatmap.Active.songPath = songPath;
            Beatmap.Active.diffName = diffName;
            Beatmap.Active.diffPath = diffPath;
            Beatmap.Active.info = songInfo;
        }

        scrollPrecision = 1 / Utils.InitPlayerPrefsFloat("precision1", 2);
        precision1.text = PlayerPrefs.GetFloat("precision1").ToString();
        precision2.text = Utils.InitPlayerPrefsFloat("precision2", 8).ToString();
        editorScaleSlider.value = Utils.InitPlayerPrefsFloat("editorScale", 1);
        primaryMode = Utils.InitPlayerPrefsInt("primaryMode", 1) == 1;
        axisMode = Utils.InitPlayerPrefsInt("axisMode", 0) == 1;

        if (diff == null)
        {
            var rawData = File.ReadAllText(Beatmap.Active.diffPath);
            diff = JsonUtility.FromJson<Beatmap.Difficulty>(rawData);
            diff.notes.Sort((a, b) => ((int)a.time) - ((int)b.time));
            diff.swaps.Sort((a, b) => ((int)a.time) - ((int)b.time));
            diff.bpmChanges.Sort((a, b) => ((int)a.time) - ((int)b.time));
        }

        mapVisuals = mapVisualRef;
        mapVisuals.diff = diff;
        info = Beatmap.Active.info;

        // Audio
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = Settings.masterVolume;
        if (Beatmap.Active.audio == null)
        {
            var audioPath = Utils.GetAudioPath(Beatmap.Active.songPath, info);
            StartCoroutine(LoadAudio(audioPath));
        }
        else SetAudio(Beatmap.Active.audio);

        // Video
        var videoPath = Utils.GetVideoPath(Beatmap.Active.songPath, info);
        mapVisuals.LoadVideo(videoPath);

        EventTrigger trigger = scrollbar.GetComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Drag;
        entry.callback.AddListener((data) => { Scroll(); });
        trigger.triggers.Add(entry);

        UpdateVisualsTransform();
        UpdateNoteButton();
        mapVisuals.UpdateBeat(scrollBeat);
    }

    public void Save(bool showMessage = true)
    {
        PlayerPrefs.SetFloat("precision1", float.Parse(precision1.text));
        PlayerPrefs.SetFloat("precision2", float.Parse(precision2.text));
        PlayerPrefs.SetFloat("editorScale", editorScale);
        PlayerPrefs.SetInt("primaryMode", primaryMode ? 1 : 0);
        PlayerPrefs.SetInt("axisMode", axisMode ? 1 : 0);
        File.WriteAllText(Beatmap.Active.diffPath, JsonUtility.ToJson(diff));
        if (showMessage) StatusMessage("Map Saved!");
    }

    public void Exit()
    {
        Save(false);
        SongHandler.loadSong = Beatmap.Active.songPath;
        Transition.Load("EditorSongs");
    }

    public void Play()
    {
        Save(false);
        PlayHandler.diff = diff;
        PlayHandler.exit = () => Transition.Load("Editor");
        var seconds = Input.GetKey(KeyCode.LeftControl) ? 0 : Utils.BeatToSeconds(scrollBeat, info.BPM);
        PlayHandler.seconds = seconds;
        PlayHandler.startSeconds = seconds;
        Transition.Load("Playing");
    }

    public void StatusMessage(string message)
    {
        statusText.text = message;
        var errorColor = statusText.color;
        errorColor.a = 1;
        statusText.color = errorColor;
    }

    public IEnumerator LoadAudio(string path)
    {
        var www = UnityWebRequestMultimedia.GetAudioClip($"file:///{Uri.EscapeDataString($"{path}")}", AudioType.OGGVORBIS);
        yield return www.SendWebRequest();
        var clip = DownloadHandlerAudioClip.GetContent(www);
        Beatmap.Active.audio = clip;
        SetAudio(clip);
    }

    void SetAudio(AudioClip clip)
    {
        audioSource.clip = clip;
        songBeats = Utils.GetSongBeats(clip.length, info.BPM);
        DrawGrid();
    }

    void ToggleNotePrimary(Beatmap.Note note)
    {
        note.primary = !note.primary;
        DrawGrid();
        transformGizmo.RedrawVisuals();
    }

    void ToggleNoteAxis(Beatmap.Note note)
    {
        note.axis = !note.axis;
        DrawGrid();
        transformGizmo.RedrawVisuals();
    }

    void Update()
    {
        // Status Text Fade
        if (statusText.color.a > 0)
        {
            var errorColor = statusText.color;
            errorColor.a = Mathf.Max(errorColor.a - Time.deltaTime, 0);
            statusText.color = errorColor;
        }

        // Save
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S)) Save();

        // Undo
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) Undo();

        // Redo
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y)) Redo();

        // Clear Copy Selected
        if (Input.GetKey(KeyCode.C) && Input.GetMouseButtonDown(1)) ClearCopySelected();

        // Copy
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
        {
            clipboard.Clear();
            copySelected.ForEach(x => clipboard.Add(Beatmap.Copy(x)));
            if (TransformGizmo.selectedObj != null) clipboard.Add(TransformGizmo.referenceObj);
            if (clipboard.Count > 0) StatusMessage("Copied " + clipboard.Count + " objects.");
            ClearCopySelected(false);
        }

        // Paste
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V))
        {
            var minTime = float.MaxValue;
            var newObjs = new List<Beatmap.GameplayObject>();
            clipboard.ForEach(x => { if (x.time < minTime) minTime = x.time; });
            clipboard.ForEach(x =>
            {
                var newObj = Beatmap.Copy(x);
                newObj.time = newObj.time - minTime + scrollBeat;
                PushObject(newObj);
                transformGizmo.RedrawVisuals();
                DrawGrid();
                newObjs.Add(newObj);
            });
            if (clipboard.Count > 0)
            {
                StatusMessage("Pasted " + clipboard.Count + " objects.");
                var action = new PasteAction();
                var obj = newObjs[0];
                newObjs.ForEach(x => { if (x.time < obj.time) obj = x; });
                action.obj = obj;
                action.objs = newObjs;
                AddAction(action);
            }
        }

        // Toggle Primary Mode
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (TransformGizmo.selectedObj == null) TogglePrimaryMode();
            else if (TransformGizmo.referenceObj is Beatmap.Note)
            {
                ToggleNotePrimary(TransformGizmo.referenceObj as Beatmap.Note);
                var action = new PrimaryAction();
                action.obj = TransformGizmo.referenceObj;
                AddAction(action);
            }
        }

        // Toggle Axis Mode
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (TransformGizmo.selectedObj == null) ToggleAxisMode();
            else if (TransformGizmo.referenceObj is Beatmap.Note)
            {
                ToggleNoteAxis(TransformGizmo.referenceObj as Beatmap.Note);
                var action = new AxisAction();
                action.obj = TransformGizmo.referenceObj;
                AddAction(action);
            }
        };

        // Select mode from number
        if (!precision1.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ModeDelete();
            if (Input.GetKeyDown(KeyCode.Alpha2)) ModeTransform();
            if (Input.GetKeyDown(KeyCode.Alpha3)) ModeNote();
            if (Input.GetKeyDown(KeyCode.Alpha4)) ModeSwap();
        }

        // Increment cursor beat
        if (playing)
        {
            scrollBeat += Utils.SecondsToBeat(Time.deltaTime, info.BPM);
            if (scrollBeat > songBeats)
            {
                scrollBeat = songBeats;
                playing = false;
            }
            mapVisuals.UpdateBeat(scrollBeat);
        }
        else if (!Input.GetKey(KeyCode.LeftControl))
        {
            scrollBeat += Input.mouseScrollDelta.y * scrollPrecision;
            if (scrollBeat < 0) scrollBeat = 0;
            if (scrollBeat > songBeats) scrollBeat = songBeats;
            if (Input.mouseScrollDelta.y != 0)
            {
                NormalizeScroll();
                mapVisuals.UpdateBeat(scrollBeat);
            }
        }

        WatchVariables();

        // Run Transform Gizmo
        if (EditorHandler.editorMode == EditorHandler.EditorMode.TRANSFORM) transformGizmo.DoProcess();

        // Play/Pause Song
        if (Input.GetKeyDown(KeyCode.Space)) ToggleSong();

        // Toggle Precision
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var scroll2text = precision2.text;
            precision2.text = precision1.text;
            precision1.text = scroll2text;
        }

        // Scroll Precision
        if (Input.GetKey(KeyCode.LeftControl) && Input.mouseScrollDelta.y != 0)
        {
            var newDenominator = float.Parse(precision1.text);
            if (Input.mouseScrollDelta.y > 0) newDenominator *= 2;
            if (Input.mouseScrollDelta.y < 0) newDenominator /= 2;
            newDenominator = Mathf.Round(Mathf.Clamp(newDenominator, precisionMin, precisionMax));
            precision1.text = newDenominator.ToString();
        }

        // Place Object
        if (Input.GetMouseButtonDown(0) && IsInVisual(Input.mousePosition) && !Input.GetKey(KeyCode.C))
        {
            if (editorMode == EditorMode.NOTE || editorMode == EditorMode.SWAP)
            {
                Beatmap.GameplayObject obj = null;

                if (editorMode == EditorMode.NOTE)
                {
                    obj = new Beatmap.Note();
                    var note = obj as Beatmap.Note;
                    diff.notes.Add(note);

                    var notePos = VisualToScreen(Input.mousePosition);
                    note.x = notePos.x / Screen.width;
                    note.y = notePos.y / Screen.height;

                    note.axis = axisMode;
                    note.primary = primaryMode;
                }
                if (editorMode == EditorMode.SWAP)
                {
                    obj = new Beatmap.Swap();
                    var swap = obj as Beatmap.Swap;
                    diff.swaps.Add(swap);

                    var cursorPos = VisualToScreen(Input.mousePosition);
                    var centerPos = new Vector2(Screen.width, Screen.height) / 2;
                    var angle = Mathf.Atan2(cursorPos.x - centerPos.x, cursorPos.y - centerPos.y) * 180 / Mathf.PI;

                    if (!(
                        (angle >= 45 && angle <= 135) ||
                        (angle <= -45 && angle >= -135)
                    )) swap.type = Beatmap.SwapType.Vertical;
                }

                obj.time = scrollBeat;

                var action = new PlaceAction();
                action.obj = obj;
                AddAction(action);

                DrawGrid();
                mapVisuals.UpdateBeat(scrollBeat);
                if (editorMode == EditorMode.NOTE) TransformGizmo.ForceInside(mapVisuals.onScreenObjs[obj]);
            }
        }
    }

    void ToggleSong()
    {
        playing = !playing;

        mapVisuals.PlayVideo(playing);
        if (audioSource.clip == null) return;
        if (playing)
        {
            var time = Utils.BeatToSeconds(scrollBeat, info.BPM);
            audioSource.time = Math.Min(time, audioSource.clip.length);
            audioSource.Play();
        }
        else
        {
            mapVisuals.UpdateBeat(scrollBeat);
            audioSource.Pause();
            NormalizeScroll();
        }
    }

    void PushGameplayObject(Beatmap.GameplayObject obj)
    {
        if (obj is Beatmap.Note)
        {
            diff.notes.Add(obj as Beatmap.Note);
            diff.notes.Sort((a, b) => ((int)a.time) - ((int)b.time));
        }
        if (obj is Beatmap.Swap)
        {
            diff.swaps.Add(obj as Beatmap.Swap);
            diff.swaps.Sort((a, b) => ((int)a.time) - ((int)b.time));
        };
        DrawGrid();
    }

    public static void Delete(Beatmap.GameplayObject obj, bool makeAction = true)
    {
        if (makeAction)
        {
            var action = new DeleteAction();
            action.obj = obj;
            AddAction(action);
        }

        RemoveObject(obj);

        if (mapVisuals.onScreenObjs.ContainsKey(obj))
        {
            var onScreenObj = mapVisuals.onScreenObjs[obj];
            if (TransformGizmo.selectedObj == onScreenObj)
            {
                TransformGizmo.selectedObj = null;
                TransformGizmo.referenceObj = null;
            }
            Destroy(onScreenObj);
            mapVisuals.onScreenObjs.Remove(obj);
            mapVisuals.UpdateBeat(scrollBeat);
        }

        refreshGrid = true;
    }

    public float GetGridY(float beat)
    {
        beat -= scrollBeat;
        var y = Screen.height * beatScale;
        y *= beat;
        y /= editorScale;
        y += Screen.height / 2;
        return y;
    }

    public bool IsOnGrid(float gridY) => gridY > -Screen.height * despawnDist & gridY < Screen.height * (1 + despawnDist);

    public void Scroll()
    {
        scrollBeat = songBeats * scrollbar.value;
        NormalizeScroll();
        mapVisuals.UpdateBeat(scrollBeat);
    }

    void NormalizeScroll() => scrollBeat = Mathf.Round(scrollBeat / scrollPrecision) * scrollPrecision;

    public void SlideEditorScale() => editorScale = editorScaleSlider.value;

    public Material gridLines;

    public void DrawGrid()
    {
        scrollbar.value = scrollBeat / songBeats;

        var seconds = Utils.BeatToSeconds(scrollBeat, info.BPM);
        var time = TimeSpan.FromSeconds(seconds);
        timeText.text = time.ToString(@"m\:ss\.ff");

        gridElements.ForEach(x => { Destroy(x); });
        gridElements.Clear();

        UpdateVisualsTransform();
        RefreshGridLines();

        var width = Screen.width * gridWidth;

        for (var i = 0; i <= songBeats; i++)
        {
            var gridY = GetGridY(i);
            if (!IsOnGrid(gridY)) continue;
            var bg = Instantiate(beatGuide);
            var scale = width / bg.GetComponent<RectTransform>().sizeDelta.x;
            bg.transform.position = new Vector3(Screen.width - (width / 2), gridY, 0);
            bg.transform.localScale = new Vector3(scale, scale, 1);
            bg.transform.SetParent(grid.transform);
            bg.GetComponentInChildren<Text>().text = i.ToString();
            gridElements.Add(bg);
        }

        Dictionary<float, List<Beatmap.GameplayObject>> displayedObjects = new Dictionary<float, List<Beatmap.GameplayObject>>();

        void DisplayGameplayObject(Beatmap.GameplayObject obj)
        {
            // TODO: Maybe figure out how to stop for loop once past grid?
            var gridY = GetGridY(obj.time);
            if (!IsOnGrid(gridY)) return;

            if (!displayedObjects.ContainsKey(obj.time))
                displayedObjects[obj.time] = new List<Beatmap.GameplayObject>();
            displayedObjects[obj.time].Add(obj);
        }

        diff.notes.ForEach(x => { DisplayGameplayObject(x); });
        diff.swaps.ForEach(x => { DisplayGameplayObject(x); });

        foreach (var key in displayedObjects.Keys)
        {
            var gridY = GetGridY(key);
            var list = displayedObjects[key];
            var index = 0;
            var fraction = width / list.Count;

            list.ForEach(x =>
            {
                var gridX = index * fraction;
                index++;
                gridX += fraction / 2;
                gridX = Screen.width - gridX;

                var note = isNote(x);
                var icon = Instantiate(note ? noteGridImage : swapGridImage);
                var scale = (width * iconSize) / icon.GetComponent<RectTransform>().sizeDelta.x;
                icon.transform.position = new Vector3(gridX, gridY, 0);
                icon.transform.localScale = new Vector3(scale, scale, 1);
                icon.transform.SetParent(grid.transform);

                var image = icon.GetComponent<RawImage>();
                if (isNote(x)) mapVisuals.NoteGraphic(x as Beatmap.Note, image);

                icon.onClick.AddListener(delegate () { FocusObject(x); });

                gridElements.Add(icon.gameObject);
            });
        }
    }

    void RefreshGridLines()
    {
        float spacing = scrollPrecision * beatScale;
        spacing /= editorScale;
        float lineFrequency = 1 / spacing;
        float top = songBeats / lineFrequency * (1 / scrollPrecision);

        gridLines.SetFloat("_LineFrequency", lineFrequency);
        gridLines.SetFloat("_Offset", scrollBeat * spacing / scrollPrecision);
        gridLines.SetFloat("_Top", top);
    }

    public static float screenHeight;
    public static float heightScalar;
    public static float widthScalar;
    public static float visualWidth;
    public static float visualHeight;
    public static float visualBoundX;
    public static float visualBoundY;

    public static bool IsInVisual(Vector2 point) => point.x > 0 && point.x < visualBoundX && point.y < Screen.height && point.y > visualBoundY;
    public static Vector2 ScreenToVisual(Vector2 point) => new Vector2(
        point.x * (visualBoundX / Screen.width),
        point.y * ((Screen.height - visualBoundY) / Screen.height) + visualBoundY
    );

    public static Vector2 VisualToScreen(Vector2 point) => new Vector2(
        point.x / (visualBoundX / Screen.width),
        (point.y - visualBoundY) / ((Screen.height - visualBoundY) / Screen.height)
    );

    void FocusObject(Beatmap.GameplayObject obj)
    {
        TransformGizmo.Deselect();
        scrollBeat = obj.time;
        WatchVariables();
        mapVisuals.UpdateBeat(scrollBeat);
        TransformGizmo.Select(mapVisuals.onScreenObjs[obj]);
    }

    void WatchVariables()
    {
        watcher.Watch(scrollBeat);
        watcher.Watch(editorScale);
        watcher.Watch(beatScale);
        watcher.Watch(despawnDist);
        watcher.Watch(gridWidth);
        watcher.Watch(iconSize);

        screenWatcher.Watch(Screen.width);
        screenWatcher.Watch(Screen.height);

        if (screenWatcher.Check())
        {
            refreshGrid = true;
            transformGizmo.RedrawVisuals();
        }

        if (watcher.Check() || refreshGrid) DrawGrid();
        refreshGrid = false;
    }

    void UpdateVisualsTransform()
    {
        visualWidth = 1 - gridWidth;
        visualHeight = 0.8f;

        var rect = mapVisuals.gameObject.GetComponent<RectTransform>();
        var sizeDelta = rect.sizeDelta;

        sizeDelta.x = 1920 * -(1 - visualWidth);

        rect.sizeDelta = sizeDelta;

        var canvasRect = this.GetComponent<RectTransform>();

        screenHeight = canvasRect.sizeDelta.y;
        heightScalar = Screen.height / screenHeight;
        widthScalar = Screen.width / canvasRect.sizeDelta.x;

        visualBoundX = Screen.width * visualWidth;
        visualBoundY = screenHeight - mapVisuals.GetBounds().height;
        visualBoundY *= heightScalar;

        rect.transform.position = new Vector2(
            visualBoundX / 2,
            rect.transform.position.y
        );
    }

    public void UpdatePrecision()
    {
        var value = float.Parse(precision1.text);

        if (value < precisionMin)
        {
            precision1.text = precisionMin.ToString();
            return;
        }
        if (value > precisionMax)
        {
            precision1.text = precisionMax.ToString();
            return;
        }

        scrollPrecision = 1 / float.Parse(precision1.text);
        RefreshGridLines();
    }

    public static bool isNote(Beatmap.GameplayObject obj) => obj is Beatmap.Note;
    public static bool isSwap(Beatmap.GameplayObject obj) => obj is Beatmap.Swap;

    public class EditorAction
    {
        public Beatmap.GameplayObject obj;
    }

    public class DeleteAction : EditorAction { }
    public class PlaceAction : EditorAction { }
    public class PrimaryAction : EditorAction { }
    public class AxisAction : EditorAction { }
    public class TimeAction : EditorAction
    {
        public float startTime;
        public float endTime;
    }
    public class TranslateNoteAction : EditorAction
    {
        public Vector2 startPos;
        public Vector2 endPos;
    }
    public class RotateNoteAction : EditorAction
    {
        public float startAngle;
        public float endAngle;
    }
    public class MoveSwapAction : EditorAction
    {
        public bool startVertical;
        public bool endVertical;
    }
    public class PasteAction : EditorAction
    {
        public List<Beatmap.GameplayObject> objs;
    }

    public static List<EditorAction> actions = new List<EditorAction>();
    public static List<EditorAction> undos = new List<EditorAction>();

    public static void AddAction(EditorAction action, bool newPath = true)
    {
        actions.Add(action);
        if (actions.Count > 100) actions.RemoveAt(0);
        if (newPath) undos.Clear();
    }

    public static void AddUndo(EditorAction action)
    {
        undos.Add(action);
        if (undos.Count > 100) undos.RemoveAt(0);
    }

    public void FocusAction(EditorAction action)
    {
        if (!mapVisuals.onScreenObjs.ContainsKey(action.obj))
        {
            scrollBeat = action.obj.time;
            mapVisuals.UpdateBeat(scrollBeat);
        }
    }

    public void Undo()
    {
        if (actions.Count == 0) return;
        var action = actions[actions.Count - 1];
        actions.Remove(action);
        AddUndo(action);
        if (!(action is TimeAction)) FocusAction(action);

        if (action is DeleteAction) AddObject(action.obj, false); // TODO: Test this "false" lol

        if (action is PlaceAction) Delete(action.obj, false);

        if (action is PrimaryAction) ToggleNotePrimary(action.obj as Beatmap.Note);

        if (action is AxisAction) ToggleNoteAxis(action.obj as Beatmap.Note);

        if (action is TimeAction)
        {
            action.obj.time = (action as TimeAction).startTime;
            DrawGrid();
            transformGizmo.RedrawVisuals();
            FocusAction(action);
        }

        if (action is TranslateNoteAction translateNote)
        {
            var noteObj = action.obj as Beatmap.Note;
            var noteAction = translateNote;
            noteObj.x = noteAction.startPos.x;
            noteObj.y = noteAction.startPos.y;
            transformGizmo.RedrawVisuals();
        }

        if (action is RotateNoteAction rotateNote)
        {
            (action.obj as Beatmap.Note).direction = rotateNote.startAngle;
            transformGizmo.RedrawVisuals();
        }

        if (action is MoveSwapAction moveSwap)
        {
            (action.obj as Beatmap.Swap).type =
            moveSwap.startVertical ? Beatmap.SwapType.Vertical : Beatmap.SwapType.Horizontal;
            transformGizmo.RedrawVisuals();
        }

        if (action is PasteAction pasteAction)
        {
            pasteAction.objs.ForEach(x => RemoveObject(x));
            transformGizmo.RedrawVisuals();
            DrawGrid();
        }
    }

    public void Redo()
    {
        if (undos.Count == 0) return;
        var action = undos[undos.Count - 1];
        undos.Remove(action);
        if (!(action is TimeAction)) FocusAction(action);

        if (action is DeleteAction) Delete(action.obj, false);

        if (action is PlaceAction)
        {
            AddObject(action.obj);
            AddAction(action, false);
        }

        if (action is PrimaryAction)
        {
            ToggleNotePrimary(action.obj as Beatmap.Note);
            AddAction(action, false);
        }

        if (action is AxisAction)
        {
            ToggleNoteAxis(action.obj as Beatmap.Note);
            AddAction(action, false);
        }

        if (action is TimeAction)
        {
            action.obj.time = (action as TimeAction).endTime;
            DrawGrid();
            transformGizmo.RedrawVisuals();
            FocusAction(action);
            AddAction(action, false);
        }

        if (action is TranslateNoteAction)
        {
            var noteObj = action.obj as Beatmap.Note;
            var noteAction = action as TranslateNoteAction;
            noteObj.x = noteAction.endPos.x;
            noteObj.y = noteAction.endPos.y;
            transformGizmo.RedrawVisuals();
            AddAction(action, false);
        }

        if (action is RotateNoteAction)
        {
            (action.obj as Beatmap.Note).direction = (action as RotateNoteAction).endAngle;
            transformGizmo.RedrawVisuals();
            AddAction(action, false);
        }

        if (action is MoveSwapAction)
        {
            (action.obj as Beatmap.Swap).type =
            (action as MoveSwapAction).endVertical ? Beatmap.SwapType.Vertical : Beatmap.SwapType.Horizontal;
            transformGizmo.RedrawVisuals();
            AddAction(action, false);
        }

        if (action is PasteAction pasteAction)
        {
            pasteAction.objs.ForEach(x => PushObject(x));
            transformGizmo.RedrawVisuals();
            DrawGrid();
            AddAction(action, false);
        }
    }

    public static void PushObject(Beatmap.GameplayObject obj)
    {
        if (obj is Beatmap.Note note) diff.notes.Add(note);
        if (obj is Beatmap.Swap swap) diff.swaps.Add(swap);
    }

    public static void RemoveObject(Beatmap.GameplayObject obj)
    {
        if (isNote(obj)) mapVisuals.diff.notes.Remove(obj as Beatmap.Note);
        if (isSwap(obj)) mapVisuals.diff.swaps.Remove(obj as Beatmap.Swap);
    }

    public void AddObject(Beatmap.GameplayObject obj, bool action = false)
    {
        PushObject(obj);

        if (action)
        {
            var placeAction = new PlaceAction();
            placeAction.obj = obj;
            AddAction(placeAction);
        }

        DrawGrid();
        mapVisuals.UpdateBeat(scrollBeat);
        if (obj is Beatmap.Note) mapVisuals.NoteGraphic(obj as Beatmap.Note, mapVisuals.onScreenObjs[obj].GetComponent<RawImage>());
    }

    public GameObject helpPanel;

    public void ShowHelp(bool show) => helpPanel.SetActive(show);

    public void ClearCopySelected(bool message = true)
    {
        if (message)
        {
            var objCount = copySelected.Count;
            if (TransformGizmo.selectedObj != null) objCount++;
            StatusMessage("Deselected " + objCount + " objects.");
        }

        copySelected.Clear();
        mapVisuals.Redraw();
    }
}
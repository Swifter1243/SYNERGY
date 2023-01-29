using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

/// <summary> Script for the internals of the editor. </summary>
public class EditorHandler : MonoBehaviour
{
    /// <summary> The current base beat of the editor. </summary>
    public static float scrollBeat = 0;
    /// <summary> The current scroll precision. </summary>
    [Range(1f / 64f, 1)]
    public float scrollPrecision = 1f / 2f;
    /// <summary> The current editor scale. </summary>
    [Range(0.2f, 3)]
    public float editorScale = 1;
    /// <summary> The editor scale slider object. </summary>
    public Slider editorScaleSlider;
    /// <summary> The amount of base beats the song extends. </summary>
    float songBeats = 100000;
    /// <summary> The width of the screen that each base beat in the grid will take up, assuming editor scale is 1. </summary>
    public float beatScale = 1f / 8f;
    /// <summary> The percentage an object on the grid can go off the screen vertically before it despawns.  </summary>
    public float despawnDist = 0.1f;
    /// <summary> Percentage of the screen the grid should take up. </summary>
    public float gridWidth = 0.2f;
    /// <summary> Percentage of the grid width that the icons will be scaled to </summary>
    public float iconSize = 0.3f;
    /// <summary> The object to spawn for each beat on the grid. </summary>
    public GameObject beatGuide;
    /// <summary> All objects that make up the grid. </summary>
    List<GameObject> gridElements = new List<GameObject>();
    /// <summary> Watches changes in numbers in order to redraw the grid. </summary>
    Utils.NumberWatcher watcher = new Utils.NumberWatcher();
    /// <summary> Watches changes in screen related numbers to redraw the grid. </summary>
    Utils.NumberWatcher screenWatcher = new Utils.NumberWatcher();
    /// <summary> Object to display for notes on the grid. </summary>
    public Button noteGridImage;
    /// <summary> Object to display for swaps on the grid. </summary>
    public Button swapGridImage;
    /// <summary> Icon for notes. </summary>
    public Texture2D noteIcon;
    /// <summary> The current active difficulty. </summary>
    public static Beatmap.Difficulty diff;
    /// <summary> The current active info. </summary>
    Beatmap.Info info;
    /// <summary> Unity won't let you assign static variables in the editor.. so this is my workaround. </summary>
    public MapVisuals mapVisualRef;
    /// <summary> The reference to the map visuals for the editor. </summary>
    public static MapVisuals mapVisuals;
    /// <summary> The reference to the transform gizmo script. </summary>
    public TransformGizmo transformGizmo;
    /// <summary> Whether the song is playing in the editor. </summary>
    bool playing = false;
    /// <summary> The audio player for the song. </summary>
    public static AudioSource audioSource;
    /// <summary> The text that displays the current time in the difficulty. </summary>
    public Text timeText;
    /// <summary> The scrollbar for the grid. </summary>
    public Scrollbar scrollbar;
    /// <summary> The text for the precision denominator. </summary>
    public InputField precision1;
    /// <summary> The text for the precision numerator. </summary>
    public Text precision2;
    /// <summary> The minimum precision denominator value. </summary>
    float precisionMin = 1;
    /// <summary> The maximum precision denominator value. </summary>
    float precisionMax = 64;
    /// <summary> If set true, grid is refreshed the next frame and this is set to false. </summary>
    public static bool refreshGrid = false;
    /// <summary> Icon showing the editor is in delete mode. </summary>
    public GameObject deleteOnIcon;
    /// <summary> Icon showing the editor is in transform mode. </summary>
    public GameObject transformOnIcon;
    /// <summary> Icon showing the editor is in note mode. </summary>
    public GameObject noteOnIcon;
    /// <summary> Icon showing the editor is in swap mode. </summary>
    public GameObject swapOnIcon;
    /// <summary> Text to display status messages in the editor. </summary>
    public Text statusText;
    /// <summary> All objects that have been copy selected. </summary>
    public static List<Beatmap.GameplayObject> copySelected = new List<Beatmap.GameplayObject>();
    /// <summary> All objects on the copy clipboard. </summary>
    List<Beatmap.GameplayObject> clipboard = new List<Beatmap.GameplayObject>();
    /// <summary> Parent object for everything relating to the grid. </summary>
    public GameObject grid;

    /// <summary> All modes the editor can be in. </summary>
    public enum EditorMode
    {
        DELETE,
        TRANSFORM,
        NOTE,
        SWAP
    }

    /// <summary> Current mode of the editor. </summary>
    public static EditorMode editorMode = EditorMode.TRANSFORM;
    /// <summary> Whether notes will be placed with an axis. </summary>
    public static bool axisMode = false;
    /// <summary> Whether placed notes are primary. </summary>
    public static bool primaryMode = true;

    /// <summary> Set the mode of the editor. </summary>
    /// <param name="mode"> The mode to set to. </param>
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

    /// <summary> The icon for the note mode button. </summary>
    public RawImage noteButtonIcon;
    /// <summary> Icon showing the editor will place notes with an axis. </summary>
    public GameObject axisOnIcon;
    /// <summary> Icon showing whether the editor will place notes as primary or secondary notes. </summary>
    public Image primaryIcon;

    /// <summary> Updates the visuals of the buttons that are relevant to notes in the editor. </summary>
    void UpdateNoteButtons()
    {
        var note = new Beatmap.Note();
        note.axis = axisMode;
        note.primary = primaryMode;
        mapVisuals.NoteGraphic(note, noteButtonIcon);
        axisOnIcon.SetActive(axisMode);
        if (primaryMode) primaryIcon.color = Beatmap.PrimaryColor;
        else primaryIcon.color = Beatmap.SecondaryColor;
    }

    /// <summary> Toggles axis mode. </summary>
    public void ToggleAxisMode()
    {
        axisMode = !axisMode;
        UpdateNoteButtons();
    }

    /// <summary> Toggles primary mode. </summary>
    public void TogglePrimaryMode()
    {
        primaryMode = !primaryMode;
        UpdateNoteButtons();
    }

    /// <summary> Sets the editor into delete mode. </summary>
    public void ModeDelete() => SetMode(EditorMode.DELETE);
    /// <summary> Sets the editor into transform mode. </summary>
    public void ModeTransform() => SetMode(EditorMode.TRANSFORM);
    /// <summary> Sets the editor into note mode. </summary>
    public void ModeNote() => SetMode(EditorMode.NOTE);
    /// <summary> Sets the editor into swap mode. </summary>
    public void ModeSwap() => SetMode(EditorMode.SWAP);

    void Start()
    {
        // This is something to initialize the editor scene if I jump straight into it from the unity editor.
        if (Beatmap.Active.diffName == null)
        {
            var songPath = new DirectoryInfo(SongHandler.songsFolder).GetDirectories()[0].ToString();
            var diffName = "/hard.dat";
            var diffPath = songPath + diffName;
            var audioPath = songPath + "/song.ogg";
            var songInfo = Beatmap.GetInfoFromPath(songPath);

            Beatmap.Active.songPath = songPath;
            Beatmap.Active.diffName = diffName;
            Beatmap.Active.diffPath = diffPath;
            Beatmap.Active.info = songInfo;
        }

        // Settings player preferences
        scrollPrecision = 1 / Utils.InitPlayerPrefsFloat("precision1", 2);
        precision1.text = PlayerPrefs.GetFloat("precision1").ToString();
        precision2.text = Utils.InitPlayerPrefsFloat("precision2", 8).ToString();
        editorScaleSlider.value = Utils.InitPlayerPrefsFloat("editorScale", 1);
        primaryMode = Utils.InitPlayerPrefsInt("primaryMode", 1) == 1;
        axisMode = Utils.InitPlayerPrefsInt("axisMode", 0) == 1;

        // If the difficulty is null, it is loaded.
        if (diff == null)
        {
            var rawData = File.ReadAllText(Beatmap.Active.diffPath);
            diff = JsonUtility.FromJson<Beatmap.Difficulty>(rawData);
            diff.notes.Sort((a, b) => ((int)a.time) - ((int)b.time));
            diff.swaps.Sort((a, b) => ((int)a.time) - ((int)b.time));
            diff.bpmChanges.Sort((a, b) => ((int)a.time) - ((int)b.time));
        }

        // Variable initialization
        mapVisuals = mapVisualRef;
        mapVisuals.diff = diff;
        info = Beatmap.Active.info;

        // Audio
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = Settings.masterVolume;
        if (Beatmap.Active.audio == null)
        {
            var audioPath = Beatmap.GetAudioPath(Beatmap.Active.songPath, info);
            StartCoroutine(LoadAudio(audioPath));
        }
        else SetAudio(Beatmap.Active.audio);

        // Video
        var videoPath = Beatmap.GetVideoPath(Beatmap.Active.songPath, info);
        mapVisuals.LoadVideo(videoPath);

        // Initializing scrollbar
        EventTrigger trigger = scrollbar.GetComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Drag;
        entry.callback.AddListener((data) => { Scroll(); });
        trigger.triggers.Add(entry);

        // Initializing visuals
        UpdateVisualsTransform();
        UpdateNoteButtons();
        mapVisuals.UpdateBeat(scrollBeat);
    }

    /// <summary> Saves the difficulty. </summary>
    /// <param name="showMessage"> Whether a status message for the save should be displayed. </param>
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

    /// <summary> Exit the editor. </summary>
    public void Exit()
    {
        Save(false);
        SongHandler.loadSong = Beatmap.Active.songPath;
        Transition.Load("EditorSongs");
    }

    /// <summary> Plays the difficulty. </summary>
    public void Play()
    {
        Save(false);
        PlayHandler.diff = diff;
        PlayHandler.exit = () => Transition.Load("Editor");
        var seconds = Input.GetKey(KeyCode.LeftControl) ? 0 : Utils.BeatToSeconds(scrollBeat, info.BPM);
        PlayHandler.startSeconds = seconds;
        Transition.Load("Playing");
    }

    /// <summary> Shows a status mesage in the editor. </summary>
    /// <param name="message"> The message to display. </param>
    public void StatusMessage(string message)
    {
        statusText.text = message;
        var errorColor = statusText.color;
        errorColor.a = 1;
        statusText.color = errorColor;
    }

    /// <summary> Loads and initializes the audio. </summary>
    /// <param name="path"> The path of the audio. </param>
    public IEnumerator LoadAudio(string path)
    {
        var www = UnityWebRequestMultimedia.GetAudioClip($"file:///{Uri.EscapeDataString($"{path}")}", AudioType.OGGVORBIS);
        yield return www.SendWebRequest();
        var clip = DownloadHandlerAudioClip.GetContent(www);
        Beatmap.Active.audio = clip;
        SetAudio(clip);
    }

    /// <summary> Sets the audio of the editor. </summary>
    /// <param name="clip"> The clip to set the audio to. </param>
    void SetAudio(AudioClip clip)
    {
        audioSource.clip = clip;
        songBeats = Utils.GetSongBeats(clip.length, info.BPM);
        DrawGrid();
    }

    /// <summary> Toggle the primary value on a note and update the visuals accordingly. </summary>
    /// <param name="note"> The note to update. </param>
    void ToggleNotePrimary(Beatmap.Note note)
    {
        note.primary = !note.primary;
        DrawGrid();
        transformGizmo.RedrawVisuals();
    }

    /// <summary> Toggle the axis value on a note and update the visuals accordingly. </summary>
    /// <param name="note"> The note to update. </param>
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

        // Watching changing variables
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

    /// <summary> Toggle the pause or playing of the song. </summary>
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

    /// <summary> Push an object to the difficulty and update visuals. </summary>
    /// <param name="obj"> Object to push. </param>
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

    /// <summary> Get the corresponding Y value for a point on the grid based on a beat. </summary>
    /// <param name="beat"> Beat of the point. </param>
    public float GetGridY(float beat)
    {
        beat -= scrollBeat;
        var y = Screen.height * beatScale;
        y *= beat;
        y /= editorScale;
        y += Screen.height / 2;
        return y;
    }

    /// <summary> Check if a y value of a point is visible on the grid. </summary>
    /// <param name="gridY"> The y value on the grid. </param>
    public bool IsOnGrid(float gridY) => gridY > -Screen.height * despawnDist & gridY < Screen.height * (1 + despawnDist);

    /// <summary> Called after scroll wheel is scrolled to update the editor accordingly. </summary>
    public void Scroll()
    {
        scrollBeat = songBeats * scrollbar.value;
        NormalizeScroll();
        mapVisuals.UpdateBeat(scrollBeat);
    }

    /// <summary> Snap the scroll value to the current precision. </summary>
    void NormalizeScroll() => scrollBeat = Mathf.Round(scrollBeat / scrollPrecision) * scrollPrecision;

    /// <summary> Called after editor scale slider value changes to update grid accordingly. </summary>
    public void SlideEditorScale() => editorScale = editorScaleSlider.value;

    /// <summary> Material for the shader that displays grid lines. </summary>
    public Material gridLines;

    /// <summary> Refresh and redraw the entire grid. </summary>
    public void DrawGrid()
    {
        // Update scrollbar position
        scrollbar.value = scrollBeat / songBeats;

        // Update time text
        var seconds = Utils.BeatToSeconds(scrollBeat, info.BPM);
        var time = TimeSpan.FromSeconds(seconds);
        timeText.text = time.ToString(@"m\:ss\.ff");

        // Clear grid elements
        gridElements.ForEach(x => { Destroy(x); });
        gridElements.Clear();

        // Update visuals and grid shader
        UpdateVisualsTransform();
        RefreshGridShader();

        // Spawn beat guides
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

        // Add new grid elements
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

    /// <summary> Refresh the grid lines shader. </summary>
    void RefreshGridShader()
    {
        float spacing = scrollPrecision * beatScale;
        spacing /= editorScale;
        float lineFrequency = 1 / spacing;
        float top = songBeats / lineFrequency * (1 / scrollPrecision);

        gridLines.SetFloat("_LineFrequency", lineFrequency);
        gridLines.SetFloat("_Offset", scrollBeat * spacing / scrollPrecision);
        gridLines.SetFloat("_Top", top);
    }

    /// <summary> The height of the screen canvas. </summary>
    public static float canvasHeight;
    /// <summary> The height of the screen canvas in relation to the screen height. </summary>
    public static float heightScalar;
    /// <summary> The width of the screen canvas in relation to the screen width. </summary>
    public static float widthScalar;
    /// <summary> Width of the map visuals. </summary>
    public static float visualWidth;
    /// <summary> Height of the map visuals. </summary>
    public static float visualHeight;
    /// <summary> Rightmost bound of the map visuals. The visuals go from the left of the screen until this point. </summary>
    public static float visualBoundX;
    /// <summary> Lower bound of the map visuals. The visuals go from the top of the screen until this point. </summary>
    public static float visualBoundY;

    /// <summary> Determines if a point on the screen is inside of the map visuals. </summary>
    /// <param name="point"> The point on the screen. </param>
    public static bool IsInVisual(Vector2 point) => point.x > 0 && point.x < visualBoundX && point.y < Screen.height && point.y > visualBoundY;

    /// <summary> Converts a point on the screen to a point on the map visuals. </summary>
    /// <param name="point"> The point on the screen. </param>
    public static Vector2 ScreenToVisual(Vector2 point) => new Vector2(
        point.x * (visualBoundX / Screen.width),
        point.y * ((Screen.height - visualBoundY) / Screen.height) + visualBoundY
    );

    /// <summary> Converts a point on the map visuals to a point on the screen. </summary>
    /// <param name="point"> The point on the map visuals. </param>
    public static Vector2 VisualToScreen(Vector2 point) => new Vector2(
        point.x / (visualBoundX / Screen.width),
        (point.y - visualBoundY) / ((Screen.height - visualBoundY) / Screen.height)
    );

    /// <summary> Moves the editor timeline to an object, selecting it with the transform gizmo if possible. </summary>
    /// <param name="obj"> The object to focus. </param>
    void FocusObject(Beatmap.GameplayObject obj)
    {
        TransformGizmo.Deselect();
        scrollBeat = obj.time;
        WatchVariables();
        mapVisuals.UpdateBeat(scrollBeat);
        TransformGizmo.Select(mapVisuals.onScreenObjs[obj]);
    }

    /// <summary> Check for changes in watched variables to refresh the grid. </summary>
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

    /// <summary> Update the map visuals transform. </summary>
    void UpdateVisualsTransform()
    {
        visualWidth = 1 - gridWidth;
        visualHeight = 0.8f;

        var rect = mapVisuals.gameObject.GetComponent<RectTransform>();
        var sizeDelta = rect.sizeDelta;

        sizeDelta.x = 1920 * -(1 - visualWidth);

        rect.sizeDelta = sizeDelta;

        var canvasRect = this.GetComponent<RectTransform>();

        canvasHeight = canvasRect.sizeDelta.y;
        heightScalar = Screen.height / canvasHeight;
        widthScalar = Screen.width / canvasRect.sizeDelta.x;

        visualBoundX = Screen.width * visualWidth;
        visualBoundY = canvasHeight - mapVisuals.GetBounds().height;
        visualBoundY *= heightScalar;

        rect.transform.position = new Vector2(
            visualBoundX / 2,
            rect.transform.position.y
        );
    }

    /// <summary> Called when the precision denominator input field is changed and updates the editor accordingly. </summary>
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
        RefreshGridShader();
    }

    /// <summary> Checks if a beatmap object is a note. </summary>
    /// <param name="obj"> The object to check. </param>
    public static bool isNote(Beatmap.GameplayObject obj) => obj is Beatmap.Note;

    /// <summary> Checks if a beatmap object is a swap. </summary>
    /// <param name="obj"> The object to check. </param>
    public static bool isSwap(Beatmap.GameplayObject obj) => obj is Beatmap.Swap;

    /// <summary> An action to register into the undo and redo system. </summary>
    public class EditorAction
    {
        /// <summary> The object relevant to this action. </summary>
        public Beatmap.GameplayObject obj;
    }

    /// <summary> An action where an object is deleted. </summary>
    public class DeleteAction : EditorAction { }
    /// <summary> An action where an object is placed </summary>
    public class PlaceAction : EditorAction { }
    /// <summary> An action where a note's primary value is toggled </summary>
    public class PrimaryAction : EditorAction { }
    /// <summary> An action where a note's axis value is toggled </summary>
    public class AxisAction : EditorAction { }
    /// <summary> An action where an object's time is changed </summary>
    public class TimeAction : EditorAction
    {
        /// <summary> The time before the action. </summary>
        public float startTime;
        /// <summary> The time after the action. </summary>
        public float endTime;
    }
    /// <summary> An action where a note is moved. </summary>
    public class TranslateNoteAction : EditorAction
    {
        /// <summary> The position before the action. </summary>
        public Vector2 startPos;
        /// <summary> The position after the action. </summary>
        public Vector2 endPos;
    }
    /// <summary> An action where a note is rotated. </summary>
    public class RotateNoteAction : EditorAction
    {
        /// <summary> The angle before the action. </summary>
        public float startAngle;
        /// <summary> An angle after the action. </summary>
        public float endAngle;
    }
    /// <summary> An action where a swap's axis is changed. </summary>
    public class MoveSwapAction : EditorAction
    {
        /// <summary> Whether the axis was vertical before the action. </summary>
        public bool startVertical;
        /// <summary> Whether the axis is vertical after the action. </summary>
        public bool endVertical;
    }
    /// <summary> An action where a bunch of objects are pasted. </summary>
    public class PasteAction : EditorAction
    {
        /// <summary> The pasted objects. </summary>
        public List<Beatmap.GameplayObject> objs;
    }

    /// <summary> A list of actions in the editor. </summary>
    public static List<EditorAction> actions = new List<EditorAction>();
    /// <summary> A list of undone actions that can be redone. </summary>
    public static List<EditorAction> undos = new List<EditorAction>();
    /// <summary> The maximum amount of actions allowed to be stored in the editor. </summary>
    static float actionLimit = 100;

    /// <summary> Register an action in the editor. </summary>
    /// <param name="action"> The action to register. </param>
    /// <param name="newPath"> Whether this action changes the undo timeline, meaning undone actions will be cleared. </param>
    public static void AddAction(EditorAction action, bool newPath = true)
    {
        actions.Add(action);
        if (actions.Count > actionLimit) actions.RemoveAt(0);
        if (newPath) undos.Clear();
    }

    /// <summary> Register an undone action in the editor. </summary>
    /// <param name="action"> The action to register. </param>
    public static void AddUndo(EditorAction action)
    {
        undos.Add(action);
        if (undos.Count > actionLimit) undos.RemoveAt(0);
    }

    /// <summary> Focus the editor on a particular action. </summary>
    /// <param name="action"> The action to focus. </param>
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

        if (action is PlaceAction) DeleteObject(action.obj, false);

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

        if (action is DeleteAction) DeleteObject(action.obj, false);

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

    /// <summary> Push an object to the difficulty class. Doesn't update visuals. </summary>
    /// <param name="obj"> Object to push. </param>
    public static void PushObject(Beatmap.GameplayObject obj)
    {
        if (obj is Beatmap.Note note) diff.notes.Add(note);
        if (obj is Beatmap.Swap swap) diff.swaps.Add(swap);
    }

    /// <summary> Remove an object from the difficulty class. Doesn't update visuals. </summary>
    /// <param name="obj"> Object to remove. </param>
    public static void RemoveObject(Beatmap.GameplayObject obj)
    {
        if (isNote(obj)) mapVisuals.diff.notes.Remove(obj as Beatmap.Note);
        if (isSwap(obj)) mapVisuals.diff.swaps.Remove(obj as Beatmap.Swap);
    }

    /// <summary> Add an object to the difficulty and update visuals. </summary>
    /// <param name="obj"> Object to add. </param>
    /// <param name="makeAction"> Whether to register an add action. </param>
    public void AddObject(Beatmap.GameplayObject obj, bool makeAction = false)
    {
        PushObject(obj);

        if (makeAction)
        {
            var placeAction = new PlaceAction();
            placeAction.obj = obj;
            AddAction(placeAction);
        }

        DrawGrid();
        mapVisuals.UpdateBeat(scrollBeat);
        if (obj is Beatmap.Note) mapVisuals.NoteGraphic(obj as Beatmap.Note, mapVisuals.onScreenObjs[obj].GetComponent<RawImage>());
    }

    /// <summary> Delete an object from the difficulty and update visuals. </summary>
    /// <param name="obj"> Object to delete. </param>
    /// <param name="makeAction"> Whether to register a delete action. </param>
    public static void DeleteObject(Beatmap.GameplayObject obj, bool makeAction = true)
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

    /// <summary> The panel that displays keybinds for the editor. </summary>
    public GameObject helpPanel;
    
    /// <summary> Hide or show the help panel. </summary>
    /// <param name="show"> Whether to hide or show the help panel. </param>
    public void ShowHelp(bool show) => helpPanel.SetActive(show);

    /// <summary> Clear the copy selected objects in the editor. </summary>
    /// <param name="message"> Whether to show a status message for the deselected objects. </param>
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
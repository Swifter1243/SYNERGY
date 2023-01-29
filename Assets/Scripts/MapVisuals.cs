using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;

public class MapVisuals : MonoBehaviour
{
    /// <summary> The difficulty for the visuals. </summary>
    public Beatmap.Difficulty diff;
    /// <summary> A shortcut for the info of the active song. </summary>
    Beatmap.Info info { get => Beatmap.Active.info; }
    /// <summary> The beat these visuals are displaying. </summary>
    public float beat;
    /// <summary> The object to spawn for notes. </summary>
    public GameObject displayedNote;
    /// <summary> The object to spawn for swaps. </summary>
    public GameObject displayedSwap;
    /// <summary> The rect transform of the visuals. </summary>
    public RectTransform rect;
    /// <summary> The canvas of the scene. </summary>
    public Canvas canvas;

    /// <summary> The displayed objects currently onscreen. </summary>
    public Dictionary<Beatmap.GameplayObject, GameObject> onScreenObjs = new Dictionary<Beatmap.GameplayObject, GameObject>();

    /// <summary> Whether the vertical mirror is enabled at the current beat. </summary>
    public bool verticalMirror = false;
    /// <summary> Whether the horizontal mirror is enabled at the current beat. </summary>
    public bool horizontalMirror = false;
    /// <summary> The threshold for animating the mirrors approaching a value where it doesn't animate anymore. </summary>
    float animThreshold = 0.001f;
    /// <summary> The current cutoff of the vertical mirror. </summary>
    float verticalMirrorAnim = 0;
    /// <summary> The current cutoff of the horizontal mirror. </summary>
    float horizontalMirrorAnim = 0;
    /// <summary> The material for the vertical mirror. </summary>
    public Material verticalMirrorMat;
    /// <summary> The material for the horizontal mirror. </summary>
    public Material horizontalMirrorMat;
    /// <summary> Whether these visuals are for gameplay. </summary>
    public bool gameplay = true;
    /// <summary> Optional threshold for beat to spawn objects. </summary>
    public float spawnCutoff = 0;

    // Initialization.
    void Awake()
    {
        verticalMirrorMat.SetFloat("_Cutoff", 0);
        horizontalMirrorMat.SetFloat("_Cutoff", 0);
        rect = this.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    /// <summary> Gets the screenspace bounds of the visuals. </summary>
    public Rect GetBounds() => RectTransformUtility.PixelAdjustRect(rect, canvas);
    
    /// <summary> Updates the visuals to a given beat. </summary>
    /// <param name="newBeat"> The beat to update to. </param>
    public void UpdateBeat(float newBeat)
    {
        beat = newBeat;

        // Remove old displayed objects
        var removedObjs = new List<Beatmap.GameplayObject>();

        foreach (var obj in onScreenObjs)
        {
            if (!isOnScreen(obj.Key))
            {
                Destroy(obj.Value);
                removedObjs.Add(obj.Key);
            }
        }

        removedObjs.ForEach(x => { onScreenObjs.Remove(x); });

        // Add new displayed objects
        diff.notes.ForEach(x => { CheckObjectSpawn(x); });
        if (!gameplay) diff.swaps.ForEach(x => { CheckObjectSpawn(x); });
        AnimateObjects();

        // Update video
        if (videoPlayer != null && videoPlayer.isPrepared) UpdateVideo();
    }
    
    /// <summary> Function to run when the state of the mirrors change. </summary>
    public Action onMirrorUpdate;

    /// <summary> Check swaps to see if mirrors need to be updated. </summary>
    public void CheckSwaps()
    {
        var verticalSwaps = diff.swaps.FindAll(x => x.type == Beatmap.SwapType.Vertical && x.time <= beat);
        var horizontalSwaps = diff.swaps.FindAll(x => x.type == Beatmap.SwapType.Horizontal && x.time <= beat);

        var newVerticalMirror = verticalSwaps.Count % 2 == 1;
        var newHorizontalMirror = horizontalSwaps.Count % 2 == 1;

        if ((newVerticalMirror != verticalMirror || newHorizontalMirror != horizontalMirror))
        {
            verticalMirror = newVerticalMirror;
            horizontalMirror = newHorizontalMirror;
            if (onMirrorUpdate != null) onMirrorUpdate();
        }
    }

    void Update()
    {
        CheckSwaps();

        // Vertical mirror animation
        if (verticalMirror && verticalMirrorAnim < 1)
        {
            if (1 - verticalMirrorAnim < animThreshold) verticalMirrorAnim = 1;
            verticalMirrorAnim = Utils.Approach(verticalMirrorAnim, 1, 20);
            verticalMirrorMat.SetFloat("_Cutoff", verticalMirrorAnim);
        }
        if (!verticalMirror && verticalMirrorAnim > 0)
        {
            if (verticalMirrorAnim < animThreshold) verticalMirrorAnim = 0;
            verticalMirrorAnim = Utils.Approach(verticalMirrorAnim, 0, 20);
            verticalMirrorMat.SetFloat("_Cutoff", verticalMirrorAnim);
        }

        // Horizontal mirror animation
        if (horizontalMirror && horizontalMirrorAnim < 1)
        {
            if (1 - horizontalMirrorAnim < animThreshold) horizontalMirrorAnim = 1;
            horizontalMirrorAnim = Utils.Approach(horizontalMirrorAnim, 1, 20);
            horizontalMirrorMat.SetFloat("_Cutoff", horizontalMirrorAnim);
        }
        if (!horizontalMirror && horizontalMirrorAnim > 0)
        {
            if (horizontalMirrorAnim < animThreshold) horizontalMirrorAnim = 0;
            horizontalMirrorAnim = Utils.Approach(horizontalMirrorAnim, 0, 20);
            horizontalMirrorMat.SetFloat("_Cutoff", horizontalMirrorAnim);
        }
    }

    /// <summary> Completely refresh visuals. </summary>
    public void Redraw()
    {
        foreach (var obj in onScreenObjs) Destroy(obj.Value);
        onScreenObjs = new Dictionary<Beatmap.GameplayObject, GameObject>();
        UpdateBeat(beat);
        AnimateObjects();
    }

    /// <summary> The object containing the video player and render texture. </summary>
    public GameObject video;
    /// <summary> The render texture for the video. </summary>
    public RawImage videoTexture;
    /// <summary> The video player for the visuals. </summary>
    public VideoPlayer videoPlayer;
    /// <summary> Whether the video is playing. </summary>
    bool playing = false;
    
    /// <summary> Load and initialize the video. </summary>
    /// <param name="path"> Path to the video file. </param>
    public void LoadVideo(string path)
    {
        if (!File.Exists(path)) return;
        video.SetActive(true);
        videoPlayer = video.GetComponent<VideoPlayer>();
        videoPlayer.url = path;
        videoPlayer.Play();
        videoPlayer.Pause();
        UpdateVideo();
    }

    /// <summary> Update the video frame and visibility. </summary>
    void UpdateVideo()
    {
        var videoOffsetSeconds = Utils.BeatToSeconds(info.videoOffset, info.BPM);
        var seconds = Utils.BeatToSeconds(beat, info.BPM) - videoOffsetSeconds;
        var videoEndTime = (float)videoPlayer.length;
        var isOn = seconds >= 0 && seconds < videoEndTime;
        videoTexture.color = Utils.ChangeAlpha(videoTexture.color, isOn ? 1 : 0);
        if (videoPlayer.isPaused) videoPlayer.time = Mathf.Min(seconds, videoEndTime);

        if (videoPlayer.isPlaying && (!isOn || !playing)) videoPlayer.Pause();
        if (!videoPlayer.isPlaying && playing && isOn) videoPlayer.Play();
    }
    
    /// <summary> Pause or play the video. </summary>
    /// <param name="play"> Whether to pause or play the video. </param>
    public void PlayVideo(bool play)
    {
        if (videoPlayer == null) return;
        playing = play;
        UpdateVideo();
    }

    /// <summary> Animate onscreen objects. </summary>
    public void AnimateObjects()
    {
        foreach (var obj in onScreenObjs.Values)
        {
            var displayedObj = obj.GetComponent<DisplayedObject>();
            displayedObj.Animate(beat);
        }
    }

    /// <summary> Check if an object in a difficulty needs to be spawned onto the visuals. </summary>
    /// <param name="obj"> Object to check. </param>
    void CheckObjectSpawn(Beatmap.GameplayObject obj)
    {
        // Don't spawn if object is below cutoff.
        if (obj.time < spawnCutoff) return;

        // Only spawn if object isn't already onscreen.
        if (isOnScreen(obj) && !onScreenObjs.ContainsKey(obj))
        {
            // Initialize note
            if (EditorHandler.isNote(obj))
            {
                var displayedObj = Instantiate(displayedNote);
                onScreenObjs[obj] = displayedObj;
                displayedObj.transform.SetParent(this.gameObject.transform);
                displayedObj.transform.localScale = new Vector3(1, 1, 1);

                var sizeDelta = new Vector2(Beatmap.noteSize, Beatmap.noteSize);
                displayedObj.GetComponent<RectTransform>().sizeDelta = sizeDelta;

                var noteObj = obj as Beatmap.Note;
                var notePos = new Vector2(
                    Screen.width * noteObj.x,
                    Screen.height * noteObj.y
                );
                notePos = ScreenToVisual(notePos);

                displayedObj.transform.position = new Vector3(
                    notePos.x,
                    notePos.y
                );
                displayedObj.transform.rotation = Quaternion.Euler(0, 0, noteObj.direction);
                displayedObj.GetComponent<DisplayedObject>().Initialize(noteObj);

                NoteGraphic(noteObj, displayedObj.GetComponent<RawImage>());
            }
            // Initialize swap
            if (EditorHandler.isSwap(obj))
            {
                var displayedObj = Instantiate(displayedSwap);
                onScreenObjs[obj] = displayedObj;
                displayedObj.transform.SetParent(this.gameObject.transform);
                displayedObj.transform.localScale = new Vector3(1, 1, 1);

                var isVertical = (obj as Beatmap.Swap).type == Beatmap.SwapType.Vertical;
                var bounds = GetBounds();
                var width = isVertical ? bounds.height : bounds.width;
                var height = bounds.height * 0.025f;

                displayedObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);

                var swapObj = obj as Beatmap.Swap;
                var swapPos = new Vector2(Screen.width, Screen.height) / 2;
                swapPos = ScreenToVisual(swapPos);

                displayedObj.transform.position = new Vector3(
                    swapPos.x,
                    swapPos.y
                );
                if (isVertical) displayedObj.transform.rotation = Quaternion.Euler(0, 0, 90);
                displayedObj.GetComponent<DisplayedObject>().Initialize(swapObj);
            }
        }
    }

    /// <summary> Transform a point in screenspace to a point on the visuals. </summary>
    /// <param name="point"> Point to transform. </param>
    public Vector2 ScreenToVisual(Vector2 point)
    {
        var bounds = GetBounds();
        var canvasSize = canvas.GetComponent<RectTransform>().sizeDelta;
        var pos = this.transform.position;

        var scalarX = bounds.width / canvasSize.x;
        var boundX = scalarX * Screen.width;
        var offsetX = pos.x - boundX / 2;
        var x = point.x * scalarX + offsetX;

        var scalarY = bounds.height / canvasSize.y;
        var boundY = scalarY * Screen.height;
        var offsetY = pos.y - boundY / 2;
        var y = point.y * scalarY + offsetY;

        return new Vector2(x, y);
    }

    /// <summary> Check if an object in the difficulty is onscreen based on it's time. </summary>
    /// <param name="obj"> Object to check. </param>
    public bool isOnScreen(Beatmap.GameplayObject obj)
    {
        var difference = Utils.BeatToSeconds(obj.time - beat, info.BPM);
        if (difference < -Beatmap.Active.spawnOutSeconds) return false;
        if (difference > Beatmap.Active.spawnInSeconds) return false;
        return true;
    }

    /// <summary> Icon for a regular note. </summary>
    public Texture2D noteIcon;
    /// <summary> Icon for an axis note. </summary>
    public Texture2D axisNoteIcon;

    /// <summary> Update an image based on the properties of a note. </summary>
    /// <param name="note"> The note which contains the properties. </param>
    /// <param name="image"> The image to update. </param>
    public void NoteGraphic(Beatmap.Note note, RawImage image)
    {
        if (note.axis) image.texture = axisNoteIcon;
        else image.texture = noteIcon;
        if (note.primary) image.color = Beatmap.PrimaryColor;
        else image.color = Beatmap.SecondaryColor;
    }
}
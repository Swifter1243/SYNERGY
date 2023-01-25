using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;

public class MapVisuals : MonoBehaviour
{
    public Beatmap.Difficulty diff;
    Beatmap.Info info { get => Beatmap.Active.info; }
    public float beat;
    public GameObject displayedNote;
    public GameObject displayedSwap;
    public RectTransform rect;
    public Canvas canvas;

    public Dictionary<Beatmap.GameplayObject, GameObject> onScreenObjs = new Dictionary<Beatmap.GameplayObject, GameObject>();

    public bool verticalMirror = false;
    public bool horizontalMirror = false;
    float animThreshold = 0.001f;
    float verticalMirrorAnim = 0;
    float horizontalMirrorAnim = 0;
    public Material verticalMirrorMat;
    public Material horizontalMirrorMat;
    public bool gameplay = true;

    void Awake()
    {
        verticalMirrorMat.SetFloat("_Cutoff", 0);
        horizontalMirrorMat.SetFloat("_Cutoff", 0);
        rect = this.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public Rect GetBounds() => RectTransformUtility.PixelAdjustRect(rect, canvas);

    public void UpdateBeat(float newBeat)
    {
        beat = newBeat;

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

        diff.notes.ForEach(x => { checkObjectSpawn(x); });
        if (!gameplay) diff.swaps.ForEach(x => { checkObjectSpawn(x); });

        AnimateObjects();

        if (videoPlayer != null && videoPlayer.isPrepared) UpdateVideo();
    }

    public Action onMirrorUpdate;

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

        // Vertical Animation
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

        // Horizontal Animation
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

    public void Redraw()
    {
        foreach (var obj in onScreenObjs) Destroy(obj.Value);
        onScreenObjs = new Dictionary<Beatmap.GameplayObject, GameObject>();
        UpdateBeat(beat);
        AnimateObjects();
    }

    public GameObject video;
    public RawImage videoTexture;
    public VideoPlayer videoPlayer;
    bool playing = false;

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

    void UpdateVideo()
    {
        var videoBeat = beat - info.videoOffset;
        var seconds = Utils.BeatToSeconds(videoBeat, info.BPM);
        var isOn = videoBeat >= 0;
        videoTexture.color = Utils.ChangeAlpha(videoTexture.color, isOn ? 1 : 0);
        if (videoPlayer.isPaused) videoPlayer.time = seconds;

        if (videoPlayer.isPlaying && (!isOn || !playing)) videoPlayer.Pause();
        if (!videoPlayer.isPlaying && playing && isOn) videoPlayer.Play();
    }

    public void PlayVideo(bool play)
    {
        if (videoPlayer == null) return;
        playing = play;
        UpdateVideo();
    }

    public void AnimateObjects()
    {
        foreach (var obj in onScreenObjs.Values)
        {
            var displayedObj = obj.GetComponent<DisplayedObject>();
            displayedObj.Animate(beat);
        }
    }

    void checkObjectSpawn(Beatmap.GameplayObject obj)
    {
        if (isOnScreen(obj) && !onScreenObjs.ContainsKey(obj))
        {
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

    public bool isOnScreen(Beatmap.GameplayObject obj)
    {
        var difference = Utils.BeatToSeconds(obj.time - beat, info.BPM);
        if (difference < -Beatmap.Active.spawnOutSeconds) return false;
        if (difference > Beatmap.Active.spawnInSeconds) return false;
        return true;
    }

    public Texture2D noteIcon;
    public Texture2D axisNoteIcon;

    public void NoteGraphic(Beatmap.Note note, RawImage graphic)
    {
        if (note.axis) graphic.texture = axisNoteIcon;
        else graphic.texture = noteIcon;
        if (note.primary) graphic.color = Beatmap.PrimaryColor;
        else graphic.color = Beatmap.SecondaryColor;
    }
}
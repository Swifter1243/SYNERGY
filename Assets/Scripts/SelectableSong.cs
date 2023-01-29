using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

/// <summary> Logic for songs in the song selection menu. </summary>
public class SelectableSong : MonoBehaviour
{
    /// <summary> The cover art of the song. </summary>
    public RawImage coverArt;
    /// <summary> Index of the song in the list. </summary>
    public int index;
    /// <summary> Info of the song </summary>
    public Beatmap.Info info;
    /// <summary> Path to the song. </summary>
    public string path;
    /// <summary> Song audio. </summary>
    public AudioClip clip;

    /// <summary> Spacing between songs in the outer section. </summary>
    public static float innerSpacing = 400;
    /// <summary> Spacings between outer songs and the selected song. </summary>
    public static float outerSpacing = 250;
    /// <summary> Scalar for the size of the song when it's selected. </summary>
    public static float innerSize = 1f;
    /// <summary> Scalar for the size of the song when it's in the outer section. </summary>
    public static float outerSize = 0.8f;
    /// <summary> Scalar for the size of the song when it's chosen. </summary>
    public static float chosenSize = 1.3f;
    /// <summary> Rotation of the songs in the outer section. </summary>
    public static Vector3 tilt = new Vector3(2.28f, 29.47f, 3.3f);
    /// <summary> Rotation of the song when it's chosen. </summary>
    public static Vector3 selectRot = new Vector3(-0.335f, -30.121f, -1.264f);
    /// <summary> The distance the song goes to the left upon being chosen. </summary>
    public static float selectDist = 200;

    /// <summary> Animate the song's location depending on the selected index in the list. </summary>
    /// <param name="selectedIndex"> The selected index in the list. </param>
    public void Animate(float selectedIndex)
    {
        // Initializing
        var deltaIndex = index - selectedIndex;
        var inner = Mathf.Abs(deltaIndex) <= 1;
        var absIndex = Mathf.Abs(deltaIndex);

        // Moving objects
        float xPos = 0;
        if (inner)
        {
            xPos = deltaIndex * innerSpacing;
            var size = Mathf.Lerp(innerSize, outerSize, Mathf.Abs(deltaIndex));
            gameObject.transform.localScale = new Vector3(size, size);
            var rot = tilt * absIndex;
            gameObject.transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        }
        else
        {
            gameObject.transform.localScale = new Vector3(outerSize, outerSize);
            var deltaSign = deltaIndex > 0 ? 1 : -1;
            xPos += deltaSign * innerSpacing;
            xPos += (absIndex - 1) * outerSpacing * deltaSign;
            var rot = tilt;
            gameObject.transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        }

        gameObject.transform.localPosition = new Vector3(xPos, 0);
    }

    /// <summary> Animate the chosen position of the song. </summary>
    /// <param name="anim"> Progress of the animation between 0 and 1. </param>
    public void AnimateChoose(float anim)
    {
        var size = Mathf.Lerp(innerSize, chosenSize, anim);
        gameObject.transform.localScale = new Vector3(size, size);
        var rot = selectRot * anim;
        gameObject.transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        var xPos = Mathf.Lerp(0, -selectDist, anim);
        gameObject.transform.localPosition = new Vector3(xPos, 0);
    }

    /// <summary> Selects this song in the song selection. </summary>
    public void Select()
    {
        SongSelection.targetIndex = index;
        SongSelection.chosenIndex = index;
    }

    void Start()
    {
        // If song is chosen then use active audio as song audio.
        if (SongSelection.chosenAnim > 0 && SongSelection.targetIndex == index) clip = Beatmap.Active.audio;
    }

    void Update()
    {
        // Load audio if needed
        if (SongSelection.targetIndex == index && clip == null)
        {
            var audioPath = Beatmap.GetAudioPath(path, info);
            if (File.Exists(audioPath)) StartCoroutine(LoadAudio(audioPath));
        }
    }

    /// <summary> Load and initialize song audio. </summary>
    /// <param name="path"> Path to the audio. </param>
    public IEnumerator LoadAudio(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.OGGVORBIS))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError)
            {
                Debug.LogError(www.error);
                yield break;
            }

            ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;

            if (www.isNetworkError) Debug.Log(www.error);
            else clip = ((DownloadHandlerAudioClip)www.downloadHandler).audioClip;
        }
    }
}

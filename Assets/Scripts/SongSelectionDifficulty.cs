using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary> The difficulty buttons in the song selection. </summary>
public class SongSelectionDifficulty : MonoBehaviour
{
    /// <summary> The name of the difficulty. </summary>
    public string diffName;
    /// <summary> The path to the difficulty. </summary>
    string diffPath;
    /// <summary> The cover art of the song.  </summary>
    public Image image;
    /// <summary> The text displaying the name of the difficulty. </summary>
    public Text text;
    /// <summary> The button component of this object. </summary>
    public Button button;
    /// <summary> The song object this difficulty is attached to. </summary>
    public SelectableSong song;
    /// <summary> The reference to the scene SongSelection class. </summary>
    public SongSelection songSelection;

    void Start()
    {
        button = GetComponent<Button>();
        songSelection.diffButtons.Add(this);
    }

    /// <summary> Check if the difficulty exists and update UI accordingly. </summary>
    /// <param name="song"> The relevant song. </param>
    public void CheckVisibility(SelectableSong song)
    {
        this.song = song;
        diffPath = Beatmap.GetDiffPath(song.path, diffName);
        var audioPath = Beatmap.GetAudioPath(song.path, song.info);
        if (File.Exists(diffPath) && File.Exists(audioPath)) Enable();
        else Disable();
    }

    /// <summary> Enable this difficulty button. </summary>
    void Enable()
    {
        image.color = new Color(1, 1, 1, 1);
        text.color = new Color(1, 1, 1, 1);
        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate () { OpenSong(); });
    }

    /// <summary> Disable this difficulty button. </summary>
    void Disable()
    {
        image.color = new Color(63 / 255, 63 / 255, 63 / 255, 1);
        text.color = new Color(0.5f, 0.5f, 0.5f, 1);
        button.interactable = false;
    }

    /// <summary> Play this song in the playing scene. </summary>
    void OpenSong()
    {
        Beatmap.Active.songPath = song.path;
        Beatmap.Active.coverArt = (Texture2D)song.coverArt.texture;
        Beatmap.Active.diffName = diffName;
        Beatmap.Active.diffPath = diffPath;
        Beatmap.Active.info = song.info;
        Beatmap.Active.audio = song.clip;

        var rawData = File.ReadAllText(diffPath);
        var diff = JsonUtility.FromJson<Beatmap.Difficulty>(rawData);
        PlayHandler.diff = diff;
        PlayHandler.startSeconds = 0;

        PlayHandler.exit = () => Transition.Load("SongSelection");
        Transition.Load("Playing");
    }
}
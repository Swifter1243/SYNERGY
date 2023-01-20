using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SongSelectionDifficulty : MonoBehaviour
{
    public string diffName;
    string diffPath;
    public Image image;
    public Text text;
    public Button button;
    public SelectableSong song;
    public SongSelection songSelection;

    void Start()
    {
        button = GetComponent<Button>();
        songSelection.diffButtons.Add(this);
    }

    public void CheckVisibility(SelectableSong song)
    {
        this.song = song;
        diffPath = Utils.GetDiffPath(song.path, diffName);
        var audioPath = Utils.GetAudioPath(song.path, song.info);
        if (File.Exists(diffPath) && File.Exists(audioPath)) Enable();
        else Disable();
    }

    void Enable()
    {
        image.color = new Color(1, 1, 1, 1);
        text.color = new Color(1, 1, 1, 1);
        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate () { OpenSong(song); });
    }

    void Disable()
    {
        image.color = new Color(63 / 255, 63 / 255, 63 / 255, 1);
        text.color = new Color(0.5f, 0.5f, 0.5f, 1);
        button.interactable = false;
    }

    void OpenSong(SelectableSong song)
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
        PlayHandler.seconds = 0;
        PlayHandler.startSeconds = 0;

        PlayHandler.exit = () => Transition.Load("SongSelection");
        Transition.Load("Playing");
    }
}
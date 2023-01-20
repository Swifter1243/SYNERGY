using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;

public class DifficultyButton : MonoBehaviour
{
    public GameObject delete;
    public GameObject add;
    public GameObject edit;
    public SongCreation songCreation;
    public Text text;
    public string diff;
    public string diffPath;
    Image image;

    void Awake()
    {
        image = this.GetComponent<Image>();
        diff = text.text.ToLower();
        songCreation.diffButtons.Add(this);
    }

    public void CreateEnable()
    {
        Enable();
        songCreation.CreateDifficulty(diffPath);
    }

    public void CheckDelete()
    {
        songCreation.contextMenu.SetActive(true);
        SongCreation.contextMode = SongCreation.ContextMode.DIFFICULTY;
        SongCreation.deletingDifficulty = this;
        songCreation.contextText.text = "Are you sure you want to delete " + diff + "?";
    }

    public void CheckVisibility()
    {
        diffPath = Utils.GetDiffPath(songCreation.songPath, diff);
        if (File.Exists(diffPath)) Enable();
        else Disable();
    }

    public void Enable()
    {
        var imageCol = image.color;
        imageCol.a = 1;
        image.color = imageCol;

        add.SetActive(false);
        delete.SetActive(true);
        edit.SetActive(true);
    }

    public void Disable()
    {
        var imageCol = image.color;
        imageCol.a = 0.5f;
        image.color = imageCol;

        add.SetActive(true);
        delete.SetActive(false);
        edit.SetActive(false);
    }

    public void EditSong()
    {
        EditorHandler.scrollBeat = 0;
        EditorHandler.actions.Clear();
        EditorHandler.undos.Clear();
        EditorHandler.audioSource = null;
        EditorHandler.diff = null;
        Beatmap.Active.audio = null;
        Beatmap.Active.video = null;

        songCreation.UpdateAudio();
        if (songCreation.audioPath == null) return;
        songCreation.SaveSongInfo();
        Beatmap.Active.diffName = this.diff;
        Beatmap.Active.diffPath = this.diffPath;
        Beatmap.Active.info = songCreation.songInfo;
        Beatmap.Active.songPath = songCreation.songPath;
        Transition.Load("Editor");
    }
}

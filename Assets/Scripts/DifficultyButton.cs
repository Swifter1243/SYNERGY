using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;

/// <summary> Difficulty button in the song info screen of the editor. </summary>
public class DifficultyButton : MonoBehaviour
{
    /// <summary> Button to delete this difficulty. </summary>
    public GameObject delete;
    /// <summary> Button to add this difficulty. </summary>
    public GameObject add;
    /// <summary> Button to edit this difficulty. </summary>
    public GameObject edit;
    /// <summary> Reference to the SongCreation class. </summary>
    public SongCreation songCreation;
    /// <summary> Reference to the text displaying the name of this difficulty. </summary>
    public Text text;
    /// <summary> The name of this difficulty. </summary>
    public string diff;
    /// <summary> The path to this difficulty. </summary>
    public string diffPath;
    /// <summary> The background image of this button. </summary>
    Image image;

    // Initializes and registers button.
    void Awake()
    {
        image = this.GetComponent<Image>();
        diff = text.text.ToLower();
        songCreation.diffButtons.Add(this);
    }

    /// <summary> Enable this button and create difficulty file. </summary>
    public void CreateEnable()
    {
        Enable();
        songCreation.CreateDifficulty(diffPath);
    }

    /// <summary> Prompt context menu on whether to delete this difficulty. </summary>
    public void CheckDelete()
    {
        songCreation.contextMenu.SetActive(true);
        SongCreation.contextMode = SongCreation.ContextMode.DIFFICULTY;
        SongCreation.deletingDifficulty = this;
        songCreation.contextText.text = "Are you sure you want to delete " + diff + "?";
    }

    /// <summary> Check if this difficulty exists and update visuals. </summary>
    public void CheckVisibility()
    {
        diffPath = Beatmap.GetDiffPath(songCreation.songPath, diff);
        if (File.Exists(diffPath)) Enable();
        else Disable();
    }

    /// <summary> Enable this button. </summary>
    public void Enable()
    {
        var imageCol = image.color;
        imageCol.a = 1;
        image.color = imageCol;

        add.SetActive(false);
        delete.SetActive(true);
        edit.SetActive(true);
    }

    /// <summary> Disable this button. </summary>
    public void Disable()
    {
        var imageCol = image.color;
        imageCol.a = 0.5f;
        image.color = imageCol;

        add.SetActive(true);
        delete.SetActive(false);
        edit.SetActive(false);
    }

    /// <summary> Switch into the editor scene with this difficulty. </summary>
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

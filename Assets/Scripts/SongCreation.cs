using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;

/// <summary> Handler for the song info screen in the editor. </summary>
public class SongCreation : MonoBehaviour
{
    /// <summary> The path to the song. </summary>
    public string songPath;
    /// <summary> The path to the song's info. </summary>
    public string infoPath;
    /// <summary> The path to the song's audio. </summary>
    public string audioPath;
    /// <summary> The current song info. </summary>
    public Beatmap.Info songInfo;
    /// <summary> The text that displays the song's path. </summary>
    public Text songPathTitle;
    /// <summary> The reference to the SongHandler in the scene. </summary>
    public SongHandler songHandler;
    /// <summary> The object that parents the song selection in the editor. </summary>
    public GameObject songSelection;
    /// <summary> The object that parents the popup to create a new map. </summary>
    public GameObject createMenuBackground;
    /// <summary> The input field for the name of a new map. </summary>
    public InputField createMenuInput;
    /// <summary> The input field for the name of the song. </summary>
    public InputField songName;
    /// <summary> The input field for the artist of the song. </summary>
    public InputField artist;
    /// <summary> The input field for the mapper of the song. </summary>
    public InputField mapper;
    /// <summary> The input field for the bpm of the song. </summary>
    public InputField bpm;
    /// <summary> The input field for the art path of the song. </summary>
    public InputField art;
    /// <summary> The input field for the audio path of the song. </summary>
    public InputField song;
    /// <summary> The input field for the video path of the song. </summary>
    public InputField video;
    /// <summary> The input field for the video offset of the song. </summary>
    public InputField videoOffset;
    /// <summary> The image data of the cover art. </summary>
    public RawImage artImage;
    /// <summary> The image to display when there is no cover art. </summary>
    public Texture2D unknownArt;
    /// <summary> The difficulty buttons. </summary>
    public List<DifficultyButton> diffButtons = new List<DifficultyButton>();
    
    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) CheckExit();
    }

    /// <summary> Creates a new song based on the new song popup. </summary>
    public void OpenNewSong()
    {
        var songName = SongHandler.songsFolder + "/" + createMenuInput.text;
        if (createMenuInput.text.Length == 0)
        {
            songHandler.CreateMenuError("Name cannot be empty!");
        }
        else if (Directory.Exists(songName))
        {
            songHandler.CreateMenuError("Song with this name already exists!");
        }
        else
        {
            var info = new Beatmap.Info();
            info.name = createMenuInput.text;
            OpenSong(songName, info);
            SaveSongInfo();
        }
    }

    /// <summary> Saves the info of the song to the info.dat. </summary>
    public void SaveSongInfo()
    {
        if (!Directory.Exists(songPath)) Directory.CreateDirectory(songPath);
        if (!File.Exists(infoPath))
        {
            var infoStream = File.CreateText(infoPath);
            infoStream.Dispose();
        };

        songInfo.name = songName.text;
        songInfo.artist = artist.text;
        songInfo.mapper = mapper.text;
        songInfo.BPM = float.Parse(bpm.text);
        songInfo.art = art.text;
        songInfo.song = song.text;
        songInfo.video = video.text;
        songInfo.videoOffset = float.Parse(videoOffset.text);

        var json = JsonUtility.ToJson(songInfo);
        File.WriteAllText(infoPath, json);
    }

    /// <summary> Opens a song's info editing screen. </summary>
    /// <param name="path"> The path of the song to open. </param>
    /// <param name="info"> The info of the song. </param>
    public void OpenSong(string path, Beatmap.Info info)
    {
        // Hiding song selection and enabling info screen
        this.gameObject.SetActive(true);
        songSelection.SetActive(false);
        createMenuBackground.SetActive(false);

        // Initializing information
        songPath = path;
        infoPath = Beatmap.GetInfoPath(path);
        songInfo = info;

        // Initializing UI
        songPathTitle.text = songPath;
        songName.text = songInfo.name;
        artist.text = songInfo.artist;
        mapper.text = songInfo.mapper;
        bpm.text = songInfo.BPM.ToString();
        art.text = songInfo.art;
        song.text = songInfo.song;
        video.text = songInfo.video;
        videoOffset.text = songInfo.videoOffset.ToString();

        UpdateAudio();
        UpdateArt();

        diffButtons.ForEach(x =>
        {
            x.CheckVisibility();
        });
    }

    /// <summary> Check the existence of the song and update UI accordingly. </summary>
    public void UpdateAudio()
    {
        var path = songPath + "\\" + song.text;
        var image = song.gameObject.GetComponent<Image>();

        if (File.Exists(path))
        {
            audioPath = path;
            image.color = new Color(1, 1, 1, 1);
        }
        else
        {
            audioPath = null;
            image.color = new Color(1, 0.7f, 0.7f, 1);
        }
    }

    /// <summary> Checks the existence of the cover art and updates UI accordingly. </summary>
    public void UpdateArt()
    {
        var coverArtPath = songPath + "/" + art.text;
        var texture = File.Exists(coverArtPath) ? Utils.LoadImage(coverArtPath) : unknownArt;
        artImage.texture = texture;
        Beatmap.Active.coverArt = texture;
    }

    /// <summary> Create a difficulty file. </summary>
    /// <param name="diffPath"> The path of the difficulty file. </param>
    public void CreateDifficulty(string diffPath)
    {
        if (!File.Exists(diffPath))
        {
            var diffStream = File.CreateText(diffPath);
            diffStream.Dispose();
            File.WriteAllText(diffPath, JsonUtility.ToJson(new Beatmap.Difficulty()));
        }
    }

    /// <summary> Delete a difficulty file. </summary>
    /// <param name="diffPath"> The path of the difficulty file. </param>
    public void DeleteDifficulty(string diffPath)
    {
        if (File.Exists(diffPath)) File.Delete(diffPath);
    }

    /// <summary> The different reasons the context menu can be enabled. </summary>
    public enum ContextMode
    {
        /// <summary> Asking if a song should be deleted. </summary>
        SONG,
        /// <summary> Asking if a difficulty should be deleted. </summary>
        DIFFICULTY,
        /// <summary> Asking if you want to exit info screen without saving. </summary>
        EXIT
    }

    /// <summary> The current reason the context menu is enabled. </summary>
    public static ContextMode contextMode;
    /// <summary> The context menu parent. </summary>
    public GameObject contextMenu;
    /// <summary> The text that displays the reason the context mode is enabled. </summary>
    public Text contextText;
    /// <summary> The difficulty button of the difficulty currently prompted to be deleted by the context menu. </summary>
    public static DifficultyButton deletingDifficulty;

    /// <summary> Agree to the terms of the context menu. </summary>
    public void ContextYes()
    {
        if (contextMode == ContextMode.DIFFICULTY)
        {
            deletingDifficulty.Disable();
            DeleteDifficulty(deletingDifficulty.diffPath);
            contextMenu.SetActive(false);
        }
        if (contextMode == ContextMode.SONG)
        {
            contextMenu.SetActive(false);
            DeleteSong();
        }
        if (contextMode == ContextMode.EXIT)
        {
            contextMenu.SetActive(false);
            songHandler.PopulateSongs();
        }
    }

    /// <summary> Disagree to the terms of the context menu. </summary>
    public void ContextNo() => contextMenu.SetActive(false);

    /// <summary> Prompt the context menu for a song to be deleted. </summary>
    public void CheckDeleteSong()
    {
        contextText.text = "Are you sure you want to delete " + songInfo.name + "?";
        contextMode = ContextMode.SONG;
        contextMenu.SetActive(true);
    }

    /// <summary> Delete a song. </summary>
    public void DeleteSong()
    {
        Directory.Delete(songPath, true);
        songHandler.PopulateSongs();
    }

    /// <summary> Open the song in the file explorer. </summary>
    public void OpenSongInExplorer() => Utils.OpenFileExplorer(songPath);

    /// <summary> Exit the song, prompt to save info if it's not saved. </summary>
    public void CheckExit()
    {
        if (songInfo == null)
        {
            songHandler.PopulateSongs();
            return;
        }
        if (
            songName.text != songInfo.name ||
            artist.text != songInfo.artist ||
            mapper.text != songInfo.mapper ||
            bpm.text != songInfo.BPM.ToString() ||
            art.text != songInfo.art ||
            song.text != songInfo.song ||
            video.text != songInfo.video ||
            videoOffset.text != songInfo.videoOffset.ToString()
        )
        {
            contextText.text = "Are you sure you want to leave without saving?";
            contextMode = ContextMode.EXIT;
            contextMenu.SetActive(true);
        }
        else songHandler.PopulateSongs();
    }

    /// <summary> Exit to the main menu. </summary>
    public void MainMenu() => Transition.Load("MainMenu");
}

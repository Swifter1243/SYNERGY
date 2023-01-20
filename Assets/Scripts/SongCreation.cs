using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;

public class SongCreation : MonoBehaviour
{
    public string songPath;
    public string infoPath;
    public string audioPath;
    public Beatmap.Info songInfo;
    public Text songPathTitle;
    public SongHandler songHandler;
    public GameObject songSelection;
    public GameObject createMenuBackground;
    public InputField createMenuInput;
    public InputField songName;
    public InputField artist;
    public InputField mapper;
    public InputField bpm;
    public InputField art;
    public InputField song;
    public InputField video;
    public InputField videoOffset;
    public RawImage artImage;
    public Texture2D unknownArt;
    public List<DifficultyButton> diffButtons = new List<DifficultyButton>();

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) CheckExit();
    }

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

    public void OpenSong(string path, Beatmap.Info info)
    {
        this.gameObject.SetActive(true);
        songSelection.SetActive(false);
        createMenuBackground.SetActive(false);

        songPath = path;
        infoPath = path + "\\info.dat";
        songInfo = info;

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

    public static Texture2D LoadImage(string path)
    {
        var image = new Texture2D(2, 2);
        var imageData = File.ReadAllBytes(path);
        image.LoadImage(imageData);
        return image;
    }

    public void UpdateArt()
    {
        var coverArtPath = songPath + "/" + art.text;
        var texture = File.Exists(coverArtPath) ? LoadImage(coverArtPath) : unknownArt;
        artImage.texture = texture;
        Beatmap.Active.coverArt = texture;
    }

    public void CreateDifficulty(string diffPath)
    {
        if (!File.Exists(diffPath))
        {
            var diffStream = File.CreateText(diffPath);
            diffStream.Dispose();
            File.WriteAllText(diffPath, JsonUtility.ToJson(new Beatmap.Difficulty()));
        }
    }

    public void DeleteDifficulty(string diffPath)
    {
        if (File.Exists(diffPath)) File.Delete(diffPath);
    }

    public enum ContextMode
    {
        LEVEL,
        DIFFICULTY,
        EXIT
    }

    public static ContextMode contextMode;
    public GameObject contextMenu;
    public Text contextText;
    public static DifficultyButton deletingDifficulty;

    public void ContextYes()
    {
        if (contextMode == ContextMode.DIFFICULTY)
        {
            deletingDifficulty.Disable();
            DeleteDifficulty(deletingDifficulty.diffPath);
            contextMenu.SetActive(false);
        }
        if (contextMode == ContextMode.LEVEL)
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

    public void ContextNo() => contextMenu.SetActive(false);

    public void CheckDeleteSong()
    {
        contextText.text = "Are you sure you want to delete " + songInfo.name + "?";
        contextMode = ContextMode.LEVEL;
        contextMenu.SetActive(true);
    }

    public void DeleteSong()
    {
        Directory.Delete(songPath, true);
        songHandler.PopulateSongs();
    }

    public void OpenFileExplorer()
    {
        var path = songPath.Replace(@"/", @"\");
        System.Diagnostics.Process.Start("explorer.exe", "/root," + path);
    }

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

    public void MainMenu() {
        Transition.Load("MainMenu");
    }
}

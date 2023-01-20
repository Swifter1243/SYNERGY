using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SongSelection : MonoBehaviour
{
    public Button songPrefab;
    public static float selectIndex = 0;
    public static int targetIndex = 0;
    public static int? chosenIndex = null;
    public static float chosenAnim = 0;
    public List<SelectableSong> songs = new List<SelectableSong>();
    public List<SongSelectionDifficulty> diffButtons = new List<SongSelectionDifficulty>();
    public Text title;
    public Text artist;
    public Text mapper;
    public GameObject songsParent;
    public AudioSource audioSource;
    public static int audioIndex = 0;
    public static float audioPreview = 0;
    float audioPreviewLength = 5;

    void Start()
    {
        if (chosenAnim < indexThreshold) audioPreview = 0;
        audioSource = GetComponent<AudioSource>();

        var folders = new DirectoryInfo(SongHandler.songsFolder).GetDirectories();

        var i = 0;
        foreach (var f in folders)
        {
            // Get Info.dat data
            var info = SongHandler.getInfoFromPath(f.ToString());

            // Spawn UI element
            Button song = Instantiate(songPrefab);
            song.transform.SetParent(songsParent.transform);
            song.transform.localScale = new Vector3(1, 1, 1);

            // Getting song data
            SelectableSong songData = song.GetComponent<SelectableSong>();
            songData.index = i;
            songData.Animate(selectIndex);
            songData.info = info;
            songData.path = f.ToString();
            songs.Add(songData);

            // Set cover art
            var coverArtPath = f + "/" + info.art;
            if (File.Exists(coverArtPath))
            {
                var coverArtData = File.ReadAllBytes(coverArtPath);
                Texture2D coverArt = new Texture2D(2, 2);
                coverArt.LoadImage(coverArtData);
                songData.coverArt.texture = coverArt;
            }

            i++;
        }

        UpdateInfo(songs[targetIndex].info);
        songPanelTransform = songPanel.GetComponent<RectTransform>();
    }

    public void MainMenu() => Transition.Load("MainMenu");


    public static float indexThreshold = 0.005f;
    static float indexDifference;
    static bool isMoving() => indexDifference > indexThreshold;

    void CalculateDifference() => indexDifference = Mathf.Abs(selectIndex - targetIndex);

    void Update()
    {
        CalculateDifference();

        // If nothing is chosen but not on selected index
        if (isMoving() && chosenAnim < indexThreshold)
        {
            var song = songs[targetIndex];
            UpdateInfo(song.info);
            selectIndex = Utils.Approach(selectIndex, targetIndex, chosenIndex == null ? 5 : 15);
            songs.ForEach(x =>
            {
                x.Animate(selectIndex);
            });
            panelUpdated = false;
        }

        // If something is chosen on selected index
        if (indexDifference < indexThreshold && chosenIndex != null && chosenAnim < 1)
        {
            chosenAnim = Utils.Approach(chosenAnim, 1, 5);
            songs[targetIndex].AnimateChoose(chosenAnim);
            AnimateChoose();
        }

        // Another song is chosen that isn't the current chosen song
        if (((isMoving() && chosenIndex != null) || chosenIndex == null) && chosenAnim > indexThreshold)
        {
            chosenAnim = Utils.Approach(chosenAnim, 0, 30);
            GetHoveredSong().AnimateChoose(chosenAnim);
            AnimateChoose();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) chosenIndex = null;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) GetHoveredSong().Select();
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.mouseScrollDelta.y > 0) DownOneSong();
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.mouseScrollDelta.y < 0) UpOneSong();

        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (chosenIndex == null) MainMenu();
            else chosenIndex = null;
        }

        // Audio
        var volumeSpeed = Time.deltaTime * 2;
        var volume = Settings.masterVolume;

        if (targetIndex != audioIndex)
        {
            audioPreview = 0;
            if (audioSource.volume > 0) audioSource.volume = Mathf.Max(audioSource.volume -= volumeSpeed * volume, 0);
            else audioIndex = targetIndex;
        }
        if (targetIndex == audioIndex)
        {
            var clip = songs[targetIndex].clip; 
            if (clip != null)
            {
                if (audioPreview == 0) {
                    audioSource.clip = clip;
                    audioSource.Play();
                    audioSource.time = clip.length / 3;
                    audioPreview = 0;
                }
                audioPreview += Time.deltaTime;
                if (audioPreview < audioPreviewLength) audioSource.volume = Mathf.Min(audioSource.volume += volumeSpeed * volume, volume);
                else Mathf.Max(audioSource.volume -= Time.deltaTime * volume, 0);
            }
        }
    }

    void UpdateInfo(Beatmap.Info info)
    {
        title.text = info.name;
        artist.text = info.artist;
        if (info.mapper.Length != 0) mapper.text = "Mapped by " + info.mapper;
        else mapper.text = "";
    }

    public CanvasGroup songPanel;
    public CanvasGroup songInfo;
    RectTransform songPanelTransform;
    bool panelUpdated = false;

    SelectableSong GetHoveredSong() => songs[(int)Mathf.Round(selectIndex)];

    void AnimateChoose()
    {
        var pos1 = new Vector2(25 - 60, -20 + 5);
        var pos2 = new Vector2(353.85f - 60, -12 + 5);
        var newPos = new Vector3(
            Mathf.Lerp(pos1.x, pos2.x, chosenAnim),
            Mathf.Lerp(pos1.y, pos2.y, chosenAnim)
        );
        songPanelTransform.localPosition = newPos;
        songPanel.alpha = Utils.EaseInExpo(chosenAnim);
        songInfo.alpha = 1 - Utils.EaseOutExpo(chosenAnim);
        songPanel.blocksRaycasts = chosenAnim > 0.5f;
        if (!panelUpdated)
        {
            panelUpdated = true;
            diffButtons.ForEach(x => x.CheckVisibility(GetHoveredSong()));
        }
    }

    public void DownOneSong()
    {
        targetIndex = Mathf.Max(0, targetIndex - 1);
        chosenIndex = null;
    }
    public void UpOneSong()
    {
        targetIndex = Mathf.Min(songs.Count - 1, targetIndex + 1);
        chosenIndex = null;
    }
}
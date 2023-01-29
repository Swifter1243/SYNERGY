using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary> Handler for song selection. </summary>
public class SongSelection : MonoBehaviour
{
    /// <summary> The object that will be spawned to represent a song. </summary>
    public Button songPrefab;
    /// <summary> The position of the song selection. </summary>
    public static float currentIndex = 0;
    /// <summary> The currently selected song. </summary>
    public static int targetIndex = 0;
    /// <summary> The currently chosen song. </summary>
    public static int? chosenIndex = null;
    /// <summary> The value used to animate the position of chosen songs. </summary>
    public static float chosenAnim = 0;
    /// <summary> All of the songs in the song selection. </summary>
    public List<SelectableSong> songs = new List<SelectableSong>();
    /// <summary> The buttons for the difficulties. </summary>
    public List<SongSelectionDifficulty> diffButtons = new List<SongSelectionDifficulty>();
    /// <summary> The text displaying the name of a song. </summary>
    public Text title;
    /// <summary> The text displaying the artist of a song. </summary>
    public Text artist;
    /// <summary> The text displaying the mapper of a song. </summary>
    public Text mapper;
    /// <summary> The object that parents all of the songs. </summary>
    public GameObject songsParent;
    /// <summary> The component that plays audio in the scene. </summary>
    public AudioSource audioSource;
    /// <summary> The index of the song currently playing audio. </summary>
    public static int audioIndex = 0;
    /// <summary> The current time of the audio preview. </summary>
    public static float audioPreview = 0;
    /// <summary> The maximum length in seconds of the audio preview. </summary>
    float audioPreviewLength = 8;

    void Start()
    {
        // Initializing audio
        if (chosenAnim < indexThreshold) audioPreview = 0;
        audioSource = GetComponent<AudioSource>();

        // Populating songs from songs folder
        var folders = new DirectoryInfo(SongHandler.songsFolder).GetDirectories();

        var i = 0;
        foreach (var f in folders)
        {
            // Get Info.dat data
            var info = Beatmap.GetInfoFromPath(f.ToString());

            // Spawn UI element
            Button song = Instantiate(songPrefab);
            song.transform.SetParent(songsParent.transform);
            song.transform.localScale = new Vector3(1, 1, 1);

            // Getting song data
            SelectableSong songData = song.GetComponent<SelectableSong>();
            songData.index = i;
            songData.Animate(currentIndex);
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

    /// <summary> Return to the main menu. </summary>
    public void MainMenu() => Transition.Load("MainMenu");

    /// <summary> The difference threshold at which approaching the target index stops updating. </summary>
    public static float indexThreshold = 0.005f;
    /// <summary> The difference between the target index and the current index. </summary>
    static float indexDifference;

    /// <summary> Check if the song selection is currently moving toward a selected song. </summary>
    static bool isMoving() => indexDifference > indexThreshold;

    /// <summary> Update indexDifference. </summary>
    void CalculateDifference() => indexDifference = Mathf.Abs(currentIndex - targetIndex);

    void Update()
    {
        CalculateDifference();

        // Loading icon
        var loadingRotation = loadingImage.transform.localRotation.eulerAngles;
        loadingRotation.z = (loadingRotation.z - Time.deltaTime * 300) % 360;
        loadingImage.transform.localRotation = Quaternion.Euler(loadingRotation);

        // If nothing is chosen but not on selected index
        if (isMoving() && chosenAnim < indexThreshold)
        {
            var song = songs[targetIndex];
            UpdateInfo(song.info);
            currentIndex = Utils.Approach(currentIndex, targetIndex, chosenIndex == null ? 5 : 15);
            songs.ForEach(x =>
            {
                x.Animate(currentIndex);
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

        // Inputs for moving around songs
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) chosenIndex = null;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) GetHoveredSong().Select();
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.mouseScrollDelta.y > 0) DownOneSong();
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.mouseScrollDelta.y < 0) UpOneSong();

        // Pressing escape goes to main menu or stops choosing current chosen song
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
                if (audioPreview == 0)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    audioSource.time = clip.length / 4;
                }
                audioPreview += Time.deltaTime;
                if (audioPreview < audioPreviewLength) audioSource.volume = Mathf.Min(audioSource.volume += volumeSpeed * volume, volume);
                else Mathf.Max(audioSource.volume -= Time.deltaTime * volume, 0);
            }
        }
    }

    /// <summary> Update the displayed song info. </summary>
    /// <param name="info"> Info of the song. </param>
    void UpdateInfo(Beatmap.Info info)
    {
        title.text = info.name;
        artist.text = info.artist;
        if (info.mapper.Length != 0) mapper.text = "Mapped by " + info.mapper;
        else mapper.text = "";
    }

    /// <summary> The canvas group for the parent displaying song difficulties. </summary>
    public CanvasGroup songPanel;
    /// <summary> The canvas group for the song information. </summary>
    public CanvasGroup songInfo;
    /// <summary> The transform of the panel displaying song difficulties. </summary>
    RectTransform songPanelTransform;
    /// <summary> The image displayed when a song is waiting on audio being loaded. </summary>
    public RawImage loadingImage;
    /// <summary> The canvas group of the object parenting the difficulty buttons. </summary>
    public CanvasGroup difficulties;
    /// <summary> Whether the difficulty buttons have been updated. </summary>
    bool panelUpdated = false;

    /// <summary> Gets the song that the song selection is currently hovering over. </summary>
    SelectableSong GetHoveredSong() => songs[(int)Mathf.Round(currentIndex)];

    /// <summary> Animates the chosen song. </summary>
    void AnimateChoose()
    {
        // Animate position
        var pos1 = new Vector2(25 - 60, -20 + 5);
        var pos2 = new Vector2(353.85f - 60, -12 + 5);
        var newPos = new Vector3(
            Mathf.Lerp(pos1.x, pos2.x, chosenAnim),
            Mathf.Lerp(pos1.y, pos2.y, chosenAnim)
        );
        songPanelTransform.localPosition = newPos;

        // Animate alpha
        songPanel.alpha = Utils.EaseInExpo(chosenAnim);
        songInfo.alpha = 1 - Utils.EaseOutExpo(chosenAnim);
        songPanel.blocksRaycasts = chosenAnim > 0.5f;

        // Update difficulty panel
        var hoveredSong = GetHoveredSong();

        if (!panelUpdated)
        {
            panelUpdated = true;
            diffButtons.ForEach(x => x.CheckVisibility(hoveredSong));
        }

        if (hoveredSong.clip != null && difficulties.alpha == 0) {
            difficulties.alpha = 1;
            loadingImage.color = Utils.ChangeAlpha(loadingImage.color, 0);
        }

        if (hoveredSong.clip == null && difficulties.alpha == 1) {
            difficulties.alpha = 0;
            loadingImage.color = Utils.ChangeAlpha(loadingImage.color, 1);
        }
    }

    /// <summary> Go down (left) one song in the song selection. </summary>
    public void DownOneSong()
    {
        targetIndex = Mathf.Max(0, targetIndex - 1);
        chosenIndex = null;
    }

    /// <summary> Go up (right) one song in the song selection. </summary>
    public void UpOneSong()
    {
        targetIndex = Mathf.Min(songs.Count - 1, targetIndex + 1);
        chosenIndex = null;
    }
}
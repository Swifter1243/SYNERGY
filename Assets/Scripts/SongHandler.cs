using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary> Handler for the editor song selection. </summary>
public class SongHandler : MonoBehaviour
{
    /// <summary> The object that will be spawned for each song. </summary>
    public Button songPrefab;
    /// <summary> The object that parents the popup to create a new map. </summary>
    public GameObject createPanel;
    /// <summary> The input field for the name of a new map. </summary>
    public InputField createText;
    /// <summary> The reference to this scene's SongCreation class. </summary>
    public SongCreation songCreation;
    /// <summary> The panel that contains the song selection UI. </summary>
    public GameObject songSelectionPanel;
    /// <summary> The panel that contains the song info editing UI. </summary>
    public GameObject songCreationPanel;
    /// <summary> The text that displays an error on the song creation panel.  </summary>
    public Text errorText;
    /// <summary> If set to a path to a song, it will be loaded in SongCreation upon scene awake. </summary>
    public static string loadSong;

    /// <summary> A universal songs folder directory for testing. </summary>
    public static string editorSongsFolder = "E:/Users/Unity/SYNERGY/Levels";
    /// <summary> The folder where songs are stored. </summary>
    public static string songsFolder
    {
        get => Utils.useEditorSongsFolder ? editorSongsFolder : Application.dataPath + "/Levels";
    }

    void Awake()
    {
        if (loadSong != null)
        {
            songCreation.OpenSong(loadSong, Beatmap.Active.info);
            loadSong = null;
        }
        else PopulateSongs();
    }

    /// <summary> Open the menu to create a new song. </summary>
    public void ShowCreateMenu()
    {
        createPanel.SetActive(true);
        createText.text = "";
        EventSystem.current.SetSelectedGameObject(createText.gameObject, null);
        createText.OnPointerClick(new PointerEventData(EventSystem.current));
    }

    /// <summary> Show an error on the song creation menu. </summary>
    public void CreateMenuError(string error)
    {
        errorText.text = error;
        var errorColor = errorText.color;
        errorColor.a = 1;
        errorText.color = errorColor;
    }
    
    void Update()
    {
        // Escapes to main menu on clicking escape
        if (Input.GetKeyDown(KeyCode.Escape)) songCreation.MainMenu();

        // Animates error text
        if (errorText.color.a > 0)
        {
            var errorColor = errorText.color;
            errorColor.a = Mathf.Max(errorColor.a - (Time.deltaTime / 2), 0);
            errorText.color = errorColor;
        }
    }

    /// <summary> Loads all of the songs from the song folder into the song selection. </summary>
    public void PopulateSongs()
    {
        // Manage Visibility
        songSelectionPanel.SetActive(true);
        songCreationPanel.SetActive(false);

        // Clear children
        for (var i = this.gameObject.transform.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(this.gameObject.transform.GetChild(i).gameObject);
        }

        // Get folders
        var folders = new DirectoryInfo(songsFolder).GetDirectories();

        foreach (var f in folders)
        {
            // Get Info.dat data
            var info = Beatmap.GetInfoFromPath(f.ToString());

            // Spawn UI element
            Button song = Instantiate(songPrefab);
            song.transform.SetParent(this.gameObject.transform);
            song.transform.localScale = new Vector3(1, 1, 1);
            song.onClick.AddListener(delegate () { songCreation.OpenSong(f.ToString(), info); });

            // Get set UI element data
            SongDisplayData songData = song.GetComponent<SongDisplayData>();
            songData.songName.text = info.name;
            songData.songArtist.text = info.artist;

            // Set cover art
            var coverArtPath = f + "/" + info.art;
            if (File.Exists(coverArtPath))
            {
                var coverArtData = File.ReadAllBytes(coverArtPath);
                Texture2D coverArt = new Texture2D(2, 2);
                coverArt.LoadImage(coverArtData);
                songData.coverArt.texture = coverArt;
            }
        }

        // Refresh scroll position
        var rectTrans = this.GetComponent<RectTransform>();
        rectTrans.position = new Vector3(
            rectTrans.position.x,
            -rectTrans.rect.height,
            rectTrans.position.z
        );
    }
}

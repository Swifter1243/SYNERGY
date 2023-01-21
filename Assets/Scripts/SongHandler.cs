using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SongHandler : MonoBehaviour
{
    public Button songPrefab;
    public GameObject createPanel;
    public InputField createText;
    public SongCreation songCreation;
    public GameObject songSelectionPanel;
    public GameObject songCreationPanel;
    public Text errorText;
    public static string loadSong;

    public static string songsFolder
    {
        get => Application.dataPath + "/Levels"; // TODO: REINSTATE
        // get => "E:/Users/Unity/SYNERGY/Levels";
    }

    // Start is called before the first frame update
    void Awake()
    {
        if (loadSong != null)
        {
            songCreation.OpenSong(loadSong, Beatmap.Active.info);
            loadSong = null;
        }
        else PopulateSongs();
    }

    public void ShowCreateMenu()
    {
        createPanel.SetActive(true);
        createText.text = "";
        EventSystem.current.SetSelectedGameObject(createText.gameObject, null);
        createText.OnPointerClick(new PointerEventData(EventSystem.current));
    }

    public void CreateMenuError(string error)
    {
        errorText.text = error;
        var errorColor = errorText.color;
        errorColor.a = 1;
        errorText.color = errorColor;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) songCreation.MainMenu();

        if (errorText.color.a > 0)
        {
            var errorColor = errorText.color;
            errorColor.a = Mathf.Max(errorColor.a - (Time.deltaTime / 2), 0);
            errorText.color = errorColor;
        }
    }

    public static Beatmap.Info getInfoFromPath(string path)
    {
        var infoPath = path + "/info.dat";
        var infoData = File.ReadAllText(infoPath);
        return JsonUtility.FromJson<Beatmap.Info>(infoData);
    }

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
            var info = getInfoFromPath(f.ToString());

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

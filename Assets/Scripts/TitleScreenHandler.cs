using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenHandler : MonoBehaviour
{
    public Texture2D cursorTexture;
    public Settings settings;

    void Start() {
        settings.Initialize();
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) settings.Close();
    }

    public void OpenSettings() => settings.gameObject.SetActive(true);

    public void Editor() => Transition.Load("EditorSongs");
    public void SongSelection() => Transition.Load("SongSelection");
    public void Tutorial() => Transition.Load("Tutorial");
}
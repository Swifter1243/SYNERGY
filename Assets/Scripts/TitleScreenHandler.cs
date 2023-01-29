using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenHandler : MonoBehaviour
{
    /// <summary> The texture for the cursor. </summary>
    public Texture2D cursorTexture;
    /// <summary> The reference to the settings class in this scene. </summary>
    public Settings settings;

    void Start() {
        settings.Initialize();
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
    }

    void Update() {
        // Close settings panel if escape is pressed.
        if (Input.GetKeyDown(KeyCode.Escape)) settings.Close();
    }

    /// <summary> Open the settings panel. </summary>
    public void OpenSettings() => settings.gameObject.SetActive(true);

    /// <summary> Open the editor song selection. </summary>
    public void Editor() => Transition.Load("EditorSongs");

    /// <summary> Open the playing song selection. </summary>
    public void SongSelection() => Transition.Load("SongSelection");

    /// <summary> Open the tutorial. </summary>
    public void Tutorial() => Transition.Load("Tutorial");

    /// <summary> Exit the game. </summary>
    public void Exit() => Application.Quit();
}
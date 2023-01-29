using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary> The logic for the level completed screen. </summary>
public class LevelEndHandler : MonoBehaviour
{
    /// <summary> The text that displays the status of the level finishing.
    // E.g. "Level Completed!" </summary>
    public Text levelCompleteText;
    /// <summary> The text displaying your score. </summary>
    public Text scoreText;
    /// <summary> The text displaying the amount of notes hit. </summary>
    public Text notesHitText;
    /// <summary> Whether the level was won. </summary>
    public static bool won = false;

    void Start()
    {
        if (won) UpdateStats();
        else
        {
            levelCompleteText.text = "Level Failed";
            levelCompleteText.color = new Color(1, 0.29f, 0.29f);
            levelCompleteText.GetComponent<Outline>().enabled = true;
            scoreText.text = "";
            notesHitText.text = "";
        }
    }

    /// <summary> Updates the text objects displaying level statistics. </summary>
    void UpdateStats()
    {
        scoreText.text = "Score: " + PlayHandler.score;
        notesHitText.text = "Notes Hit: " + PlayHandler.notesHit + "/" + PlayHandler.diff.notes.Count;
    }

    /// <summary> Exits the level. </summary>
    public void RunExit() => PlayHandler.exit();

    /// <summary> Restarts the level. </summary>
    public void Restart() => Transition.Load("Playing");
}

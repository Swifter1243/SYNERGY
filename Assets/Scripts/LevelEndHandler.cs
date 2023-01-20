using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelEndHandler : MonoBehaviour
{
    public Text levelCompleteText;
    public Text scoreText;
    public Text notesHitText;
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

    void UpdateStats()
    {
        scoreText.text = "Score: " + PlayHandler.score;
        notesHitText.text = "Notes Hit: " + PlayHandler.notesHit + "/" + PlayHandler.diff.notes.Count;
    }


    public void RunExit() => PlayHandler.exit();
    public void Restart()
    {
        PlayHandler.seconds = PlayHandler.startSeconds;
        Transition.Load("Playing");
    }
}

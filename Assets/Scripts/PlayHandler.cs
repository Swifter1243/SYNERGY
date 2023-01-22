using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

public class PlayHandler : MonoBehaviour
{
    public static float seconds = 0;
    public static float startSeconds = 0;
    float songBeats = 100000;
    public Text title;
    public Text artist;
    public RawImage cover;
    public MapVisuals mapVisuals;
    public static Beatmap.Info info;
    public static Beatmap.Difficulty diff;
    static AudioSource audioSource;
    public static Action exit;
    static float maxHealth = 2000;
    public static float health;
    static float missScore = 120;
    public static float score = 0;
    public static float notesHit = 0;

    class IntroAnimation
    {
        Text referenceText;
        RawImage referenceImage;
        float start;
        Vector3 position;
        RectTransform transform;

        public IntroAnimation(RawImage referenceImage, float start, RectTransform transform)
        {
            this.referenceImage = referenceImage;
            this.start = start;
            this.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            this.transform = transform;
        }
        public IntroAnimation(Text referenceText, float start, RectTransform transform)
        {
            this.referenceText = referenceText;
            this.start = start;
            this.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            this.transform = transform;
        }

        bool passedAnimate = false;

        public void Animate(float seconds)
        {
            var arriveEnd = start + 2;
            var waitEnd = start + 4;
            var leaveEnd = start + 5;

            if (seconds >= leaveEnd)
            {
                if (!passedAnimate)
                {
                    passedAnimate = true;
                    ChangeAlpha(0);
                }
                return;
            }

            var arriveFrac = Utils.GetFraction(start, arriveEnd, seconds);
            var leaveFrac = Utils.GetFraction(waitEnd, leaveEnd, seconds);

            if (seconds < start) ChangeAlpha(0);

            if (arriveFrac != -1)
            {
                ChangeAlpha(arriveFrac);
                var newFrac = Utils.EaseOutExpo(arriveFrac);
                ChangePosition(Mathf.Lerp(position.x - 300, position.x, newFrac));
            }

            if (leaveFrac != -1)
            {
                ChangeAlpha(1 - leaveFrac);
                var newFrac = Utils.EaseInExpo(leaveFrac);
                ChangePosition(Mathf.Lerp(position.x, position.x + 100, newFrac));
            }
        }

        void ChangePosition(float position) => transform.position = new Vector3(position, transform.position.y, transform.position.z);

        void ChangeAlpha(float alpha)
        {
            if (referenceText != null)
            {
                var color = referenceText.color;
                color.a = alpha;
                referenceText.color = color;
            }
            if (referenceImage != null)
            {
                var color = referenceImage.color;
                color.a = alpha;
                referenceImage.color = color;
            }
        }
    }

    List<IntroAnimation> animations = new List<IntroAnimation>();

    public Texture2D cursorTexture;
    public GameObject UI;

    void Start()
    {
        cursorTransform = secondaryCursor.GetComponent<RectTransform>();
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
        CursorVisibility();
        if (Settings.hideUI) UI.SetActive(false);

        score = 0;
        notesHit = 0;
        missStreak = 0;
        health = maxHealth / 2;
        scoreValueUtils = new Utils.RectTransformUtils(scoreValue);
        scoreValueUtils.right = GetScoreValueWidth(health);

        info = Beatmap.Active.info;
        mapVisuals.diff = diff;

        mapVisuals.onMirrorUpdate = () =>
        {
            horizontalMirror = mapVisuals.horizontalMirror;
            verticalMirror = mapVisuals.verticalMirror;
            CursorVisibility();
            UpdateSecondaryCursor();
        };

        // Video
        var videoPath = Utils.GetVideoPath(Beatmap.Active.songPath, info);
        mapVisuals.LoadVideo(videoPath, info);

        animations.Add(new IntroAnimation(title, 0.5f, title.GetComponent<RectTransform>()));
        animations.Add(new IntroAnimation(artist, 0.6f, artist.GetComponent<RectTransform>()));
        animations.Add(new IntroAnimation(cover, 0.7f, cover.GetComponent<RectTransform>()));

        title.text = info.name;
        artist.text = info.artist;
        cover.texture = Beatmap.Active.coverArt;

        audioSource = GetComponent<AudioSource>();
        audioSource.volume = Settings.masterVolume;
        audioSource.clip = Beatmap.Active.audio;
        audioSource.time = seconds;
        audioSource.Play();
        mapVisuals.PlayVideo(true);
        songBeats = Utils.GetSongBeats(audioSource.clip.length, info.BPM);

        var maxTime = TimeSpan.FromSeconds(audioSource.clip.length);
        maxTimeText = maxTime.ToString(@"m\:ss");
    }

    bool paused = false;
    public GameObject pauseMenu;

    public void Pause()
    {
        paused = true;
        audioSource.Pause();
        mapVisuals.PlayVideo(false);
        pauseMenu.SetActive(true);
    }

    public void UnPause()
    {
        paused = false;
        audioSource.UnPause();
        mapVisuals.PlayVideo(true);
        pauseMenu.SetActive(false);
    }

    public void Restart()
    {
        seconds = startSeconds;
        Transition.Load("Playing");
    }

    public void RunExit() => exit();

    bool horizontalMirror = false;
    bool verticalMirror = false;
    public RawImage secondaryCursor;
    RectTransform cursorTransform;
    Utils.NumberWatcher sizeWatcher = new Utils.NumberWatcher();
    float cursorAnimTime = 0.5f;
    float cursorAnim = 0;
    public static List<Vector3> primaryPos = new List<Vector3>();
    public static List<Vector3> secondaryPos = new List<Vector3>();
    float listLimit = 3;

    public static Vector3 GetMirroredCursor(Vector3 cursorPos, bool horizontal, bool vertical)
    {
        var screenDelta = new Vector3(Screen.width, Screen.height) / 2;
        cursorPos -= screenDelta;
        if (horizontal) cursorPos.y *= -1;
        if (vertical) cursorPos.x *= -1;
        cursorPos += screenDelta;
        return cursorPos;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) if (paused) UnPause(); else Pause();

        if (paused) return;

        ScoreValueApproach(health);

        sizeWatcher.Watch(Screen.width);
        if (sizeWatcher.Check()) ResizeCursor();
        cursorAnim -= Time.deltaTime;
        cursorAnim = Math.Max(cursorAnim, 0);
        var mirroredPos = UpdateSecondaryCursor();

        for (var i = primaryPos.Count; i <= listLimit; i++)
        {
            primaryPos.Add(Input.mousePosition);
            secondaryPos.Add(mirroredPos);
        }
        primaryPos.RemoveAt(0);
        secondaryPos.RemoveAt(0);

        seconds += Time.deltaTime;
        var beat = Utils.SecondsToBeat(seconds, info.BPM);

        animations.ForEach(x => { x.Animate(seconds); });
        mapVisuals.UpdateBeat(beat);
        UpdateText();

        if (beat > songBeats)
        {
            LevelEndHandler.won = true;
            Transition.Load("LevelEnd");
        }
    }

    public Text timeText;
    public Text scoreText;
    string maxTimeText;

    void UpdateText()
    {
        var currentTime = TimeSpan.FromSeconds(seconds);
        var currentTimeText = currentTime.ToString(@"m\:ss");
        timeText.text = currentTimeText + " / " + maxTimeText;

        if (updateScoreText) {
            scoreText.text = "Score: " + score;
            updateScoreText = false;
        }
    }

    Vector3 UpdateSecondaryCursor()
    {
        var cursorScale = secondaryCursor.transform.localScale;
        var scaleX = cursorScale.x > 0 ? 1 : -1;
        var scaleY = cursorScale.y > 0 ? 1 : -1;
        var fraction = Utils.EaseInExpo(cursorAnim / cursorAnimTime);
        cursorScale.x = Mathf.Lerp(scaleX, scaleX * 1.5f, fraction);
        cursorScale.y = Mathf.Lerp(scaleY, scaleY * 1.5f, fraction);
        secondaryCursor.transform.localScale = cursorScale;
        var mirroredPos = GetMirroredCursor(Input.mousePosition, horizontalMirror, verticalMirror);
        secondaryCursor.transform.position = mirroredPos;
        return mirroredPos;
    }

    void CursorVisibility()
    {
        secondaryCursor.gameObject.SetActive(horizontalMirror || verticalMirror);
        secondaryCursor.transform.localScale = new Vector2(verticalMirror ? -1 : 1, horizontalMirror ? -1 : 1);
        cursorAnim = cursorAnimTime;
        primaryPos.Clear();
        secondaryPos.Clear();
    }

    void ResizeCursor()
    {
        var screenX = GetComponent<RectTransform>().sizeDelta.x;
        var sizeScalar = screenX / Screen.width;
        var cursorSize = cursorTexture.width * sizeScalar;
        cursorTransform.sizeDelta = new Vector2(cursorSize, cursorSize);
    }

    public RectTransform scoreBar;
    public RectTransform scoreValue;
    public Utils.RectTransformUtils scoreValueUtils;

    float GetScoreValueWidth(float value)
    {
        var fraction = value / maxHealth;
        var width = scoreBar.sizeDelta.x;
        return (1 - fraction) * width;
    }

    void ScoreValueApproach(float value)
    {
        var newWidth = GetScoreValueWidth(value);
        scoreValueUtils.right = Utils.Approach(scoreValueUtils.right, newWidth, 5);
    }

    static float missStreak = 0;
    static bool updateScoreText = false;
    public static void AddScore(float value)
    {
        score += Mathf.Round(value);
        notesHit++;
        health = Mathf.Clamp(health + value, 0, maxHealth);
        missStreak = 0;
        updateScoreText = true;
    }
    
    public static void Miss()
    {
        missStreak++;
        var missVal = -missScore * missStreak;
        health = Mathf.Clamp(health + missVal, 0, maxHealth);
        if (health == 0 && !Settings.noFail)
        {
            LevelEndHandler.won = false;
            Transition.Load("LevelEnd");
        }
    }
}

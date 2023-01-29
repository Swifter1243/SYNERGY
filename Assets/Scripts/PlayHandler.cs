using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

/// <summary> Handler for playing scene. </summary>
public class PlayHandler : MonoBehaviour
{
    /// <summary> The current sections into the song. </summary>
    public static float seconds = 0;
    /// <summary> The seconds into the song to initialize the level with. </summary>
    public static float startSeconds = 0;
    /// <summary> The amount of base beats the song extends. </summary>
    float songBeats = 100000;
    /// <summary> The text for the title of the song in the bottom left. </summary>
    public Text title;
    /// <summary> The text for the artist of the song in the bottom left. </summary>
    public Text artist;
    /// <summary> The cover art of the song. </summary>
    public RawImage cover;
    /// <summary> The map visuals for the song. </summary>
    public MapVisuals mapVisuals;
    /// <summary> The current info of the song. </summary>
    public static Beatmap.Info info;
    /// <summary> The difficulty of the song. </summary>
    public static Beatmap.Difficulty diff;
    /// <summary> The audiosource for the song. </summary>
    static AudioSource audioSource;
    /// <summary> The function to run to exit the level. </summary>
    public static Action exit;
    /// <summary> The maximum amount of health in the health bar. </summary>
    static float maxHealth = 2000;
    /// <summary> The current amount of health. </summary>
    public static float health;
    /// <summary> The base health given when a note is hit. </summary>
    public static float hitHealth = 100;
    /// <summary> The base health reduced upon a miss. </summary>
    static float missHealth = 200;
    /// <summary> How much each additional miss in a streak takes away health. </summary>
    static float missStreakWeight = 0.6f;
    /// <summary> The current level score. </summary>
    public static float score = 0;
    /// <summary> The amount of notes hit in the level. </summary>
    public static float notesHit = 0;

    /// <summary> A class used to animate the song info at the start of the level. </summary>
    class IntroAnimation
    {
        /// <summary> The relevant text object being animated (if not null) </summary>
        Text referenceText;
        /// <summary> The relevant image object being animated (if not null) </summary>
        RawImage referenceImage;
        /// <summary> The beat the animation starts at. </summary>
        float start;
        /// <summary> The initial position of the animation.  </summary>
        Vector3 position;
        /// <summary> The transform of the relevant object being animated. </summary>
        RectTransform transform;

        /// <summary> A class used to animate the song info at the start of the level. </summary>
        /// <param name="referenceImage"> The relevant image object being animated.  </param>
        /// <param name="start"> The beat the animation starts at.  </param>
        /// <param name="transform"> The transform of the relevant object being animated. </param>
        public IntroAnimation(RawImage referenceImage, float start, RectTransform transform)
        {
            this.referenceImage = referenceImage;
            this.start = start;
            this.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            this.transform = transform;
        }
        /// <summary> A class used to animate the song info at the start of the level. </summary>
        /// <param name="referenceText"> The relevant image object being animated.  </param>
        /// <param name="start"> The beat the animation starts at.  </param>
        /// <param name="transform"> The transform of the relevant object being animated. </param>
        public IntroAnimation(Text referenceText, float start, RectTransform transform)
        {
            this.referenceText = referenceText;
            this.start = start;
            this.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            this.transform = transform;
        }

        /// <summary> If the animation has finished. </summary>
        bool animationFinished = false;

        /// <summary> Animate the object at a given time in the song. </summary>
        /// <param name="seconds"> Current seconds into the song. </param>
        public void Animate(float seconds)
        {
            var arriveEnd = start + 2;
            var waitEnd = start + 4;
            var leaveEnd = start + 5;

            if (seconds >= leaveEnd)
            {
                if (!animationFinished)
                {
                    animationFinished = true;
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
                ChangeXPosition(Mathf.Lerp(position.x - 300, position.x, newFrac));
            }

            if (leaveFrac != -1)
            {
                ChangeAlpha(1 - leaveFrac);
                var newFrac = Utils.EaseInExpo(leaveFrac);
                ChangeXPosition(Mathf.Lerp(position.x, position.x + 100, newFrac));
            }
        }

        /// <summary> Change the X position of the object transform. </summary>
        /// <param name="value"> The new x position. </param>
        void ChangeXPosition(float value) => transform.position = new Vector3(value, transform.position.y, transform.position.z);

        /// <summary> Change the alpha value of the object. </summary>
        /// <param name="alpha"> The value of the alpha. </param>
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

    /// <summary> A list of all the objects animated for the intro. </summary>
    List<IntroAnimation> animations = new List<IntroAnimation>();

    /// <summary> The image for the primary cursor. </summary>
    public Texture2D cursorTexture;

    /// <summary> The image for the secondary cursor. </summary>
    public RawImage secondaryCursor;

    /// <summary> An object parenting everything considered UI. </summary>
    public GameObject UI;

    void Start()
    {
        // Initialize cursor
        secondaryCursorTransform = secondaryCursor.GetComponent<RectTransform>();
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
        CursorVisibility();
        if (Settings.hideUI) UI.SetActive(false);

        // Initialize score
        score = 0;
        notesHit = 0;
        missStreak = 0;
        health = maxHealth / 2;
        scoreValueUtils = new Utils.RectTransformUtils(scoreValue);
        scoreValueUtils.right = GetHealthValueWidth(health);

        // Initialize info and map visuals
        info = Beatmap.Active.info;
        mapVisuals.diff = diff;
        mapVisuals.spawnCutoff = Utils.SecondsToBeat(startSeconds, info.BPM);
        seconds = Math.Max(startSeconds - 1, 0);

        mapVisuals.onMirrorUpdate = () =>
        {
            horizontalMirror = mapVisuals.horizontalMirror;
            verticalMirror = mapVisuals.verticalMirror;
            CursorVisibility();
            UpdateSecondaryCursor();
        };

        // Initialize video
        var videoPath = Beatmap.GetVideoPath(Beatmap.Active.songPath, info);
        mapVisuals.LoadVideo(videoPath);

        // Initialize intro animations
        animations.Add(new IntroAnimation(title, 0.5f, title.GetComponent<RectTransform>()));
        animations.Add(new IntroAnimation(artist, 0.6f, artist.GetComponent<RectTransform>()));
        animations.Add(new IntroAnimation(cover, 0.7f, cover.GetComponent<RectTransform>()));

        // Initialize 
        title.text = info.name;
        artist.text = info.artist;
        cover.texture = Beatmap.Active.coverArt;

        // Initialize audio source
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = Settings.masterVolume;
        audioSource.clip = Beatmap.Active.audio;
        audioSource.time = seconds;
        audioSource.Play();
        mapVisuals.PlayVideo(true);
        songBeats = Utils.GetSongBeats(audioSource.clip.length, info.BPM);

        // Set the song length text
        var maxTime = TimeSpan.FromSeconds(audioSource.clip.length);
        levelLength = maxTime.ToString(@"m\:ss");
    }

    /// <summary> Whether the level is paused </summary>
    bool paused = false;
    /// <summary> The parent for the pause menu </summary>
    public GameObject pauseMenu;

    /// <summary> Pause the level </summary>
    public void Pause()
    {
        paused = true;
        audioSource.Pause();
        mapVisuals.PlayVideo(false);
        pauseMenu.SetActive(true);
    }

    /// <summary> Unpause the level </summary>
    public void UnPause()
    {
        paused = false;
        audioSource.UnPause();
        mapVisuals.PlayVideo(true);
        pauseMenu.SetActive(false);
    }

    /// <summary> Restart the level </summary>
    public void Restart() => Transition.Load("Playing");

    /// <summary> Exit the level </summary>
    public void RunExit() => exit();

    /// <summary> Whether the horizontal mirror is enabled. </summary>
    bool horizontalMirror = false;
    /// <summary> Whether the vertical mirror is enabled. </summary>
    bool verticalMirror = false;
    /// <summary> Transform of the secondary cursor. </summary>
    RectTransform secondaryCursorTransform;

    /// <summary> Watcher for the size of the screen.  </summary>
    Utils.NumberWatcher sizeWatcher = new Utils.NumberWatcher();

    /// <summary> The amount of time for the secondary cursor pop in animation. </summary>
    float cursorAnimTime = 0.5f;
    /// <summary> The current animation time for the secondary cursor pop in animation. </summary>
    float cursorAnim = 0;

    /// <summary> The position history of the primary cursor for last few frames. </summary>
    public static List<Vector3> primaryPos = new List<Vector3>();
    /// <summary> The position history of the secondary cursor for last few frames. </summary>
    public static List<Vector3> secondaryPos = new List<Vector3>();
    /// <summary> The amount of frames to store the cursor information. </summary>
    float listLimit = 3;

    /// <summary> Get the mirrored cursor position in screenspace based on current mirrors. </summary>
    /// <param name="cursorPos"> The position of the cursor. </param>
    /// <param name="horizontal"> Whether the horizontal mirror is activated. </param>
    /// <param name="vertical"> Whether the vertical mirror is activated. </param>
    public static Vector3 GetMirroredCursor(Vector3 cursorPos, bool horizontal, bool vertical)
    {
        if (!horizontal & !vertical) return cursorPos;
        var screenDelta = new Vector3(Screen.width, Screen.height) / 2;
        cursorPos -= screenDelta;
        if (horizontal) cursorPos.y *= -1;
        if (vertical) cursorPos.x *= -1;
        cursorPos += screenDelta;
        return cursorPos;
    }

    void Update()
    {
        // Pause the song on pressing escape
        if (Input.GetKeyDown(KeyCode.Escape)) if (paused) UnPause(); else Pause();

        // Don't do frame logic if paused
        if (paused) return;

        // Animate health bar
        HealthValueApproach(health);

        // Resize cursor if screen size changes
        sizeWatcher.Watch(Screen.width);
        if (sizeWatcher.Check()) ResizeCursor();

        // Update secondary cursor pop in animation
        cursorAnim -= Time.deltaTime;
        cursorAnim = Math.Max(cursorAnim, 0);
        var mirroredPos = UpdateSecondaryCursor();

        // Register cursor positions to frame history
        for (var i = primaryPos.Count; i <= listLimit; i++)
        {
            primaryPos.Add(Input.mousePosition);
            secondaryPos.Add(mirroredPos);
        }
        primaryPos.RemoveAt(0);
        secondaryPos.RemoveAt(0);

        // Progress song seconds
        seconds += Time.deltaTime;
        var beat = Utils.SecondsToBeat(seconds, info.BPM);

        // Update visuals
        animations.ForEach(x => { x.Animate(seconds); });
        mapVisuals.UpdateBeat(beat);
        if (!Settings.hideUI) UpdateText();

        // End level if song finished
        if (beat > songBeats)
        {
            LevelEndHandler.won = true;
            Transition.Load("LevelEnd");
        }
    }

    /// <summary> The text showing the current level time. </summary>
    public Text timeText;
    /// <summary> The text showing the current score. </summary>
    public Text scoreText;
    /// <summary> A string displaying the level length. </summary>
    string levelLength;

    /// <summary> Update the onscreen text </summary>
    void UpdateText()
    {
        var currentTime = TimeSpan.FromSeconds(seconds);
        var currentTimeText = currentTime.ToString(@"m\:ss");
        timeText.text = currentTimeText + " / " + levelLength;

        if (updateScoreText)
        {
            scoreText.text = "Score: " + score;
            updateScoreText = false;
        }
    }

    /// <summary> Update the size and position of the secondary cursor, returning the mirrored position. </summary>
    Vector3 UpdateSecondaryCursor()
    {
        // Initialize scale
        var cursorScale = secondaryCursor.transform.localScale;
        var scaleX = cursorScale.x > 0 ? 1 : -1;
        var scaleY = cursorScale.y > 0 ? 1 : -1;

        // Update scale based on animation
        var fraction = Utils.EaseInExpo(cursorAnim / cursorAnimTime);
        cursorScale.x = Mathf.Lerp(scaleX, scaleX * 1.5f, fraction);
        cursorScale.y = Mathf.Lerp(scaleY, scaleY * 1.5f, fraction);
        secondaryCursor.transform.localScale = cursorScale;

        // Update position
        var mirroredPos = GetMirroredCursor(Input.mousePosition, horizontalMirror, verticalMirror);
        secondaryCursor.transform.position = mirroredPos;
        return mirroredPos;
    }

    /// <summary> Update the visibility of the secondary cursor. </summary>
    void CursorVisibility()
    {
        secondaryCursor.gameObject.SetActive(horizontalMirror || verticalMirror);
        secondaryCursor.transform.localScale = new Vector2(verticalMirror ? -1 : 1, horizontalMirror ? -1 : 1);
        cursorAnim = cursorAnimTime;
        primaryPos.Clear();
        secondaryPos.Clear();
    }

    /// <summary> Resize cursor based on screen height. </summary>
    void ResizeCursor()
    {
        var screenX = GetComponent<RectTransform>().sizeDelta.x;
        var sizeScalar = screenX / Screen.width;
        var cursorSize = cursorTexture.width * sizeScalar;
        secondaryCursorTransform.sizeDelta = new Vector2(cursorSize, cursorSize);
    }

    /// <summary> Transform for the health bar base. </summary>
    public RectTransform scoreBar;
    /// <summary> Transform for the health bar value. </summary>
    public RectTransform scoreValue;
    /// <summary> Transform handler for the healthbar value transform. </summary>
    public Utils.RectTransformUtils scoreValueUtils;

    /// <summary> Gets the width of the health bar given an amount of health. </summary>
    /// <param name="value"> The amount of heatlh. </param>
    float GetHealthValueWidth(float value)
    {
        var fraction = value / maxHealth;
        var width = scoreBar.sizeDelta.x;
        return (1 - fraction) * width;
    }

    /// <summary> Approaches the width of the health bar to a certain amount of health. </summary>
    /// <param name="value"> The amount of heatlh. </param>
    void HealthValueApproach(float value)
    {
        var newWidth = GetHealthValueWidth(value);
        scoreValueUtils.right = Utils.Approach(scoreValueUtils.right, newWidth, 5);
    }

    /// <summary> The amount of misses in a row. </summary>
    static float missStreak = 0;
    /// <summary> If set to true, the score text will be updated the next frame. </summary>
    static bool updateScoreText = false;

    /// <summary> Adds score to the current session. </summary>
    /// <param name="score"> The amount of score to add. </param>
    public static void AddScore(float value)
    {
        score += Mathf.Round(value);
        notesHit++;
        var addedHealth = (value / Beatmap.NoteScore.total) * hitHealth;
        health = Mathf.Clamp(health + addedHealth, 0, maxHealth);
        missStreak = Math.Max(0, missStreak - 1);
        updateScoreText = true;
    }

    /// <summary> Registers a miss in the current session. </summary>
    public static void Miss()
    {
        var missVal = -(missHealth + missHealth * missStreak * missStreakWeight);
        missStreak++;
        health = Mathf.Clamp(health + missVal, 0, maxHealth);
        if (health == 0 && !Settings.noFail)
        {
            LevelEndHandler.won = false;
            Transition.Load("LevelEnd");
        }
    }
}

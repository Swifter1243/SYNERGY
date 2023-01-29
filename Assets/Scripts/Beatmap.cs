using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System;
using System.IO;

/// <summary> Logic relevant to the internals of beatmaps. </summary>
public class Beatmap : MonoBehaviour
{
    /// <summary> Class containing information about a beatmap. </summary>
    public class Info
    {
        /// <summary> Name of the song. </summary>
        public string name;
        /// <summary> Artist of the song. </summary>
        public string artist;
        /// <summary> Mapper of the song. </summary>
        public string mapper;
        /// <summary> Beats per minute of the song. </summary>
        public float BPM = 100;
        /// <summary> Cover art filename relative to the song folder. </summary>
        public string art = "cover.jpg";
        /// <summary> Song audio filename relative to the song folder. </summary>
        public string song = "song.ogg";
        /// <summary> Video filename relative to the song folder. </summary>
        public string video = "video.mp4";
        /// <summary> Beats to offset the time of the video by. </summary>
        public float videoOffset = 0;
    }

    /// <summary> Copy a Beatmap object. </summary>
    /// <param name="o"> Object to copy. </param>
    public static T Copy<T>(T o) where T : BeatmapObject
    {
        if (o is Note note)
        {
            var obj = new Note();
            obj.time = note.time;
            obj.primary = note.primary;
            obj.axis = note.axis;
            obj.direction = note.direction;
            obj.x = note.x;
            obj.y = note.y;
            return obj as T;
        }
        if (o is Swap swap)
        {
            var obj = new Swap();
            obj.time = swap.time;
            obj.type = swap.type;
            return obj as T;
        }
        if (o is BPMChange BPMchange)
        {
            var obj = new BPMChange();
            obj.time = BPMchange.time;
            obj.endTime = BPMchange.endTime;
            obj.start = BPMchange.start;
            obj.end = BPMchange.end;
            return obj as T;
        }
        if (o is GameplayObject)
        {
            var obj = new GameplayObject();
            obj.time = o.time;
            return obj as T;
        }
        if (o is BeatmapObject)
        {
            var obj = new BeatmapObject();
            obj.time = o.time;
            return obj as T;
        }
        return o;
    }

    /// <summary> An object in a beatmap difficulty. </summary>
    public class BeatmapObject
    {
        public float time = 0;
    }

    /// <summary> An object in a beatmap difficulty that is used for gameplay. </summary>
    public class GameplayObject : BeatmapObject { };

    /// <summary> A note object in a beatmap difficulty. </summary>
    [Serializable]
    public class Note : GameplayObject
    {
        /// <summary> Whether this note is a primary note. If false, it is a secondary note. </summary>
        public bool primary = true;
        /// <summary> Whether this note has an axis. </summary>
        public bool axis = false;
        /// <summary> The angle in degrees this note will be facing. </summary>
        public float direction = 0;
        /// <summary> The x position of the note. 0 is left of the screen, 1 is right of the screen. </summary>
        public float x = 0;
        /// <summary> The y position of the note. 0 is bottom of the screen, 1 is top of the screen. </summary>
        public float y = 0;
    }

    /// <summary> Types of swaps. </summary>
    public enum SwapType
    {
        Horizontal,
        Vertical
    }

    /// <summary> A swap object in a beatmap difficulty. </summary>
    [Serializable]
    public class Swap : GameplayObject
    {
        /// <summary> The type of this swap. </summary>
        public SwapType type = SwapType.Horizontal;
    }

    /// <summary> A bpm change in a beatmap difficulty. Currently unused. </summary>
    [Serializable]
    public class BPMChange : BeatmapObject
    {
        public float endTime = 0;
        public float start = 0;
        public float end = 0;
    }

    /// <summary> A beatmap difficulty. </summary>
    public class Difficulty
    {
        /// <summary> Note objects in this difficulty. </summary>
        public List<Note> notes;
        /// <summary> Swap objects in this difficulty. </summary>
        public List<Swap> swaps;
        /// <summary> BPM changes in this difficulty. </summary>
        public List<BPMChange> bpmChanges;
    }

    /// <summary> Static class containing information about the active difficulty. </summary>
    public static class Active
    {
        /// <summary> The name of the active difficulty. </summary>
        public static string diffName;
        /// <summary> The path to the active difficulty file. </summary>
        public static string diffPath;
        /// <summary> The path to the audio file of the active song. </summary>
        public static string songPath;
        /// <summary> The loaded audio data of the active song. </summary>
        public static AudioClip audio;
        /// <summary> The video player containing the video of the active song. </summary>
        public static VideoPlayer video;
        /// <summary> The info of the active difficulty. </summary>
        public static Info info;
        /// <summary> The image data of the cover art of the active song. </summary>
        public static Texture2D coverArt;
        /// <summary> The amount of seconds for a note to spawn in. </summary>
        public static float spawnInSeconds = 1.1f;
        /// <summary> The amount of seconds a note is hittable for before it's time. </summary>
        public static float hittableSeconds = 0.2f;
        /// <summary> The amount of seconds the note will spawn out for. 
        /// The note is considered missed halfway through this duration. </summary>
        public static float spawnOutSeconds = 0.65f;
    }

    /// <summary> The primary color in the game color scheme. </summary>
    public static Color PrimaryColor = new Color(45.9f / 100, 78.8f / 100, 86.3f / 100);
    /// <summary> The secondary color in the game color scheme. </summary>
    public static Color SecondaryColor = new Color(88.6f / 100, 55.7f / 100, 88.6f / 100);

    /// <summary> The pixel size of notes. (Roughly... there's some logic applied to it depending on resolution) </summary>
    public static float noteSize = 90;

    public static class NoteScore
    {
        /// <summary> The score given for hitting a note. </summary>
        public static float hit = 20;
        /// <summary> The maximum score given for hitting a note straight through the center.
        // Zero points are given if the player cuts through the very edge. </summary>
        public static float center = 40;
        /// <summary> The maximum score given for hitting a note directly through it's axis, if any.
        // Zero points are given past hitting the axis at an angle greater than 45 degrees. </summary>
        public static float axis = 70;
        
        public static float total = hit + center + axis;

        // If a note has no axis, hit and center scores are multiplied to maximize to the same as hit + center + axis.

        /// <summary> Window of seconds from the note's time that would allow for full score. 
        /// This value is the length of the window from before to after. </summary>
        public static float timingLenienceSeconds = 0.07f;

        /// <summary> The higher percent of the full range of center scores that would count for full points.
        /// So 5% center lenience would mean that cutting with 95% accuracy is rounded to 100%. </summary>
        public static float centerLenience = 0.08f;

        /// <summary> The higher percent of the full range of axis scores that would count for full points.
        /// So 5% axis lenience would mean that cutting within a 5% degree of the axis gives full points. </summary>
        public static float angleLenience = 0.08f;
    }

    /// <summary> Calculates the percentage of the early and late windows that would count for full score. </summary>
    public class NoteLeniences
    {
        /// <summary> The higher percentage of the early window that will count for full score.
        /// So if the value is 0.5, then hitting the note in the last half of the early window counts for full points. </summary>
        public float lenienceIn;
        /// <summary> The lower percentage of the late window that will count for full score.
        /// So if the value is 0.5, then hitting the note in the first half of the late window counts for full points. </summary>
        public float lenienceOut;

        public NoteLeniences()
        {
            lenienceIn = Active.spawnInSeconds - NoteScore.timingLenienceSeconds / 2;
            lenienceIn /= Active.spawnInSeconds;
            lenienceOut = Active.spawnOutSeconds - NoteScore.timingLenienceSeconds / 2;
            lenienceOut /= Active.spawnOutSeconds;
        }
    }

    /// <summary> Get the Info class from info file of a song. </summary>
    /// <param name="path"> The path of the song. </param>
    public static Beatmap.Info GetInfoFromPath(string path)
    {
        var infoPath = GetInfoPath(path);
        var infoData = File.ReadAllText(infoPath);
        return JsonUtility.FromJson<Beatmap.Info>(infoData);
    }

    /// <summary> Gets the path to a difficulty's dat file in a song. </summary>
    /// <param name="songPath"> The path of the song. </param>
    /// <param name="diff"> The name of the difficulty. </param>
    public static string GetDiffPath(string songPath, string diff) => songPath + "\\" + diff + ".dat";

    /// <summary> Gets the path to a song's info.dat. </summary>
    /// <param name="songPath"> The path of the song. </param>
    public static string GetInfoPath(string songPath) => songPath + "\\info.dat";

    /// <summary> Gets the path to the audio file of a song. </summary>
    /// <param name="songPath"> The path of the song. </param>
    /// <param name="info"> The info of the song. </param>
    public static string GetAudioPath(string songPath, Beatmap.Info info) => songPath + "\\" + info.song;

    /// <summary> Gets the path to the video file of a song. </summary>
    /// <param name="songPath"> The path of the song. </param>
    /// <param name="info"> The info of the song. </param>
    public static string GetVideoPath(string songPath, Beatmap.Info info) => songPath + "\\" + info.video;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System;

public class Beatmap : MonoBehaviour
{
    public class Info
    {
        public string name;
        public string artist;
        public string mapper;
        public float BPM = 100;
        public string art = "cover.jpg";
        public string song = "song.ogg";
        public string video = "video.mp4";
        public float videoOffset = 0;
    }

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

    public class BeatmapObject
    {
        public float time = 0;
    }

    public class GameplayObject : BeatmapObject { };

    [Serializable]
    public class Note : GameplayObject
    {
        public bool primary = true;
        public bool axis = false;
        public float direction = 0;
        public float x = 0;
        public float y = 0;
    }

    public enum SwapType
    {
        Horizontal,
        Vertical
    }

    [Serializable]
    public class Swap : GameplayObject
    {
        public SwapType type = SwapType.Horizontal;
    }

    [Serializable]
    public class BPMChange : BeatmapObject
    {
        public float endTime = 0;
        public float start = 0;
        public float end = 0;
    }

    public class Difficulty
    {
        public List<Note> notes;
        public List<Swap> swaps;
        public List<BPMChange> bpmChanges;
    }

    public static class Active
    {
        public static string diffName;
        public static string diffPath;
        public static string songPath;
        public static AudioClip audio;
        public static VideoPlayer video;
        public static Info info;
        public static Texture2D coverArt;
        public static float spawnIn = 5;
        public static float spawnOut = 1;
    }

    public static Color PrimaryColor = new Color(45.9f / 100, 78.8f / 100, 86.3f / 100);
    public static Color SecondaryColor = new Color(88.6f / 100, 55.7f / 100, 88.6f / 100);

    public static float noteSize = 90;

    public static class NoteScore
    {
        public static float hit = 20;
        public static float center = 40;
        public static float axis = 70;
    }
}
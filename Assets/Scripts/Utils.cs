using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class Utils : MonoBehaviour
{
    public static bool useEditorDirectory = false; // TODO: Make this false on release.
    public static float BeatToSeconds(float beat, float bpm) => beat / bpm * 60;
    public static float SecondsToBeat(float seconds, float bpm) => seconds * bpm / 60;
    public static float GetSongBeats(float songSeconds, float bpm) => Mathf.Ceil(SecondsToBeat(songSeconds, bpm));
    public static float EaseInExpo(float number) => number == 0 ? 0 : Mathf.Pow(2, 10 * number - 10);
    public static float EaseOutExpo(float number) => number == 1 ? 1 : 1 - Mathf.Pow(2, -10 * number);
    public static float EaseInSine(float number) => 1 - Mathf.Cos((number * Mathf.PI) / 2);
    public static float EaseOutSine(float number) => Mathf.Sin((number * Mathf.PI) / 2);
    public static float EaseInCubic(float number) => number * number * number;
    public static float EaseOutCubic(float number) => 1 - Mathf.Pow(1 - number, 3);
    public static float GetFraction(float min, float max, float number)
    {
        if (number < min || number > max) return -1;
        var difference = max - min;
        var inRange = number - min;
        return inRange / difference;
    }
    public static float SetLenience(float lenience, float value) => value >= lenience ? 1 : value / lenience;

    public static Color ChangeAlpha(Color color, float alpha) => ChangeColor(color, x =>
    {
        x.a = alpha;
        return x;
    });
    public static Color ChangeColor(Color color, Func<Color, Color> fn) => fn(color);
    public class NumberWatcher
    {
        List<float> oldWatched = new List<float>();
        List<float> watched = new List<float>();

        public void Watch(float value) => watched.Add(value);

        public bool Check()
        {
            var pass = true;
            var index = 0;

            if (oldWatched.Count != watched.Count)
            {
                Step();
                return true;
            }

            watched.ForEach(x =>
            {
                if (x != oldWatched[index]) pass = false;
                index++;
            });

            Step();
            return !pass;
        }

        void Step()
        {
            oldWatched.Clear();
            oldWatched.AddRange(watched);
            watched.Clear();
        }
    }
    public static float InitPlayerPrefsFloat(string key, float value)
    {
        if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetFloat(key, value);
        return PlayerPrefs.GetFloat(key);
    }
    public static int InitPlayerPrefsInt(string key, int value)
    {
        if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetInt(key, value);
        return PlayerPrefs.GetInt(key);
    }
    public static string InitPlayerPrefsString(string key, string value)
    {
        if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetString(key, value);
        return PlayerPrefs.GetString(key);
    }
    public class Vector3Interpolator
    {
        FloatInterpolator x;
        FloatInterpolator y;
        FloatInterpolator z;

        public Vector3Interpolator(Vector3 init, float lerpAmount)
        {
            x = new FloatInterpolator(init.x, lerpAmount);
            y = new FloatInterpolator(init.y, lerpAmount);
            z = new FloatInterpolator(init.z, lerpAmount);
        }

        public Vector3 approach(Vector3 target)
        {
            return new Vector3(
                x.approach(target.x),
                y.approach(target.y),
                z.approach(target.z)
            );
        }
    }

    public class FloatInterpolator
    {
        float lerpAmount = 0.5f;
        float last;

        public FloatInterpolator(float init, float lerpAmount)
        {
            this.last = init;
            this.lerpAmount = lerpAmount;
        }

        public float approach(float target)
        {
            var newFloat = Mathf.Lerp(this.last, target, Time.deltaTime / this.lerpAmount);
            this.last = newFloat;
            return newFloat;
        }
    }

    public static float GetAngleFromPos(Vector3 pos, Vector3 center = new Vector3()) => Mathf.Atan2(pos.x - center.x, pos.y - center.y) * Mathf.Rad2Deg;
    public static Vector2 GetPosFromAngle(float angle) => new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle));

    public class RectTransformUtils
    {
        RectTransform rect;
        public RectTransformUtils(RectTransform rect) => this.rect = rect;

        public float left
        {
            get => rect.offsetMin.x;
            set => rect.offsetMin = new Vector2(value, rect.offsetMin.y);
        }

        public float right
        {
            get => -rect.offsetMax.x;
            set => rect.offsetMax = new Vector2(-value, rect.offsetMax.y);
        }

        public float top
        {
            get => -rect.offsetMax.y;
            set => rect.offsetMax = new Vector2(rect.offsetMax.x, -value);
        }

        public float bottom
        {
            get => rect.offsetMin.y;
            set => rect.offsetMin = new Vector2(rect.offsetMin.x, value);
        }
    }

    public static float Approach(float old, float target, float speed = 1) => Mathf.Lerp(old, target, Time.deltaTime * speed);
    public static float MirrorAngleX(float angle)
    {
        angle = (180 - angle) % 360;
        return angle < 0 ? 360 + angle : angle;
    }
    public static Texture2D LoadImage(string path)
    {
        var image = new Texture2D(2, 2);
        var imageData = File.ReadAllBytes(path);
        image.LoadImage(imageData);
        return image;
    }

    public static void OpenFileExplorer(string path)
    {
        var safePath = path.Replace(@"/", @"\");
        System.Diagnostics.Process.Start("explorer.exe", "/root," + safePath);
    }
}

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

[Obsolete]
class OnBuild : IPostprocessBuild
{
    public int callbackOrder { get { return 0; } }
    public void OnPostprocessBuild(BuildTarget target, string path)
    {
        // Move levels to build if not using editor songs folder
        if (!Utils.useEditorSongsFolder)
        {
            var dataPath = Path.GetDirectoryName(path) + "\\SYNERGY_Data";
            var levelsPath = dataPath + "\\Levels";
            var editorSongsPath = SongHandler.editorSongsFolder;

            Utils.CopyDirectory(editorSongsPath, levelsPath, true);
        }
    }
}
#endif

/// <summary> A collection of useful tools for programming this game. </summary>
public class Utils : MonoBehaviour
{
    /// <summary> Whether the levels should be from a hardcoded directory, or based on the application directory.
    /// Used for testing purposes, should be false on release. </summary>
    public static bool useEditorSongsFolder = false;

    /// <summary> Convert a beat in a song to seconds. </summary>
    /// <param name="beat"> The beat to convert. </param>
    /// <param name="BPM"> The BPM of the song. </param>
    public static float BeatToSeconds(float beat, float BPM) => beat / BPM * 60;

    /// <summary> Convert seconds to a beat in a song. </summary>
    /// <param name="seconds"> The seconds. </param>
    /// <param name="BPM"> The BPM of the song. </param>
    public static float SecondsToBeat(float seconds, float BPM) => seconds * BPM / 60;

    /// <summary> Get the amount of beats in a song. </summary>
    /// <param name="songSeconds"> The length in seconds of the song. </param>
    /// <param name="BPM"> The BPM of the song. </param>
    public static float GetSongBeats(float songSeconds, float BPM) => Mathf.Ceil(SecondsToBeat(songSeconds, BPM));

    /// <summary> Remap a number using the EaseInExpo easing. (https://easings.net/) </summary>
    /// <param name="number"> The number to remap. </param>
    public static float EaseInExpo(float number) => number == 0 ? 0 : Mathf.Pow(2, 10 * number - 10);

    /// <summary> Remap a number using the EaseOutExpo easing. (https://easings.net/) </summary>
    /// <param name="number"> The number to remap. </param>
    public static float EaseOutExpo(float number) => number == 1 ? 1 : 1 - Mathf.Pow(2, -10 * number);

    /// <summary> Remap a number using the EaseInSine easing. (https://easings.net/) </summary>
    /// <param name="number"> The number to remap. </param>
    public static float EaseInSine(float number) => 1 - Mathf.Cos((number * Mathf.PI) / 2);

    /// <summary> Remap a number using the EaseOutSine easing. (https://easings.net/) </summary>
    /// <param name="number"> The number to remap. </param>
    public static float EaseOutSine(float number) => Mathf.Sin((number * Mathf.PI) / 2);

    /// <summary> Remap a number using the EaseInCubic easing. (https://easings.net/) </summary>
    /// <param name="number"> The number to remap. </param>
    public static float EaseInCubic(float number) => number * number * number;

    /// <summary> Remap a number using the EaseOutCubic easing. (https://easings.net/) </summary>
    /// <param name="number"> The number to remap. </param>
    public static float EaseOutCubic(float number) => 1 - Mathf.Pow(1 - number, 3);

    /// <summary> Get a percentage between a range of numbers of where a given number sits. Returns -1 if the number is outside of the range. </summary>
    /// <param name="min"> Minimum of the range. </param>
    /// <param name="max"> Maximum of the range. </param>
    /// <param name="number"> Number to calculate the percentage for. </param>
    public static float GetFraction(float min, float max, float number)
    {
        if (number < min || number > max) return -1;
        var difference = max - min;
        var inRange = number - min;
        return inRange / difference;
    }

    /// <summary> Remap a value between 0 and 1 to be constantly 1 past a given threshold, while smoothly approaching that threshold before that point. </summary>
    /// <param name="lenience"> The threshold. </param>
    /// <param name="value"> Value to remap. </param>
    public static float SetLenience(float lenience, float value) => value >= lenience ? 1 : value / lenience;

    /// <summary> Change the alpha value of a color. </summary>
    /// <param name="color"> The color to change. </param>
    /// <param name="alpha"> The new alpha value. </param>
    public static Color ChangeAlpha(Color color, float alpha) => ChangeColor(color, x =>
    {
        x.a = alpha;
        return x;
    });

    /// <summary> Run a function on a color. </summary>
    /// <param name="color"> The color to change. </param>
    /// <param name="fn"> The function to run on the color. </param>
    public static Color ChangeColor(Color color, Func<Color, Color> fn) => fn(color);

    /// <summary> Class to look for changes in numbers. </summary>
    public class NumberWatcher
    {
        /// <summary> Variables in the last run of Step(). </summary>
        List<float> oldWatched = new List<float>();
        /// <summary> Current variables being watched. </summary>
        List<float> watched = new List<float>();

        /// <summary> Add a variable to be watched. </summary>
        /// <param name="value"> The value to watch. </param>
        public void Watch(float value) => watched.Add(value);

        /// <summary> Check if all variables are the same in both lists, and return true if that's the case. </summary>
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

        /// <summary> Move current watched variables to the oldWatched list to be compared. </summary>
        void Step()
        {
            oldWatched.Clear();
            oldWatched.AddRange(watched);
            watched.Clear();
        }
    }

    /// <summary> Get a float from PlayerPrefs, initialize if needed. </summary>
    /// <param name="key"> The key of the value being stored. </param>
    /// <param name="value"> The value being stored. </param>
    public static float InitPlayerPrefsFloat(string key, float value)
    {
        if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetFloat(key, value);
        return PlayerPrefs.GetFloat(key);
    }

    /// <summary> Get an int from PlayerPrefs, initialize if needed. </summary>
    /// <param name="key"> The key of the value being stored. </param>
    /// <param name="value"> The value being stored. </param>
    public static int InitPlayerPrefsInt(string key, int value)
    {
        if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetInt(key, value);
        return PlayerPrefs.GetInt(key);
    }

    /// <summary> Get a string from PlayerPrefs, initialize if needed. </summary>
    /// <param name="key"> The key of the value being stored. </param>
    /// <param name="value"> The value being stored. </param>
    public static string InitPlayerPrefsString(string key, string value)
    {
        if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetString(key, value);
        return PlayerPrefs.GetString(key);
    }

    /// <summary> Class used to approach a Vector3 gradually. </summary>
    public class Vector3Interpolator
    {
        /// <summary> The interpolator for the x component. </summary>
        FloatInterpolator x;
        /// <summary> The interpolator for the y component. </summary>
        FloatInterpolator y;
        /// <summary> The interpolator for the z component. </summary>
        FloatInterpolator z;

        /// <summary> The speed of approaching. </summary>
        public float lerpSpeed
        {
            get => x.lerpSpeed;
            set
            {
                x.lerpSpeed = value;
                y.lerpSpeed = value;
                z.lerpSpeed = value;
            }
        }

        /// <summary> Class used to approach a Vector3 gradually. </summary>
        /// <param name="init"> The value to initialize the interpolator with. </param>
        /// <param name="lerpSpeed"> The speed of approaching. </summary>
        public Vector3Interpolator(Vector3 init, float lerpSpeed)
        {
            x = new FloatInterpolator(init.x, lerpSpeed);
            y = new FloatInterpolator(init.y, lerpSpeed);
            z = new FloatInterpolator(init.z, lerpSpeed);
        }

        /// <summary> Approach a target Vector3. </summary>
        /// <param name="target"> Target to approach. </param>
        public Vector3 Approach(Vector3 target)
        {
            return new Vector3(
                x.Approach(target.x),
                y.Approach(target.y),
                z.Approach(target.z)
            );
        }
    }

    /// <summary> Class used to approach a float gradually. </summary>
    public class FloatInterpolator
    {
        /// <summary> The speed of approaching. </summary>
        public float lerpSpeed = 0.5f;
        /// <summary> The previous value. </summary>
        float last;

        /// <summary> Class used to approach a float gradually. </summary>
        /// <param name="init"> The float to initialize the interpolator with. </param>
        /// <param name="lerpSpeed"> The speed of approaching. </param>
        public FloatInterpolator(float init, float lerpSpeed)
        {
            this.last = init;
            this.lerpSpeed = lerpSpeed;
        }

        /// <summary> Approach a target float. </summary>
        /// <param name="target"> Target to approach. </param>
        public float Approach(float target)
        {
            var newFloat = Mathf.Lerp(this.last, target, Time.deltaTime / lerpSpeed);
            this.last = newFloat;
            return newFloat;
        }
    }

    /// <summary> Get the angle from one position to another position. </summary>
    /// <param name="pos"> The first position. </param>
    /// <param name="center"> The second position, which you can think of the center/origin. </param>
    public static float GetAngleFromPos2D(Vector3 pos, Vector3 center = new Vector3()) => Mathf.Atan2(pos.x - center.x, pos.y - center.y) * Mathf.Rad2Deg;

    /// <summary> Get the unit position of a given angle. </summary>
    /// <param name="angle"> The angle to generate the position from. </param>
    public static Vector2 GetPosFromAngle2D(float angle) => new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle));

    /// <summary> Utilities for RectTransforms. </summary>
    public class RectTransformUtils
    {
        /// <summary> The relevant RectTransform. </summary>
        RectTransform rect;
        public RectTransformUtils(RectTransform rect) => this.rect = rect;

        /// <summary> The left side of the RectTransform. </summary>
        public float left
        {
            get => rect.offsetMin.x;
            set => rect.offsetMin = new Vector2(value, rect.offsetMin.y);
        }

        /// <summary> The right side of the RectTransform. </summary>
        public float right
        {
            get => -rect.offsetMax.x;
            set => rect.offsetMax = new Vector2(-value, rect.offsetMax.y);
        }

        /// <summary> The top side of the RectTransform. </summary>
        public float top
        {
            get => -rect.offsetMax.y;
            set => rect.offsetMax = new Vector2(rect.offsetMax.x, -value);
        }

        /// <summary> The bottom side of the RectTransform. </summary>
        public float bottom
        {
            get => rect.offsetMin.y;
            set => rect.offsetMin = new Vector2(rect.offsetMin.x, value);
        }
    }

    /// <summary> Approach a value over time. </summary>
    /// <param name="old"> The old value, approaching the target. </param>
    /// <param name="target"> The target value to approach. </param>
    /// <param name="speed"> The speed of approaching. </param>
    public static float Approach(float old, float target, float speed = 1) => Mathf.Lerp(old, target, Time.deltaTime * speed);

    /// <summary> Mirror an angle across the X axis. </summary>
    /// <param name="angle"> The angle to mirror. </param>
    public static float MirrorAngleX(float angle)
    {
        angle = (180 - angle) % 360;
        return angle < 0 ? 360 + angle : angle;
    }

    /// <summary> Load an image from a path. </summary>
    /// <param name="path"> The path of the image. </param>
    public static Texture2D LoadImage(string path)
    {
        var image = new Texture2D(2, 2);
        var imageData = File.ReadAllBytes(path);
        image.LoadImage(imageData);
        return image;
    }

    /// <summary> Open a path in the file explorer. </summary>
    /// <param name="path"> The path to open. </param>
    public static void OpenFileExplorer(string path)
    {
        var safePath = path.Replace(@"/", @"\");
        System.Diagnostics.Process.Start("explorer.exe", "/root," + safePath);
    }

    /// <summary> Copy a directory from one location to another. </summary>
    /// <param name="sourceDir"> The source directory to copy from. </param>
    /// <param name="destinationDir"> The destination directory to be copied to. </param>
    /// <param name="recursive"> Whether to recursively copy contents of copied directories. </param>
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }

    /// <summary> Get the closest point on a line from a certain point. </summary>
    /// <param name="linePoint"> The point anchoring the line. </param>
    /// <param name="lineDirection"> The direction of the line. </param>
    /// <param name="point"> The point to get the closest point from. </param>
    public static Vector3 GetClosestPointOnLine(Vector3 linePoint, Vector3 lineDirection, Vector3 point) {
        var lineToPoint = point - linePoint;
        var lineDirSqr = lineDirection.sqrMagnitude;
        var dotLTPAndDirection = Vector3.Dot(lineToPoint, lineDirection);
        var dist = dotLTPAndDirection / lineDirSqr;
        return linePoint + lineDirection * dist;
    }

    /// <summary> Check whether an anchored vector intersects with a circle. </summary>
    /// <param name="circlePoint"> The position of the circle. </param>
    /// <param name="circleRadius"> The radius of the circle. </param>
    /// <param name="linePoint"> The anchor point of the vector. </param>
    /// <param name="lineDirection"> The direction of the vector </param>
    public static bool VectorIntersectsCircle(Vector3 circlePoint, float circleRadius, Vector3 linePoint, Vector3 lineDirection) {
        bool PointIntersectsCircle(Vector3 point) {
            var pointToCircle = circlePoint - point;
            return pointToCircle.magnitude < circleRadius;
        }

        var A = linePoint;
        var B = linePoint + lineDirection;

        if (PointIntersectsCircle(A)) return true;
        if (PointIntersectsCircle(B)) return true;

        var closestPointOnLine = GetClosestPointOnLine(linePoint, lineDirection, circlePoint);
        var distToA = (closestPointOnLine - A).magnitude;
        var distToB = (closestPointOnLine - B).magnitude;
        var lineLength = (lineDirection).magnitude;
        var tooFar = distToA > lineLength || distToB > lineLength;

        return PointIntersectsCircle(closestPointOnLine) && !tooFar;
    }
}

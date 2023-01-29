using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;

/// <summary> A displayed gameplay object in the beatmap difficulty in the visuals. </summary>
public class DisplayedObject : MonoBehaviour
{
    /// <summary> The object this visual is referring to in the difficulty. </summary>
    public Beatmap.GameplayObject referenceObj;
    /// <summary> Whether this visual is interactable or not. </summary>
    public bool gameplay = true;
    /// <summary> The last beat this visual was updated with. </summary>
    float lastBeat = -1;
    /// <summary> Whether this visual is a note. </summary>
    bool isNote;
    /// <summary> Whether this visual has been hit yet. </summary>
    bool isHit = false;
    /// <summary> All of the outline components for every gameobject creating this visual. </summary>
    public List<Outline> outlines = new List<Outline>();

    // Note
    /// <summary> The left approaching circle. </summary>
    public RawImage timingSide1;
    /// <summary> The right approaching circle. </summary>
    public RawImage timingSide2;

    // Swap
    /// <summary> The bottom left arrow. </summary>
    public RawImage side1Bottom;
    /// <summary> The top left arrow. </summary>
    public RawImage side1Top;
    /// <summary> The bottom right arrow. </summary>
    public RawImage side2Bottom;
    /// <summary> The top right arrow. </summary>
    public RawImage side2Top;

    /// <summary> A shortcut reference to the active song info. </summary>
    Beatmap.Info info { get => Beatmap.Active.info; }

    /// <summary> Initialize this visual. </summary>
    /// <param name="referenceObj"> The object this visual is referring to in the difficulty.  </param>
    public void Initialize(Beatmap.GameplayObject referenceObj)
    {
        this.referenceObj = referenceObj;
        isNote = referenceObj is Beatmap.Note;

        // Initialize outlines.
        outlines.Add(this.GetComponent<Outline>());
        if (isNote)
        {
            outlines.Add(timingSide1.gameObject.GetComponent<Outline>());
            outlines.Add(timingSide2.gameObject.GetComponent<Outline>());
        }
        else
        {
            outlines.Add(side1Bottom.GetComponent<Outline>());
            outlines.Add(side1Top.GetComponent<Outline>());
            outlines.Add(side2Bottom.GetComponent<Outline>());
            outlines.Add(side2Top.GetComponent<Outline>());
        }
    }

    /// <summary> The different states the visual outlines can be in. </summary>
    public enum OutlineType
    {
        OFF,
        TRANSFORMING,
        COPYING
    }

    /// <summary> Change the state of the visual outlines. </summary>
    public void ChangeOutline(OutlineType outline)
    {
        if (outline == OutlineType.OFF) outlines.ForEach(x =>
        {
            x.enabled = false;
        });
        if (outline == OutlineType.TRANSFORMING) outlines.ForEach(x =>
        {
            x.enabled = true;
            x.effectColor = Color.red;
        });
        if (outline == OutlineType.COPYING) outlines.ForEach(x =>
        {
            x.enabled = true;
            x.effectColor = Color.cyan;
        });
    }

    /// <summary> Update the visuals of this object based on a beat. </summary>
    public void Animate(float beat)
    {
        if (lastBeat == beat) return;
        lastBeat = beat;

        // Get object time in seconds
        var time = Utils.BeatToSeconds(referenceObj.time, info.BPM);
        var seconds = Utils.BeatToSeconds(beat, info.BPM);

        // Get the times of start and ending
        var startTime = time - Beatmap.Active.spawnInSeconds;
        var endTime = time + Beatmap.Active.spawnOutSeconds;

        // Get the window fraction for both sides
        var spawnInFrac = Utils.GetFraction(startTime, time, seconds);
        var spawnOutFrac = Utils.GetFraction(time, endTime, seconds);

        isNote = referenceObj is Beatmap.Note;

        // Note code
        if (isNote)
        {
            var image = GetComponent<RawImage>();

            // Constants
            var timingDist = 100;
            var timingScale = 1.2f;

            image.raycastTarget = IsSelectable(beat);

            // Before hit
            if (!isHit)
            {
                if (spawnInFrac != -1)
                {
                    // Adjust opacity
                    var easingWeight = 0.8f;
                    var alpha = Utils.EaseInExpo(spawnInFrac) * easingWeight + Utils.EaseOutExpo(spawnInFrac) * (1 - easingWeight);
                    image.color = Utils.ChangeAlpha(image.color, alpha);
                    timingSide1.color = Utils.ChangeAlpha(timingSide1.color, alpha);
                    timingSide2.color = Utils.ChangeAlpha(timingSide2.color, alpha);

                    // Move timing sides
                    var easeMotion = Utils.EaseInExpo((1 - spawnInFrac)) * 0.2f;
                    var timing = 1 - spawnInFrac;
                    if ((referenceObj as Beatmap.Note).axis)
                    {
                        var dist = timing * timingDist + easeMotion * 50;
                        timingSide1.transform.localPosition = new Vector3(-dist, 0, 0);
                        timingSide2.transform.localPosition = new Vector3(dist, 0, 0);
                    }
                    else
                    {
                        var scale = 1 + timing * timingScale;
                        scale = scale + easeMotion;
                        timingSide1.transform.localPosition = new Vector3(0, 0, 0);
                        timingSide2.transform.localPosition = new Vector3(0, 0, 0);
                        timingSide1.transform.localScale = new Vector3(scale, scale, scale);
                        timingSide2.transform.localScale = new Vector3(-scale, scale, scale);
                    }
                }
                else
                {
                    // Adjust image opacity
                    var newFrac = Utils.EaseInExpo(1 - spawnOutFrac);
                    image.color = Utils.ChangeAlpha(image.color, newFrac);

                    // Adjust timing side opacity
                    var fadeOutFrac = Mathf.Max(1 - spawnOutFrac * 5, 0);
                    timingSide1.color = Utils.ChangeAlpha(timingSide1.color, fadeOutFrac);
                    timingSide2.color = Utils.ChangeAlpha(timingSide2.color, fadeOutFrac);
                }
            }

            // After hit
            else
            {
                // Fade out image
                image.color = Utils.ChangeAlpha(image.color, Mathf.Max(0, image.color.a - Time.deltaTime * 15));

                // Scale image
                var growth = Time.deltaTime * 7;
                this.gameObject.transform.localScale += new Vector3(growth, growth, growth);
            }
        }

        // Swap code
        else
        {
            var heightDist = side1Top.GetComponent<RectTransform>().sizeDelta.x;

            if (spawnInFrac != -1)
            {
                // Move arrows
                var newFrac = Utils.EaseInExpo(spawnInFrac);
                var moveFrac = Utils.EaseOutExpo((1 - spawnInFrac));
                var dist = moveFrac * heightDist * 2;
                var rot = moveFrac * -90;

                void AnimSide(RawImage image, bool leftDir = true, bool side2 = false)
                {
                    image.color = Utils.ChangeAlpha(image.color, newFrac);

                    var sideRot = leftDir ? rot : -rot;
                    if (side2) sideRot *= -1;

                    var rect = image.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector3(0, leftDir ? dist : -dist, 0);
                    rect.localRotation = Quaternion.Euler(0, 0, sideRot);
                }

                AnimSide(side1Top);
                AnimSide(side1Bottom, false);
                AnimSide(side2Top, true, true);
                AnimSide(side2Bottom, false, true);
            }
            else
            {
                // Fade out arrows
                var fadeOutFrac = Mathf.Max(1 - spawnOutFrac, 0);
                void AnimSide(RawImage image) => image.color = Utils.ChangeAlpha(image.color, fadeOutFrac);

                AnimSide(side1Top);
                AnimSide(side1Bottom);
                AnimSide(side2Top);
                AnimSide(side2Bottom);
            }
        }
    }

    /// <summary> The amount of seconds until the reference object time at a given beat. </summary>
    /// <param name="beat"> Current beat. </param>
    public float SecondsUntilTime(float beat) => Utils.BeatToSeconds(referenceObj.time - beat, info.BPM);

    /// <summary> Whether the object is in the second half of the late window at a given beat. </summary>
    /// <param name="beat"> Current beat. </param>
    public bool IsDespawning(float beat) => SecondsUntilTime(beat) < -Beatmap.Active.spawnOutSeconds / 2;

    /// <summary> Whether this object is selectable at a given beat. </summary>
    /// <param name="beat"> Current beat. </param>
    public bool IsSelectable(float beat)
    {
        if (SecondsUntilTime(beat) > Beatmap.Active.spawnInSeconds / 2) return false;
        if (IsDespawning(beat)) return false;
        return true;
    }

    /// <summary> Whether this object is hittable at a given beat. </summary>
    /// <param name="beat"> Current beat. </param>
    public bool IsHittable(float beat)
    {
        if (SecondsUntilTime(beat) > Beatmap.Active.hittableSeconds) return false;
        if (IsDespawning(beat)) return false;
        return true;
    }

    void Update()
    {
        if (gameplay && isNote)
        {
            // Checks for note misses.
            if (IsDespawning(lastBeat) && !isHit)
            {
                var hitObj = Instantiate(hit);
                hitObj.transform.SetParent(this.gameObject.transform.parent);
                hitObj.GetComponent<Hit>().Setup(this.transform.position, Random.Range(0, 360), "MISS", null);
                PlayHandler.Miss();
                isHit = true;
            }
            // Checks for note hits.
            else
            {
                CheckNoteHit(PlayHandler.primaryPos);
                CheckNoteHit(PlayHandler.secondaryPos, false);
            }
        }
    }

    /// <summary> The visual hit indicator to spawn when this object is hit. </summary>
    public GameObject hit;

    /// <summary> The direction in degrees of a 2D vector (3D but ignores Z axis) </summary>
    /// <param name="vector"> The vector to calculate the direction of. </param>
    float AngleFromVector(Vector3 vector)
    {
        vector.Normalize();
        var angle = Mathf.Asin(vector.x) * Mathf.Rad2Deg;
        if (vector.y < 0) angle = 180 - angle;
        else
        if (vector.x < 0) angle = 360 + angle;
        return 180 - angle;
    }

    /// <summary> Checks if a note has been hit based on cursor information. </summary>
    /// <param name="positions"> List of cursor positions for the last few frames. </param>
    /// <param name="primary"> Whether the cursor is primary or not. </param>
    void CheckNoteHit(List<Vector3> positions, bool primary = true)
    {
        // Ignore hit check if the note isn't hittable or there are no mouse positions.
        if (!IsHittable(lastBeat)) return;
        if (isHit) return;
        if (positions.Count == 0) return;

        // Check if mouse vector intersects with note.
        var noteCenter = this.transform.position;
        var noteRadius = Beatmap.noteSize / 2;
        var cursorPos = positions[positions.Count - 1];
        var lineDir = -GetCursorVector(positions);
        if (Utils.VectorIntersectsCircle(noteCenter, noteRadius, cursorPos, lineDir))
        {
            // Initialize effects of hit
            var image = GetComponent<RawImage>();
            image.color = Utils.ChangeAlpha(image.color, 1);
            timingSide1.gameObject.SetActive(false);
            timingSide2.gameObject.SetActive(false);
            isHit = true;

            // Check if hit is good
            var cursorVec = GetCursorVector(positions);
            var note = (referenceObj as Beatmap.Note);

            var goodHit = note.primary == primary;
            var result = "BAD";
            var axisScore = note.axis ? GetAxisScore(cursorVec, note.direction) : 0;
            if (note.axis && axisScore / Beatmap.NoteScore.axis < 0.5f) goodHit = false;

            if (goodHit)
            {
                // Calculate score
                var score = 0f;
                score += Beatmap.NoteScore.hit;
                score += GetCenterScore(positions);
                if (note.axis) score += axisScore;
                else
                {
                    var noAxis = Beatmap.NoteScore.hit + Beatmap.NoteScore.center;
                    var total = noAxis + Beatmap.NoteScore.axis;
                    score *= (total / noAxis);
                }
                score *= GetTimingScore();
                result = Mathf.Round(score).ToString();
                PlayHandler.AddScore(score);
            }
            else PlayHandler.Miss();

            // Spawn hit object
            var rot = AngleFromVector(cursorVec);
            var hitObj = Instantiate(hit);
            hitObj.transform.SetParent(this.gameObject.transform.parent);
            hitObj.GetComponent<Hit>().Setup(positions[positions.Count - 1], rot, result, goodHit);
        }
    }

    /// <summary> Get the current vector for the mouse's movement from this frame to the last. </summary>
    /// <param name="positions"> List of cursor positions for the last few frames. </param>
    Vector3 GetCursorVector(List<Vector3> positions)
    {
        var change = new Vector3();
        for (var i = 0; i < positions.Count - 1; i++)
            change += positions[i + 1] - positions[i];
        return change / (positions.Count - 1);
    }

    /// <summary> Get the vector from the mouse position to the center of this object. </summary>
    /// <param name="positions"> List of cursor positions for the last few frames. </param>
    Vector3 GetCenterVector(List<Vector3> positions) => this.transform.position - positions[positions.Count - 1];

    /// <summary> Get the center score of this object based on given cursor path. </summary>
    /// <param name="positions"> List of cursor positions for the last few frames. </param>
    float GetCenterScore(List<Vector3> positions)
    {
        var vec = GetCursorVector(positions);
        var pos = positions[0];
        var line = new Ray(pos, vec);
        var center = this.gameObject.transform.position;
        var dist = Vector3.Cross(line.direction, center - line.origin).magnitude;
        var score = 1 - Mathf.Min(1, dist / (Beatmap.noteSize / 2));
        score = Utils.SetLenience(1 - Beatmap.NoteScore.centerLenience, score);
        score *= Beatmap.NoteScore.center;

        return score;
    }

    /// <summary> Get the axis score of a note based on given cursor direction. </summary>
    /// <param name="cursorVec"> The cursor direction vector. </param>
    /// <param name="direction"> The note axis direction. </param>
    float GetAxisScore(Vector3 cursorVec, float direction)
    {
        var dir2 = (direction + 180) % 360;
        var dirVec1 = Utils.GetPosFromAngle2D((direction + 90) % 360);
        var dirVec2 = Utils.GetPosFromAngle2D((dir2 + 90) % 360);
        var diff1 = Vector2.Angle(cursorVec, dirVec1);
        var diff2 = Vector2.Angle(cursorVec, dirVec2);
        var score = 1 - (Mathf.Min(diff1, diff2) / 90);
        score = Utils.SetLenience(1 - Beatmap.NoteScore.angleLenience, score);
        score *= Beatmap.NoteScore.axis;

        return score;
    }

    // Gets the score multiplier between 0 and 1 based on timing. 
    float GetTimingScore()
    {
        // Get object time in seconds
        var time = Utils.BeatToSeconds(referenceObj.time, info.BPM);

        // Get the times of start and ending
        var startTime = time - Beatmap.Active.hittableSeconds;
        var endTime = time + Beatmap.Active.spawnOutSeconds;

        // Get the fraction for both sides
        var inFrac = Utils.GetFraction(startTime, time, PlayHandler.seconds);
        var outFrac = Utils.GetFraction(time, endTime, PlayHandler.seconds);

        // Get the leniences
        var leniences = new Beatmap.NoteLeniences();

        // Remap based on leniences and return
        if (inFrac != -1)
        {
            inFrac = Utils.SetLenience(leniences.lenienceIn, inFrac);
            return inFrac;
        }
        else
        {
            outFrac = Utils.SetLenience(leniences.lenienceOut, 1 - outFrac);
            return outFrac;
        }
    }
}
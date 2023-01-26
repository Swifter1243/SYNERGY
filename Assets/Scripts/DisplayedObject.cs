using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;

public class DisplayedObject : MonoBehaviour
{
    public Beatmap.GameplayObject referenceObj;
    public bool gameplay = true;
    float lastBeat = -1;
    bool isNote;
    bool isHit = false;
    public List<Outline> outlines = new List<Outline>();

    // Note
    public RawImage timingSide1;
    public RawImage timingSide2;

    // Swap
    public RawImage side1Bottom;
    public RawImage side1Top;
    public RawImage side2Bottom;
    public RawImage side2Top;


    Beatmap.Info info { get => Beatmap.Active.info; }

    public void Initialize(Beatmap.GameplayObject referenceObj)
    {
        this.referenceObj = referenceObj;
        isNote = referenceObj is Beatmap.Note;

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

    public enum OutlineType
    {
        OFF,
        TRANSFORMING,
        COPYING
    }

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

        // Get the fraction for both sides
        var spawnInFrac = Utils.GetFraction(startTime, time, seconds);
        var spawnOutFrac = Utils.GetFraction(time, endTime, seconds);

        isNote = referenceObj is Beatmap.Note;

        if (isNote)
        {
            var image = GetComponent<RawImage>();
            var timingDist = 100;
            var timingScale = 1.2f;

            image.raycastTarget = IsSelectable(beat);

            if (!isHit)
            {
                if (spawnInFrac != -1)
                {
                    image.color = Utils.ChangeAlpha(image.color, spawnInFrac);

                    timingSide1.color = Utils.ChangeAlpha(timingSide1.color, spawnInFrac);
                    timingSide2.color = Utils.ChangeAlpha(timingSide2.color, spawnInFrac);

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
                    var newFrac = Utils.EaseInExpo(1 - spawnOutFrac);
                    image.color = Utils.ChangeAlpha(image.color, newFrac);

                    var fadeOutFrac = Mathf.Max(1 - spawnOutFrac * 5, 0);
                    timingSide1.color = Utils.ChangeAlpha(timingSide1.color, fadeOutFrac);
                    timingSide2.color = Utils.ChangeAlpha(timingSide2.color, fadeOutFrac);
                }
            }
            else
            {
                image.color = Utils.ChangeAlpha(image.color, Mathf.Max(0, image.color.a - Time.deltaTime * 15));
                var growth = Time.deltaTime * 7;
                this.gameObject.transform.localScale += new Vector3(growth, growth, growth);
            }
        }
        else
        {
            var heightDist = side1Top.GetComponent<RectTransform>().sizeDelta.x;

            if (spawnInFrac != -1)
            {
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
                var fadeOutFrac = Mathf.Max(1 - spawnOutFrac, 0);
                void AnimSide(RawImage image) => image.color = Utils.ChangeAlpha(image.color, fadeOutFrac);

                AnimSide(side1Top);
                AnimSide(side1Bottom);
                AnimSide(side2Top);
                AnimSide(side2Bottom);
            }
        }
    }

    public float SecondsUntilTime(float beat) => Utils.BeatToSeconds(referenceObj.time - beat, info.BPM);

    public bool IsDespawning(float beat) => SecondsUntilTime(beat) < -Beatmap.Active.spawnOutSeconds / 2;

    public bool IsSelectable(float beat)
    {
        if (SecondsUntilTime(beat) > Beatmap.Active.spawnInSeconds / 2) return false;
        if (IsDespawning(beat)) return false;
        return true;
    }

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
            if (IsDespawning(lastBeat) && !isHit)
            {
                var hitObj = Instantiate(hit);
                hitObj.transform.SetParent(this.gameObject.transform.parent);
                hitObj.GetComponent<Hit>().Setup(this.transform.position, Random.Range(0, 360), "MISS", null);
                PlayHandler.Miss();
                isHit = true;
            }
            else
            {
                CheckNoteHit(PlayHandler.primaryPos);
                CheckNoteHit(PlayHandler.secondaryPos, false);
            }
        }
    }

    public GameObject hit;

    float AngleFromVector(Vector3 vector)
    {
        vector.Normalize();
        var angle = Mathf.Asin(vector.x) * Mathf.Rad2Deg;
        if (vector.y < 0) angle = 180 - angle;
        else
        if (vector.x < 0) angle = 360 + angle;
        return 180 - angle;
    }

    void CheckNoteHit(List<Vector3> positions, bool primary = true)
    {
        if (!IsHittable(lastBeat)) return;
        if (isHit) return;
        if (positions.Count == 0) return;
        var centerVec = GetCenterVector(positions);
        if (centerVec.magnitude < (Beatmap.noteSize / 2))
        {
            var image = GetComponent<RawImage>();
            image.color = Utils.ChangeAlpha(image.color, 1);
            timingSide1.gameObject.SetActive(false);
            timingSide2.gameObject.SetActive(false);
            isHit = true;

            var cursorVec = GetCursorVector(positions);
            var note = (referenceObj as Beatmap.Note);

            var goodHit = note.primary == primary;
            var result = "BAD";
            var axisScore = note.axis ? GetAxisScore(cursorVec, note.direction) : 0;
            if (note.axis && axisScore < 0.5f) goodHit = false;

            if (goodHit)
            {
                var score = 0f;
                score += Beatmap.NoteScore.hit;
                score += GetCenterScore(positions) * Beatmap.NoteScore.center;
                if (note.axis) score += axisScore * Beatmap.NoteScore.axis;
                else
                {
                    var noAxis = Beatmap.NoteScore.hit + Beatmap.NoteScore.center;
                    var total = noAxis + Beatmap.NoteScore.axis;
                    score *= (total / noAxis);
                }
                score *= GetTimingScore();
                PlayHandler.AddScore(score);
                result = Mathf.Round(score).ToString();
            }
            else PlayHandler.Miss();

            var rot = AngleFromVector(cursorVec);
            var hitObj = Instantiate(hit);
            hitObj.transform.SetParent(this.gameObject.transform.parent);
            hitObj.GetComponent<Hit>().Setup(positions[positions.Count - 1], rot, result, goodHit);
        }
    }

    Vector3 GetCursorVector(List<Vector3> positions)
    {
        var change = new Vector3();
        for (var i = 0; i < positions.Count - 1; i++)
            change += positions[i + 1] - positions[i];
        return change / (positions.Count - 1);
    }

    Vector3 GetCenterVector(List<Vector3> positions) => this.transform.position - positions[positions.Count - 1];

    float GetCenterScore(List<Vector3> positions)
    {
        var vec = GetCursorVector(positions);
        var pos = positions[0];
        var line = new Ray(pos, vec);
        var center = this.gameObject.transform.position;
        var dist = Vector3.Cross(line.direction, center - line.origin).magnitude;
        var score = 1 - Mathf.Min(1, dist / (Beatmap.noteSize / 2));

        return Utils.SetLenience(1 - Beatmap.NoteScore.centerLenience, score);
    }

    float GetAxisScore(Vector3 cursorVec, float direction)
    {
        var dir2 = (direction + 180) % 360;
        var dirVec1 = Utils.GetPosFromAngle((direction + 90) % 360);
        var dirVec2 = Utils.GetPosFromAngle((dir2 + 90) % 360);
        var diff1 = Vector2.Angle(cursorVec, dirVec1);
        var diff2 = Vector2.Angle(cursorVec, dirVec2);
        var score = 1 - (Mathf.Min(diff1, diff2) / 90);

        return Utils.SetLenience(1 - Beatmap.NoteScore.angleLenience, score);
    }

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
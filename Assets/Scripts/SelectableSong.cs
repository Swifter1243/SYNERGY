using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class SelectableSong : MonoBehaviour
{
    public RawImage coverArt;
    public int index;
    public Beatmap.Info info;
    public string path;
    public AudioClip clip;

    public static float innerSpacing = 400;
    public static float outerSpacing = 250;
    public static float innerSize = 1f;
    public static float outerSize = 0.8f;
    public static float chosenSize = 1.3f;
    public static Vector3 tilt = new Vector3(2.28f, 29.47f, 3.3f);
    public static Vector3 selectRot = new Vector3(-0.335f, -30.121f, -1.264f);
    public static float selectDist = 200;

    public void Animate(float position)
    {
        var deltaIndex = index - position;
        var inner = Mathf.Abs(deltaIndex) <= 1;

        var absIndex = Mathf.Abs(deltaIndex);

        float xPos = 0;
        if (inner)
        {
            xPos = deltaIndex * innerSpacing;
            var size = Mathf.Lerp(innerSize, outerSize, Mathf.Abs(deltaIndex));
            gameObject.transform.localScale = new Vector3(size, size);
            var rot = tilt * absIndex;
            gameObject.transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        }
        else
        {
            gameObject.transform.localScale = new Vector3(outerSize, outerSize);
            var deltaSign = deltaIndex > 0 ? 1 : -1;
            xPos += deltaSign * innerSpacing;
            xPos += (absIndex - 1) * outerSpacing * deltaSign;
            var rot = tilt;
            gameObject.transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        }

        gameObject.transform.localPosition = new Vector3(xPos, 0);
    }

    public void AnimateChoose(float anim)
    {
        var size = Mathf.Lerp(innerSize, chosenSize, anim);
        gameObject.transform.localScale = new Vector3(size, size);
        var rot = selectRot * anim;
        gameObject.transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        var xPos = Mathf.Lerp(0, -selectDist, anim);
        gameObject.transform.localPosition = new Vector3(xPos, 0);
    }

    public void Select()
    {
        SongSelection.targetIndex = index;
        SongSelection.chosenIndex = index;
    }

    void Start() {
        if (SongSelection.chosenAnim > 0 && SongSelection.targetIndex == index) clip = Beatmap.Active.audio;
    }

    void Update()
    {
        if (SongSelection.targetIndex == index && clip == null)
        {
            var audioPath = Utils.GetAudioPath(path, info);
            if (File.Exists(audioPath)) StartCoroutine(LoadAudio(audioPath));
        }
    }

    public IEnumerator LoadAudio(string path)
    {
        var www = UnityWebRequestMultimedia.GetAudioClip($"file:///{Uri.EscapeDataString($"{path}")}", AudioType.OGGVORBIS);
        yield return www.SendWebRequest();
        var clip = DownloadHandlerAudioClip.GetContent(www);
        this.clip = clip;
    }
}

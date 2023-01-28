using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary> The hit visual for notes. </summary>
public class Hit : MonoBehaviour
{
    /// <summary> The text object on the hit. </summary>
    public Text text;
    /// <summary> The amount of seconds this object has existed for.  </summary>
    float seconds = 0;
    /// <summary> The rect transform of this object. </summary>
    RectTransform rect;

    /// <summary> Setup the hit visual. </summary>
    /// <param name="position"> The position of the hit. </param>
    /// <param name="rotation"> The rotation of the hit. </param>
    /// <param name="message"> The message to display on the text. </param>
    /// <param name="goodHit"> Whether the hit was good or not. Leave null to assume a miss. </param>
    public void Setup(Vector3 position, float rotation, string message, bool? goodHit)
    {
        rotation = (rotation + 90) % 360;
        this.transform.position = position;
        this.transform.rotation = Quaternion.Euler(0, 0, rotation);
        text.transform.rotation = Quaternion.Euler(0, 0, 0);

        text.text = message;
        if (goodHit == null)
        {
            var image = this.GetComponent<RawImage>();
            image.color = Utils.ChangeAlpha(image.color, 0);
            text.color = new Color(1, 0.8f, 0.8f);
        }
        else if (!(bool)goodHit) text.color = new Color(1, 0.5f, 0.5f);
        if (Settings.hideUI) text.gameObject.SetActive(false);

        var width = 200;
        rect = this.GetComponent<RectTransform>();
        var scale = width / rect.sizeDelta.x;
        this.transform.localScale = new Vector2(scale, scale);
    }

    /// <summary> The starting distance of the text. </summary>
    float distStart = 30;
    /// <summary> The ending distance of the text. </summary>
    float distEnd = 60;
    /// <summary> The amount of time the text animation lasts for. </summary>
    float endTime = 1;
    /// <summary> The time that the text starts fading away. </summary>
    float startFade = 0.8f;
    /// <summary> The starting size of the slice. </summary>
    Vector2 sliceStartSize = new Vector2(130, 200);
    /// <summary> The ending size of the slice. </summary>
    Vector2 sliceEndSize = new Vector2(230, 0);
    /// <summary> The end of the slice animation. </summary>
    float endHit = 0.2f;

    void Update()
    {
        // Score
        seconds += Time.deltaTime;
        var fraction = Utils.EaseOutExpo(seconds / endTime);

        text.transform.localPosition =
        new Vector3(0, Mathf.Lerp(distStart, distEnd, fraction));

        var alphaFrac = Utils.GetFraction(startFade, endTime, seconds);
        if (alphaFrac != -1) text.color = Utils.ChangeAlpha(text.color, 1 - alphaFrac);

        // Slice
        var sliceFrac = Utils.EaseOutExpo(seconds / endHit);
        if (sliceFrac <= 1)
        {
            rect.sizeDelta = new Vector2(
                Mathf.Lerp(sliceStartSize.x, sliceEndSize.x, sliceFrac),
                Mathf.Lerp(sliceStartSize.y, sliceEndSize.y, sliceFrac)
            );
        }
        else rect.sizeDelta = sliceEndSize;

        // End
        if (seconds > endTime) Destroy(this.gameObject);
    }
}

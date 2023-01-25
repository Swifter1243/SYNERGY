using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hit : MonoBehaviour
{
    public Text text;
    float time = 0;
    RectTransform rect;

    public void Setup(Vector3 position, float rotation, string score, bool? goodHit)
    {
        rotation = (rotation + 90) % 360;
        this.transform.position = position;
        this.transform.rotation = Quaternion.Euler(0, 0, rotation);
        text.transform.rotation = Quaternion.Euler(0, 0, 0);

        text.text = score;
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

    float distStart = 30;
    float distEnd = 60;
    float endTime = 1;
    float startFade = 0.8f;
    Vector2 hitStartSize = new Vector2(130, 200);
    Vector2 hitEndSize = new Vector2(230, 0);
    float endHit = 0.2f;

    void Update()
    {
        // Score
        time += Time.deltaTime;
        var fraction = Utils.EaseOutExpo(time / endTime);

        text.transform.localPosition =
        new Vector3(0, Mathf.Lerp(distStart, distEnd, fraction));

        var alphaFrac = Utils.GetFraction(startFade, endTime, time);
        if (alphaFrac != -1) text.color = Utils.ChangeAlpha(text.color, 1 - alphaFrac);

        // Hit
        var hitFrac = Utils.EaseOutExpo(time / endHit);
        if (hitFrac <= 1)
        {
            rect.sizeDelta = new Vector2(
                Mathf.Lerp(hitStartSize.x, hitEndSize.x, hitFrac),
                Mathf.Lerp(hitStartSize.y, hitEndSize.y, hitFrac)
            );
        }
        else rect.sizeDelta = hitEndSize;

        if (time > endTime) Destroy(this.gameObject);
    }
}

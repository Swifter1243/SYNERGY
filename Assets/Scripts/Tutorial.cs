using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    public List<GameObject> slides;
    public List<Button> slideButtons;
    int activeSlide = 0;
    static float startApproachDist = 50;
    float approachDist = 0;

    void SelectSlide(int index)
    {
        slides[activeSlide].SetActive(false);
        slides[index].SetActive(true);

        var oldImage = slideButtons[activeSlide].GetComponent<Image>();
        var newImage = slideButtons[index].GetComponent<Image>();
        oldImage.color = Utils.ChangeAlpha(oldImage.color, 100f / 255);
        newImage.color = Utils.ChangeAlpha(newImage.color, 1);

        if (index > activeSlide) approachDist = startApproachDist;
        if (index < activeSlide) approachDist = -startApproachDist;
        activeSlide = index;
    }

    public void SlideLeft() { if (activeSlide > 0) SelectSlide(activeSlide - 1); }
    public void SlideRight() { if (activeSlide + 1 < slides.Count) SelectSlide(activeSlide + 1); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) SlideLeft();
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) SlideRight();
        if (Input.GetKeyDown(KeyCode.Escape)) Exit();

        var slidePos = slides[activeSlide].transform.localPosition;
        slidePos.x = approachDist;
        slides[activeSlide].transform.localPosition = slidePos;

        approachDist = Utils.Approach(approachDist, 0, 5);
    }

    void Start()
    {
        var i = 0;
        slideButtons.ForEach(x =>
        {
            var index = i;
            x.onClick.AddListener(delegate () { SelectSlide(index); });
            i++;
        });
    }

    public void Exit() => Transition.Load("MainMenu");
}

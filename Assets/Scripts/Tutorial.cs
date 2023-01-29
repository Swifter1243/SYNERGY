using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Handler for the tutorial scene. </summary>
public class Tutorial : MonoBehaviour
{
    /// <summary> The slide panels. </summary>
    public List<GameObject> slides;
    /// <summary> The buttons to go to each slide. </summary>
    public List<Button> slideButtons;
    /// <summary> The index of the current slide. </summary>
    int activeSlide = 0;
    /// <summary> The distance to move a slide when it's being animated. </summary>
    static float startApproachDist = 50;
    /// <summary> The value used to animate the position of the current slide. </summary>
    float approachDist = 0;

    /// <summary> Select a slide in the tutorial. </summary>
    /// <param name="index"> The index of the slide to select. </param>
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

    /// <summary> Move one slide to the left. </summary>
    public void SlideLeft() { if (activeSlide > 0) SelectSlide(activeSlide - 1); }
    /// <summary> Move one slide to the right. </summary>
    public void SlideRight() { if (activeSlide + 1 < slides.Count) SelectSlide(activeSlide + 1); }

    void Update()
    {
        // Keybinds
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) SlideLeft();
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) SlideRight();
        if (Input.GetKeyDown(KeyCode.Escape)) Exit();

        // Update slide position
        var slidePos = slides[activeSlide].transform.localPosition;
        slidePos.x = approachDist;
        slides[activeSlide].transform.localPosition = slidePos;
        approachDist = Utils.Approach(approachDist, 0, 5);
    }

    void Start()
    {
        // Add slide buttons
        var i = 0;
        slideButtons.ForEach(x =>
        {
            var index = i;
            x.onClick.AddListener(delegate () { SelectSlide(index); });
            i++;
        });
    }

    /// <summary> Exit to main menu. </summary>
    public void Exit() => Transition.Load("MainMenu");
}

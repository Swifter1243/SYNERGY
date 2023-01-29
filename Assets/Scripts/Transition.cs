using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> System to handle transitioning scenes. </summary>
public class Transition : MonoBehaviour
{
    /// <summary> The material that displays the wipe effect. </summary>
    public Material material;
    /// <summary> The current time of the transition. </summary>
    public static float transitionTime = 0.5f;
    /// <summary> The scene to transition to. </summary>
    public static string transitionScene;
    /// <summary> The amount of frames to skip updating the animation for. </summary>
    public static float dontUpdate = 0;

    void Update()
    {
        if (transitionTime < 1)
        {
            if (dontUpdate > 0) dontUpdate--;
            if (transitionTime >= 0.5f && transitionScene != null)
            {
                material.SetFloat("_TransitionTime", 0.5f);
                SceneManager.LoadScene(transitionScene, LoadSceneMode.Single);
                transitionScene = null;
                dontUpdate = 2;
            }
            if (dontUpdate == 0)
            {
                material.SetFloat("_TransitionTime", transitionTime);
                transitionTime += Time.deltaTime * 3;
            }
        }
    }

    /// <summary> Load a given scene. </summary>
    public static void Load(string scene)
    {
        if (transitionTime > 1)
        {
            transitionScene = scene;
            transitionTime = 0;
        }
    }
}
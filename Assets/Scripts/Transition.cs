using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Transition : MonoBehaviour
{
    public Material material;
    public static float transitionTime = 0.5f;
    public static string transitionScene;
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

    public static void Load(string scene)
    {
        if (transitionTime > 1)
        {
            transitionScene = scene;
            transitionTime = 0;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileUtils : MonoBehaviour
{

    private int FramesPerSec;
    private float frequency = 1.0f;
    private string fps;

    private static MobileUtils instance;
    public static MobileUtils Instance { get; }

    void Start()
    {
        if(instance == null)
		{
            instance = this;
		}
        else if(instance != this)
		{
            Destroy(gameObject);
		}
        //StartCoroutine(FPS());
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
    }

    private IEnumerator FPS()
    {
        for (; ; )
        {
            // Capture frame-per-second
            int lastFrameCount = Time.frameCount;
            float lastTime = Time.realtimeSinceStartup;
            yield return new WaitForSeconds(frequency);
            float timeSpan = Time.realtimeSinceStartup - lastTime;
            int frameCount = Time.frameCount - lastFrameCount;

            // Display it

            fps = string.Format("FPS: {0}", Mathf.RoundToInt(frameCount / timeSpan));
        }
    }


    /*void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 72;
        GUI.Label(new Rect(10, 150, 200, 80), fps, style);
    }*/
}

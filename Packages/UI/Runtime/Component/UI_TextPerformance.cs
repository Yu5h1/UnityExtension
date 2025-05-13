using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib;
using Yu5h1Lib.UI;

public class UI_TextPerformance : MonoBehaviour
{
    public TextAdapter textAdapter;


    private Timer timer = new Timer();
    private Coroutine coroutine;
    //public float a;
    private void Start() {

    }
    private void OnEnable()
    {
    }
    private void Update()
    {
        //textAdapter.canvasRenderer.SetAlpha(a);
    }
    public void ShowInnerThoughts(string content,Color color, float fadeInTime,float lifeTime,float fadeOutTime = -1)
	{
        gameObject.SetActive(true);
        if (content.IsEmpty())
            return;
        textAdapter.text = content;
        textAdapter.color = color;
        this.StartCoroutine(ref coroutine, FadeProcess(content, fadeInTime, lifeTime, fadeOutTime));
    }
    public void ShowNarration()
    {

    }
    
    IEnumerator FadeProcess (string content, float fadeInTime, float lifeTime, float fadeOutTime = -1)
    {
        timer.useUnscaledTime = true;
        timer.duration = fadeInTime;        
        timer.Start();
        while (!timer.IsCompleted)
        {
            timer.Tick();
            textAdapter.color = textAdapter.color.ChangeAlpha(Mathf.Lerp(0, 1, timer.normalized));
            yield return null;
        }
        yield return new WaitForSeconds(lifeTime);
        //textAdapter.CrossFadeAlpha(1, fadeTime, false);
        //yield return new WaitForSeconds(fadeTime+lifeTime);
        //textAdapter.CrossFadeAlpha(1, 0, true);
        //textAdapter.CrossFadeAlpha(0, fadeTime, false);
        timer.duration = fadeOutTime < 0 ? fadeInTime : fadeOutTime;
        timer.Start();
        while (!timer.IsCompleted)
        {
            timer.Tick();
            textAdapter.color = textAdapter.color.ChangeAlpha(Mathf.Lerp(1, 0, timer.normalized));
            yield return null;
        }

    }
    public void Stop()
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
        textAdapter.text = "";
    }
}

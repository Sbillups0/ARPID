using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathFade : MonoBehaviour
{
    public static DeathFade Instance { get; private set; }

    [Header("Fade UI")]
    public Image fadeImage;
    public float fadeDuration = 0.6f;
    public float holdBlackTime = 0.15f;

    bool busy;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
        }
    }

    public void DieAndReload()
    {
        if (busy) return;
        StartCoroutine(FadeAndReloadRoutine());
    }

    public void FadeAndLoadScene(string sceneName)
    {
        if (busy) return;
        StartCoroutine(FadeAndLoadRoutine(sceneName));
    }

    public void FadeAndReloadCurrentScene()
    {
        if (busy) return;
        StartCoroutine(FadeAndReloadRoutine());
    }

    IEnumerator FadeAndLoadRoutine(string sceneName)
    {
        busy = true;

        yield return FadeTo(1f, fadeDuration);
        yield return new WaitForSecondsRealtime(holdBlackTime);

        SceneManager.LoadScene(sceneName);
        busy = false;
    }

    IEnumerator FadeAndReloadRoutine()
    {
        busy = true;

        yield return FadeTo(1f, fadeDuration);
        yield return new WaitForSecondsRealtime(holdBlackTime);

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
        busy = false;
    }

    public IEnumerator FadeFromBlack(float duration)
    {
        yield return FadeTo(0f, duration);
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        float startAlpha = fadeImage.color.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t / duration);

            var c = fadeImage.color;
            c.a = a;
            fadeImage.color = c;

            yield return null;
        }

        var final = fadeImage.color;
        final.a = targetAlpha;
        fadeImage.color = final;
    }
}
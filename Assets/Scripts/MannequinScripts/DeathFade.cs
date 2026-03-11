using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathFade : MonoBehaviour
{
    public static DeathFade Instance { get; private set; }

    [Header("Fade UI")]
    public Image fadeImage;              // full-screen black image (alpha 0 -> 1)
    public float fadeDuration = 0.6f;
    public float holdBlackTime = 0.15f;

    bool dying;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Optional: keep across reloads
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
        if (dying) return;
        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        dying = true;

        // Fade to black
        yield return FadeTo(1f, fadeDuration);

        yield return new WaitForSeconds(holdBlackTime);

        // Reload current scene
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
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
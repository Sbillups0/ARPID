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

    [Header("Canvas binding")]
    public Canvas canvas;                 // assign DeathScreen canvas here (or auto)
    public string cameraTag = "MainCamera";

    bool dying;
    Coroutine currentRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);

        if (canvas == null) canvas = GetComponent<Canvas>();

        ResetFadeState();
        BindCanvasToCurrentCamera();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // IMPORTANT: after reload, the camera is new. Rebind canvas to it.
        BindCanvasToCurrentCamera();
        ResetFadeState();
    }

    void BindCanvasToCurrentCamera()
    {
        if (canvas == null) return;

        // Only needed for Screen Space - Camera
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Camera cam = Camera.main;

            // Fallback by tag if needed
            if (cam == null)
            {
                var go = GameObject.FindGameObjectWithTag(cameraTag);
                if (go != null) cam = go.GetComponent<Camera>();
            }

            canvas.worldCamera = cam;
        }
    }

    void ResetFadeState()
    {
        dying = false;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

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
        currentRoutine = StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        dying = true;

        yield return FadeTo(1f, fadeDuration);
        yield return new WaitForSecondsRealtime(holdBlackTime);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
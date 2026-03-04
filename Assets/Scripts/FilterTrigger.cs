using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FilterTrigger : MonoBehaviour
{
    public Volume filterVolume;
    public float duration = 15f;

    Coroutine routine;

    private void Awake()
    {
        if (filterVolume != null)
            filterVolume.weight = 0f; // start OFF
    }

    private void OnTriggerEnter(Collider other)
    {
        // // In XR, the collider entering may be a child; root tag check is safest
        // if (!other.transform.root.CompareTag("Player")) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Apply());
    }

    IEnumerator Apply()
    {
        // Make the change SUPER obvious for testing
        filterVolume.weight = 1f;

        // Optional: temporarily crank exposure so you KNOW it triggered
        if (filterVolume.profile.TryGet(out ColorAdjustments ca))
        {
            ca.postExposure.value = 1.5f; // bright
        }

        yield return new WaitForSeconds(duration);

        // Revert exposure back to your normal vintage value
        if (filterVolume.profile.TryGet(out ColorAdjustments ca2))
        {
            ca2.postExposure.value = -0.2f;
        }

        filterVolume.weight = 0f;
        routine = null;
    }
}
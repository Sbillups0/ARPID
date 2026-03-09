using UnityEngine;

public class ExitDisablesMannequins : MonoBehaviour
{
    public FreezeOnLook[] mannequins;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.transform.root.CompareTag("Player")) return;

        foreach (var m in mannequins)
        {
            if (m == null) continue;
            m.armed = false;   // forces Freeze() in Update
        }
    }
}
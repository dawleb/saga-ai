using UnityEngine;

public class SelectionMarkerPulse : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.04f;

    private Vector3 baseScale;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float pulse =
            1f +
            Mathf.Sin(Time.time * pulseSpeed) *
            pulseAmount;

        transform.localScale =
            baseScale * pulse;
    }
}
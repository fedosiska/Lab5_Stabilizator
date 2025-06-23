using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class SinDisturbance : MonoBehaviour
{
    [Header("Амплитуда (м)")] public Vector3 amplitude = new(0.3f, 0f, 0.3f);
    [Header("Частота  (Гц)")] public Vector3 frequency = new(0.5f, 0.7f, 0.4f);
    public bool useLocalSpace = true;

    Vector3 _origin;
    float   _t0;
    bool    _playing;

    public void Play()
    {
        _origin  = useLocalSpace ? transform.localPosition : transform.position;
        _t0      = Time.time;
        _playing = true;
    }

    public void Stop()
    {
        if (!_playing) return;
        _playing = false;
        if (useLocalSpace) transform.localPosition = _origin;
        else               transform.position      = _origin;
    }

    void Update()
    {
        if (!_playing) return;
        float t = Time.time - _t0;

        Vector3 off = new(
            amplitude.x * Mathf.Sin(2f * Mathf.PI * frequency.x * t),
            amplitude.y * Mathf.Sin(2f * Mathf.PI * frequency.y * t),
            amplitude.z * Mathf.Sin(2f * Mathf.PI * frequency.z * t));

        if (useLocalSpace) transform.localPosition = _origin + off;
        else               transform.position      = _origin + off;
    }
}

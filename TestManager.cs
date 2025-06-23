using UnityEngine;

public class TestManager : MonoBehaviour
{
    public SinDisturbance shaker;
    public ArmStabilizer  stabilizer;

    [Header("Hot-keys")] public KeyCode startKey = KeyCode.B;
    [Header("Hot-keys")] public KeyCode stopKey  = KeyCode.E;

    bool _running;

    public void StartTest()
    {
        if (_running) return;
        stabilizer.AnchorNow();
        shaker.Play();
        _running = true;
    }

    public void StopTest()
    {
        if (!_running) return;
        stabilizer.Release();
        shaker.Stop();  
        _running = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(startKey)) StartTest();
        if (Input.GetKeyDown(stopKey )) StopTest ();
    }
}

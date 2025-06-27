using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class ArmController : MonoBehaviour
{
    private const string DLL = "libarm";

    [DllImport(DLL)] private static extern IntPtr Arm_Create(double bx, double by, double bz);
    [DllImport(DLL)] private static extern void Arm_SetAngles(IntPtr h, double[] ang, int n);
    [DllImport(DLL)] private static extern void Arm_GetJointPos(IntPtr h, double[] pos, ref int cnt);
    [DllImport(DLL)] private static extern int Arm_GetJointCount(IntPtr h);
    [DllImport(DLL)] private static extern void Arm_Destroy(IntPtr h);
    [DllImport(DLL)] private static extern int Arm_SolveIK(IntPtr h, double tx, double ty, double tz, double[] ang, int n);

    [Header("Materials")]
    public Material jointMat;
    public Material linkMat;
    public Material effectorMat;

    private IntPtr _handle = IntPtr.Zero;
    private GameObject[] _nodes;
    private GameObject[] _links;
    private GameObject _eff;

    private float[] _deg;

    private const float R_NODE = 0.3f;
    private const float R_LINK = 0.1f;

    private Transform _visualRoot;

private void Start()
    {
        Debug.Log("=== ARM CONTROLLER START ===");

        _handle = Arm_Create(0, 0, 0);
        if (_handle == IntPtr.Zero)
        {
            Debug.LogError("Arm: native create failed");
            enabled = false;
            return;
        }

        Debug.Log("✓ Native arm created successfully");

        int dof = Arm_GetJointCount(_handle);
        Debug.Log($"✓ DOF count: {dof}");

        _deg = new float[dof];
        Debug.Log($"✓ Angle array created, size: {_deg.Length}");

        TestInitialPosition();
        TestAngleSetting();
        TestSimpleIK();

        BuildVisuals(dof);
        Redraw();
    }

private void TestInitialPosition()
{
    Debug.Log("\n--- TEST 1: Initial Position ---");
    
    double[] initialAngles = new double[_deg.Length];
    Arm_SetAngles(_handle, initialAngles, initialAngles.Length);
    
    double[] buf = new double[(_deg.Length + 1) * 3];
    int used = 0;
    Arm_GetJointPos(_handle, buf, ref used);
    
    Debug.Log($"Used buffer size: {used}");
    
    for (int i = 0; i <= _deg.Length; ++i)
    {
        Vector3 pos = new Vector3(
            (float)buf[i * 3],
            (float)buf[i * 3 + 1], 
            (float)buf[i * 3 + 2]);
        
        if (i == 0)
            Debug.Log($"Base position: {pos}");
        else if (i == _deg.Length)
            Debug.Log($"End effector: {pos} (expected: (0, 8.5, 0))");
        else
            Debug.Log($"Joint {i}: {pos}");
    }
}

private void TestAngleSetting()
{
    Debug.Log("\n--- TEST 2: Angle Setting ---");
    
    double[] testAngles = new double[_deg.Length];
    testAngles[0] = Mathf.PI / 2;
    
    Arm_SetAngles(_handle, testAngles, testAngles.Length);
    
    double[] buf = new double[(_deg.Length + 1) * 3];
    int used = 0;
    Arm_GetJointPos(_handle, buf, ref used);
    
    Vector3 endEffector = new Vector3(
        (float)buf[_deg.Length * 3],
        (float)buf[_deg.Length * 3 + 1],
        (float)buf[_deg.Length * 3 + 2]);
    
    Debug.Log($"After rotating first joint 90°: {endEffector} (expected around: (8.5, 0, 0))");
}

private void TestSimpleIK()
{
    Debug.Log("\n--- TEST 3: Simple IK Test ---");
    
    Vector3 target = new Vector3(0, 6, 0);
    Debug.Log($"Trying to reach target: {target}");
    
    double[] resultAngles = new double[_deg.Length];
    int result = Arm_SolveIK(_handle, target.x, target.y, target.z, resultAngles, resultAngles.Length);
    
    Debug.Log($"IK result: {(result == 1 ? "SUCCESS" : "FAILED")}");
    
    if (result == 1)
    {
        Debug.Log("Resulting angles (degrees):");
        for (int i = 0; i < resultAngles.Length; ++i)
        {
            Debug.Log($"  Joint {i}: {resultAngles[i] * Mathf.Rad2Deg:F1}°");
        }
        
        Arm_SetAngles(_handle, resultAngles, resultAngles.Length);
        double[] buf = new double[(_deg.Length + 1) * 3];
        int used = 0;
        Arm_GetJointPos(_handle, buf, ref used);
        
        Vector3 actualPos = new Vector3(
            (float)buf[_deg.Length * 3],
            (float)buf[_deg.Length * 3 + 1],
            (float)buf[_deg.Length * 3 + 2]);
        
        Debug.Log($"Actual end effector position: {actualPos}");
        Debug.Log($"Distance from target: {Vector3.Distance(actualPos, target):F3}");
    }
    else
    {
        Debug.Log("IK failed! Let's check why...");
        

        float maxReach = 8.5f;
        float targetDistance = target.magnitude;
        
        Debug.Log($"Target distance from base: {targetDistance:F2}");
        Debug.Log($"Maximum arm reach: {maxReach:F2}");
        Debug.Log($"Target reachable: {(targetDistance <= maxReach ? "YES" : "NO")}");
    }
}

    private void Update()
    {
    }

    private void OnDestroy(){
        if (_handle != IntPtr.Zero)
            Arm_Destroy(_handle);
    }

    private void BuildVisuals(int n){
    const float R_NODE = 0.25f;
    const float R_LINK = 0.10f;

    _visualRoot = new GameObject("ArmVisualRoot").transform;
    _visualRoot.SetParent(transform, false); 
    _visualRoot.localPosition = Vector3.zero;
    _visualRoot.localRotation = Quaternion.identity;

    _nodes = new GameObject[n + 1];
    for (int i = 0; i <= n; ++i)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = i == 0 ? "BaseJoint" : $"Joint_{i}";
        g.transform.SetParent(_visualRoot, false);
        g.transform.localScale = Vector3.one * R_NODE;
        g.GetComponent<Renderer>().material =
            jointMat ? jointMat : new Material(Shader.Find("Standard"));
        _nodes[i] = g;
    }

    _links = new GameObject[n];
    for (int i = 0; i < n; ++i)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        c.name = $"Link_{i + 1}";
        c.transform.SetParent(_visualRoot, false);
        c.transform.localScale = new Vector3(R_LINK, 1f, R_LINK);
        c.GetComponent<Renderer>().material =
            linkMat ? linkMat : new Material(Shader.Find("Standard"));
        _links[i] = c;
    }

    _eff = GameObject.CreatePrimitive(PrimitiveType.Cube);
    _eff.name = "EndEffector";
    _eff.transform.SetParent(_visualRoot, false);
    _eff.transform.localScale = Vector3.one * (R_NODE * 1.4f);
    _eff.GetComponent<Renderer>().material =
        effectorMat ? effectorMat : new Material(Shader.Find("Standard"));
}
    private void Redraw(){
    double[] rad = new double[_deg.Length];
    for (int i = 0; i < _deg.Length; ++i) rad[i] = _deg[i] * Mathf.Deg2Rad;
    Arm_SetAngles(_handle, rad, rad.Length);

    double[] buf = new double[15];
    int used = 0;
    Arm_GetJointPos(_handle, buf, ref used);

    for (int i = 0; i < _nodes.Length; ++i)
    {
        _nodes[i].transform.localPosition = new Vector3(
            (float)buf[i * 3 + 0],
            (float)buf[i * 3 + 1],
            (float)buf[i * 3 + 2]);
    }
    _eff.transform.localPosition = _nodes[^1].transform.localPosition;

    for (int i = 0; i < _links.Length; ++i)
    {
        Vector3 a = _nodes[i].transform.localPosition;
        Vector3 b = _nodes[i + 1].transform.localPosition;

        Vector3 mid = (a + b) * 0.5f;
        Vector3 dir = (b - a).normalized;
        float   len = Vector3.Distance(a, b);

        var t = _links[i].transform;
        t.localPosition = mid;
        t.localScale    = new Vector3(0.10f, len * 0.5f, 0.10f);
        t.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
    }
}

    public bool SolveIK(Vector3 worldTarget){
        double[] outRad = new double[_deg.Length];
        int ok = Arm_SolveIK(_handle,
                             worldTarget.x, worldTarget.y, worldTarget.z,
                             outRad, outRad.Length);

        if (ok == 1)
        {
            for (int i = 0; i < _deg.Length; ++i)
                _deg[i] = (float)(outRad[i] * Mathf.Rad2Deg);

            _eff.GetComponent<Renderer>().material.color = Color.green;
            Redraw();
            return true;
        }
        _eff.GetComponent<Renderer>().material.color = Color.red;
        return false;
    }

    public void SetAngleDeg(int idx, float val){
        if (idx < 0 || idx >= _deg.Length) return;
        _deg[idx] = val;
        _eff.GetComponent<Renderer>().material.color = Color.green;
        Redraw();
    }

    public float[] GetAnglesDeg() => _deg;
    public Vector3 EffectorWorldPos(){
        return _nodes[^1].transform.position;
    }
}

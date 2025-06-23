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

private void Start(){
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


    private void Update()
    {
    }

    private void OnDestroy()
    {
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
    for (int i = 0; i < _nodes.Length; ++i)
    {
        var g  = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = i == 0 ? "Base" : $"Joint_{i}";
        g.transform.localScale = Vector3.one * R_NODE;
        g.transform.SetParent(_visualRoot, false);
        var m  = jointMat ? jointMat : new Material(Shader.Find("Standard"));
        g.GetComponent<Renderer>().material = m;
        _nodes[i] = g;
    }

    _links = new GameObject[n];
    for (int i = 0; i < n; ++i)
    {
        var c  = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        c.name = $"Link_{i + 1}";
        c.transform.localScale = new Vector3(R_LINK, 1f, R_LINK);
        c.transform.SetParent(_visualRoot, false);            // ★
        var m  = linkMat ? linkMat : new Material(Shader.Find("Standard"));
        c.GetComponent<Renderer>().material = m;
        _links[i] = c;
    }
    _eff  = GameObject.CreatePrimitive(PrimitiveType.Cube);
    _eff.name = "EndEffector";
    _eff.transform.localScale = Vector3.one * (R_NODE * 1.4f);
    _eff.transform.SetParent(_visualRoot, false);
    var em = effectorMat ? effectorMat : new Material(Shader.Find("Standard"));
    _eff.GetComponent<Renderer>().material = em;
}

    private void Redraw(){
    double[] rad = new double[_deg.Length];
    for (int i = 0; i < _deg.Length; ++i) rad[i] = _deg[i] * Mathf.Deg2Rad;
    Arm_SetAngles(_handle, rad, rad.Length);

    double[] buf = new double[15];
    int used = 0;
    Arm_GetJointPos(_handle, buf, ref used);

    Vector3 baseWorld = transform.position;

    for (int i = 0; i < _nodes.Length; ++i)
    {
        Vector3 world = new(
            (float)buf[i * 3],
            (float)buf[i * 3 + 1],
            (float)buf[i * 3 + 2]);

        _nodes[i].transform.localPosition = world - baseWorld;
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

    public bool SolveIK(Vector3 worldTarget)
    {
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

    public void SetAngleDeg(int idx, float val)
    {
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

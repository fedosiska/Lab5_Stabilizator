using UnityEngine;

[RequireComponent(typeof(ArmController))]
public class ArmStabilizer : MonoBehaviour
{
    [Header("Сглаживание углов")]
    public bool smooth = true;
    [Range(0f,10f)] public float lerpSpeed = 5f;

    ArmController arm;
    Vector3 anchorPos;
    float[] prevAngles;
    bool isAnchored;

    void Awake() => arm = GetComponent<ArmController>();

    public void AnchorNow()
    {
        anchorPos   = arm.EffectorWorldPos();
        prevAngles  = (float[])arm.GetAnglesDeg().Clone();
        isAnchored  = true;
    }

    public void Release() => isAnchored = false;

    void LateUpdate()
    {
        if (!isAnchored) return;

        var before = (float[])arm.GetAnglesDeg().Clone();

        if (!arm.SolveIK(anchorPos)) return;

        if (!smooth) return;

        var target = arm.GetAnglesDeg();
        float dt   = Time.deltaTime;

        for (int i = 0; i < target.Length; ++i)
        {
            prevAngles[i] = Mathf.Lerp(
                before[i],
                target[i],
                1f - Mathf.Exp(-lerpSpeed * dt));

            arm.SetAngleDeg(i, prevAngles[i]);
        }
    }
}

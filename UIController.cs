using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour{
    [Header("Main Components")]
    public ArmController arm;

    [Header("Angle Sliders + Labels")]
    public Slider[]  angleSliders;
    public TMP_Text[] angleLabels;

    [Header("Target Position (XYZ)")]
    public Slider[] posSliders;
    public TMP_Text[] posLabels;

    [Header("Other UI")]
    public TMP_Text ikStatus;
    public Button resetBtn;

    private const float SliderUpdateDelay = 0.10f;
    private float _nextSliderTime = 0f;
    private const float IkDelay = 0.10f;
    private float _nextIkTime = 0f;

    private void Start(){
        if (!arm || angleSliders.Length != 4 || angleLabels.Length != 4 ||
            posSliders.Length   != 3 || posLabels.Length   != 3 ||
            !ikStatus || !resetBtn)
        {
            Debug.LogError("UIController: not all fields assigned!");
            enabled = false;
            return;
        }
        for (int i = 0; i < angleSliders.Length; ++i){
            int idx = i;
            angleSliders[i].onValueChanged.AddListener(v => OnAngleChanged(idx, v));
            switch (i)
            {
                case 1: angleSliders[i].minValue = -90f; angleSliders[i].maxValue =  90f; break;
                case 2: angleSliders[i].minValue =   0f; angleSliders[i].maxValue = 150f; break;
                default:angleSliders[i].minValue = -180f;angleSliders[i].maxValue = 180f; break;
            }
        }

        posSliders[0].minValue = -5f;  posSliders[0].maxValue =  5f;
        posSliders[1].minValue =  0f;  posSliders[1].maxValue =  8f;
        posSliders[2].minValue = -5f;  posSliders[2].maxValue =  5f;

        for (int i = 0; i < posSliders.Length; ++i){
            int idx = i;
            posSliders[i].onValueChanged.AddListener(_ => OnTargetSlider());
        }

        resetBtn.onClick.AddListener(ResetAll);
        SyncFromArm();
        UpdateIkStatus(true);
    }

    private void OnAngleChanged(int idx, float deg){
    UpdateAngleLabel(idx, deg);
    if (Time.time < _nextSliderTime) return;
    _nextSliderTime = Time.time + SliderUpdateDelay;

    arm.SetAngleDeg(idx, deg);
    UpdateIkStatus(true);
    }

    private void OnTargetSlider(){
        if (Time.time < _nextIkTime) return;
        _nextIkTime = Time.time + IkDelay;

        Vector3 tgt = new Vector3(
            posSliders[0].value,
            posSliders[1].value,
            posSliders[2].value);

        bool ok = arm.SolveIK(tgt);
        UpdateIkStatus(ok);
        UpdatePosLabels(); 
        if (ok) SyncFromArm();
    }

    private void ResetAll(){
        for (int i = 0; i < angleSliders.Length; ++i){
            angleSliders[i].value = 0f;
            arm.SetAngleDeg(i, 0f);
            UpdateAngleLabel(i, 0f);
        }

        posSliders[0].value = 0f;
        posSliders[1].value = 2f;
        posSliders[2].value = 0f;
        UpdatePosLabels();

        UpdateIkStatus(true);
        arm.SolveIK(arm.EffectorWorldPos());
    }

    private void SyncFromArm(){
        float[] a = arm.GetAnglesDeg();
        for (int i = 0; i < a.Length; ++i)
        {
            angleSliders[i].SetValueWithoutNotify(a[i]);
            UpdateAngleLabel(i, a[i]);
        }

        Vector3 eff = arm.EffectorWorldPos();
        posSliders[0].SetValueWithoutNotify(eff.x);
        posSliders[1].SetValueWithoutNotify(eff.y);
        posSliders[2].SetValueWithoutNotify(eff.z);
        UpdatePosLabels();
    }

    private void UpdateAngleLabel(int idx, float deg){
        angleLabels[idx].text = $"Joint {idx}: {deg:F1}°";
    }

    private void UpdatePosLabels(){
        string[] axis = { "X", "Y", "Z" };
        for (int i = 0; i < posLabels.Length; ++i)
            posLabels[i].text = $"{axis[i]}: {posSliders[i].value:F1}";
    }

    private void UpdateIkStatus(bool ok){
        ikStatus.text  = ok ? "IK: OK" : "IK: Unreachable";
        ikStatus.color = ok ? Color.green : Color.red;
    }
}

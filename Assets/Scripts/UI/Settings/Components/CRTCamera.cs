using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class CRTCamera : MonoBehaviour
{
    [SerializeField] int crtRendererIndex = 1;      // CRTURPAsset_Renderer in PC_RPAsset's list
    [SerializeField] int normalRendererIndex = -1;  // -1 = the pipeline's default renderer

    UniversalAdditionalCameraData data;

    void Awake() => data = GetComponent<Camera>().GetUniversalAdditionalCameraData();

    void OnEnable()
    {
        Apply(GameSettings.CRTEnabled);
        GameSettings.OnCRTChanged += Apply;
    }

    void OnDisable() => GameSettings.OnCRTChanged -= Apply;

    void Apply(bool on) => data.SetRenderer(on ? crtRendererIndex : normalRendererIndex);
}
using Nrjwolf.Tools.AttachAttributes;
using TMPro;
using UnityEngine;

namespace UnholyTrinity.Camera
{
    [RequireComponent(typeof(TMP_Text))]
    public class CameraViewLabel : MonoBehaviour
    {
        [SerializeField][GetComponent] TMP_Text _text;
        private void OnEnable() => CameraManager.ViewChanged += OnViewChanged;
        private void OnDisable() => CameraManager.ViewChanged -= OnViewChanged;

        // Start, not OnEnable, so CameraManager's Awake has definitely run.
        private void Start() => OnViewChanged(CameraManager.Instance.CurrentView);

        private void OnViewChanged(CameraView view)
        {
            _text.text = view switch
            {
                CameraView.TopDown    => "TOP DOWN [TAB]",
                CameraView.CloseThird => "CLOSE THIRD PERSON [TAB]",
                CameraView.Free       => "FREE CAM [TAB]",
                CameraView.Overview   => "OVERVIEW [TAB]",
                _                     => string.Empty,
            };
        }
    }
}
using UnholyTrinity.Helpers;
using Nrjwolf.Tools.AttachAttributes;
using UnholyTrinity.Managers;
using Unity.Cinemachine;
using UnholyTrinity.Players;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnholyTrinity.Camera
{
    /// Switches Cinemachine camera priorities based on whose turn it is.
    /// Player turn      -> that player's top-down camera (or close third if toggled)
    /// Resolution phase -> the impartial center camera
    public class CameraManager : Singleton<CameraManager>
    {
        private GameManager gm;

        [SerializeField] [GetComponent]
        private CinemachineCamera resolutionCamera;

        // Lets the player toggle top-down <-> close third. Delete if you handle input elsewhere.
        [SerializeField] private InputAction toggleCloseViewAction =
            new("ToggleCloseView", InputActionType.Button, "<Keyboard>/tab");

        private const int IdlePriority = 0;

        private const int
            OverviewPriority =
                1; // center cam's resting priority, so something is always live

        private const int ActivePriority = 10;

        private CinemachineCamera _active;
        private bool _closeView;
        private TurnPhase _currentPhase;
        private PlayerCameraRig _currentPlayerRig;

        protected override void Awake()
        {
            base.Awake();
            TurnManager.NewPhaseEvent += OnPhaseChanged;
            GameManager.NewTurnEvent += OnNewTurn;
            toggleCloseViewAction.performed += OnToggleInput;
            toggleCloseViewAction.Enable();
            resolutionCamera.Priority = OverviewPriority;
        }

        private void Start()
        {
            gm = GameManager.Instance;
        }

        public void RegisterPlayerTracked(Transform p, float weight,
            float radius)
        {
            var targetGroup =
                resolutionCamera.GetComponent<CinemachineTargetGroup>();
            targetGroup.AddMember(p, weight, radius);
        }

        private void OnDisable()
        {
            TurnManager.NewPhaseEvent -= OnPhaseChanged;
            GameManager.NewTurnEvent -= OnNewTurn;
            toggleCloseViewAction.performed -= OnToggleInput;
            toggleCloseViewAction.Disable();
        }

        private void OnPhaseChanged(TurnPhase phase)
        {
            _currentPhase = phase;
            if (phase != TurnPhase.Resolution)
            {
                return;
            }

            Activate(resolutionCamera);
        }

        private void OnNewTurn(Player player) => ResetAndRefresh(player);

        private void OnToggleInput(InputAction.CallbackContext _) =>
            ToggleCloseView();

        /// Hook a UI button to this too if you want one.
        private void ToggleCloseView()
        {
            if (_currentPhase == TurnPhase.Resolution)
            {
                return; // no toggle during resolution
            }

            _closeView = !_closeView;
            Refresh();
        }

        private void ResetAndRefresh(Player player)
        {
            _closeView = false; // every turn starts from the top-down view
            _currentPlayerRig = player.CameraRig;
            Refresh();
        }

        private void Refresh()
        {
            Activate(_closeView ? _currentPlayerRig.CloseThird : _currentPlayerRig.TopDown);
        }

        private void Activate(CinemachineCamera cam)
        {
            if (cam == _active)
            {
                return;
            }

            if (_active != null)
            {
                _active.Priority = _active == resolutionCamera
                    ? OverviewPriority
                    : IdlePriority;
            }

            _active = cam;
            _active.Priority = ActivePriority;
        }
    }
}
using Unity.Cinemachine;
using UnholyTrinity.Players;
using UnityEngine;
using UnityEngine.InputSystem;

/// Switches Cinemachine camera priorities based on whose turn it is.
/// Player turn      -> that player's top-down camera (or close third if toggled)
/// Resolution phase -> the impartial center camera
public class CameraManager : MonoBehaviour
{
    [SerializeField] private GameManager gm;
    [SerializeField] private CinemachineCamera resolutionCamera;

    // Lets the player toggle top-down <-> close third. Delete if you handle input elsewhere.
    [SerializeField] private InputAction toggleCloseViewAction =
        new("ToggleCloseView", InputActionType.Button, "<Keyboard>/tab");

    private const int IdlePriority = 0;
    private const int OverviewPriority = 1;   // center cam's resting priority, so something is always live
    private const int ActivePriority = 10;

    private CinemachineCamera _active;
    private bool _closeView;

    private void Awake()
    {
        resolutionCamera.Priority = OverviewPriority;
    }

    private void OnEnable()
    {
        TurnManager.NewPhaseEvent += OnPhaseChanged;
        GameManager.NewTurnEvent += OnNewTurn;
        toggleCloseViewAction.performed += OnToggleInput;
        toggleCloseViewAction.Enable();
    }

    private void OnDisable()
    {
        TurnManager.NewPhaseEvent -= OnPhaseChanged;
        GameManager.NewTurnEvent -= OnNewTurn;
        toggleCloseViewAction.performed -= OnToggleInput;
        toggleCloseViewAction.Disable();
    }

    private void OnPhaseChanged(TurnPhase _) => ResetAndRefresh();
    private void OnNewTurn(Player _) => ResetAndRefresh();
    private void OnToggleInput(InputAction.CallbackContext _) => ToggleCloseView();

    /// Hook a UI button to this too if you want one.
    public void ToggleCloseView()
    {
        if (gm.CurrentPlayer == null) return; // no toggle during resolution
        _closeView = !_closeView;
        Refresh();
    }

    private void ResetAndRefresh()
    {
        _closeView = false; // every turn starts from the top-down view
        Refresh();
    }

    private void Refresh()
    {
        Player current = gm.CurrentPlayer;

        if (current == null)
        {
            Activate(resolutionCamera);
            return;
        }

        PlayerCameraRig rig = current.CameraRig;
        if (rig == null)
        {
            Debug.LogWarning($"{current.name} has no CameraRig assigned.", current);
            return;
        }

        Activate(_closeView ? rig.CloseThird : rig.TopDown);
    }

    private void Activate(CinemachineCamera cam)
    {
        if (cam == null || cam == _active) return;

        if (_active != null)
        {
            _active.Priority = _active == resolutionCamera ? OverviewPriority : IdlePriority;
        }

        _active = cam;
        _active.Priority = ActivePriority;
    }
}
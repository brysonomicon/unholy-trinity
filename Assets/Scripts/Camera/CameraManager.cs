using System;
using System.Collections;
using System.Collections.Generic;
using UnholyTrinity.Helpers;
using Nrjwolf.Tools.AttachAttributes;
using Unity.Cinemachine;
using UnholyTrinity.Players;
using UnholyTrinity.Turns;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnholyTrinity.Camera
{
public enum CameraView
{
    TopDown,
    CloseThird,
    Free,
    Overview
}

/// Switches Cinemachine camera priorities based on whose turn it is.
public class CameraManager : Singleton<CameraManager>
{
    [SerializeField] [GetComponent] private CinemachineCamera resolutionCamera;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private InputAction toggleCloseViewAction =
        new("ToggleCloseView", InputActionType.Button, "<Keyboard>/tab");

    [Header("Free Cam")] [SerializeField] private FreeCamController freeCam;
    public static event Action<CameraView> ViewChanged;

    // Overview = the resolution camera, which is also what's live before the first turn starts.
    public CameraView CurrentView =>
        _freeCam ? CameraView.Free
        : _currentPhase == TurnPhase.Resolution || _currentPlayerRig == null ? CameraView.Overview
        : _closeView ? CameraView.CloseThird
        : CameraView.TopDown;

    private void NotifyView() => ViewChanged?.Invoke(CurrentView);

    private const int FreePriority = 20; // above ActivePriority so it overrides everything

    private CinemachineCamera _freeCamera;
    private bool _freeCam;

    private const int IdlePriority = 0;

    private const int OverviewPriority = 1; // center cam's resting priority, so something is always live

    private const int ActivePriority = 10;

    private CinemachineCamera _active;
    private bool _closeView;
    private TurnPhase _currentPhase;
    private PlayerCameraRig _currentPlayerRig;

    private Dictionary<TurnPhase, Player> playerPhaseMap = new();

    protected override void Awake()
    {
        base.Awake();
        toggleCloseViewAction.performed += OnToggleInput;
        toggleCloseViewAction.Enable();
        resolutionCamera.Priority = OverviewPriority;

        _freeCamera = freeCam.GetComponent<CinemachineCamera>();
        _freeCamera.Priority = IdlePriority;
        freeCam.enabled = false;
    }

    private void OnEnable()
    {
        TurnManager.Tick += OnTick;
        TurnManager.NewPhaseEvent += OnPhaseChanged;
        Player.PlayerSpawned += RegisterPlayerTracked;
    }

    public void RegisterPlayerTracked(Player player)
    {
        var targetGroup =
            resolutionCamera.GetComponent<CinemachineTargetGroup>();
        targetGroup.AddMember(player.transform, 1, 1);
        playerPhaseMap[player.turnPhase] = player;
    }

    private void OnDisable()
    {
        TurnManager.NewPhaseEvent -= OnPhaseChanged;
        Player.PlayerSpawned -= RegisterPlayerTracked;
        TurnManager.Tick -= OnTick;
        toggleCloseViewAction.performed -= OnToggleInput;
        toggleCloseViewAction.Disable();
    }

    private IEnumerator OnTick(SAMType type)
    {
        if (type != SAMType.Shoot)
        {
            yield break;
        }
        Activate(resolutionCamera);
        NotifyView();
        //wait 1 frame for camera to update
        yield return null;
        while (brain.ActiveBlend != null)
        {
            yield return null;
        }
    }

    private void OnPhaseChanged(TurnPhase phase)
    {
        _currentPhase = phase;
        if (phase == TurnPhase.Resolution)
        {
            Activate(resolutionCamera);
            NotifyView();
            return;
        }

        ResetAndRefresh(playerPhaseMap[phase]);
    }

    private void OnToggleInput(InputAction.CallbackContext _) => CycleView();

    private void CycleView()
    {
        if (_freeCam)
        {
            ExitFreeCam();
            return;
        }

        bool canUseCloseView = _currentPhase != TurnPhase.Resolution && _currentPlayerRig != null;

        if (canUseCloseView && !_closeView)
        {
            _closeView = true;
            Refresh();
        }
        else
        {
            SetFreeCam(true);
        }
    }

    private void ExitFreeCam()
    {
        _closeView = false; // the cycle restarts at top-down
        SetFreeCam(false);

        // Skip during resolution: the resolution camera is already live underneath
        if (_currentPhase != TurnPhase.Resolution && _currentPlayerRig != null)
        {
            Refresh();
        }
    }

    private void SetFreeCam(bool on)
    {
        _freeCam = on;

        if (on)
        {
            // Start exactly where the view currently is, so nothing jumps.
            Transform view = UnityEngine.Camera.main.transform;
            freeCam.transform.SetPositionAndRotation(view.position, view.rotation);
            freeCam.enabled = true;
            _freeCamera.Priority = FreePriority;
        }
        else
        {
            freeCam.enabled = false;
            _freeCamera.Priority = IdlePriority;
        }

        NotifyView();
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
        NotifyView();
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
using Nrjwolf.Tools.AttachAttributes;
using UnholyTrinity.Hex;
using UnholyTrinity.Players;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TilePicker : MonoBehaviour
{
    [SerializeField][GetComponent] private MapManager mapManager;
    [SerializeField] private Camera cam;
    [SerializeField] private UIManager uiManager;

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null ||
            !mouse.leftButton.wasPressedThisFrame ||
            EventSystem.current.IsPointerOverGameObject()
        ){
            return;
        }

        Player currentPlayer = GameManager.Instance.CurrentPlayer;

        if (currentPlayer == null ||
            !TryGetClickedCoord(mouse, out HexCoord clickedCoord)
        ){
            return;
        }

        Debug.Log($"{currentPlayer.name} clicked {clickedCoord}, " +
                  $"in shot cone: {currentPlayer.IsInShotCone(clickedCoord)}");
        
        currentPlayer.SelectTile(clickedCoord);
        uiManager.RefreshCards();
    }

    private bool TryGetClickedCoord(Mouse mouse, out HexCoord clickedCoord)
    {
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        Plane ground = new Plane(Vector3.up, mapManager.Layout.Origin);

        if (!ground.Raycast(ray, out float distance))
        {
            clickedCoord = default;
            return false;
        }

        clickedCoord = mapManager.Layout.WorldToHex(ray.GetPoint(distance));
        return true;
    }
}

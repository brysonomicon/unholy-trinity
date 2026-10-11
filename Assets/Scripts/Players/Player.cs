using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private MapManager mapManager;
        [SerializeField] private HexCoord coord;
        [SerializeField] private float heightOffset = 0.5f;

        private void OnEnable()
        {
            mapManager.LayoutChanged += SnapToCoord;
        }

        private void OnDisable()
        {
            mapManager.LayoutChanged -= SnapToCoord;
        }

        private void Start()
        {
            SnapToCoord();
        }

        private void OnValidate()
        {
            if (mapManager != null)
            {
                SnapToCoord();
            }
        }

        private void SnapToCoord()
        {
            transform.position = mapManager.Layout.HexToWorld(coord) + Vector3.up * heightOffset;

            if (mapManager.Map != null &&
                !mapManager.Map.IsWalkable(coord))
            {
                Debug.LogWarning($"{name} is on non walkable coord {coord}");
            }
        }
    }
}


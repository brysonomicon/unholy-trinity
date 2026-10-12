using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public class Player : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] private MapManager mapManager;
        [SerializeField] private HexCoord coord;
        [SerializeField] private float heightOffset = 0.5f;

        [Header("Combat")]
        [SerializeField] private Weapon weapon;
        [SerializeField] private AimDirection aimDirection;

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

        private void OnDrawGizmos()
        {
            if (mapManager == null ||
                mapManager.Map == null ||
                weapon == null ||
                weapon.Data == null)
            {
                return;
            }

            foreach (AimTile aimTile in weapon.Data.GetAimTiles(mapManager.Map, coord, aimDirection))
            {
                Gizmos.color = Color.Lerp(Color.yellow, Color.red, aimTile.Falloff);
                Gizmos.DrawSphere(mapManager.Layout.HexToWorld(aimTile.Coord) + Vector3.up * 0.6f, 0.2f);
            }
        }
    }
}


using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public class Player : MonoBehaviour
    {
        [Header("Map")] [SerializeField] private MapManager mapManager;
        [field: SerializeField] public HexCoord Coord { get; private set; }
        [SerializeField] private float heightOffset = 0.5f;

        [Header("Combat")]
        [field: SerializeField]
        public Weapon CurrentWeapon { get; private set; }

        [field: SerializeField]
        public AimDirection CurrentAimDirection { get; private set; }

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
            transform.position = mapManager.Layout.HexToWorld(Coord) +
                                 Vector3.up * heightOffset;

            if (mapManager.Map != null &&
                !mapManager.Map.IsWalkable(Coord))
            {
                Debug.LogWarning($"{name} is on non walkable coord {Coord}");
            }
        }

        private void OnDrawGizmos()
        {
            if (mapManager == null ||
                mapManager.Map == null ||
                CurrentWeapon == null ||
                CurrentWeapon.Data == null)
            {
                return;
            }

            foreach (AimTile aimTile in CurrentWeapon.Data.GetAimTiles(
                         mapManager.Map, Coord, CurrentAimDirection))
            {
                Gizmos.color =
                    Color.Lerp(Color.yellow, Color.red, aimTile.Falloff);
                Gizmos.DrawSphere(
                    mapManager.Layout.HexToWorld(aimTile.Coord) +
                    Vector3.up * 0.6f, 0.2f);
            }
        }

        public void MovePlayer()
        {
            Debug.Log($"{name} is moving");
        }

        public void RotatePlayer(bool clockwise)
        {
            // [CB] TODO: Actually rotate the player.
            Debug.Log($"{name} was facing {CurrentAimDirection}");
            CurrentAimDirection = clockwise
                ? CurrentAimDirection.Next()
                : CurrentAimDirection.Previous();
            Debug.Log($"{name} is now facing {CurrentAimDirection}");
        }

        public void Fire(Weapon weapon)
        {
            if (weapon.CanFire)
            {
                weapon.Fire();
            }
        }
    }
}
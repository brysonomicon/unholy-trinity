using System;
using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private GameManager gm;

        // unserialized, passed to player by spawner instead
        private MapManager mapManager;

        [Header("Map")]
        [field: SerializeField] public HexCoord Coord { get; private set; }
        [SerializeField] private float heightOffset = 0.5f;

        [Header("Combat")]
        [SerializeField] private WeaponData startingWeapon;
        [field: SerializeField] 
        public Weapon CurrentWeapon { get; private set; }

        [field: SerializeField]
        public AimDirection CurrentAimDirection { get; private set; }

        [Header("Health")]
        [SerializeField, Min(1)] private int startingHealth = 100;

        [field: SerializeField]
        public int CurrentHealth { get; private set; }

        public bool IsDead => CurrentHealth <= 0;
        

        // private void OnEnable()
        // {
        //     mapManager.LayoutChanged += SnapToCoord;
        // }

        // private void OnDisable()
        // {
        //     mapManager.LayoutChanged -= SnapToCoord;
        // }

        private void Awake()
        {
            CurrentHealth = startingHealth;
            CurrentWeapon = new Weapon(startingWeapon);
            GameManager.NewTurnEvent += OnNewTurn;
        }

        private void OnDestroy()
        {
            GameManager.NewTurnEvent -= OnNewTurn;

            if (mapManager != null)
            {
                mapManager.LayoutChanged -= SnapToCoord;
            }
        }

        // called by the spawner 
        public void Initialize(MapManager mapManager, HexCoord spawnCoord)
        {
            this.mapManager = mapManager;
            Coord = spawnCoord;
            mapManager.LayoutChanged += SnapToCoord;
            SnapToCoord();
        }

        // private void Start()
        // {
        //     SnapToCoord();
        //     gm.RegisterPlayer(this);
        // }

        private void OnNewTurn(Player obj)
        {
            // [CB] This might be entirely handled by the UI manager. IDK.
            if(obj == this)
            {
                // Do stuff to enable actions
            }
            else
            {
                // Do stuff to disable actions
            }
        }

        // private void OnValidate()
        // {
        //     if (mapManager != null)
        //     {
        //         SnapToCoord();
        //     }
        // }

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

        public void TakeDamage(int damageAmount)
        {
             if (IsDead)
             {
                return;
             }

             CurrentHealth = Mathf.Max(CurrentHealth - damageAmount, 0);
             Debug.Log($"{name} took {damageAmount} damage (ouch), {CurrentHealth} health remaining");

             if (IsDead)
             {
                Debug.Log($"{name} died. RIP");
             }
        }
    }
}
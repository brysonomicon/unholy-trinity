using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;
using UnityEngine;

namespace UnholyTrinity.Players
{
    // works out what the plan collects and how health and armour change, tick by tick, in the same
    // order GameManager.ResolveTick resolves them. it works on copies, so nothing here touches real
    // state. the opponent's plan is hidden, so only this player's own route is forecast
    public partial class Player
    {
        // raised when the forecast changes or is cleared, so UI can update
        public static event System.Action<Player> PlanForecastChangedEvent;

        // the forecast of the plan so far, or null when this player isn't planning
        public PlanForecast CurrentForecast { get; private set; }

        // where the plan ends up after its last planned tick
        public class PlanForecast
        {
            public int Health { get; }
            public int Armour { get; }

            // pickup state after the last planned tick, null when the scene has no pickups
            public PickupField Pickups { get; }

            public PlanForecast(int health, int armour, PickupField pickups)
            {
                Health = health;
                Armour = armour;
                Pickups = pickups;
            }
        }

        private void UpdateForecast()
        {
            CurrentForecast = BuildForecast();

            PickupManager pickupManager = gm.Pickups;
            if (pickupManager != null && CurrentForecast.Pickups != null)
            {
                pickupManager.ShowPlannedState(CurrentForecast.Pickups, this);
            }

            PlanForecastChangedEvent?.Invoke(this);
        }

        private void ClearForecast()
        {
            CurrentForecast = null;

            PickupManager pickupManager = gm.Pickups;
            if (pickupManager != null)
            {
                pickupManager.ShowLiveState(this);
            }

            PlanForecastChangedEvent?.Invoke(this);
        }

        private PlanForecast BuildForecast()
        {
            PickupManager pickupManager = gm.Pickups;
            PickupField pickups = pickupManager != null && pickupManager.Field != null
                ? pickupManager.Field.Clone()
                : null;

            HexMap map = mapManager.Map;
            PlannedVitals vitals = new(this);
            HexCoord coord = Coord;
            List<HexCoord> path = new();

            foreach (UnholyAction action in plannedActions)
            {
                // move phase: health tiles on each hex entered, and a lethal one ends the move there
                path.Clear();

                foreach (HexDirection step in action.PlannedMove)
                {
                    if (vitals.IsDead)
                    {
                        break;
                    }

                    // a blocked step ends the move, the same as ResolveMove
                    HexCoord next = GetStepDestination(coord, step);
                    if (next == coord)
                    {
                        break;
                    }

                    coord = next;
                    path.Add(coord);
                    ApplyHealthTile(map, coord, HealthTrigger.OnEnter, vitals);
                }

                ApplyHealthTile(map, coord, HealthTrigger.OnStop, vitals);

                // after moving, pickups respawn and then get collected along the path
                if (pickups != null)
                {
                    pickups.AdvanceTicks(1);
                    pickups.CollectAlong(path, coord, vitals);
                }
            }

            return new PlanForecast(vitals.Health, vitals.Armour, pickups);
        }

        // a copy of the player's health and armour that the forecast changes instead of the real ones.
        // it follows the same rules as the player's own TakeDamage, TryHeal and TryAddArmour
        private class PlannedVitals : IVitals, IWeaponCollector
        {
            private readonly int maxHealth;
            private readonly int maxArmour;
            private readonly int absorbPercent;
            private readonly bool roundAbsorbUp;

            public int Health { get; private set; }
            public int Armour { get; private set; }
            public bool IsDead => Health <= 0;

            public PlannedVitals(Player player)
            {
                maxHealth = player.maxHealth;
                maxArmour = player.maxArmour;
                absorbPercent = player.armourAbsorbPercent;
                roundAbsorbUp = player.roundAbsorbUp;
                Health = player.CurrentHealth;
                Armour = player.CurrentArmour;
            }

            public void TakeDamage(int damageAmount)
            {
                if (IsDead || damageAmount <= 0)
                {
                    return;
                }

                int absorbed = GetArmourAbsorbed(damageAmount, Armour, absorbPercent, roundAbsorbUp);
                Armour -= absorbed;
                Health = Mathf.Max(Health - (damageAmount - absorbed), 0);
            }

            public bool TryHeal(int amount)
            {
                if (IsDead || amount <= 0 || Health >= maxHealth)
                {
                    return false;
                }

                Health = Mathf.Min(Health + amount, maxHealth);
                return true;
            }

            public bool TryAddArmour(int amount)
            {
                if (IsDead || amount <= 0 || Armour >= maxArmour)
                {
                    return false;
                }

                Armour = Mathf.Min(Armour + amount, maxArmour);
                return true;
            }

            public void Kill() => Health = 0;

            // weapons are always picked up while alive, the same as Player.TryGiveWeapon, so the
            // pickup counts as taken. using them later in the plan isn't forecast yet
            public bool TryGiveWeapon(WeaponData data) => !IsDead;
        }
    }
}

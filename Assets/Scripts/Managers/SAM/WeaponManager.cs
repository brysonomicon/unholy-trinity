using System.Collections.Generic;
using System.Linq;
using UnholyTrinity.Helpers;
using UnholyTrinity.Hex;
using UnholyTrinity.Hex.Interfaces;
using UnholyTrinity.Managers;
using UnholyTrinity.Players;
using UnholyTrinity.Weapons;

namespace Managers
{
    public struct FireData
    {
        public IMovable Shooter;
        public WeaponData Data;
        public AimDirection FiredAim;
        public List<AimTile> FiredTiles;
    }

    public struct ExplosionData
    {
        public IMovable Shooter;
        public HexCoord Impact;
        public WeaponData Data;
    }

    public class WeaponManager : Singleton<WeaponManager>
    {
        private readonly List<FireData> _beams = new();

        private void OnEnable()
        {
            RailgunData.BeamFired += StartBeam;
            Rocket.RocketExplosion += HandleExplosion;
        }

        private void OnDisable()
        {
            RailgunData.BeamFired -= StartBeam;
            Rocket.RocketExplosion -= HandleExplosion;
        }

        // damages every player standing on a hit tile. cones never include the shooter's tile
        // so players can't hit themselves with rail but can hit themselves with rocket splash
        public void ResolveShot(
            WeaponData weaponData,
            List<AimTile> aimTiles)
        {
            foreach (AimTile at in aimTiles)
            {
                foreach (Player player in
                         GameManager.Instance.Players.Where(player =>
                             at.Coord == player.Coord))
                {
                    int damageAmount = weaponData.GetDamage(at);

                    if (damageAmount <= 0)
                    {
                        return;
                    }

                    player.TakeDamage(damageAmount);
                }
            }
        }

        private void StartBeam(
            IMovable shooter,
            WeaponData weaponData,
            IAimer aimer
        )
        {
            List<AimTile> firedTiles = weaponData.GetAimTiles(
                MapManager.Instance.Map,
                shooter.Coord,
                aimer.CurrentAimDirection);
            _beams.Add(new FireData()
            {
                Shooter = shooter,
                Data = weaponData,
                FiredAim = aimer.CurrentAimDirection,
                FiredTiles = firedTiles,
            });
        }

        // LG deals damage based on "tracking" so it needs opponent path before it's calculated.
        // shoot: the beam is created from the shooters current tile and aim
        // aim: a rotation only affects later ticks, so the beam keeps firedAim for the whole tick
        // move: both players move and the beam moves with the shooter
        // damage is then based on the average of two samples:
        // start: target's start tile in the cone as fired, just like the railgun
        // end: target's end tile in firedAim's cone from the shooter's end tile
        // this way moving in lockstep deals full damage as expected.
        // TODO with momentum: the two samples work for one hex per tick, with more
        // tiles crossed mid-tick would get skipped and players go different distances, so
        // this will need to sample both paths by a fraction of the tick
        private void ResolveBeams()
        {
            // (Player shooter, WeaponData weaponData,
            //     AimDirection firedAim,
            //     List<AimTile> firedTiles)
            foreach (FireData fireData in _beams)
            {
                List<AimTile> endTiles = GetBeamTilesFromCurrentCoord(
                    fireData.Data, fireData.Shooter.Coord, fireData.FiredAim);

                foreach (Player target in GameManager.Instance.Players
                             .Where(player =>
                                 !ReferenceEquals(player, fireData.Shooter))
                        )
                {
                    float startFalloff =
                        GetFalloff(target.MoveStartCoord, fireData.FiredTiles);
                    float endFalloff = GetFalloff(target.Coord, endTiles);
                    float averageFalloff = (startFalloff + endFalloff) / 2f;
                    int damageAmount = fireData.Data
                        .GetDamage(new AimTile(target.Coord, averageFalloff));

                    if (damageAmount > 0)
                    {
                        target.TakeDamage(damageAmount);
                    }
                }
            }

            _beams.Clear();
        }

        // tiles outside the cone count as 0, FirstOrDefault gives a default AimTile with 0 falloff
        private static float GetFalloff(HexCoord targetCoord,
            List<AimTile> aimTiles)
        {
            return aimTiles
                .FirstOrDefault(aimTile => aimTile.Coord == targetCoord)
                .Falloff;
        }

        // the fired aim's cone from where the player stands now, so a beam follows its shooter
        // only meaningful after the move phase
        private static List<AimTile> GetBeamTilesFromCurrentCoord(
            WeaponData weaponData,
            HexCoord firedFrom,
            AimDirection firedAim)
        {
            return weaponData.GetAimTiles(MapManager.Instance.Map, firedFrom,
                firedAim);
        }

        private void HandleExplosion(HexCoord impact, RocketLauncherData data)
        {
            ResolveShot(data,
                data.GetBlastTiles(MapManager.Instance.Map, impact));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnholyTrinity.Hex;
using UnholyTrinity.Hex.Interfaces;
using UnholyTrinity.Interfaces;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
public class Rocket : MonoBehaviour, IMovable
{
    public static event Action<HexCoord, RocketLauncherData> RocketExplosion;

    [SerializeField] private float heightOffset = 0.5f;
    [SerializeField] private AimPreview blastPreview;

    // includes the tile fired from and the impact tile
    private List<HexCoord> path;
    public int MoveIndex { get; private set; }

    public HexCoord Momentum { get; private set; }
    public int MomentumGainPerTick { get; }

    public HexTile Tile { get; set; }

    public List<HexDirection> MoveSteps { get; set; }

    public RocketLauncherData Data { get; private set; }
    public HexCoord Coord => path[MoveIndex];

    private IVitals _shooter;

    private void OnEnable()
    {
        (this as IMovable).Register(this);
    }

    private void OnDisable()
    {
        (this as IMovable).Unregister(this);
    }

    public void Launch(
        RocketLauncherData data,
        IVitals shooter,
        HexCoord origin,
        HexCoord target
    )
    {
        Data = data;
        path = origin.LineTo(target);
        _shooter = shooter;
        MoveIndex = 0;
        //! TODO: Some sort of visualizer abstraction
        // blastPreview.Show(data.GetBlastTiles(mapManager.Map, target),
        //     mapManager.Layout);
    }

    // advances a single rocket forward by the speed defined in the weapon data
    private IEnumerator<(int, HexCoord)> AdvanceRocket()
    {
        bool shouldDetonate = false;

        yield return (Data.Speed, Coord);

        for (int step = 0;
             step < Data.Speed && !shouldDetonate;
             step++)
        {
            Move();
            shouldDetonate =
                !(this as IMovable).CanMove || IsDirectHit();

            if (shouldDetonate)
            {
                Detonate();
            }

            yield return (
                shouldDetonate ? 0 : Data.Speed - (step + 1),
                Coord);
        }
    }

    private bool IsDirectHit()
    {
        if (!Tile.HasCollision)
        {
            return false;
        }

        int validCollisions = Tile.StandingOn.Select(mover =>
            (Rocket)mover != this
            && (mover as IVitals) != _shooter
        ).ToList().Count;
        return validCollisions != 0;
    }

    private void Detonate()
    {
        RocketExplosion?.Invoke(Coord, Data);
        Destroy(gameObject);
    }

    public void BeginMovePhase()
    {
        throw new NotImplementedException();
    }

    public void EndMovePhase()
    {
        throw new NotImplementedException();
    }

    public void Move()
    {
        if (!(this as IMovable).CanMove)
        {
            return;
        }

        MoveIndex++;
    }
}
}

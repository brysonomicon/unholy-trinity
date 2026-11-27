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
    public static event Action<HexCoord, RocketLauncherData>
        RocketExplosion;

    [SerializeField] private float heightOffset = 0.5f;
    [SerializeField] private AimPreview blastPreview;

    public int MoveIndex { get; set; }

    public HexCoord Momentum { get; private set; }
    public int MomentumGainPerTick => 0;

    public HexTile Tile { get; set; }
    public HexCoord Coord { get; set; }

    public List<HexDirection> MoveSteps { get; private set; }

    private RocketLauncherData Data { get; set; }

    private IVitals _shooter;

    private void OnEnable()
    {
        (this as IMovable).Register(this);
    }

    private void OnDisable()
    {
        (this as IMovable).Unregister(this);
    }

    private bool _shouldDetonate;

    public void Launch(
        RocketLauncherData data,
        IVitals shooter,
        HexCoord origin,
        HexCoord target
    )
    {
        Data = data;
        var tileList = origin.LineTo(target);
        var steps = new List<HexDirection>();
        for (int i = 0; i < tileList.Count - 1; i++)
        {
            var tile = tileList[i];
            steps.Add(tile.DirectionTo(tileList[i + 1]));
        }

        MoveSteps = steps;
        Momentum = tileList.Last() - tileList.First();
        _shooter = shooter;
        MoveIndex = 0;

        //! TODO: Some sort of visualizer abstraction
        // blastPreview.Show(data.GetBlastTiles(mapManager.Map, target),
        //     mapManager.Layout);
    }

    // advances a single rocket forward by the speed defined in the weapon data
    public void Move()
    {
        (this as IMovable).Move();
        _shouldDetonate =
            !(this as IMovable).CanMove || IsDirectHit();
        if (_shouldDetonate)
        {
            Detonate();
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
        RocketExplosion?.Invoke(Tile.Coord, Data);
        Destroy(gameObject);
    }

    public void BeginMovePhase()
    {
    }

    public void EndMovePhase()
    {
    }
}
}
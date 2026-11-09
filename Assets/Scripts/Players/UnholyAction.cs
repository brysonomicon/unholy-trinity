using System;
using UnholyTrinity.Hex;
using UnholyTrinity.Simulation;
using UnholyTrinity.Weapons;

namespace UnholyTrinity.Players
{
    // one tick of a players plan.
    // contains the Planned values so that cards can read planned values.
    // movement is plain data the simulator reads, so plans can be previewed and
    // recorded for replays. shoot and aim still use callbacks
    public class UnholyAction
    {
        public Weapon PlannedShot { get; set; }
        public HexCoord? PlannedTarget { get; set; }
        public AimDirection PlannedAim { get; set; }

        // the direction the player picked this tick, null = hold. the simulator turns this plus
        // momentum into the actual move, so the plan never stores a path
        public HexDirection? PlannedMove { get; set; }

        // aim resolves before move, so this tick's aim decides whether the move accelerates
        public MoveInput MoveInput => new(PlannedMove, PlannedAim.ToFacing());

        public Action Shoot { get; set; }
        public Action Aim { get; set; }

        public UnholyAction(AimDirection startingAim)
        {
            PlannedAim = startingAim;
        }
    }
}

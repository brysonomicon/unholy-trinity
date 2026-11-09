using System;
using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;

namespace UnholyTrinity.Players
{
    // one tick of a players plan.
    // contains the Planned values so that cards can read planned values
    // until we can figure out how to communicate the plan with just 
    // callbacks
    public class UnholyAction
    {
        public Weapon PlannedShot { get; set; }
        public HexCoord? PlannedTarget { get; set; }
        public AimDirection PlannedAim { get; set; }
        public List<HexDirection> PlannedMove { get; set; } = new();

        // the direction the player clicked this tick, null = hold
        // kept so the move can be recalculated if the tick's aim changes
        public HexDirection? PlannedDirection { get; set; }

        // momentum at the end of this tick, stored so the next tick can
        // build off of it during the plan without touching real state
        public HexCoord PlannedMomentum { get; set; }

        public Action Shoot { get; set; }
        public Action Aim { get; set; }
        public Func<IEnumerator<(int, HexCoord)>> Move { get; set ;}

        public UnholyAction(AimDirection startingAim)
        {
            PlannedAim = startingAim;
        }
    }
}


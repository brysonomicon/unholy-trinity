using System.Collections.Generic;
using System.Linq;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// Resolves the move phase of a tick. Units move at the same time, one step at a time: on each step every
    /// moving unit takes one hex in priority order (the order of the inputs), so whoever reaches a hex on an
    /// earlier step gets there first and priority only breaks same-step ties. Tile rules run on every hex
    /// entered and on the hex each unit ends on.
    ///
    /// Previews and resolution both run through here, so what the player is shown is what resolution does.
    /// </summary>
    public static class TickSimulator
    {
        /// <summary>Caps the steps in one tick so rules that add steps (e.g. boost chains) can't loop forever.</summary>
        public const int MaxSteps = 32;

        public static readonly IReadOnlyList<ITileRule> DefaultRules = new ITileRule[] { new PickupRule() };

        public static TickLog Simulate(SimState state, IReadOnlyList<UnitTickInput> inputs) =>
            Simulate(state, inputs, DefaultRules);

        /// <summary>
        /// Simulates one tick, changing <paramref name="state"/> in place. Units missing from
        /// <paramref name="inputs"/> get no input and come last in priority.
        /// </summary>
        public static TickLog Simulate(SimState state, IReadOnlyList<UnitTickInput> inputs, IReadOnlyList<ITileRule> rules)
        {
            var log = new TickLog();
            foreach (var rule in rules)
                rule.OnTickStarted(state, log);

            var movers = BuildMoves(state, inputs);

            int step = 0;
            while (step < MaxSteps && movers.Any(mover => mover.Move.IsMoving))
            {
                log.Step = ++step;
                foreach (var (unit, move) in movers)
                    if (move.IsMoving)
                        TakeStep(state, unit, move, rules, log);
            }

            log.Step = step + 1;
            foreach (var (unit, move) in movers)
            {
                foreach (var rule in rules)
                {
                    if (unit.IsDead)
                        break;
                    rule.OnStop(state, unit, log);
                    LogIfKilled(unit, move, log);
                }

                unit.Velocity = MoveModel.VelocityAfter(move);
            }

            return log;
        }

        /// <summary>
        /// Simulates <paramref name="plan"/> tick by tick on a copy of <paramref name="state"/> that holds only
        /// <paramref name="unitId"/>. Other units are left out because their plans are hidden until resolution.
        /// </summary>
        public static List<PreviewTick> PreviewPlan(SimState state, int unitId, IReadOnlyList<MoveInput> plan)
        {
            var preview = state.CloneWithOnly(unitId);
            var ticks = new List<PreviewTick>(plan.Count);

            foreach (var move in plan)
            {
                var log = Simulate(preview, new[] { new UnitTickInput(unitId, move) });
                ticks.Add(new PreviewTick(log, preview.GetUnit(unitId).Clone()));
            }

            return ticks;
        }

        static List<(UnitState Unit, UnitMove Move)> BuildMoves(SimState state, IReadOnlyList<UnitTickInput> inputs)
        {
            var ordered = inputs
                .Select(input => (Unit: state.GetUnit(input.UnitId), input.Move))
                .Concat(state.Units
                    .Where(unit => inputs.All(input => input.UnitId != unit.Id))
                    .Select(unit => (Unit: unit, Move: MoveInput.None)));

            return ordered
                .Where(entry => !entry.Unit.IsDead)
                .Select(entry => (entry.Unit, MoveModel.Resolve(entry.Unit, entry.Move)))
                .ToList();
        }

        static void TakeStep(SimState state, UnitState unit, UnitMove move, IReadOnlyList<ITileRule> rules, TickLog log)
        {
            var from = unit.Coord;
            var next = from.Neighbor(move.Direction);

            // Units block each other until the unit-vs-unit hit rules are designed. This also keeps two units
            // from ending on the same hex.
            if (!state.Map.IsWalkable(next) || state.IsOccupied(next))
            {
                move.Halt();
                log.Add(TickEventType.Blocked, unit.Id, next, from);
                return;
            }

            unit.Coord = next;
            move.CompleteStep();
            log.Add(TickEventType.Moved, unit.Id, next, from);

            foreach (var rule in rules)
            {
                rule.OnEnter(state, unit, move, log);
                if (LogIfKilled(unit, move, log))
                    return;
            }
        }

        /// <summary>Death ends the unit's move where it stands.</summary>
        static bool LogIfKilled(UnitState unit, UnitMove move, TickLog log)
        {
            if (!unit.IsDead)
                return false;

            move.Halt();
            log.Add(TickEventType.Died, unit.Id, unit.Coord);
            return true;
        }
    }
}

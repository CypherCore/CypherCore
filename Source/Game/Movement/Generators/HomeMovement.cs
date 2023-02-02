// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Movement;

namespace Game.AI
{
    public class HomeMovementGenerator<T> : MovementGeneratorMedium<T> where T : Creature
    {
        public HomeMovementGenerator()
        {
            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
        }

        public override void DoInitialize(T owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            owner.SetNoSearchAssistance(false);

            SetTargetLocation(owner);
        }

        public override void DoReset(T owner) 
        {
            RemoveFlag(MovementGeneratorFlags.Deactivated);
            DoInitialize(owner);
        }

        public override bool DoUpdate(T owner, uint diff)
        {
            if (HasFlag(MovementGeneratorFlags.Interrupted) || owner.MoveSpline.Finalized())
            {
                AddFlag(MovementGeneratorFlags.InformEnabled);
                return false;
            }
            return true;
        }

        public override void DoDeactivate(T owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.RoamingMove);
        }

        public override void DoFinalize(T owner, bool active, bool movementInform)
        {
            if (!owner.IsCreature())
                return;

            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
                owner.ClearUnitState(UnitState.RoamingMove | UnitState.Evade);

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
            {
                if (!owner.HasCanSwimFlagOutOfCombat())
                    owner.RemoveUnitFlag(UnitFlags.CanSwim);

                owner.SetSpawnHealth();
                owner.LoadCreaturesAddon();
                if (owner.IsVehicle())
                    owner.GetVehicleKit().Reset(true);

                CreatureAI ai = owner.GetAI();
                if (ai != null)
                    ai.JustReachedHome();
            }
        }

        void SetTargetLocation(T owner)
        {
            // if we are ROOT/STUNNED/DISTRACTED even after aura clear, finalize on next update - otherwise we would get stuck in evade
            if (owner.HasUnitState(UnitState.Root | UnitState.Stunned | UnitState.Distracted))
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                return;
            }

            owner.ClearUnitState(UnitState.AllErasable & ~UnitState.Evade);
            owner.AddUnitState(UnitState.RoamingMove);

            Position destination = owner.GetHomePosition();
            MoveSplineInit init = new(owner);
            /*
             * TODO: maybe this never worked, who knows, top is always this generator, so this code calls GetResetPosition on itself
             *
             * if (owner->GetMotionMaster()->empty() || !owner->GetMotionMaster()->top()->GetResetPosition(owner, x, y, z))
             * {
             *     owner->GetHomePosition(x, y, z, o);
             *     init.SetFacing(o);
             * }
             */

            owner.UpdateAllowedPositionZ(destination.posX, destination.posY, ref destination.posZ);
            init.MoveTo(destination);
            init.SetFacing(destination.GetOrientation());
            init.SetWalk(false);
            init.Launch();
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Home;
        }
    }
}

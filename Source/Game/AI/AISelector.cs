// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Movement;

namespace Game.AI
{
    public class AISelector
    {
        public static CreatureAI SelectAI(Creature creature)
        {
            if (creature.IsPet())
                return new PetAI(creature);

            //scriptname in db
            CreatureAI scriptedAI = Global.ScriptMgr.GetCreatureAI(creature);
            if (scriptedAI != null)
                return scriptedAI;

            switch (creature.GetCreatureTemplate().AIName)
            {
                case "AggressorAI":
                    return new AggressorAI(creature);
                case "ArcherAI":
                    return new ArcherAI(creature);
                case "CombatAI":
                    return new CombatAI(creature);
                case "CritterAI":
                    return new CritterAI(creature);
                case "GuardAI":
                    return new GuardAI(creature);
                case "NullCreatureAI":
                    return new NullCreatureAI(creature);
                case "PassiveAI":
                    return new PassiveAI(creature);
                case "PetAI":
                    return new PetAI(creature);
                case "ReactorAI":
                    return new ReactorAI(creature);
                case "SmartAI":
                    return new SmartAI(creature);
                case "TotemAI":
                    return new TotemAI(creature);
                case "TriggerAI":
                    return new TriggerAI(creature);
                case "TurretAI":
                    return new TurretAI(creature);
                case "VehicleAI":
                    return new VehicleAI(creature);
            }

            // select by NPC flags
            if (creature.IsVehicle())
                return new VehicleAI(creature);
            else if (creature.HasUnitTypeMask(UnitTypeMask.ControlableGuardian) && ((Guardian)creature).GetOwner().IsTypeId(TypeId.Player))
                return new PetAI(creature);
            else if (creature.HasNpcFlag(NPCFlags.SpellClick))
                return new NullCreatureAI(creature);
            else if (creature.IsGuard())
                return new GuardAI(creature);
            else if (creature.HasUnitTypeMask(UnitTypeMask.ControlableGuardian))
                return new PetAI(creature);
            else if (creature.IsTotem())
                return new TotemAI(creature);
            else if (creature.IsTrigger())
            {
                if (creature.m_spells[0] != 0)
                    return new TriggerAI(creature);
                else
                    return new NullCreatureAI(creature);
            }
            else if (creature.IsCritter() && !creature.HasUnitTypeMask(UnitTypeMask.Guardian))
                return new CritterAI(creature);

            if (creature.IsCivilian() && !creature.IsNeutralToAll())
                return new AggressorAI(creature);

            if (creature.IsCivilian() || creature.IsNeutralToAll())
                return new ReactorAI(creature);

            return new NullCreatureAI(creature);
        }

        public static MovementGenerator SelectMovementGenerator(Unit unit)
        {
            MovementGeneratorType type = unit.GetDefaultMovementType();
            Creature creature = unit.ToCreature();
            if (creature != null && creature.GetPlayerMovingMe() == null)
                type = creature.GetDefaultMovementType();

            return type switch
            {
                MovementGeneratorType.Random => new RandomMovementGenerator(),
                MovementGeneratorType.Waypoint => new WaypointMovementGenerator(),
                MovementGeneratorType.Idle => new IdleMovementGenerator(),
                _ => null,
            };
        }

        public static GameObjectAI SelectGameObjectAI(GameObject go)
        {
            // scriptname in db
            GameObjectAI scriptedAI = Global.ScriptMgr.GetGameObjectAI(go);
            if (scriptedAI != null)
                return scriptedAI;

            return go.GetAIName() switch
            {
                "SmartGameObjectAI" => new SmartGameObjectAI(go),
                _ => new GameObjectAI(go),
            };
        }
    }
}

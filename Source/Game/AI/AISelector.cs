/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

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
            else if (creature.HasFlag64(UnitFields.NpcFlags, NPCFlags.SpellClick))
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

            if (!creature.IsCivilian() && !creature.IsNeutralToAll())
                return new AggressorAI(creature);

            if (creature.IsCivilian() || creature.IsNeutralToAll())
                return new ReactorAI(creature);

            return new NullCreatureAI(creature);
        }

        public static IMovementGenerator SelectMovementAI(Creature creature)
        {
            switch (creature.m_defaultMovementType)
            {
                case MovementGeneratorType.Random:
                    return new RandomMovementGenerator();
                case MovementGeneratorType.Waypoint:
                    return new WaypointMovementGenerator();
            }
            return null;
        }

        public static GameObjectAI SelectGameObjectAI(GameObject go)
        {
            // scriptname in db
            GameObjectAI scriptedAI = Global.ScriptMgr.GetGameObjectAI(go);
            if (scriptedAI != null)
                return scriptedAI;

            switch (go.GetAIName())
            {
                case "GameObjectAI":
                    return new GameObjectAI(go);
                case "SmartGameObjectAI":
                    return new SmartGameObjectAI(go);
            }
            return new NullGameObjectAI(go);
        }
    }
}

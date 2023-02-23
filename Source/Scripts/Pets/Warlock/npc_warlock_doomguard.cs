// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ICreature;
using Game.Spells;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        [CreatureScript(new uint[] { 11859, 59000 })]
        // Doomguard - 11859, Terrorguard - 59000
        public class npc_warlock_doomguard : SmartAI
        {
            public EventMap events = new();
            public double maxDistance;

            public npc_warlock_doomguard(Creature creature) : base(creature)
            {
                Unit owner = me.GetOwner();
                if (owner == null)
                    return;

                creature.SetLevel(owner.GetLevel());
                creature.UpdateLevelDependantStats();
                creature.SetReactState(ReactStates.Aggressive);
                creature.SetCreatorGUID(owner.GetGUID());

                var summon = creature.ToTempSummon();

                if (summon != null)
                {
                    summon.SetCanFollowOwner(true);
                    summon.GetMotionMaster().Clear();
                    summon.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, summon.GetFollowAngle());
                }
            }

            public override void Reset()
            {
                me.SetClass(Class.Rogue);
                me.SetPowerType(PowerType.Energy);
                me.SetMaxPower(PowerType.Energy, 200);
                me.SetPower(PowerType.Energy, 200);

                events.Reset();
                events.ScheduleEvent(1, TimeSpan.FromSeconds(3));

                me.SetControlled(true, UnitState.Root);
                maxDistance = SpellManager.Instance.GetSpellInfo(WarlockSpells.PET_DOOMBOLT, Difficulty.None).RangeEntry.RangeMax[0];
            }

            public override void UpdateAI(uint diff)
            {
                UpdateVictim();
                Unit owner = me.GetOwner();

                if (me.GetOwner())
                {
                    Unit victim = owner.GetVictim();

                    if (owner.GetVictim())
                        me.Attack(victim, false);
                }

                events.Update(diff);

                uint eventId = events.ExecuteEvent();

                while (eventId != 0)
                {
                    switch (eventId)
                    {
                        case 1:
                            if (!me.GetVictim())
                            {
                                me.SetControlled(false, UnitState.Root);
                                events.ScheduleEvent(eventId, TimeSpan.FromSeconds(1));

                                return;
                            }

                            me.SetControlled(true, UnitState.Root);
                            me.CastSpell(me.GetVictim(), WarlockSpells.PET_DOOMBOLT, new CastSpellExtraArgs(TriggerCastFlags.None).SetOriginalCaster(me.GetOwnerGUID()));
                            events.ScheduleEvent(eventId, TimeSpan.FromSeconds(3));

                            break;
                    }

                    eventId = events.ExecuteEvent();
                }
            }
        }
    }
}
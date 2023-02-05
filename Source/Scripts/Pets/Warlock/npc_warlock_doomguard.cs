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
        [Script]
        // Doomguard - 11859, Terrorguard - 59000
        public class npc_warlock_doomguard : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public class npc_warlock_doomguardAI : ScriptedAI
            {
                public EventMap events = new();
                public float maxDistance;

                public npc_warlock_doomguardAI(Creature creature) : base(creature)
                {
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

            public npc_warlock_doomguard() : base("npc_warlock_doomguard")
            {
            }

            //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
            //ORIGINAL LINE: CreatureAI* GetAI(Creature* creature) const
            public CreatureAI GetAI(Creature creature)
            {
                return new npc_warlock_doomguardAI(creature);
            }
        }
    }
}
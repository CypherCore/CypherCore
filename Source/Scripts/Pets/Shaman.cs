// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Pets
{
    namespace Shaman
    {
        internal struct SpellIds
        {
            //npc_pet_shaman_earth_elemental
            public const uint AngeredEarth = 36213;

            //npc_pet_shaman_fire_elemental
            public const uint FireBlast = 57984;
            public const uint FireNova = 12470;
            public const uint FireShield = 13376;
        }

        [Script]
        internal class npc_pet_shaman_earth_elemental : ScriptedAI
        {
            public npc_pet_shaman_earth_elemental(Creature creature) : base(creature)
            {
            }

            public override void Reset()
            {
                _scheduler.CancelAll();

                _scheduler.Schedule(TimeSpan.FromSeconds(0),
                                    task =>
                                    {
                                        DoCastVictim(SpellIds.AngeredEarth);
                                        task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
                                    });
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim())
                    return;

                _scheduler.Update(diff);

                DoMeleeAttackIfReady();
            }
        }

        [Script]
        public class npc_pet_shaman_fire_elemental : ScriptedAI
        {
            public npc_pet_shaman_fire_elemental(Creature creature) : base(creature)
            {
            }

            public override void Reset()
            {
                _scheduler.CancelAll();

                _scheduler.Schedule(TimeSpan.FromSeconds(5),
                                    TimeSpan.FromSeconds(20),
                                    task =>
                                    {
                                        DoCastVictim(SpellIds.FireNova);
                                        task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
                                    });

                _scheduler.Schedule(TimeSpan.FromSeconds(5),
                                    TimeSpan.FromSeconds(20),
                                    task =>
                                    {
                                        DoCastVictim(SpellIds.FireShield);
                                        task.Repeat(TimeSpan.FromSeconds(2));
                                    });

                _scheduler.Schedule(TimeSpan.FromSeconds(0),
                                    task =>
                                    {
                                        DoCastVictim(SpellIds.FireBlast);
                                        task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
                                    });
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim())
                    return;

                _scheduler.Update(diff);

                if (me.HasUnitState(UnitState.Casting))
                    return;

                DoMeleeAttackIfReady();
            }
        }
    }
}
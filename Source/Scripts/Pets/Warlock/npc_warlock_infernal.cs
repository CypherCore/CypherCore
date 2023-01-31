using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ICreature;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        [Script]
        public class npc_warlock_infernal : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public class npc_warlock_infernalAI : ScriptedAI
            {
                public Position spawnPos = new();

                public npc_warlock_infernalAI(Creature c) : base(c)
                {
                }

                public override void Reset()
                {
                    spawnPos = me.GetPosition();

                    // if we leave default State (ASSIST) it will passively be controlled by warlock
                    me.SetReactState(ReactStates.Passive);

                    // melee Damage
                    Unit owner = me.GetOwner();

                    if (me.GetOwner())
                    {
                        Player player = owner.ToPlayer();

                        if (owner.ToPlayer())
                        {
                            bool isLordSummon = me.GetEntry() == 108452;

                            int spellPower = player.SpellBaseDamageBonusDone(SpellSchoolMask.Fire);
                            int dmg = MathFunctions.CalculatePct(spellPower, isLordSummon ? 30 : 50);
                            int diff = MathFunctions.CalculatePct(dmg, 10);

                            me.SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, dmg - diff);
                            me.SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, dmg + diff);


                            if (isLordSummon)
                                return;

                            if (player.HasAura(SpellIds.LORD_OF_THE_FLAMES) &&
                                !player.HasAura(SpellIds.LORD_OF_THE_FLAMES_CD))
                            {
                                List<float> angleOffsets = new()
                                                           {
                                                               (float)Math.PI / 2.0f,
                                                               (float)Math.PI,
                                                               3.0f * (float)Math.PI / 2.0f
                                                           };

                                for (uint i = 0; i < 3; ++i)
                                    player.CastSpell(me, SpellIds.LORD_OF_THE_FLAMES_SUMMON, true);

                                player.CastSpell(player, SpellIds.LORD_OF_THE_FLAMES_CD, true);
                            }
                        }
                    }
                }

                public override void UpdateAI(uint UnnamedParameter)
                {
                    if (!me.HasAura(SpellIds.IMMOLATION))
                        DoCast(SpellIds.IMMOLATION);

                    // "The Infernal deals strong area of effect Damage, and will be drawn to attack targets near the impact point"
                    if (!me.GetVictim())
                    {
                        Unit preferredTarget = me.GetAttackerForHelper();

                        if (preferredTarget != null)
                            me.GetAI().AttackStart(preferredTarget);
                    }

                    DoMeleeAttackIfReady();
                }
            }

            public npc_warlock_infernal() : base("npc_warlock_infernal")
            {
            }

            //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
            //ORIGINAL LINE: CreatureAI* GetAI(Creature* creature) const
            public CreatureAI GetAI(Creature creature)
            {
                return new npc_warlock_infernalAI(creature);
            }
        }
    }
}
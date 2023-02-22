// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IUnit;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {

        [CreatureScript(89)]
        public class npc_warlock_infernal : ScriptedAI
        {
            public Position spawnPos = new();

            public npc_warlock_infernal(Creature creature) : base(creature)
            {
                if (!me.TryGetOwner(out var owner))
                    return;

                if (owner.TryGetAsPlayer(out var player) && player.HasAura(WarlockSpells.INFERNAL_BRAND))
                    me.AddAura(WarlockSpells.INFERNAL_BRAND_INFERNAL_AURA, me);

                creature.SetLevel(owner.GetLevel());
                creature.UpdateLevelDependantStats();
                creature.SetReactState(ReactStates.Assist);
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
                spawnPos = me.GetPosition();

                // if we leave default State (ASSIST) it will passively be controlled by warlock
                me.SetReactState(ReactStates.Passive);

                // melee Damage
                if (me.TryGetOwner(out var owner) && owner.TryGetAsPlayer(out var player))
                {
                    bool isLordSummon = me.GetEntry() == 108452;

                    double spellPower = player.SpellBaseDamageBonusDone(SpellSchoolMask.Fire);
                    double dmg = MathFunctions.CalculatePct(spellPower, isLordSummon ? 30 : 50);
                    double diff = MathFunctions.CalculatePct(dmg, 10);

                    me.SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, dmg - diff);
                    me.SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, dmg + diff);


                    if (isLordSummon)
                        return;

                    if (player.HasAura(WarlockSpells.LORD_OF_THE_FLAMES) &&
                        !player.HasAura(WarlockSpells.LORD_OF_THE_FLAMES_CD))
                    {
                        List<double> angleOffsets = new()
                                                           {
                                                               (double)Math.PI / 2.0f,
                                                               (double)Math.PI,
                                                               3.0f * (double)Math.PI / 2.0f
                                                           };

                        for (uint i = 0; i < 3; ++i)
                            player.CastSpell(me, WarlockSpells.LORD_OF_THE_FLAMES_SUMMON, true);

                        player.CastSpell(player, WarlockSpells.LORD_OF_THE_FLAMES_CD, true);
                    }
                }
            }

            public override void UpdateAI(uint UnnamedParameter)
            {
                if (!me.HasAura(WarlockSpells.IMMOLATION))
                    DoCast(WarlockSpells.IMMOLATION);

                // "The Infernal deals strong area of effect Damage, and will be drawn to attack targets near the impact point"
                if (!me.GetVictim())
                {
                    Unit preferredTarget = me.GetAttackerForHelper();

                    if (preferredTarget != null)
                        me.GetAI().AttackStart(preferredTarget);
                }

                DoMeleeAttackIfReady();
            }

            public override void OnMeleeAttack(CalcDamageInfo damageInfo, WeaponAttackType attType, bool extra)
            {
                if (me != damageInfo.Attacker || !me.TryGetOwner(out var owner))
                    return;

                if (owner.TryGetAsPlayer(out var player) && player.HasAura(WarlockSpells.INFERNAL_BRAND))
                    me.AddAura(WarlockSpells.INFERNAL_BRAND_ENEMY_AURA, damageInfo.Target);
            }
        }
    }
}
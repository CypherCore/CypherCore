// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Pets
{
    namespace DeathKnight
    {
        struct SpellIds
        {
            public const uint SummonGargoyle1 = 49206;
            public const uint SummonGargoyle2 = 50514;
            public const uint DismissGargoyle = 50515;
            public const uint Sanctuary = 54661;
        }

        [Script]
        class npc_pet_dk_ebon_gargoyle : CasterAI
        {
            public npc_pet_dk_ebon_gargoyle(Creature creature) : base(creature) { }

            public override void InitializeAI()
            {
                base.InitializeAI();
                ObjectGuid ownerGuid = me.GetOwnerGUID();
                if (ownerGuid.IsEmpty())
                    return;

                // Find victim of Summon Gargoyle spell
                List<Unit> targets = new();
                var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(me, me, 30.0f);
                var searcher = new UnitListSearcher(me, targets, u_check);
                Cell.VisitAllObjects(me, searcher, 30.0f);
                foreach (var target in targets)
                {
                    if (target.HasAura(SpellIds.SummonGargoyle1, ownerGuid))
                    {
                        me.Attack(target, false);
                        break;
                    }
                }
            }

            public override void JustDied(Unit killer)
            {
                // Stop Feeding Gargoyle when it dies
                Unit owner = me.GetOwner();
                if (owner)
                    owner.RemoveAurasDueToSpell(SpellIds.SummonGargoyle2);
            }

            // Fly away when dismissed
            public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
            {
                if (spellInfo.Id != SpellIds.DismissGargoyle || !me.IsAlive())
                    return;

                Unit owner = me.GetOwner();
                if (!owner || owner != caster)
                    return;

                // Stop Fighting
                me.SetUnitFlag(UnitFlags.NonAttackable);

                // Sanctuary
                me.CastSpell(me, SpellIds.Sanctuary, true);
                me.SetReactState(ReactStates.Passive);

                //! HACK: Creature's can't have MOVEMENTFLAG_FLYING
                // Fly Away
                me.SetCanFly(true);
                me.SetSpeedRate(UnitMoveType.Flight, 0.75f);
                me.SetSpeedRate(UnitMoveType.Run, 0.75f);
                float x = me.GetPositionX() + 20 * (float)Math.Cos(me.GetOrientation());
                float y = me.GetPositionY() + 20 * (float)Math.Sin(me.GetOrientation());
                float z = me.GetPositionZ() + 40;
                me.GetMotionMaster().Clear();
                me.GetMotionMaster().MovePoint(0, x, y, z);

                // Despawn as soon as possible
                me.DespawnOrUnsummon(TimeSpan.FromSeconds(4));
            }


        }

        [Script]
        class npc_pet_dk_guardian : AggressorAI
        {
            public npc_pet_dk_guardian(Creature creature) : base(creature) { }

            public override bool CanAIAttack(Unit target)
            {
                if (!target)
                    return false;
                Unit owner = me.GetOwner();
                if (owner && !target.IsInCombatWith(owner))
                    return false;
                return base.CanAIAttack(target);
            }
        }
    }
}

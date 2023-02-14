// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Pets
{
    namespace Priest
    {
        internal struct SpellIds
        {
            public const uint GlyphOfShadowFiend = 58228;
            public const uint ShadowFiendDeath = 57989;
            public const uint LightWellCharges = 59907;
        }

        [Script]
        internal class npc_pet_pri_lightwell : PassiveAI
        {
            public npc_pet_pri_lightwell(Creature creature) : base(creature)
            {
                DoCast(creature, SpellIds.LightWellCharges, new CastSpellExtraArgs(false));
            }

            public override void EnterEvadeMode(EvadeReason why)
            {
                if (!me.IsAlive())
                    return;

                me.CombatStop(true);
                EngagementOver();
                me.ResetPlayerDamageReq();
            }
        }

        [Script]
        internal class npc_pet_pri_shadowfiend : PetAI
        {
            public npc_pet_pri_shadowfiend(Creature creature) : base(creature)
            {
            }

            public override void IsSummonedBy(WorldObject summoner)
            {
                Unit unitSummoner = summoner.ToUnit();

                if (unitSummoner == null)
                    return;

                if (unitSummoner.HasAura(SpellIds.GlyphOfShadowFiend))
                    DoCastAOE(SpellIds.ShadowFiendDeath);
            }
        }
    }
}
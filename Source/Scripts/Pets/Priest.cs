// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Pets
{
    namespace Priest
    {
        struct SpellIds
        {
            public const uint GlyphOfShadowFiend = 58228;
            public const uint ShadowFiendDeath = 57989;
            public const uint LightWellCharges = 59907;
        }

        [Script]
        class npc_pet_pri_lightwell : PassiveAI
        {
            public npc_pet_pri_lightwell(Creature creature) : base(creature)
            {
                DoCast(creature, SpellIds.LightWellCharges, new Game.Spells.CastSpellExtraArgs(false));
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
        class npc_pet_pri_shadowfiend : PetAI
        {
            public npc_pet_pri_shadowfiend(Creature creature) : base(creature) { }

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

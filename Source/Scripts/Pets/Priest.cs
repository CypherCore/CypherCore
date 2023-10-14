// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Pets.Priest
{
    [Script] // 198236 - Divine Image
    class npc_pet_pri_divine_image : PassiveAI
    {
        const uint SpellPriestDivineImageSpellCheck = 405216;
        const uint SpellPriestInvokeTheNaaru = 196687;

        public npc_pet_pri_divine_image(Creature creature) : base(creature) { }

        public override void IsSummonedBy(WorldObject summoner)
        {
            me.CastSpell(me, SpellPriestInvokeTheNaaru);

            if (me.ToTempSummon().IsGuardian() && summoner.IsUnit())
                (me as Guardian).SetBonusDamage((int)summoner.ToUnit().SpellBaseHealingBonusDone(SpellSchoolMask.Holy));
        }

        public override void OnDespawn()
        {
            Unit owner = me.GetOwner();
            if (owner != null)
                owner.RemoveAura(SpellPriestDivineImageSpellCheck);
        }
    }

    [Script] // 189820 - Lightwell
    class npc_pet_pri_lightwell : PassiveAI
    {
        const uint SpellPriestLightwellCharges = 59907;

        public npc_pet_pri_lightwell(Creature creature) : base(creature)
        {
            DoCast(me, SpellPriestLightwellCharges, false);
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

    // 19668 - Shadowfiend
    [Script] // 62982 - Mindbender
    class npc_pet_pri_shadowfiend_mindbender : PetAI
    {
        const uint SpellPriestAtonement = 81749;
        const uint SpellPriestAtonementPassive = 195178;

        public npc_pet_pri_shadowfiend_mindbender(Creature creature) : base(creature) { }

        public override void IsSummonedBy(WorldObject summonerWO)
        {
            Unit summoner = summonerWO.ToUnit();
            if (summoner == null)
                return;

            if (summoner.HasAura(SpellPriestAtonement))
                DoCastSelf(SpellPriestAtonementPassive, TriggerCastFlags.FullMask);
        }
    }
}
// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(5740)] // 5740 - Rain of Fire Updated 7.1.5
    internal class spell_warl_rain_of_fire : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandleDummyTick, 2, AuraType.PeriodicDummy));
        }

        private void HandleDummyTick(AuraEffect aurEff)
        {
            List<AreaTrigger> rainOfFireAreaTriggers = GetTarget().GetAreaTriggers(WarlockSpells.RAIN_OF_FIRE);
            List<ObjectGuid> targetsInRainOfFire = new();

            foreach (AreaTrigger rainOfFireAreaTrigger in rainOfFireAreaTriggers)
            {
                var insideTargets = rainOfFireAreaTrigger.GetInsideUnits();
                targetsInRainOfFire.AddRange(insideTargets);
            }

            foreach (ObjectGuid insideTargetGuid in targetsInRainOfFire)
            {
                Unit insideTarget = Global.ObjAccessor.GetUnit(GetTarget(), insideTargetGuid);

                if (insideTarget)
                    if (!GetTarget().IsFriendlyTo(insideTarget))
                        GetTarget().CastSpell(insideTarget, WarlockSpells.RAIN_OF_FIRE_DAMAGE, true);
            }
        }
    }
}
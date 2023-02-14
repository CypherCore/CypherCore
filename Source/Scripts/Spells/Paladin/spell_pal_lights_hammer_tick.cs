// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Paladin
{
    // Light's Hammer (Periodic Dummy) - 114918
    [SpellScript(114918)]
    public class spell_pal_lights_hammer_tick : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void OnTick(AuraEffect UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                if (caster.GetOwner())
                {
                    CastSpellExtraArgs args = new CastSpellExtraArgs();
                    args.SetTriggerFlags(TriggerCastFlags.FullMask);
                    args.SetOriginalCaster(caster.GetOwner().GetGUID());
                    caster.CastSpell(new Position(caster.GetPositionX(), caster.GetPositionY(), caster.GetPositionZ()), PaladinSpells.ARCING_LIGHT_HEAL, args);
                    caster.CastSpell(new Position(caster.GetPositionX(), caster.GetPositionY(), caster.GetPositionZ()), PaladinSpells.ARCING_LIGHT_DAMAGE, args);
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
        }
    }
}

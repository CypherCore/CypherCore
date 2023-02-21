// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script]
public class spell_dk_unholy_assault : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Validate(SpellInfo spell)
    {
        return ValidateSpellInfo(DeathKnightSpells.FESTERING_WOUND);
    }

    public override void Register() {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 2, SpellEffectName.Dummy, SpellScriptHookType.Launch));
    }

	private void HandleScriptEffect(int effIndex) {
		GetCaster().CastSpell(GetHitUnit(), DeathKnightSpells.FESTERING_WOUND, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, GetEffectValue()));
	}
}
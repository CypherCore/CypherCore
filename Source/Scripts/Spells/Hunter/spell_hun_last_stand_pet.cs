// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script]
internal class spell_hun_last_stand_pet : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.PetLastStandTriggered);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var                caster = GetCaster();
		CastSpellExtraArgs args   = new(TriggerCastFlags.FullMask);
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)caster.CountPctFromMaxHealth(30));
		caster.CastSpell(caster, HunterSpells.PetLastStandTriggered, args);
	}
}
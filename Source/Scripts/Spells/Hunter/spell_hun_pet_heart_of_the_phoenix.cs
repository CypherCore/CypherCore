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
internal class spell_hun_pet_heart_of_the_phoenix : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		if (!GetCaster().IsPet())
			return false;

		return true;
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.PetHeartOfThePhoenixTriggered, HunterSpells.PetHeartOfThePhoenixDebuff);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		var caster = GetCaster();
		var owner  = caster.GetOwner();

		if (owner)
			if (!caster.HasAura(HunterSpells.PetHeartOfThePhoenixDebuff))
			{
				CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
				args.AddSpellMod(SpellValueMod.BasePoint0, 100);
				owner.CastSpell(caster, HunterSpells.PetHeartOfThePhoenixTriggered, args);
				caster.CastSpell(caster, HunterSpells.PetHeartOfThePhoenixDebuff, true);
			}
	}
}
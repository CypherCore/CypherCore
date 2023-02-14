// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_chaos_blast : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.ChaosBlast);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		var basepoints0 = 100;
		var caster      = GetCaster();
		var target      = GetHitUnit();

		if (target)
		{
			CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.BasePoint0, basepoints0);
			caster.CastSpell(target, GenericSpellIds.ChaosBlast, args);
		}
	}
}
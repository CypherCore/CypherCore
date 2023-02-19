// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 32826 - Polymorph (Visual)
internal class spell_mage_polymorph_visual : SpellScript, IHasSpellEffects
{
	private const uint NPC_AUROSALIA = 18744;

	private readonly uint[] PolymorhForms =
	{
		MageSpells.SquirrelForm, MageSpells.GiraffeForm, MageSpells.SerpentForm, MageSpells.DradonhawkForm, MageSpells.WorgenForm, MageSpells.SheepForm
	};

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PolymorhForms);
	}

	public override void Register()
	{
		// add dummy effect spell handler to Polymorph visual
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		Unit target = GetCaster().FindNearestCreature(NPC_AUROSALIA, 30.0f);

		if (target)
			if (target.IsTypeId(TypeId.Unit))
				target.CastSpell(target, PolymorhForms[RandomHelper.IRand(0, 5)], true);
	}
}
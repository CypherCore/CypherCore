// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_impale_leviroth : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		if (Global.ObjectMgr.GetCreatureTemplate(CreatureIds.Leviroth) == null)
			return false;

		return true;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var target = GetHitCreature();

		if (target)
			if (target.GetEntry() == CreatureIds.Leviroth &&
			    !target.HealthBelowPct(95))
			{
				target.CastSpell(target, ItemSpellIds.LevirothSelfImpale, true);
				target.ResetPlayerDamageReq();
			}
	}
}
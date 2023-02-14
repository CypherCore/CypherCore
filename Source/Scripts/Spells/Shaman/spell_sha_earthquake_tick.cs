// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 77478 - Earthquake tick
[SpellScript(77478)]
internal class spell_sha_earthquake_tick : SpellScript, ISpellOnHit, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.EarthquakeKnockingDown) && spellInfo.GetEffects().Count > 1;
	}

	public void OnHit()
	{
		var target = GetHitUnit();

		if (target != null)
			if (RandomHelper.randChance(GetEffectInfo(1).CalcValue()))
			{
				var areaTriggers     = GetCaster().GetAreaTriggers(ShamanSpells.Earthquake);
				var foundAreaTrigger = areaTriggers.Find(at => at.GetGUID() == GetSpell().GetOriginalCasterGUID());

				if (foundAreaTrigger != null)
				{
					var eq = foundAreaTrigger.GetAI<areatrigger_sha_earthquake>();

					if (eq != null)
						if (eq.AddStunnedTarget(target.GetGUID()))
							GetCaster().CastSpell(target, ShamanSpells.EarthquakeKnockingDown, true);
				}
			}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamageCalc, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.LaunchTarget));
	}

	private void HandleDamageCalc(uint effIndex)
	{
		SetEffectValue((int)(GetCaster().SpellBaseDamageBonusDone(SpellSchoolMask.Nature) * 0.391f));
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(190984)]
public class spell_druid_solar_wrath : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private struct Spells
	{
		public static readonly uint SPELL_DRUID_SOLAR_WRATH = 190984;
		public static readonly uint SPELL_DRUID_NATURES_BALANCE = 202430;
		public static readonly uint SPELL_DRUID_SUNFIRE_DOT = 164815;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.SPELL_DRUID_SUNFIRE_DOT, Spells.SPELL_DRUID_SOLAR_WRATH, Spells.SPELL_DRUID_NATURES_BALANCE);
	}

	private void HandleHitTarget(uint UnnamedParameter)
	{
		var target = GetHitUnit();

		if (target != null)
			if (GetCaster().HasAura(Spells.SPELL_DRUID_NATURES_BALANCE))
			{
				var sunfireDOT = target.GetAura(Spells.SPELL_DRUID_SUNFIRE_DOT, GetCaster().GetGUID());

				if (sunfireDOT != null)
				{
					var duration    = sunfireDOT.GetDuration();
					var newDuration = duration + 4 * Time.InMilliseconds;

					if (newDuration > sunfireDOT.GetMaxDuration())
						sunfireDOT.SetMaxDuration(newDuration);

					sunfireDOT.SetDuration(newDuration);
				}
			}

		if (GetCaster() && RandomHelper.randChance(20) && GetCaster().HasAura(DruidSpells.SPELL_DRU_ECLIPSE))
			GetCaster().CastSpell(null, DruidSpells.SPELL_DRU_LUNAR_EMPOWEREMENT, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}
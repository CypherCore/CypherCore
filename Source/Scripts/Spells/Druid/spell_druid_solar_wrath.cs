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
		public static readonly uint SOLAR_WRATH = 190984;
		public static readonly uint NATURES_BALANCE = 202430;
		public static readonly uint SUNFIRE_DOT = 164815;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.SUNFIRE_DOT, Spells.SOLAR_WRATH, Spells.NATURES_BALANCE);
	}

	private void HandleHitTarget(uint UnnamedParameter)
	{
		var target = GetHitUnit();

		if (target != null)
			if (GetCaster().HasAura(Spells.NATURES_BALANCE))
			{
				var sunfireDOT = target.GetAura(Spells.SUNFIRE_DOT, GetCaster().GetGUID());

				if (sunfireDOT != null)
				{
					var duration    = sunfireDOT.GetDuration();
					var newDuration = duration + 4 * Time.InMilliseconds;

					if (newDuration > sunfireDOT.GetMaxDuration())
						sunfireDOT.SetMaxDuration(newDuration);

					sunfireDOT.SetDuration(newDuration);
				}
			}

		if (GetCaster() && RandomHelper.randChance(20) && GetCaster().HasAura(DruidSpells.ECLIPSE))
			GetCaster().CastSpell(null, DruidSpells.LUNAR_EMPOWEREMENT, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}
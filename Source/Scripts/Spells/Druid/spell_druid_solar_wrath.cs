using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(190984)]
public class spell_druid_solar_wrath : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


	private struct Spells
	{
		public static uint SPELL_DRUID_SOLAR_WRATH = 190984;
		public static uint SPELL_DRUID_NATURES_BALANCE = 202430;
		public static uint SPELL_DRUID_SUNFIRE_DOT = 164815;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.SPELL_DRUID_SUNFIRE_DOT, Spells.SPELL_DRUID_SOLAR_WRATH, Spells.SPELL_DRUID_NATURES_BALANCE);
	}

	private void HandleHitTarget(uint UnnamedParameter)
	{
		Unit target = GetHitUnit();
		if (target != null)
		{
			if (GetCaster().HasAura(Spells.SPELL_DRUID_NATURES_BALANCE))
			{
				Aura sunfireDOT = target.GetAura(Spells.SPELL_DRUID_SUNFIRE_DOT, GetCaster().GetGUID());
				if (sunfireDOT != null)
				{
					int duration    = sunfireDOT.GetDuration();
					int newDuration = duration + 4 * Time.InMilliseconds;

					if (newDuration > sunfireDOT.GetMaxDuration())
					{
						sunfireDOT.SetMaxDuration(newDuration);
					}

					sunfireDOT.SetDuration(newDuration);
				}
			}
		}
		if (GetCaster() && RandomHelper.randChance(20) && GetCaster().HasAura(DruidSpells.SPELL_DRU_ECLIPSE))
		{
			GetCaster().CastSpell(null, DruidSpells.SPELL_DRU_LUNAR_EMPOWEREMENT, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}
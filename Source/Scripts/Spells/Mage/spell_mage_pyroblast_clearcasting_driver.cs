using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(44448)]
public class spell_mage_pyroblast_clearcasting_driver : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		Unit caster = GetCaster();

		bool _spellCanProc = (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_SCORCH || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIREBALL || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIRE_BLAST || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FLAMESTRIKE || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_PYROBLAST || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_PHOENIX_FLAMES || (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_DRAGON_BREATH && caster.HasAura(MageSpells.SPELL_MAGE_ALEXSTRASZAS_FURY)));

		if (_spellCanProc)
		{
			return true;
		}
		return false;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		bool procCheck = false;

		Unit caster = GetCaster();

		if ((eventInfo.GetHitMask() & ProcFlagsHit.Normal) != 0)
		{
			if (caster.HasAura(MageSpells.SPELL_MAGE_HEATING_UP))
			{
				caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HEATING_UP);
			}
			return;
		}

		if (!caster.HasAura(MageSpells.SPELL_MAGE_HEATING_UP) && !caster.HasAura(MageSpells.SPELL_MAGE_HOT_STREAK))
		{
			caster.CastSpell(caster, MageSpells.SPELL_MAGE_HEATING_UP, true);

			procCheck = true;

			AuraEffect burn = caster.GetAuraEffect(MageSpells.SPELL_MAGE_CONTROLLED_BURN, 0);
			if (burn != null)
			{
				if (RandomHelper.randChance(burn.GetAmount()))
				{
					procCheck = false;
				}
			}
		}


		if (caster.HasAura(MageSpells.SPELL_MAGE_HEATING_UP) && !caster.HasAura(MageSpells.SPELL_MAGE_HOT_STREAK) && !procCheck)
		{
			caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HEATING_UP);
			caster.CastSpell(caster, MageSpells.SPELL_MAGE_HOT_STREAK, true);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));

	}
}
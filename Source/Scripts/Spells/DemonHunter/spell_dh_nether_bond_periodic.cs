// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(207811)]
public class spell_dh_nether_bond_periodic : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();


	private Unit m_BondUnit;

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		// Try to get the bonded Unit
		if (m_BondUnit == null)
			m_BondUnit = GetBondUnit();

		// If still not found, return
		if (m_BondUnit == null)
			return;

		ulong casterHealBp   = 0;
		ulong casterDamageBp = 0;
		ulong targetHealBp   = 0;
		ulong targetDamageBp = 0;

		var casterHp  = caster.GetHealthPct();
		var targetHp  = m_BondUnit.GetHealthPct();
		var healthPct = (casterHp + targetHp) / 2.0f;

		if (casterHp < targetHp)
		{
			casterHealBp   = caster.CountPctFromMaxHealth(healthPct) - caster.GetHealth();
			targetDamageBp = m_BondUnit.GetHealth() - m_BondUnit.CountPctFromMaxHealth(healthPct);
		}
		else
		{
			casterDamageBp = caster.GetHealth() - caster.CountPctFromMaxHealth(healthPct);
			targetHealBp   = m_BondUnit.CountPctFromMaxHealth(healthPct) - m_BondUnit.GetHealth();
		}

		caster.CastSpell(caster, DemonHunterSpells.NETHER_BOND_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, casterDamageBp).AddSpellMod(SpellValueMod.BasePoint1, casterHealBp));
		caster.CastSpell(m_BondUnit, DemonHunterSpells.NETHER_BOND_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, targetDamageBp).AddSpellMod(SpellValueMod.BasePoint1, targetHealBp));
	}

	private Unit GetBondUnit()
	{
		var caster = GetCaster();

		if (caster == null)
			return null;

		var units  = new List<Unit>();
		var check  = new AnyUnitInObjectRangeCheck(caster, 100.0f);
		var search = new UnitListSearcher(caster, units, check, GridType.All);
		Cell.VisitGrid(caster, search, 100.0f);

		foreach (var u in units)
			if (u.HasAura(DemonHunterSpells.NETHER_BOND, caster.GetGUID()))
				return u;

		return null;
	}

	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		m_BondUnit = GetBondUnit();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
	}
}
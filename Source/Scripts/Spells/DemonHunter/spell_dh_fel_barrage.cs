using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(211053)]
public class spell_dh_fel_barrage : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private int _charges = 1;

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (!Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_FEL_BARRAGE, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_FEL_BARRAGE_TRIGGER, Difficulty.None))
			return false;

		return true;
	}

	public override bool Load()
	{
		var caster = GetCaster();

		if (caster == null || GetSpellInfo() == null)
			return false;

		var chargeCategoryId = GetSpellInfo().ChargeCategoryId;

		while (caster.GetSpellHistory().HasCharge(chargeCategoryId))
		{
			caster.GetSpellHistory().ConsumeCharge(chargeCategoryId);
			_charges++;
		}

		return true;
	}

	private void HandleTrigger(AuraEffect UnnamedParameter)
	{
		var caster = GetCaster();
		var target = GetTarget();

		if (caster == null || target == null)
			return;

		var args = new CastSpellExtraArgs();
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)_charges);
		args.SetTriggerFlags(TriggerCastFlags.FullMask);
		caster.CastSpell(target, DemonHunterSpells.SPELL_DH_FEL_BARRAGE_TRIGGER, args);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleTrigger, 0, AuraType.PeriodicDummy));
	}
}
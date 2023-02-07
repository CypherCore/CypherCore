using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 342246 - Alter Time Aura
internal class spell_mage_alter_time_aura : AuraScript, IHasAuraEffects
{
	private ulong _health;
	private Position _pos;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.AlterTimeVisual, MageSpells.MasterOfTime, MageSpells.Blink);
	}

	public override void Register()
	{
		AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.OverrideActionbarSpells, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
		AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.OverrideActionbarSpells, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit unit = GetTarget();
		_health = unit.GetHealth();
		_pos    = new Position(unit.GetPosition());
	}

	private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit unit = GetTarget();

		if (unit.GetDistance(_pos) <= 100.0f &&
		    GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
		{
			unit.SetHealth(_health);
			unit.NearTeleportTo(_pos);

			if (unit.HasAura(MageSpells.MasterOfTime))
			{
				SpellInfo blink = Global.SpellMgr.GetSpellInfo(MageSpells.Blink, Difficulty.None);
				unit.GetSpellHistory().ResetCharges(blink.ChargeCategoryId);
			}

			unit.CastSpell(unit, MageSpells.AlterTimeVisual);
		}
	}
}
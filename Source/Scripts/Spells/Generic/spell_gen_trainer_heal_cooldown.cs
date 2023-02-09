using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 132334 - Trainer Heal Cooldown (SERVERSIDE)
internal class spell_gen_trainer_heal_cooldown : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(SharedConst.SpellReviveBattlePets);
	}

	public override bool Load()
	{
		return GetUnitOwner().IsPlayer();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(UpdateReviveBattlePetCooldown, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
	}

	private void UpdateReviveBattlePetCooldown(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Player    target                   = GetUnitOwner().ToPlayer();
		SpellInfo reviveBattlePetSpellInfo = Global.SpellMgr.GetSpellInfo(SharedConst.SpellReviveBattlePets, Difficulty.None);

		if (target.GetSession().GetBattlePetMgr().IsBattlePetSystemEnabled())
		{
			TimeSpan expectedCooldown  = TimeSpan.FromMilliseconds(GetAura().GetMaxDuration());
			TimeSpan remainingCooldown = target.GetSpellHistory().GetRemainingCategoryCooldown(reviveBattlePetSpellInfo);

			if (remainingCooldown > TimeSpan.Zero)
			{
				if (remainingCooldown < expectedCooldown)
					target.GetSpellHistory().ModifyCooldown(reviveBattlePetSpellInfo, expectedCooldown - remainingCooldown);
			}
			else
			{
				target.GetSpellHistory().StartCooldown(reviveBattlePetSpellInfo, 0, null, false, expectedCooldown);
			}
		}
	}
}
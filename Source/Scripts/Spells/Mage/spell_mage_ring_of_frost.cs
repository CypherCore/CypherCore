using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 136511 - Ring of Frost
internal class spell_mage_ring_of_frost : AuraScript, IHasAuraEffects
{
	private ObjectGuid _ringOfFrostGUID;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.RingOfFrostSummon, MageSpells.RingOfFrostFreeze) && !Global.SpellMgr.GetSpellInfo(MageSpells.RingOfFrostSummon, Difficulty.None).GetEffects().Empty();
	}

	public override void Register()
	{
		AuraEffects.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.ProcTriggerSpell));
		AuraEffects.Add(new EffectApplyHandler(Apply, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		TempSummon ringOfFrost = GetRingOfFrostMinion();

		if (ringOfFrost)
			GetTarget().CastSpell(ringOfFrost.GetPosition(), MageSpells.RingOfFrostFreeze, new CastSpellExtraArgs(true));
	}

	private void Apply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		List<TempSummon> minions = new();
		GetTarget().GetAllMinionsByEntry(minions, (uint)Global.SpellMgr.GetSpellInfo(MageSpells.RingOfFrostSummon, GetCastDifficulty()).GetEffect(0).MiscValue);

		// Get the last summoned RoF, save it and despawn older ones
		foreach (var summon in minions)
		{
			TempSummon ringOfFrost = GetRingOfFrostMinion();

			if (ringOfFrost)
			{
				if (summon.GetTimer() > ringOfFrost.GetTimer())
				{
					ringOfFrost.DespawnOrUnsummon();
					_ringOfFrostGUID = summon.GetGUID();
				}
				else
				{
					summon.DespawnOrUnsummon();
				}
			}
			else
			{
				_ringOfFrostGUID = summon.GetGUID();
			}
		}
	}

	private TempSummon GetRingOfFrostMinion()
	{
		Creature creature = ObjectAccessor.GetCreature(GetOwner(), _ringOfFrostGUID);

		if (creature)
			return creature.ToTempSummon();

		return null;
	}
}
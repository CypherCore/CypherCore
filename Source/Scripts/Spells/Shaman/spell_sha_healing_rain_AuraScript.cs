using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 73920 - Healing Rain (Aura)
internal class spell_sha_healing_rain_AuraScript : AuraScript, IHasAuraEffects
{
	private ObjectGuid _visualDummy;
	private float _x;
	private float _y;
	private float _z;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public void SetVisualDummy(TempSummon summon)
	{
		_visualDummy = summon.GetGUID();
		summon.GetPosition(out _x, out _y, out _z);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffecRemoved, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 1, AuraType.PeriodicDummy));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		GetTarget().CastSpell(new Position(_x, _y, _z), ShamanSpells.HealingRainHeal, new CastSpellExtraArgs(aurEff));
	}

	private void HandleEffecRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var summon = ObjectAccessor.GetCreature(GetTarget(), _visualDummy);

		summon?.DespawnOrUnsummon();
	}
}
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(207407)]
public class spell_dh_artifact_soul_carver_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private void PeriodicTick(AuraEffect UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.CastSpell(caster, ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_MISSILE, SpellValueMod.BasePoint0, (int)ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDamage));
	}
}
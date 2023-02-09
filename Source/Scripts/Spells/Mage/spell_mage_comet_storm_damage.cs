using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 228601 - Comet Storm (Damage)
internal class spell_mage_comet_storm_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.CometStormDamage);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleEffectHitTarget(int effIndex)
	{
		GetCaster().CastSpell(GetHitDest(), MageSpells.CometStormDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetOriginalCastId(GetSpell().m_originalCastId));
	}
}
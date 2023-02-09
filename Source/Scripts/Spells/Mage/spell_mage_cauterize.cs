using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[Script] // 86949 - Cauterize
internal class spell_mage_cauterize : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(SuppressSpeedBuff, 2, SpellEffectName.TriggerSpell, SpellScriptHookType.Launch));
	}

	private void SuppressSpeedBuff(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);
	}
}
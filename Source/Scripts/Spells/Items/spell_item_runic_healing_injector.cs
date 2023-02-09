using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script] // 67489 - Runic Healing Injector
internal class spell_item_runic_healing_injector : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().IsPlayer();
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHeal(uint effIndex)
	{
		Player caster = GetCaster().ToPlayer();

		if (caster != null)
			if (caster.HasSkill(SkillType.Engineering))
				SetHitHeal((int)(GetHitHeal() * 1.25f));
	}
}
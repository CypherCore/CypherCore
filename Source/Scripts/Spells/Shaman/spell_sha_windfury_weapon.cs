using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 33757 - Windfury Weapon
internal class spell_sha_windfury_weapon : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.WindfuryEnchantment);
	}

	public override bool Load()
	{
		return GetCaster().IsPlayer();
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleEffect(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);

		Item mainHand = GetCaster().ToPlayer().GetWeaponForAttack(WeaponAttackType.BaseAttack, false);

		if (mainHand != null)
			GetCaster().CastSpell(mainHand, ShamanSpells.WindfuryEnchantment, GetSpell());
	}
}
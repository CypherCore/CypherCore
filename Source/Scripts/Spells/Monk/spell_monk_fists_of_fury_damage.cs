using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(MonkSpells.SPELL_MONK_FISTS_OF_FURY_DAMAGE)]
public class spell_monk_fists_of_fury_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDamage(uint UnnamedParameter)
	{
		if (!GetCaster())
			return;

		var l_Target = GetHitUnit();
		var l_Player = GetCaster().ToPlayer();

		if (l_Target == null || l_Player == null)
			return;

		var l_Damage = l_Player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 5.25f;
		l_Damage = l_Player.SpellDamageBonusDone(l_Target, GetSpellInfo(), l_Damage, DamageEffectType.Direct, GetSpellInfo().GetEffect(0));
		l_Damage = l_Target.SpellDamageBonusTaken(l_Player, GetSpellInfo(), l_Damage, DamageEffectType.Direct);

		SetHitDamage(l_Damage);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}
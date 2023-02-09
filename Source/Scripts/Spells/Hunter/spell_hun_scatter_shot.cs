using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[Script]
internal class spell_hun_scatter_shot : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		var caster = GetCaster().ToPlayer();
		// break auto Shot and varhit
		caster.InterruptSpell(CurrentSpellTypes.AutoRepeat);
		caster.AttackStop();
		caster.SendAttackSwingCancelAttack();
	}
}
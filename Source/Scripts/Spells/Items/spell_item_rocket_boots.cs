using System.Collections.Generic;
using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_rocket_boots : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public override bool Load()
	{
		return GetCaster().GetTypeId() == TypeId.Player;
	}

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(ItemSpellIds.RocketBootsProc);
	}

	public SpellCastResult CheckCast()
	{
		if (GetCaster().IsInWater())
			return SpellCastResult.OnlyAbovewater;

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(uint effIndex)
	{
		Player caster = GetCaster().ToPlayer();

		Battleground bg = caster.GetBattleground();

		if (bg)
			bg.EventPlayerDroppedFlag(caster);

		caster.GetSpellHistory().ResetCooldown(ItemSpellIds.RocketBootsProc);
		caster.CastSpell(caster, ItemSpellIds.RocketBootsProc, true);
	}
}
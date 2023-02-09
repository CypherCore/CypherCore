using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// 198337 - Charge Effect (dropping Blazing Trail)
	[Script] // 218104 - Charge Effect
	internal class spell_warr_charge_effect : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarriorSpells.CHARGE_PAUSE_RAGE_DECAY, WarriorSpells.CHARGE_ROOT_EFFECT, WarriorSpells.CHARGE_SLOW_EFFECT);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleCharge, 0, SpellEffectName.Charge, SpellScriptHookType.LaunchTarget));
		}

		private void HandleCharge(uint effIndex)
		{
			var caster = GetCaster();
			var target = GetHitUnit();
			caster.CastSpell(caster, WarriorSpells.CHARGE_PAUSE_RAGE_DECAY, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, 0));
			caster.CastSpell(target, WarriorSpells.CHARGE_ROOT_EFFECT, true);
			caster.CastSpell(target, WarriorSpells.CHARGE_SLOW_EFFECT, true);
		}
	}
}
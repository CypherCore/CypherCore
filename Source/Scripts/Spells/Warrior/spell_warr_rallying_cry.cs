using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	[Script] // 97462 - Rallying Cry
	internal class spell_warr_rallying_cry : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarriorSpells.RALLYING_CRY);
		}

		public override bool Load()
		{
			return GetCaster().IsTypeId(TypeId.Player);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}

		private void HandleScript(int effIndex)
		{
			CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)GetHitUnit().CountPctFromMaxHealth(GetEffectValue()));

			GetCaster().CastSpell(GetHitUnit(), WarriorSpells.RALLYING_CRY, args);
		}
	}
}
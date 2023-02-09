using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(new uint[]
	             {
		             67518, 19505
	             })] // 67518, 19505 - Devour Magic
	internal class spell_warl_devour_magic : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarlockSpells.GLYPH_OF_DEMON_TRAINING, WarlockSpells.DEVOUR_MAGIC_HEAL) && spellInfo.GetEffects().Count > 1;
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(OnSuccessfulDispel, 0, SpellEffectName.Dispel, SpellScriptHookType.EffectSuccessfulDispel));
		}

		private void OnSuccessfulDispel(uint effIndex)
		{
			var                caster = GetCaster();
			CastSpellExtraArgs args   = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.BasePoint0, GetEffectInfo(1).CalcValue(caster));

			caster.CastSpell(caster, WarlockSpells.DEVOUR_MAGIC_HEAL, args);

			// Glyph of Felhunter
			var owner = caster.GetOwner();

			if (owner)
				if (owner.GetAura(WarlockSpells.GLYPH_OF_DEMON_TRAINING) != null)
					owner.CastSpell(owner, WarlockSpells.DEVOUR_MAGIC_HEAL, args);
		}
	}
}
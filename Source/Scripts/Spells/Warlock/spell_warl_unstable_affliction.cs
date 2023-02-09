using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(new uint[]
	             {
		             30108, 34438, 34439, 35183
	             })] // 30108, 34438, 34439, 35183 - Unstable Affliction
	internal class spell_warl_unstable_affliction : AuraScript, IAfterAuraDispel
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarlockSpells.UNSTABLE_AFFLICTION_DISPEL);
		}

		public void HandleDispel(DispelInfo dispelInfo)
		{
			var caster = GetCaster();

			if (caster)
			{
				var aurEff = GetEffect(1);

				if (aurEff != null)
				{
					// backfire Damage and silence
					CastSpellExtraArgs args = new(aurEff);
					args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount() * 9);
					caster.CastSpell(dispelInfo.GetDispeller(), WarlockSpells.UNSTABLE_AFFLICTION_DISPEL, args);
				}
			}
		}
	}
}
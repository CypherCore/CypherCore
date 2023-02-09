using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 106898 - Stampeding Roar
	internal class spell_dru_stampeding_roar : SpellScript, ISpellBeforeCast
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.BearForm);
		}

		public void BeforeCast()
		{
			// Change into cat form
			if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.BearForm)
				GetCaster().CastSpell(GetCaster(), DruidSpellIds.BearForm, true);
		}
	}
}
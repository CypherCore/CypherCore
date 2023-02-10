using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid
{
	[Script] // 52610 - Savage Roar
	internal class spell_dru_savage_roar : SpellScript, ISpellCheckCast
	{
		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();

			if (caster.GetShapeshiftForm() != ShapeShiftForm.CatForm)
				return SpellCastResult.OnlyShapeshift;

			return SpellCastResult.SpellCastOk;
		}
	}
}
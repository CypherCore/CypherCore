using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 783 - Travel Form (dummy)
	internal class spell_dru_travel_form_dummy : SpellScript, ISpellCheckCast
	{
		public override bool Validate(SpellInfo spellEntry)
		{
			return ValidateSpellInfo(DruidSpellIds.FormAquaticPassive, DruidSpellIds.FormAquatic, DruidSpellIds.FormStag);
		}

		public SpellCastResult CheckCast()
		{
			var player = GetCaster().ToPlayer();

			if (!player)
				return SpellCastResult.CustomError;

			var spellId = (player.HasSpell(DruidSpellIds.FormAquaticPassive) && player.IsInWater()) ? DruidSpellIds.FormAquatic : DruidSpellIds.FormStag;

			var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, GetCastDifficulty());

			return spellInfo.CheckLocation(player.GetMapId(), player.GetZoneId(), player.GetAreaId(), player);
		}
	}
}
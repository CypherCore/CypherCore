// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	// 1066 - Aquatic Form
	// 33943 - Flight Form
	// 40120 - Swift Flight Form
	[Script] // 165961 - Stag Form
	internal class spell_dru_travel_form_AuraScript : AuraScript, IHasAuraEffects
	{
		private uint triggeredSpellId;
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.FormStag, DruidSpellIds.FormAquaticPassive, DruidSpellIds.FormAquatic, DruidSpellIds.FormFlight, DruidSpellIds.FormSwiftFlight);
		}

		public override bool Load()
		{
			return GetCaster().IsTypeId(TypeId.Player);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
			AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		}

		public static uint GetFormSpellId(Player player, Difficulty difficulty, bool requiresOutdoor)
		{
			// Check what form is appropriate
			if (player.HasSpell(DruidSpellIds.FormAquaticPassive) &&
			    player.IsInWater()) // Aquatic form
				return DruidSpellIds.FormAquatic;

			if (!player.IsInCombat() &&
			    player.GetSkillValue(SkillType.Riding) >= 225 &&
			    CheckLocationForForm(player, difficulty, requiresOutdoor, DruidSpellIds.FormFlight) == SpellCastResult.SpellCastOk) // Flight form
				return player.GetSkillValue(SkillType.Riding) >= 300 ? DruidSpellIds.FormSwiftFlight : DruidSpellIds.FormFlight;

			if (!player.IsInWater() &&
			    CheckLocationForForm(player, difficulty, requiresOutdoor, DruidSpellIds.FormStag) == SpellCastResult.SpellCastOk) // Stag form
				return DruidSpellIds.FormStag;

			return 0;
		}

		private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			// If it stays 0, it removes Travel Form dummy in AfterRemove.
			triggeredSpellId = 0;

			// We should only handle aura interrupts.
			if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Interrupt)
				return;

			// Check what form is appropriate
			triggeredSpellId = GetFormSpellId(GetTarget().ToPlayer(), GetCastDifficulty(), true);

			// If chosen form is current aura, just don't remove it.
			if (triggeredSpellId == ScriptSpellId)
				PreventDefaultAction();
		}

		private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			if (triggeredSpellId == ScriptSpellId)
				return;

			var player = GetTarget().ToPlayer();

			if (triggeredSpellId != 0) // Apply new form
				player.CastSpell(player, triggeredSpellId, new CastSpellExtraArgs(aurEff));
			else // If not set, simply remove Travel Form dummy
				player.RemoveAura(DruidSpellIds.TravelForm);
		}

		private static SpellCastResult CheckLocationForForm(Player targetPlayer, Difficulty difficulty, bool requireOutdoors, uint spell_id)
		{
			var spellInfo = Global.SpellMgr.GetSpellInfo(spell_id, difficulty);

			if (requireOutdoors && !targetPlayer.IsOutdoors())
				return SpellCastResult.OnlyOutdoors;

			return spellInfo.CheckLocation(targetPlayer.GetMapId(), targetPlayer.GetZoneId(), targetPlayer.GetAreaId(), targetPlayer);
		}
	}
}
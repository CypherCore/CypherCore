// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 51010 - Dire Brew
internal class spell_item_dire_brew : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
	}

	private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = GetTarget();

		uint model    = 0;
		var  gender   = target.GetGender();
		var  chrClass = CliDB.ChrClassesStorage.LookupByKey(target.GetClass());

		if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Plate)) != 0)
			model = gender == Gender.Male ? ModelIds.ClassPlateMale : ModelIds.ClassPlateFemale;
		else if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Mail)) != 0)
			model = gender == Gender.Male ? ModelIds.ClassMailMale : ModelIds.ClassMailFemale;
		else if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Leather)) != 0)
			model = gender == Gender.Male ? ModelIds.ClassLeatherMale : ModelIds.ClassLeatherFemale;
		else if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Cloth)) != 0)
			model = gender == Gender.Male ? ModelIds.ClassClothMale : ModelIds.ClassClothFemale;

		if (model != 0)
			target.SetDisplayId(model);
	}
}
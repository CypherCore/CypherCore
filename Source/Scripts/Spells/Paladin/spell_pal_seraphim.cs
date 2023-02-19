// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;
using Game.DataStorage;

namespace Scripts.Spells.Paladin
{
    // 152262 - Seraphim
    [SpellScript(152262)]
    public class spell_pal_seraphim : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(PaladinSpells.SERAPHIM, PaladinSpells.SHIELD_OF_THE_RIGHTEOUS);
        }

        public SpellCastResult CheckCast()
        {
            uint ChargeCategoryId = Global.SpellMgr.GetSpellInfo(PaladinSpells.SHIELD_OF_THE_RIGHTEOUS, Difficulty.None).ChargeCategoryId;
            
            if (!GetCaster().GetSpellHistory().HasCharge(ChargeCategoryId))
            {
                return SpellCastResult.NoPower;
            }

            return SpellCastResult.Success;
        }

        private void HandleDummy(int effIndex)
        {
            uint ChargeCategoryId = Global.SpellMgr.GetSpellInfo(PaladinSpells.SHIELD_OF_THE_RIGHTEOUS, Difficulty.None).ChargeCategoryId;
            SpellHistory spellHistory = GetCaster().GetSpellHistory();

            spellHistory.ConsumeCharge(ChargeCategoryId);
            spellHistory.ForceSendSpellCharge(CliDB.SpellCategoryStorage.LookupByKey(ChargeCategoryId));
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }
}

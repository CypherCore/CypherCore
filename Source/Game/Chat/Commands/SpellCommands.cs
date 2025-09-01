// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Spells;
using System.Collections.Generic;

namespace Game.Chat
{
    class SpellCommands
    {
        [CommandNonGroup("cooldown", RBACPermissions.CommandCooldown)]
        static bool HandleCooldownCommand(CommandHandler handler, OptionalArg<SpellInfo> spellIdArg)
        {
            Unit target = handler.GetSelectedUnit();
            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            Player owner = target.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (owner == null)
            {
                owner = handler.GetSession().GetPlayer();
                target = owner;
            }

            string nameLink = handler.GetNameLink(owner);
            if (!spellIdArg.HasValue)
            {
                target.GetSpellHistory().ResetAllCooldowns();
                target.GetSpellHistory().ResetAllCharges();
                handler.SendSysMessage(CypherStrings.RemoveallCooldown, nameLink);
            }
            else
            {
                if (spellIdArg.Value == null)
                {
                    handler.SendSysMessage(CypherStrings.UnknownSpell, owner == handler.GetSession().GetPlayer() ? handler.GetCypherString(CypherStrings.You) : nameLink);
                    return false;
                }

                target.GetSpellHistory().ResetCooldown(spellIdArg.Value.Id, true);
                target.GetSpellHistory().ResetCharges(spellIdArg.Value.ChargeCategoryId);
                handler.SendSysMessage(CypherStrings.RemoveallCooldown, spellIdArg.Value.Id, owner == handler.GetSession().GetPlayer() ? handler.GetCypherString(CypherStrings.You) : nameLink);
            }
            return true;
        }

        [CommandNonGroup("aura", RBACPermissions.CommandAura)]
        static bool HandleAuraCommand(CommandHandler handler, uint spellId)
        {
            Unit target = handler.GetSelectedUnit();
            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

                return false;
            }

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, target.GetMap().GetDifficultyID());
            if (spellInfo == null)
                return false;

            ObjectGuid castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, target.GetMapId(), spellId, target.GetMap().GenerateLowGuid(HighGuid.Cast));
            AuraCreateInfo createInfo = new(castId, spellInfo, target.GetMap().GetDifficultyID(), SpellConst.MaxEffectMask, target);
            createInfo.SetCaster(target);

            Aura.TryRefreshStackOrCreate(createInfo);

            return true;
        }

        [CommandNonGroup("unaura", RBACPermissions.CommandUnaura)]
        static bool HandleUnAuraCommand(CommandHandler handler, uint spellId = 0)
        {
            Unit target = handler.GetSelectedUnit();
            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

                return false;
            }

            if (spellId == 0)
            {
                target.RemoveAllAuras();
                return true;
            }

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            if (spellInfo != null)
            {
                target.RemoveAurasDueToSpell(spellInfo.Id);
                return true;
            }

            return true;
        }

        [CommandNonGroup("setskill", RBACPermissions.CommandSetskill)]
        static bool HandleSetSkillCommand(CommandHandler handler, VariantArg<SkillLinkData, uint> skillId, uint level, OptionalArg<uint> maxSkillArg)
        {
            Player target = handler.GetSelectedPlayerOrSelf();
            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            SkillLineRecord skillLine = CliDB.SkillLineStorage.LookupByKey(skillId);
            if (skillLine == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidSkillId, skillId);
                return false;
            }

            bool targetHasSkill = target.GetSkillValue((SkillType)skillId.GetValue()) != 0;

            // If our target does not yet have the skill they are trying to add to them, the chosen level also becomes
            // the max level of the new profession.
            ushort max = (ushort)maxSkillArg.GetValueOrDefault(targetHasSkill ? target.GetPureMaxSkillValue((SkillType)skillId.GetValue()) : level);

            if (level == 0 || level > max)
                return false;

            // If the player has the skill, we get the current skill step. If they don't have the skill, we
            // add the skill to the player's book with step 1 (which is the first rank, in most cases something
            // like 'Apprentice <skill>'.
            target.SetSkill((SkillType)skillId.GetValue(), (uint)(targetHasSkill ? target.GetSkillStep((SkillType)skillId.GetValue()) : 1), level, max);
            handler.SendSysMessage(CypherStrings.SetSkill, skillId, skillLine.DisplayName[handler.GetSessionDbcLocale()], handler.GetNameLink(target), level, max);
            return true;
        }
    }
}

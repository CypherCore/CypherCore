/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Spells;
using System.Collections.Generic;

namespace Game.Chat
{
    internal class SpellCommands
    {
        [CommandNonGroup("cooldown", RBACPermissions.CommandCooldown)]
        private static bool HandleCooldownCommand(StringArguments args, CommandHandler handler)
        {
            var target = handler.GetSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            var owner = target.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (!owner)
            {
                owner = handler.GetSession().GetPlayer();
                target = owner;
            }

            var nameLink = handler.GetNameLink(owner);
            if (args.Empty())
            {
                target.GetSpellHistory().ResetAllCooldowns();
                target.GetSpellHistory().ResetAllCharges();
                handler.SendSysMessage(CypherStrings.RemoveallCooldown, nameLink);
            }
            else
            {
                // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
                var spellIid = handler.ExtractSpellIdFromLink(args);
                if (spellIid == 0)
                    return false;

                var spellInfo = Global.SpellMgr.GetSpellInfo(spellIid, target.GetMap().GetDifficultyID());
                if (spellInfo == null)
                {
                    handler.SendSysMessage(CypherStrings.UnknownSpell, owner == handler.GetSession().GetPlayer() ? handler.GetCypherString(CypherStrings.You) : nameLink);
                    return false;
                }

                target.GetSpellHistory().ResetCooldown(spellIid, true);
                target.GetSpellHistory().ResetCharges(spellInfo.ChargeCategoryId);
                handler.SendSysMessage(CypherStrings.RemoveallCooldown, spellIid, owner == handler.GetSession().GetPlayer() ? handler.GetCypherString(CypherStrings.You) : nameLink);
            }
            return true;
        }

        [CommandNonGroup("aura", RBACPermissions.CommandAura)]
        private static bool HandleAuraCommand(StringArguments args, CommandHandler handler)
        {
            var target = handler.GetSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

                return false;
            }

            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
            var spellId = handler.ExtractSpellIdFromLink(args);

            var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, target.GetMap().GetDifficultyID());
            if (spellInfo != null)
            {
                var castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, target.GetMapId(), spellId, target.GetMap().GenerateLowGuid(HighGuid.Cast));
                Aura.TryRefreshStackOrCreate(spellInfo, castId, SpellConst.MaxEffectMask, target, target, target.GetMap().GetDifficultyID());
            }

            return true;
        }

        [CommandNonGroup("unaura", RBACPermissions.CommandUnaura)]
        private static bool UnAura(StringArguments args, CommandHandler handler)
        {
            var target = handler.GetSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

                return false;
            }

            var argstr = args.NextString();
            if (argstr == "all")
            {
                target.RemoveAllAuras();
                return true;
            }

            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
            var spellId = handler.ExtractSpellIdFromLink(args);
            if (spellId == 0)
                return false;

            target.RemoveAurasDueToSpell(spellId);

            return true;
        }

        [CommandNonGroup("setskill", RBACPermissions.CommandSetskill)]
        private static bool SetSkill(StringArguments args, CommandHandler handler)
        {
            // number or [name] Shift-click form |color|Hskill:skill_id|h[name]|h|r
            var skillStr = handler.ExtractKeyFromLink(args, "Hskill");
            if (string.IsNullOrEmpty(skillStr))
                return false;

            if (!uint.TryParse(skillStr, out var skill) || skill == 0)
            {
                handler.SendSysMessage(CypherStrings.InvalidSkillId, skill);
                return false;
            }

            var level = args.NextUInt32();
            if (level == 0)
                return false;

            var target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            var skillLine = CliDB.SkillLineStorage.LookupByKey(skill);
            if (skillLine == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidSkillId, skill);
                return false;
            }

            var targetHasSkill = target.GetSkillValue((SkillType)skill) != 0;

            var maxPureSkill = args.NextUInt16();
            // If our target does not yet have the skill they are trying to add to them, the chosen level also becomes
            // the max level of the new profession.
            var max = maxPureSkill != 0 ? maxPureSkill : targetHasSkill ? target.GetPureMaxSkillValue((SkillType)skill) : (ushort)level;

            if (level == 0 || level > max || max <= 0)
                return false;

            // If the player has the skill, we get the current skill step. If they don't have the skill, we
            // add the skill to the player's book with step 1 (which is the first rank, in most cases something
            // like 'Apprentice <skill>'.
            target.SetSkill((SkillType)skill, (uint)(targetHasSkill ? target.GetSkillStep((SkillType)skill) : 1), level, max);
            handler.SendSysMessage(CypherStrings.SetSkill, skill, skillLine.DisplayName[handler.GetSessionDbcLocale()], handler.GetNameLink(target), level, max);
            return true;
        }
    }
}

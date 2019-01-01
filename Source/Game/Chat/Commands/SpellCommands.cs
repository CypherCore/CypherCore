/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
    class SpellCommands
    {
        [CommandNonGroup("cooldown", RBACPermissions.CommandCooldown)]
        static bool HandleCooldownCommand(StringArguments args, CommandHandler handler)
        {
            Unit target = handler.getSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            Player owner = target.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (!owner)
            {
                owner = handler.GetSession().GetPlayer();
                target = owner;
            }

            string nameLink = handler.GetNameLink(owner);
            if (args.Empty())
            {
                target.GetSpellHistory().ResetAllCooldowns();
                target.GetSpellHistory().ResetAllCharges();
                handler.SendSysMessage(CypherStrings.RemoveallCooldown, nameLink);
            }
            else
            {
                // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
                uint spellIid = handler.extractSpellIdFromLink(args);
                if (spellIid == 0)
                    return false;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellIid);
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
        static bool Auracommand(StringArguments args, CommandHandler handler)
        {
            Unit target = handler.getSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

                return false;
            }

            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
            uint spellId = handler.extractSpellIdFromLink(args);

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            if (spellInfo != null)
            {
                ObjectGuid castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, target.GetMapId(), spellId, target.GetMap().GenerateLowGuid(HighGuid.Cast));
                Aura.TryRefreshStackOrCreate(spellInfo, castId, SpellConst.MaxEffectMask, target, target);
            }

            return true;
        }

        [CommandNonGroup("unaura", RBACPermissions.CommandUnaura)]
        static bool UnAura(StringArguments args, CommandHandler handler)
        {
            Unit target = handler.getSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

                return false;
            }

            string argstr = args.NextString();
            if (argstr == "all")
            {
                target.RemoveAllAuras();
                return true;
            }

            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
            uint spellId = handler.extractSpellIdFromLink(args);
            if (spellId == 0)
                return false;

            target.RemoveAurasDueToSpell(spellId);

            return true;
        }

        [CommandNonGroup("maxskill", RBACPermissions.CommandMaxskill)]
        static bool HandleMaxSkillCommand(StringArguments args, CommandHandler handler)
        {
            Player player = handler.getSelectedPlayerOrSelf();
            if (!player)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // each skills that have max skill value dependent from level seted to current level max skill value
            player.UpdateSkillsToMaxSkillsForLevel();
            return true;
        }

        [CommandNonGroup("setskill", RBACPermissions.CommandSetskill)]
        static bool SetSkill(StringArguments args, CommandHandler handler)
        {
            // number or [name] Shift-click form |color|Hskill:skill_id|h[name]|h|r
            string skillStr = handler.extractKeyFromLink(args, "Hskill");
            if (string.IsNullOrEmpty(skillStr))
                return false;

            if (!uint.TryParse(skillStr, out uint skill) || skill == 0)
            {
                handler.SendSysMessage(CypherStrings.InvalidSkillId, skill);
                return false;
            }

            uint level = args.NextUInt32();
            if (level == 0)
                return false;

            Player target = handler.getSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            SkillLineRecord skillLine = CliDB.SkillLineStorage.LookupByKey(skill);
            if (skillLine == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidSkillId, skill);
                return false;
            }

            bool targetHasSkill = target.GetSkillValue((SkillType)skill) != 0;

            ushort maxPureSkill = args.NextUInt16();
            // If our target does not yet have the skill they are trying to add to them, the chosen level also becomes
            // the max level of the new profession.
            ushort max = maxPureSkill != 0 ? maxPureSkill : targetHasSkill ? target.GetPureMaxSkillValue((SkillType)skill) : (ushort)level;

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

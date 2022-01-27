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
using Game.Entities;
using Game.Spells;
using System;

namespace Game.Chat
{
    [CommandGroup("cast", RBACPermissions.CommandCast)]
    class CastCommands
    {
        [Command("", RBACPermissions.CommandCast)]
        static bool HandleCastCommand(CommandHandler handler, SpellInfo spell, string triggeredStr)
        {
            Unit target = handler.GetSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            if (!CheckSpellExistsAndIsValid(handler, spell))
                return false;

            TriggerCastFlags triggerFlags = TriggerCastFlags.None;
            if (!triggeredStr.IsEmpty())
            {
                if ("triggered".Contains(triggeredStr)) // check if "triggered" starts with *triggeredStr (e.g. "trig", "trigger", etc.)
                    triggerFlags = TriggerCastFlags.FullDebugMask;
                else
                    return false;
            }

            handler.GetSession().GetPlayer().CastSpell(target, spell.Id, new CastSpellExtraArgs(triggerFlags));
            return true;
        }

        [Command("back", RBACPermissions.CommandCastBack)]
        static bool HandleCastBackCommand(CommandHandler handler, StringArguments args)
        {
            Creature caster = handler.GetSelectedCreature();
            if (!caster)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
            uint spellId = handler.ExtractSpellIdFromLink(args);
            if (spellId == 0)
                return false;

            if (CheckSpellExistsAndIsValid(handler, spellId))
                return false;

            string triggeredStr = args.NextString();
            if (!string.IsNullOrEmpty(triggeredStr))
            {
                if (triggeredStr != "triggered")
                    return false;
            }

            bool triggered = (triggeredStr != null);

            caster.CastSpell(handler.GetSession().GetPlayer(), spellId, triggered);

            return true;
        }

        [Command("dist", RBACPermissions.CommandCastDist)]
        static bool HandleCastDistCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
            uint spellId = handler.ExtractSpellIdFromLink(args);
            if (spellId == 0)
                return false;

            if (CheckSpellExistsAndIsValid(handler, spellId))
                return false;

            float dist = args.NextSingle();

            string triggeredStr = args.NextString();
            if (!string.IsNullOrEmpty(triggeredStr))
            {
                if (triggeredStr != "triggered")
                    return false;
            }

            bool triggered = (triggeredStr != null);

            float x, y, z;
            handler.GetSession().GetPlayer().GetClosePoint(out x, out y, out z, dist);

            handler.GetSession().GetPlayer().CastSpell(new Position(x, y, z), spellId, new CastSpellExtraArgs(triggered));

            return true;
        }

        [Command("self", RBACPermissions.CommandCastSelf)]
        static bool HandleCastSelfCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            Unit target = handler.GetSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
            uint spellId = handler.ExtractSpellIdFromLink(args);
            if (spellId == 0)
                return false;

            if (!CheckSpellExistsAndIsValid(handler, spellId))
                return false;

            target.CastSpell(target, spellId, false);

            return true;
        }

        [Command("target", RBACPermissions.CommandCastTarget)]
        static bool HandleCastTargetCommad(CommandHandler handler, StringArguments args)
        {
            Creature caster = handler.GetSelectedCreature();
            if (!caster)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            if (!caster.GetVictim())
            {
                handler.SendSysMessage(CypherStrings.SelectedTargetNotHaveVictim);
                return false;
            }

            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
            uint spellId = handler.ExtractSpellIdFromLink(args);
            if (spellId == 0)
                return false;

            if (CheckSpellExistsAndIsValid(handler, spellId))
                return false;

            string triggeredStr = args.NextString();
            if (!string.IsNullOrEmpty(triggeredStr))
            {
                if (triggeredStr != "triggered")
                    return false;
            }

            bool triggered = (triggeredStr != null);

            caster.CastSpell(caster.GetVictim(), spellId, triggered);

            return true;
        }

        [Command("dest", RBACPermissions.CommandCastDest)]
        static bool HandleCastDestCommand(CommandHandler handler, StringArguments args)
        {
            Unit caster = handler.GetSelectedUnit();
            if (!caster)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
            uint spellId = handler.ExtractSpellIdFromLink(args);
            if (spellId == 0)
                return false;

            if (CheckSpellExistsAndIsValid(handler, spellId))
                return false;

            float x = args.NextSingle();
            float y = args.NextSingle();
            float z = args.NextSingle();

            if (x == 0f || y == 0f || z == 0f)
                return false;

            string triggeredStr = args.NextString();
            if (!string.IsNullOrEmpty(triggeredStr))
            {
                if (triggeredStr != "triggered")
                    return false;
            }

            bool triggered = (triggeredStr != null);

            caster.CastSpell(new Position(x, y, z), spellId, new CastSpellExtraArgs(triggered));

            return true;
        }

        static bool CheckSpellExistsAndIsValid(CommandHandler handler, uint spellId)
        {
            return CheckSpellExistsAndIsValid(handler, Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None));
        }
        
        static bool CheckSpellExistsAndIsValid(CommandHandler handler, SpellInfo spellInfo)
        {
            if (spellInfo == null)
            {
                handler.SendSysMessage(CypherStrings.CommandNospellfound);
                return false;
            }

            if (!Global.SpellMgr.IsSpellValid(spellInfo, handler.GetPlayer()))
            {
                handler.SendSysMessage(CypherStrings.CommandSpellBroken, spellInfo.Id);
                return false;
            }
            return true;
        }
    }
}

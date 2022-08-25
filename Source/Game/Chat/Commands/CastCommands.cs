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
    [CommandGroup("cast")]
    class CastCommands
    {
        [Command("", RBACPermissions.CommandCast)]
        static bool HandleCastCommand(CommandHandler handler, uint spellId, [OptionalArg] string triggeredStr)
        {
            Unit target = handler.GetSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            if (!CheckSpellExistsAndIsValid(handler, spellId))
                return false;

            TriggerCastFlags? triggerFlags = GetTriggerFlags(triggeredStr);
            if (!triggerFlags.HasValue)
                return false;

            handler.GetSession().GetPlayer().CastSpell(target, spellId, new CastSpellExtraArgs(triggerFlags.Value));
            return true;
        }

        [Command("back", RBACPermissions.CommandCastBack)]
        static bool HandleCastBackCommand(CommandHandler handler, uint spellId, [OptionalArg] string triggeredStr)
        {
            Creature caster = handler.GetSelectedCreature();
            if (!caster)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            if (CheckSpellExistsAndIsValid(handler, spellId))
                return false;

            TriggerCastFlags? triggerFlags = GetTriggerFlags(triggeredStr);
            if (!triggerFlags.HasValue)
                return false;

            caster.CastSpell(handler.GetSession().GetPlayer(), spellId, new CastSpellExtraArgs(triggerFlags.Value));

            return true;
        }

        [Command("dist", RBACPermissions.CommandCastDist)]
        static bool HandleCastDistCommand(CommandHandler handler, uint spellId, float dist, [OptionalArg] string triggeredStr)
        {
            if (CheckSpellExistsAndIsValid(handler, spellId))
                return false;

            TriggerCastFlags? triggerFlags = GetTriggerFlags(triggeredStr);
            if (!triggerFlags.HasValue)
                return false;

            float x, y, z;
            handler.GetSession().GetPlayer().GetClosePoint(out x, out y, out z, dist);

            handler.GetSession().GetPlayer().CastSpell(new Position(x, y, z), spellId, new CastSpellExtraArgs(triggerFlags.Value));

            return true;
        }

        [Command("self", RBACPermissions.CommandCastSelf)]
        static bool HandleCastSelfCommand(CommandHandler handler, uint spellId, [OptionalArg] string triggeredStr)
        {
            Unit target = handler.GetSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            if (!CheckSpellExistsAndIsValid(handler, spellId))
                return false;

            TriggerCastFlags? triggerFlags = GetTriggerFlags(triggeredStr);
            if (!triggerFlags.HasValue)
                return false;

            target.CastSpell(target, spellId, new CastSpellExtraArgs(triggerFlags.Value));

            return true;
        }

        [Command("target", RBACPermissions.CommandCastTarget)]
        static bool HandleCastTargetCommad(CommandHandler handler, uint spellId, [OptionalArg] string triggeredStr)
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

            if (CheckSpellExistsAndIsValid(handler, spellId))
                return false;

            TriggerCastFlags? triggerFlags = GetTriggerFlags(triggeredStr);
            if (!triggerFlags.HasValue)
                return false;

            caster.CastSpell(caster.GetVictim(), spellId, new CastSpellExtraArgs(triggerFlags.Value));

            return true;
        }

        [Command("dest", RBACPermissions.CommandCastDest)]
        static bool HandleCastDestCommand(CommandHandler handler, uint spellId, float x, float y, float z, [OptionalArg] string triggeredStr)
        {
            Unit caster = handler.GetSelectedUnit();
            if (!caster)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            if (CheckSpellExistsAndIsValid(handler, spellId))
                return false;

            TriggerCastFlags? triggerFlags = GetTriggerFlags(triggeredStr);
            if (!triggerFlags.HasValue)
                return false;

            caster.CastSpell(new Position(x, y, z), spellId, new CastSpellExtraArgs(triggerFlags.Value));

            return true;
        }

        static TriggerCastFlags? GetTriggerFlags(string triggeredStr)
        {
            if (!triggeredStr.IsEmpty())
            {
                if (triggeredStr.StartsWith("triggered")) // check if "triggered" starts with *triggeredStr (e.g. "trig", "trigger", etc.)
                    return TriggerCastFlags.FullDebugMask;
                else
                    return null;
            }
            return TriggerCastFlags.None;
        }
        
        static bool CheckSpellExistsAndIsValid(CommandHandler handler, uint spellId)
        {
            var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
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

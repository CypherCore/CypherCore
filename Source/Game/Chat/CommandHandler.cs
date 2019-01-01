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

using Framework.Collections;
using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Chat
{
    public class CommandHandler
    {
        public CommandHandler(WorldSession session = null)
        {
            _session = session;
        }

        public bool ParseCommand(string fullCmd)
        {
            if (string.IsNullOrEmpty(fullCmd))
                return false;

            string text = fullCmd;
            // chat case (.command or !command format)
            if (_session != null)
            {
                if (text[0] != '!' && text[0] != '.')
                    return false;
            }

            if (text.Length < 2)
                return false;

            // ignore messages staring from many dots.
            if ((text[0] == '.' && text[1] == '.') || (text[0] == '!' && text[1] == '!'))
                return false;

            // skip first . or ! (in console allowed use command with . and ! and without its)
            if (text[0] == '!' || text[0] == '.')
                text = text.Substring(1);

            if (!ExecuteCommandInTable(CommandManager.GetCommands(), text, fullCmd))
            {
                if (_session != null && !_session.HasPermission(RBACPermissions.CommandsNotifyCommandNotFoundError))
                    return false;

                SendSysMessage(CypherStrings.NoCmd);
            }

            return true;
        }

        public bool ExecuteCommandInTable(ICollection<ChatCommand> table, string text, string fullcmd)
        {
            StringArguments args = new StringArguments(text);
            string cmd = args.NextString();

            foreach (var command in table)
            {
                //if (!command.Name.Equals(cmd))
                    //continue;

                if (!hasStringAbbr(command.Name, cmd))
                    continue;

                bool match = false;
                if (command.Name.Length > cmd.Length)
                {
                    foreach (var command2 in table)
                    {
                        if (!hasStringAbbr(command2.Name, cmd))
                            continue;

                        if (command2.Name.Equals(cmd))
                        {
                            match = true;
                            break;
                        }
                    }
                }
                if (match)
                    continue;

                if (!command.ChildCommands.Empty())
                {
                    string arg = args.NextString("");
                    if (!ExecuteCommandInTable(command.ChildCommands, arg, fullcmd))
                    {
                        if (_session != null && !_session.HasPermission(RBACPermissions.CommandsNotifyCommandNotFoundError))
                            return false;

                        if (!arg.IsEmpty())
                            SendSysMessage(CypherStrings.NoSubcmd);
                        else
                            SendSysMessage(CypherStrings.CmdSyntax);

                        ShowHelpForCommand(command.ChildCommands, arg);
                    }

                    return true;
                }

                // must be available and have handler
                if (command.Handler == null || !IsAvailable(command))
                    continue;

                _sentErrorMessage = false;
                if (command.Handler(new StringArguments(!command.Name.IsEmpty() ? args.NextString("") : text), this))
                {
                    if (GetSession() == null) // ignore console
                        return true;

                    Player player = GetPlayer();
                    if (!Global.AccountMgr.IsPlayerAccount(GetSession().GetSecurity()))
                    {
                        ObjectGuid guid = player.GetTarget();
                        uint areaId = player.GetAreaId();
                        string areaName = "Unknown";
                        string zoneName = "Unknown";

                        AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(areaId);
                        if (area != null)
                        {
                            var locale = GetSessionDbcLocale();
                            areaName = area.AreaName[locale];
                            AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
                            if (zone != null)
                                zoneName = zone.AreaName[locale];
                        }
                        
                        Log.outCommand(GetSession().GetAccountId(), "Command: {0} [Player: {1} ({2}) (Account: {3}) Postion: {4} Map: {5} ({6}) Area: {7} ({8}) Zone: {9} Selected: {10} ({11})]",
                            fullcmd, player.GetName(), player.GetGUID().ToString(), GetSession().GetAccountId(), player.GetPosition(), player.GetMapId(),
                            player.GetMap() ? player.GetMap().GetMapName() : "Unknown", areaId, areaName, zoneName, (player.GetSelectedUnit()) ? player.GetSelectedUnit().GetName() : "", guid.ToString());
                    }
                }
                else if (!HasSentErrorMessage())
                {
                    if (!command.Help.IsEmpty())
                        SendSysMessage(command.Help);
                    else
                        SendSysMessage(CypherStrings.CmdSyntax);
                }

                return true;
            }

            return false;
        }

        public bool ShowHelpForCommand(ICollection<ChatCommand> table, string text)
        {
            StringArguments args = new StringArguments(text);
            if (!args.Empty())
            {
                string cmd = args.NextString();
                foreach (var command in table)
                {
                    // must be available (ignore handler existence for show command with possible available subcommands)
                    if (!IsAvailable(command))
                        continue;

                    if (!hasStringAbbr(command.Name, cmd))
                        continue;

                    // have subcommand
                    string subcmd = !cmd.IsEmpty() ? args.NextString() : "";
                    if (!command.ChildCommands.Empty() && !subcmd.IsEmpty())
                    {
                        if (ShowHelpForCommand(command.ChildCommands, subcmd))
                            return true;
                    }

                    if (!command.Help.IsEmpty())
                        SendSysMessage(command.Help);

                    if (!command.ChildCommands.Empty())
                        if (ShowHelpForSubCommands(command.ChildCommands, command.Name, subcmd))
                            return true;

                    return !command.Help.IsEmpty();
                }
            }
            else
            {
                foreach (var command in table)
                {
                    // must be available (ignore handler existence for show command with possible available subcommands)
                    if (!IsAvailable(command))
                        continue;

                    if (!command.Name.IsEmpty())
                        continue;

                    if (!command.Help.IsEmpty())
                        SendSysMessage(command.Help);

                    if (!command.ChildCommands.Empty())
                        if (ShowHelpForSubCommands(command.ChildCommands, "", ""))
                            return true;

                    return !command.Help.IsEmpty();
                }
            }

            return ShowHelpForSubCommands(table, "", text);
        }

        public bool ShowHelpForSubCommands(ICollection<ChatCommand> table, string cmd, string subcmd)
        {
            string list = "";
            foreach (var command in table)
            {
                // must be available (ignore handler existence for show command with possible available subcommands)
                if (!IsAvailable(command))
                    continue;

                // for empty subcmd show all available
                if (!subcmd.IsEmpty() && !hasStringAbbr(command.Name, subcmd))
                    continue;

                if (GetSession() != null)
                    list += "\n    ";
                else
                    list += "\n\r    ";

                list += command.Name;

                if (!command.ChildCommands.Empty())
                    list += " ...";
            }

            if (list.IsEmpty())
                return false;

            if (table == CommandManager.GetCommands())
            {
                SendSysMessage(CypherStrings.AvailableCmd);
                SendSysMessage(list);
            }
            else
                SendSysMessage(CypherStrings.SubcmdsList, cmd, list);

            return true;
        }

        public virtual bool IsAvailable(ChatCommand cmd)
        {
            return HasPermission(cmd.Permission);
        }

        public virtual bool HasPermission(RBACPermissions permission) { return _session.HasPermission(permission); }

        public string extractKeyFromLink(StringArguments args, params string[] linkType)
        {
            int throwaway;
            return extractKeyFromLink(args, linkType, out throwaway);
        }
        public string extractKeyFromLink(StringArguments args, string[] linkType, out int found_idx)
        {
            string throwaway;
            return extractKeyFromLink(args, linkType, out found_idx, out throwaway);
        }
        public string extractKeyFromLink(StringArguments args, string[] linkType, out int found_idx, out string something1)
        {
            found_idx = 0;
            something1 = null;

            // skip empty
            if (args.Empty())
                return null;

            // return non link case
            if (args[0] != '|')
                return args.NextString();

            if (args[1] == 'c')
            {
                string check = args.NextString("|");
                if (string.IsNullOrEmpty(check))
                    return null;
            }
            else
                args.NextChar();

            string cLinkType = args.NextString(":");
            if (string.IsNullOrEmpty(cLinkType))
                return null;

            for (var i = 0; i < linkType.Length; ++i)
            {
                if (cLinkType == linkType[i])
                {
                    string cKey = args.NextString(":|");       // extract key

                    something1 = args.NextString(":|");      // extract something

                    args.NextString("]");                         // restart scan tail and skip name with possible spaces
                    args.NextString();                              // skip link tail (to allow continue strtok(NULL, s) use after return from function
                    found_idx = i;
                    return cKey;
                }
            }

            args.NextString();
            SendSysMessage(CypherStrings.WrongLinkType);
            return null;
        }

        public void extractOptFirstArg(StringArguments args, out string arg1, out string arg2)
        {
            string p1 = args.NextString();
            string p2 = args.NextString();

            if (string.IsNullOrEmpty(p2))
            {
                p2 = p1;
                p1 = null;
            }

            arg1 = p1;
            arg2 = p2;
        }

        public GameTele extractGameTeleFromLink(StringArguments args)
        {
            string cId = extractKeyFromLink(args, "Htele");
            if (string.IsNullOrEmpty(cId))
                return null;

            if (!uint.TryParse(cId, out uint id))
                return null;

            return Global.ObjectMgr.GetGameTele(id);
        }

        public string extractQuotedArg(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            if (!str.Contains("\""))
                return null;

            return str.Replace("\"", String.Empty);
        }
        string extractPlayerNameFromLink(StringArguments args)
        {
            // |color|Hplayer:name|h[name]|h|r
            string name = extractKeyFromLink(args, "Hplayer");
            if (name.IsEmpty())
                return "";

            if (!ObjectManager.NormalizePlayerName(ref name))
                return "";

            return name;
        }
        public bool extractPlayerTarget(StringArguments args, out Player player)
        {
            ObjectGuid guid;
            string name;
            return extractPlayerTarget(args, out player, out guid, out name);
        }
        public bool extractPlayerTarget(StringArguments args, out Player player, out ObjectGuid playerGuid)
        {
            string name;
            return extractPlayerTarget(args, out player, out playerGuid, out name);
        }

        public bool extractPlayerTarget(StringArguments args, out Player player, out ObjectGuid playerGuid, out string playerName)
        {
            player = null;
            playerGuid = ObjectGuid.Empty;
            playerName = "";

            if (!args.Empty())
            {
                string name = extractPlayerNameFromLink(args);
                if (string.IsNullOrEmpty(name))
                {
                    SendSysMessage(CypherStrings.PlayerNotFound);
                    _sentErrorMessage = true;
                    return false;
                }

                player = Global.ObjAccessor.FindPlayerByName(name);
                ObjectGuid guid = player == null ? ObjectManager.GetPlayerGUIDByName(name) : ObjectGuid.Empty;

                playerGuid = player != null ? player.GetGUID() : guid;
                playerName = player != null || !guid.IsEmpty() ? name : "";
            }
            else
            {
                player = getSelectedPlayer();
                playerGuid = player != null ? player.GetGUID() : ObjectGuid.Empty;
                playerName = player != null ? player.GetName() : "";
            }

            if (player == null && playerGuid.IsEmpty() && string.IsNullOrEmpty(playerName))
            {
                SendSysMessage(CypherStrings.PlayerNotFound);
                _sentErrorMessage = true;
                return false;
            }

            return true;
        }
        public ulong extractLowGuidFromLink(StringArguments args, ref HighGuid guidHigh)
        {
            int type;

            string[] guidKeys =
            {
                "Hplayer",
                "Hcreature",
                "Hgameobject"
            };
            // |color|Hcreature:creature_guid|h[name]|h|r
            // |color|Hgameobject:go_guid|h[name]|h|r
            // |color|Hplayer:name|h[name]|h|r
            string idS = extractKeyFromLink(args, guidKeys, out type);
            if (string.IsNullOrEmpty(idS))
                return 0;

            switch (type)
            {
                case 0:
                    {
                        guidHigh = HighGuid.Player;
                        if (!ObjectManager.NormalizePlayerName(ref idS))
                            return 0;

                        Player player = Global.ObjAccessor.FindPlayerByName(idS);
                        if (player)
                            return player.GetGUID().GetCounter();

                        ObjectGuid guid = ObjectManager.GetPlayerGUIDByName(idS);
                        if (guid.IsEmpty())
                            return 0;

                        return guid.GetCounter();
                    }
                case 1:
                    {
                        guidHigh = HighGuid.Creature;
                        if (!ulong.TryParse(idS, out ulong lowguid))
                            return 0;
                        return lowguid;
                    }
                case 2:
                    {
                        guidHigh = HighGuid.GameObject;
                        if (!ulong.TryParse(idS, out ulong lowguid))
                            return 0;
                        return lowguid;
                    }
            }

            // unknown type?
            return 0;
        }

        static string[] spellKeys =
        {
            "Hspell",                                               // normal spell
            "Htalent",                                              // talent spell
            "Henchant",                                             // enchanting recipe spell
            "Htrade",                                               // profession/skill spell
            "Hglyph",                                               // glyph
        };
        public uint extractSpellIdFromLink(StringArguments args)
        {
            // number or [name] Shift-click form |color|Henchant:recipe_spell_id|h[prof_name: recipe_name]|h|r
            // number or [name] Shift-click form |color|Hglyph:glyph_slot_id:glyph_prop_id|h[value]|h|r
            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r
            // number or [name] Shift-click form |color|Htalent:talent_id, rank|h[name]|h|r
            // number or [name] Shift-click form |color|Htrade:spell_id, skill_id, max_value, cur_value|h[name]|h|r
            int type = 0;
            string param1Str = null;
            string idS = extractKeyFromLink(args, spellKeys, out type, out param1Str);
            if (string.IsNullOrEmpty(idS))
                return 0;

            if (!uint.TryParse(idS, out uint id))
                return 0;               

            switch (type)
            {
                case 0:
                    return id;
                case 1:
                    {
                        // talent
                        TalentRecord talentEntry = CliDB.TalentStorage.LookupByKey(id);
                        if (talentEntry == null)
                            return 0;

                        return talentEntry.SpellID;
                    }
                case 2:
                case 3:
                    return id;
                case 4:
                    {
                        if (!uint.TryParse(param1Str, out uint glyph_prop_id))
                            glyph_prop_id = 0;

                        GlyphPropertiesRecord glyphPropEntry = CliDB.GlyphPropertiesStorage.LookupByKey(glyph_prop_id);
                        if (glyphPropEntry == null)
                            return 0;

                        return glyphPropEntry.SpellID;
                    }
            }

            // unknown type?
            return 0;
        }

        public Player getSelectedPlayer()
        {
            if (_session == null)
                return null;

            ObjectGuid selected = _session.GetPlayer().GetTarget();

            if (selected.IsEmpty())
                return _session.GetPlayer();

            return Global.ObjAccessor.FindPlayer(selected);
        }
        public Unit getSelectedUnit()
        {
            if (_session == null)
                return null;

            Unit selected = _session.GetPlayer().GetSelectedUnit();
            if (selected)
                return selected;

            return _session.GetPlayer();
        }
        public WorldObject GetSelectedObject()
        {
            if (_session == null)
                return null;

            ObjectGuid selected = _session.GetPlayer().GetTarget();

            if (selected.IsEmpty())
                return GetNearbyGameObject();

            return Global.ObjAccessor.GetUnit(_session.GetPlayer(), selected);
        }
        public Creature getSelectedCreature()
        {
            if (_session == null)
                return null;

            return ObjectAccessor.GetCreatureOrPetOrVehicle(_session.GetPlayer(), _session.GetPlayer().GetTarget());
        }
        public Player getSelectedPlayerOrSelf()
        {
            if (_session == null)
                return null;

            ObjectGuid selected = _session.GetPlayer().GetTarget();
            if (selected.IsEmpty())
                return _session.GetPlayer();

            // first try with selected target
            Player targetPlayer = Global.ObjAccessor.FindConnectedPlayer(selected);
            // if the target is not a player, then return self
            if (!targetPlayer)
                targetPlayer = _session.GetPlayer();

            return targetPlayer;
        }

        public GameObject GetObjectFromPlayerMapByDbGuid(ulong lowguid)
        {
            if (_session == null)
                return null;

            var bounds = _session.GetPlayer().GetMap().GetGameObjectBySpawnIdStore().LookupByKey(lowguid);
            if (!bounds.Empty())
                return bounds.First();

            return null;
        }

        public Creature GetCreatureFromPlayerMapByDbGuid(ulong lowguid)
        {
            if (!_session)
                return null;

            // Select the first alive creature or a dead one if not found
            Creature creature = null;
            var bounds = _session.GetPlayer().GetMap().GetCreatureBySpawnIdStore().LookupByKey(lowguid);
            foreach (var it in bounds)
            {
                creature = it;
                if (it.IsAlive())
                    break;
            }

            return creature;
        }
        GameObject GetNearbyGameObject()
        {
            if (_session == null)
                return null;

            Player pl = _session.GetPlayer();
            NearestGameObjectCheck check = new NearestGameObjectCheck(pl);
            GameObjectLastSearcher searcher = new GameObjectLastSearcher(pl, check);
            Cell.VisitGridObjects(pl, searcher, MapConst.SizeofGrids);
            return searcher.GetTarget();
        }

        public string playerLink(string name, bool console = false)
        {
            return console ? name : "|cffffffff|Hplayer:" + name + "|h[" + name + "]|h|r";
        }
        public virtual string GetNameLink()
        {
            return GetNameLink(_session.GetPlayer());
        }
        public string GetNameLink(Player obj)
        {
            return playerLink(obj.GetName());
        }
        public virtual bool needReportToTarget(Player chr)
        {
            Player pl = _session.GetPlayer();
            return pl != chr && pl.IsVisibleGloballyFor(chr);
        }
        public bool HasLowerSecurity(Player target, ObjectGuid guid, bool strong = false)
        {
            WorldSession target_session = null;
            uint target_account = 0;

            if (target != null)
                target_session = target.GetSession();
            else if (!guid.IsEmpty())
                target_account = ObjectManager.GetPlayerAccountIdByGUID(guid);

            if (target_session == null && target_account == 0)
            {
                SendSysMessage(CypherStrings.PlayerNotFound);
                _sentErrorMessage = true;
                return true;
            }

            return HasLowerSecurityAccount(target_session, target_account, strong);
        }
        public bool HasLowerSecurityAccount(WorldSession target, uint target_account, bool strong = false)
        {
            AccountTypes target_ac_sec;

            // allow everything from console and RA console
            if (_session == null)
                return false;

            // ignore only for non-players for non strong checks (when allow apply command at least to same sec level)
            if (!Global.AccountMgr.IsPlayerAccount(_session.GetSecurity()) && !strong && !WorldConfig.GetBoolValue(WorldCfg.GmLowerSecurity))
                return false;

            if (target != null)
                target_ac_sec = target.GetSecurity();
            else if (target_account != 0)
                target_ac_sec = Global.AccountMgr.GetSecurity(target_account);
            else
                return true;                                        // caller must report error for (target == NULL && target_account == 0)

            if (_session.GetSecurity() < target_ac_sec || (strong && _session.GetSecurity() <= target_ac_sec))
            {
                SendSysMessage(CypherStrings.YoursSecurityIsLow);
                _sentErrorMessage = true;
                return true;
            }
            return false;
        }

        bool hasStringAbbr(string name, string part)
        {
            // non "" command
            if (!name.IsEmpty())
            {
                // "" part from non-"" command
                if (part.IsEmpty())
                    return false;

                int partIndex = 0;
                while (true)
                {
                    if (partIndex >= part.Length)
                        return true;
                    else if (partIndex >= name.Length)
                        return false;
                    else if (char.ToLower(name[partIndex]) != char.ToLower(part[partIndex]))
                        return false;
                    ++partIndex;
                }
            }
            // allow with any for ""

            return true;
        }

        public WorldSession GetSession()
        {
            return _session;
        }
        public Player GetPlayer()
        {
            return _session.GetPlayer();
        }
        public string GetCypherString(CypherStrings str)
        {
            return Global.ObjectMgr.GetCypherString(str);
        }

        public virtual LocaleConstant GetSessionDbcLocale()
        {
            return _session.GetSessionDbcLocale();
        }
        public virtual byte GetSessionDbLocaleIndex()
        {
            return (byte)_session.GetSessionDbLocaleIndex();
        }
        public string GetParsedString(CypherStrings cypherString, params object[] args)
        {
            return string.Format(Global.ObjectMgr.GetCypherString(cypherString), args);
        }

        public void SendSysMessage(CypherStrings cypherString, params object[] args)
        {
            SendSysMessage(Global.ObjectMgr.GetCypherString(cypherString), args);
        }
        public virtual void SendSysMessage(string str, params object[] args)
        {
            _sentErrorMessage = true;
            string msg = string.Format(str, args);

            ChatPkt messageChat = new ChatPkt();

            var lines = new StringArray(msg, "\n", "\r");
            for (var i = 0; i < lines.Length; ++i)
            {
                messageChat.Initialize(ChatMsg.System, Language.Universal, null, null, lines[i]);
                _session.SendPacket(messageChat);
            }            
        }

        public void SendNotification(CypherStrings str, params object[] args)
        {
            _session.SendNotification(str, args);
        }

        public void SendGlobalSysMessage(string str)
        {
            // Chat output
            ChatPkt data = new ChatPkt();
            data.Initialize(ChatMsg.System, Language.Universal, null, null, str);
            Global.WorldMgr.SendGlobalMessage(data);
        }

        public void SendGlobalGMSysMessage(string str)
        {
            // Chat output
            ChatPkt data = new ChatPkt();
            data.Initialize(ChatMsg.System, Language.Universal, null, null, str);
            Global.WorldMgr.SendGlobalGMMessage(data);
        }

        public bool GetPlayerGroupAndGUIDByName(string name, out Player player, out Group group, out ObjectGuid guid, bool offline = false)
        {
            player = null;
            guid = ObjectGuid.Empty;
            group = null;

            if (!name.IsEmpty())
            {
                if (!ObjectManager.NormalizePlayerName(ref name))
                {
                    SendSysMessage(CypherStrings.PlayerNotFound);
                    return false;
                }

                player = Global.ObjAccessor.FindPlayerByName(name);
                if (offline)
                    guid = ObjectManager.GetPlayerGUIDByName(name);
            }

            if (player)
            {
                group = player.GetGroup();
                if (guid.IsEmpty() || !offline)
                    guid = player.GetGUID();
            }
            else
            {
                if (getSelectedPlayer())
                    player = getSelectedPlayer();
                else
                    player = _session.GetPlayer();

                if (guid.IsEmpty() || !offline)
                    guid = player.GetGUID();
                group = player.GetGroup();
            }

            return true;
        }

        public bool HasSentErrorMessage() { return _sentErrorMessage; }

        internal bool _sentErrorMessage;
        WorldSession _session;
    }

    public class ConsoleHandler : CommandHandler
    {
        public override bool IsAvailable(ChatCommand cmd)
        {
            return cmd.AllowConsole;
        }

        public override bool HasPermission(RBACPermissions permission)
        {
            return true;
        }

        public override void SendSysMessage(string str, params object[] args)
        {
            _sentErrorMessage = true;
            string msg = string.Format(str, args);

            Log.outInfo(LogFilter.Server, msg);
        }

        public override string GetNameLink()
        {
            return GetCypherString(CypherStrings.ConsoleCommand);
        }

        public override bool needReportToTarget(Player chr)
        {
            return true;
        }

        public override LocaleConstant GetSessionDbcLocale()
        {
            return Global.WorldMgr.GetDefaultDbcLocale();
        }

        public override byte GetSessionDbLocaleIndex()
        {
            return (byte)Global.WorldMgr.GetDefaultDbcLocale();
        }
    }
}
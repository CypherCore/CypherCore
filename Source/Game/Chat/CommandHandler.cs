﻿/*
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

using Framework.Collections;
using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Chat
{
    public class CommandHandler
    {
        public CommandHandler(WorldSession session = null)
        {
            _session = session;
        }

        public virtual bool ParseCommand(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // chat case (.command or !command format)
            if (text[0] != '!' && text[0] != '.')
                return false;

            /// ignore single . and ! in line
            if (text.Length < 2)
                return false;

            // ignore messages staring from many dots.
            if (text[1] == '!' || text[1] == '.')
                return false;

            return _ParseCommands(text.Substring(1));
        }

        public bool _ParseCommands(string text)
        {
            if (ExecuteCommandInTable(CommandManager.GetCommands(), text, text))
                return true;

            // Pretend commands don't exist for regular players
            if (_session != null && !_session.HasPermission(RBACPermissions.CommandsNotifyCommandNotFoundError))
                return false;

            // Send error message for GMs
            SendSysMessage(CypherStrings.NoCmd);
            return true;
        }

        public bool ExecuteCommandInTable(ICollection<ChatCommand> table, string text, string fullcmd)
        {
            var args = new StringArguments(text);
            var cmd = args.NextString();

            foreach (var command in table)
            {
                //if (!command.Name.Equals(cmd))
                    //continue;

                if (!HasStringAbbr(command.Name, cmd))
                    continue;

                var match = false;
                if (command.Name.Length > cmd.Length)
                {
                    foreach (var command2 in table)
                    {
                        if (!HasStringAbbr(command2.Name, cmd))
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
                    var arg = args.NextString("");
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

                    var player = GetPlayer();
                    if (!Global.AccountMgr.IsPlayerAccount(GetSession().GetSecurity()))
                    {
                        var guid = player.GetTarget();
                        var areaId = player.GetAreaId();
                        var areaName = "Unknown";
                        var zoneName = "Unknown";

                        var area = CliDB.AreaTableStorage.LookupByKey(areaId);
                        if (area != null)
                        {
                            var locale = GetSessionDbcLocale();
                            areaName = area.AreaName[locale];
                            var zone = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
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
            var args = new StringArguments(text);
            if (!args.Empty())
            {
                var cmd = args.NextString();
                foreach (var command in table)
                {
                    // must be available (ignore handler existence for show command with possible available subcommands)
                    if (!IsAvailable(command))
                        continue;

                    if (!HasStringAbbr(command.Name, cmd))
                        continue;

                    // have subcommand
                    var subcmd = !cmd.IsEmpty() ? args.NextString() : "";
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
            var list = "";
            foreach (var command in table)
            {
                // must be available (ignore handler existence for show command with possible available subcommands)
                if (!IsAvailable(command))
                    continue;

                // for empty subcmd show all available
                if (!subcmd.IsEmpty() && !HasStringAbbr(command.Name, subcmd))
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

        public string ExtractKeyFromLink(StringArguments args, params string[] linkType)
        {
            return ExtractKeyFromLink(args, linkType, out _);
        }
        public string ExtractKeyFromLink(StringArguments args, string[] linkType, out int found_idx)
        {
            return ExtractKeyFromLink(args, linkType, out found_idx, out _);
        }
        public string ExtractKeyFromLink(StringArguments args, string[] linkType, out int found_idx, out string something1)
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
                var check = args.NextString("|");
                if (string.IsNullOrEmpty(check))
                    return null;
            }
            else
                args.NextChar();

            var cLinkType = args.NextString(":");
            if (string.IsNullOrEmpty(cLinkType))
                return null;

            for (var i = 0; i < linkType.Length; ++i)
            {
                if (cLinkType == linkType[i])
                {
                    var cKey = args.NextString(":|");       // extract key

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

        public void ExtractOptFirstArg(StringArguments args, out string arg1, out string arg2)
        {
            var p1 = args.NextString();
            var p2 = args.NextString();

            if (string.IsNullOrEmpty(p2))
            {
                p2 = p1;
                p1 = null;
            }

            arg1 = p1;
            arg2 = p2;
        }

        public GameTele ExtractGameTeleFromLink(StringArguments args)
        {
            var cId = ExtractKeyFromLink(args, "Htele");
            if (string.IsNullOrEmpty(cId))
                return null;

            if (uint.TryParse(cId, out var id))
                return Global.ObjectMgr.GetGameTele(id);

            return Global.ObjectMgr.GetGameTele(cId);
        }

        public string ExtractQuotedArg(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            if (!str.Contains("\""))
                return null;

            return str.Replace("\"", String.Empty);
        }

        private string ExtractPlayerNameFromLink(StringArguments args)
        {
            // |color|Hplayer:name|h[name]|h|r
            var name = ExtractKeyFromLink(args, "Hplayer");
            if (name.IsEmpty())
                return "";

            if (!ObjectManager.NormalizePlayerName(ref name))
                return "";

            return name;
        }
        public bool ExtractPlayerTarget(StringArguments args, out Player player)
        {
            return ExtractPlayerTarget(args, out player, out _, out _);
        }
        public bool ExtractPlayerTarget(StringArguments args, out Player player, out ObjectGuid playerGuid)
        {
            return ExtractPlayerTarget(args, out player, out playerGuid, out _);
        }

        public bool ExtractPlayerTarget(StringArguments args, out Player player, out ObjectGuid playerGuid, out string playerName)
        {
            player = null;
            playerGuid = ObjectGuid.Empty;
            playerName = "";

            if (!args.Empty())
            {
                var name = ExtractPlayerNameFromLink(args);
                if (string.IsNullOrEmpty(name))
                {
                    SendSysMessage(CypherStrings.PlayerNotFound);
                    _sentErrorMessage = true;
                    return false;
                }

                player = Global.ObjAccessor.FindPlayerByName(name);
                var guid = player == null ? Global.CharacterCacheStorage.GetCharacterGuidByName(name) : ObjectGuid.Empty;

                playerGuid = player != null ? player.GetGUID() : guid;
                playerName = player != null || !guid.IsEmpty() ? name : "";
            }
            else
            {
                player = GetSelectedPlayer();
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
        public ulong ExtractLowGuidFromLink(StringArguments args, ref HighGuid guidHigh)
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
            var idS = ExtractKeyFromLink(args, guidKeys, out type);
            if (string.IsNullOrEmpty(idS))
                return 0;

            switch (type)
            {
                case 0:
                    {
                        guidHigh = HighGuid.Player;
                        if (!ObjectManager.NormalizePlayerName(ref idS))
                            return 0;

                        var player = Global.ObjAccessor.FindPlayerByName(idS);
                        if (player)
                            return player.GetGUID().GetCounter();

                        var guid = Global.CharacterCacheStorage.GetCharacterGuidByName(idS);
                        if (guid.IsEmpty())
                            return 0;

                        return guid.GetCounter();
                    }
                case 1:
                    {
                        guidHigh = HighGuid.Creature;
                        if (!ulong.TryParse(idS, out var lowguid))
                            return 0;
                        return lowguid;
                    }
                case 2:
                    {
                        guidHigh = HighGuid.GameObject;
                        if (!ulong.TryParse(idS, out var lowguid))
                            return 0;
                        return lowguid;
                    }
            }

            // unknown type?
            return 0;
        }

        private static string[] spellKeys =
        {
            "Hspell",                                               // normal spell
            "Htalent",                                              // talent spell
            "Henchant",                                             // enchanting recipe spell
            "Htrade",                                               // profession/skill spell
            "Hglyph",                                               // glyph
        };
        public uint ExtractSpellIdFromLink(StringArguments args)
        {
            // number or [name] Shift-click form |color|Henchant:recipe_spell_id|h[prof_name: recipe_name]|h|r
            // number or [name] Shift-click form |color|Hglyph:glyph_slot_id:glyph_prop_id|h[value]|h|r
            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r
            // number or [name] Shift-click form |color|Htalent:talent_id, rank|h[name]|h|r
            // number or [name] Shift-click form |color|Htrade:spell_id, skill_id, max_value, cur_value|h[name]|h|r
            var idS = ExtractKeyFromLink(args, spellKeys, out var type, out var param1Str);
            if (string.IsNullOrEmpty(idS))
                return 0;

            if (!uint.TryParse(idS, out var id))
                return 0;               

            switch (type)
            {
                case 0:
                    return id;
                case 1:
                    {
                        // talent
                        var talentEntry = CliDB.TalentStorage.LookupByKey(id);
                        if (talentEntry == null)
                            return 0;

                        return talentEntry.SpellID;
                    }
                case 2:
                case 3:
                    return id;
                case 4:
                    {
                        if (!uint.TryParse(param1Str, out var glyph_prop_id))
                            glyph_prop_id = 0;

                        var glyphPropEntry = CliDB.GlyphPropertiesStorage.LookupByKey(glyph_prop_id);
                        if (glyphPropEntry == null)
                            return 0;

                        return glyphPropEntry.SpellID;
                    }
            }

            // unknown type?
            return 0;
        }

        public Player GetSelectedPlayer()
        {
            if (_session == null)
                return null;

            var selected = _session.GetPlayer().GetTarget();

            if (selected.IsEmpty())
                return _session.GetPlayer();

            return Global.ObjAccessor.FindPlayer(selected);
        }
        public Unit GetSelectedUnit()
        {
            if (_session == null)
                return null;

            var selected = _session.GetPlayer().GetSelectedUnit();
            if (selected)
                return selected;

            return _session.GetPlayer();
        }
        public WorldObject GetSelectedObject()
        {
            if (_session == null)
                return null;

            var selected = _session.GetPlayer().GetTarget();

            if (selected.IsEmpty())
                return GetNearbyGameObject();

            return Global.ObjAccessor.GetUnit(_session.GetPlayer(), selected);
        }
        public Creature GetSelectedCreature()
        {
            if (_session == null)
                return null;

            return ObjectAccessor.GetCreatureOrPetOrVehicle(_session.GetPlayer(), _session.GetPlayer().GetTarget());
        }
        public Player GetSelectedPlayerOrSelf()
        {
            if (_session == null)
                return null;

            var selected = _session.GetPlayer().GetTarget();
            if (selected.IsEmpty())
                return _session.GetPlayer();

            // first try with selected target
            var targetPlayer = Global.ObjAccessor.FindConnectedPlayer(selected);
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

        private GameObject GetNearbyGameObject()
        {
            if (_session == null)
                return null;

            var pl = _session.GetPlayer();
            var check = new NearestGameObjectCheck(pl);
            var searcher = new GameObjectLastSearcher(pl, check);
            Cell.VisitGridObjects(pl, searcher, MapConst.SizeofGrids);
            return searcher.GetTarget();
        }

        public string PlayerLink(string name, bool console = false)
        {
            return console ? name : "|cffffffff|Hplayer:" + name + "|h[" + name + "]|h|r";
        }
        public virtual string GetNameLink()
        {
            return GetNameLink(_session.GetPlayer());
        }
        public string GetNameLink(Player obj)
        {
            return PlayerLink(obj.GetName());
        }
        public virtual bool NeedReportToTarget(Player chr)
        {
            var pl = _session.GetPlayer();
            return pl != chr && pl.IsVisibleGloballyFor(chr);
        }
        public bool HasLowerSecurity(Player target, ObjectGuid guid, bool strong = false)
        {
            WorldSession target_session = null;
            uint target_account = 0;

            if (target != null)
                target_session = target.GetSession();
            else if (!guid.IsEmpty())
                target_account = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(guid);

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

        private bool HasStringAbbr(string name, string part)
        {
            // non "" command
            if (!name.IsEmpty())
            {
                // "" part from non-"" command
                if (part.IsEmpty())
                    return false;

                var partIndex = 0;
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

        public virtual Locale GetSessionDbcLocale()
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

        public void SendSysMessage(string str, params object[] args)
        {
            SendSysMessage(string.Format(str, args));
        }
        public void SendSysMessage(CypherStrings cypherString, params object[] args)
        {
            SendSysMessage(string.Format(Global.ObjectMgr.GetCypherString(cypherString), args));
        }

        public virtual void SendSysMessage(string str, bool escapeCharacters = false)
        {
            _sentErrorMessage = true;

            if (escapeCharacters)
                str.Replace("|", "||");

            var messageChat = new ChatPkt();

            var lines = new StringArray(str, "\n", "\r");
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
            var data = new ChatPkt();
            data.Initialize(ChatMsg.System, Language.Universal, null, null, str);
            Global.WorldMgr.SendGlobalMessage(data);
        }

        public void SendGlobalGMSysMessage(string str)
        {
            // Chat output
            var data = new ChatPkt();
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
                    guid = Global.CharacterCacheStorage.GetCharacterGuidByName(name);
            }

            if (player)
            {
                group = player.GetGroup();
                if (guid.IsEmpty() || !offline)
                    guid = player.GetGUID();
            }
            else
            {
                if (GetSelectedPlayer())
                    player = GetSelectedPlayer();
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
        private WorldSession _session;
    }

    internal class AddonChannelCommandHandler : CommandHandler
    {
        public static string PREFIX = "TrinityCore";

        private string echo;
        private bool hadAck;
        private bool humanReadable;

        public AddonChannelCommandHandler(WorldSession session) : base(session) { }

        public override bool ParseCommand(string text)
        {
            if (text.IsEmpty())
                return false;

            if (text.Length < 5) // str[1] through str[4] is 4-character command counter
                return false;

            echo = text.Substring(1);

            switch (text[0])
            {
                case 'p': // p Ping
                    SendAck();
                    return true;
                case 'h': // h Issue human-readable command
                case 'i': // i Issue command
                    if (text.Length < 6)
                        return false;
                    humanReadable = (text[0] == 'h');
                    if (_ParseCommands(text + 5)) // actual command starts at str[5]
                    {
                        if (!hadAck)
                            SendAck();
                        if (HasSentErrorMessage())
                            SendFailed();
                        else
                            SendOK();
                    }
                    else
                    {
                        SendSysMessage(CypherStrings.NoCmd);
                        SendFailed();
                    }
                    return true;
                default:
                    return false;
            }
        }

        private void Send(string msg)
        {
            var chat = new ChatPkt();
            chat.Initialize(ChatMsg.Whisper, Language.Addon, GetSession().GetPlayer(), GetSession().GetPlayer(), msg, 0, "", Locale.enUS, PREFIX);
            GetSession().SendPacket(chat);
        }

        private void SendAck() // a Command acknowledged, no body
        {
            Send($"a{echo:4}\0");
            hadAck = true;
        }

        private void SendOK() // o Command OK, no body
        {
            Send($"o{echo:4}\0");
        }

        private void SendFailed() // f Command failed, no body
        {
            Send($"f{echo:4}\0");
        }

        public override void SendSysMessage(string str, bool escapeCharacters)
        {
            if (!hadAck)
                SendAck();

            var msg = new StringBuilder("m");
            msg.Append(echo, 0, 4);
            var body = str;
            if (escapeCharacters)
                body.Replace("|", "||");

            int pos, lastpos;
            for (lastpos = 0, pos = body.IndexOf('\n', lastpos); pos != -1; lastpos = pos + 1, pos = body.IndexOf('\n', lastpos))
            {
                var line = msg;
                line.Append(body, lastpos, pos - lastpos);
                Send(line.ToString());
            }
            msg.Append(body, lastpos, pos - lastpos);
            Send(msg.ToString());
        }
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

        public override void SendSysMessage(string str, bool escapeCharacters)
        {
            _sentErrorMessage = true;

            Log.outInfo(LogFilter.Server, str);
        }

        public override bool ParseCommand(string str)
        {
            if (str.IsEmpty())
                return false;

            // Console allows using commands both with and without leading indicator
            if (str[0] == '.' || str[0] == '!')
                str = str.Substring(1);

            return _ParseCommands(str);
        }

        public override string GetNameLink()
        {
            return GetCypherString(CypherStrings.ConsoleCommand);
        }

        public override bool NeedReportToTarget(Player chr)
        {
            return true;
        }

        public override Locale GetSessionDbcLocale()
        {
            return Global.WorldMgr.GetDefaultDbcLocale();
        }

        public override byte GetSessionDbLocaleIndex()
        {
            return (byte)Global.WorldMgr.GetDefaultDbcLocale();
        }
    }
}
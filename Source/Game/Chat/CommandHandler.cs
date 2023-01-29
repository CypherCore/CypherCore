// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Collections;
using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;
using Game.Networking.Packets;

namespace Game.Chat
{
    public class CommandHandler
    {
        private static readonly string[] _spellKeys =
        {
            "Hspell",   // normal spell
			"Htalent",  // talent spell
			"Henchant", // enchanting recipe spell
			"Htrade",   // profession/skill spell
			"Hglyph"    // glyph
		};

        private readonly WorldSession _session;

        private bool _sentErrorMessage;

        public CommandHandler(WorldSession session = null)
        {
            _session = session;
        }

        public virtual bool ParseCommands(string text)
        {
            if (text.IsEmpty())
                return false;

            // chat case (.command or !command format)
            if (text[0] != '!' &&
                text[0] != '.')
                return false;

            /// ignore single . and ! in line
            if (text.Length < 2)
                return false;

            // ignore messages staring from many dots.
            if (text[1] == text[0])
                return false;

            if (text[1] == ' ')
                return false;

            return _ParseCommands(text[1..]);
        }

        public bool _ParseCommands(string text)
        {
            if (ChatCommandNode.TryExecuteCommand(this, text))
                return true;

            // Pretend commands don't exist for regular players
            if (_session != null &&
                !_session.HasPermission(RBACPermissions.CommandsNotifyCommandNotFoundError))
                return false;

            // Send error message for GMs
            SendSysMessage(CypherStrings.CmdInvalid, text);

            return true;
        }

        public virtual bool IsAvailable(ChatCommandNode cmd)
        {
            return HasPermission(cmd._permission.RequiredPermission);
        }

        public virtual bool IsHumanReadable()
        {
            return true;
        }

        public virtual bool HasPermission(RBACPermissions permission)
        {
            return _session.HasPermission(permission);
        }

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
                string check = args.NextString("|");

                if (string.IsNullOrEmpty(check))
                    return null;
            }
            else
            {
                args.NextChar();
            }

            string cLinkType = args.NextString(":");

            if (string.IsNullOrEmpty(cLinkType))
                return null;

            for (var i = 0; i < linkType.Length; ++i)
                if (cLinkType == linkType[i])
                {
                    string cKey = args.NextString(":|"); // extract key

                    something1 = args.NextString(":|"); // extract something

                    args.NextString("]"); // restart scan tail and skip Name with possible spaces
                    args.NextString();    // skip link tail (to allow continue strtok(NULL, s) use after return from function
                    found_idx = i;

                    return cKey;
                }

            args.NextString();
            SendSysMessage(CypherStrings.WrongLinkType);

            return null;
        }

        public string ExtractQuotedArg(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            if (!str.Contains("\""))
                return str;

            return str.Replace("\"", string.Empty);
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

            if (args != null &&
                !args.Empty())
            {
                string name = ExtractPlayerNameFromLink(args);

                if (string.IsNullOrEmpty(name))
                {
                    SendSysMessage(CypherStrings.PlayerNotFound);
                    _sentErrorMessage = true;

                    return false;
                }

                player = Global.ObjAccessor.FindPlayerByName(name);
                ObjectGuid guid = player == null ? Global.CharacterCacheStorage.GetCharacterGuidByName(name) : ObjectGuid.Empty;

                playerGuid = player != null ? player.GetGUID() : guid;
                playerName = player != null || !guid.IsEmpty() ? name : "";
            }
            else
            {
                player = GetSelectedPlayer();
                playerGuid = player != null ? player.GetGUID() : ObjectGuid.Empty;
                playerName = player != null ? player.GetName() : "";
            }

            if (player == null &&
                playerGuid.IsEmpty() &&
                string.IsNullOrEmpty(playerName))
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
                "Hplayer", "Hcreature", "Hgameobject"
            };

            // |color|Hcreature:creature_guid|h[Name]|h|r
            // |color|Hgameobject:go_guid|h[Name]|h|r
            // |color|Hplayer:Name|h[Name]|h|r
            string idS = ExtractKeyFromLink(args, guidKeys, out type);

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

                        ObjectGuid guid = Global.CharacterCacheStorage.GetCharacterGuidByName(idS);

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

            // unknown Type?
            return 0;
        }

        public uint ExtractSpellIdFromLink(StringArguments args)
        {
            // number or [Name] Shift-click form |color|Henchant:recipe_spell_id|h[prof_name: recipe_name]|h|r
            // number or [Name] Shift-click form |color|Hglyph:glyph_slot_id:glyph_prop_id|h[value]|h|r
            // number or [Name] Shift-click form |color|Hspell:spell_id|h[Name]|h|r
            // number or [Name] Shift-click form |color|Htalent:talent_id, rank|h[Name]|h|r
            // number or [Name] Shift-click form |color|Htrade:spell_id, skill_id, max_value, cur_value|h[Name]|h|r
            string idS = ExtractKeyFromLink(args, _spellKeys, out int type, out string param1Str);

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

            // unknown Type?
            return 0;
        }

        public Player GetSelectedPlayer()
        {
            if (_session == null)
                return null;

            ObjectGuid selected = _session.GetPlayer().GetTarget();

            if (selected.IsEmpty())
                return _session.GetPlayer();

            return Global.ObjAccessor.FindConnectedPlayer(selected);
        }

        public Unit GetSelectedUnit()
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

            ObjectGuid selected = _session.GetPlayer().GetTarget();

            if (selected.IsEmpty())
                return _session.GetPlayer();

            // first try with selected Target
            Player targetPlayer = Global.ObjAccessor.FindConnectedPlayer(selected);

            // if the Target is not a player, then return self
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

        public string PlayerLink(string name)
        {
            return _session != null ? "|cffffffff|Hplayer:" + name + "|h[" + name + "]|h|r" : name;
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
                target_account = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(guid);

            if (target_session == null &&
                target_account == 0)
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
            if (!Global.AccountMgr.IsPlayerAccount(_session.GetSecurity()) &&
                !strong &&
                !WorldConfig.GetBoolValue(WorldCfg.GmLowerSecurity))
                return false;

            if (target != null)
                target_ac_sec = target.GetSecurity();
            else if (target_account != 0)
                target_ac_sec = Global.AccountMgr.GetSecurity(target_account, (int)Global.WorldMgr.GetRealmId().Index);
            else
                return true; // caller must report error for (Target == NULL && target_account == 0)

            if (_session.GetSecurity() < target_ac_sec ||
                (strong && _session.GetSecurity() <= target_ac_sec))
            {
                SendSysMessage(CypherStrings.YoursSecurityIsLow);
                _sentErrorMessage = true;

                return true;
            }

            return false;
        }

        public bool IsConsole()
        {
            return _session == null;
        }

        public WorldSession GetSession()
        {
            return _session;
        }

        public Player GetPlayer()
        {
            return _session?.GetPlayer();
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

            ChatPkt messageChat = new();

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
            ChatPkt data = new();
            data.Initialize(ChatMsg.System, Language.Universal, null, null, str);
            Global.WorldMgr.SendGlobalMessage(data);
        }

        public void SendGlobalGMSysMessage(string str)
        {
            // Chat output
            ChatPkt data = new();
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

                if (guid.IsEmpty() ||
                    !offline)
                    guid = player.GetGUID();
            }
            else
            {
                if (GetSelectedPlayer())
                    player = GetSelectedPlayer();
                else
                    player = _session.GetPlayer();

                if (guid.IsEmpty() ||
                    !offline)
                    guid = player.GetGUID();

                group = player.GetGroup();
            }

            return true;
        }

        public bool HasSentErrorMessage()
        {
            return _sentErrorMessage;
        }

        public void SetSentErrorMessage(bool val)
        {
            _sentErrorMessage = val;
        }

        private string ExtractPlayerNameFromLink(StringArguments args)
        {
            // |color|Hplayer:Name|h[Name]|h|r
            string name = ExtractKeyFromLink(args, "Hplayer");

            if (name.IsEmpty())
                return "";

            if (!ObjectManager.NormalizePlayerName(ref name))
                return "";

            return name;
        }

        private GameObject GetNearbyGameObject()
        {
            if (_session == null)
                return null;

            Player pl = _session.GetPlayer();
            NearestGameObjectCheck check = new(pl);
            GameObjectLastSearcher searcher = new(pl, check);
            Cell.VisitGridObjects(pl, searcher, MapConst.SizeofGrids);

            return searcher.GetTarget();
        }

        private bool HasStringAbbr(string name, string part)
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
                    if (partIndex >= part.Length ||
                        part[partIndex] == ' ')
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
    }

    internal class AddonChannelCommandHandler : CommandHandler
    {
        public static string PREFIX = "TrinityCore";

        private string echo;
        private bool hadAck;
        private bool humanReadable;

        public AddonChannelCommandHandler(WorldSession session) : base(session)
        {
        }

        public override bool ParseCommands(string str)
        {
            if (str.Length < 5)
                return false;

            char opcode = str[0];
            echo = str[1..];

            switch (opcode)
            {
                case 'p': // p Ping
                    SendAck();

                    return true;
                case 'h': // h Issue human-readable command
                case 'i': // i Issue command
                    if (str.Length < 6)
                        return false;

                    humanReadable = opcode == 'h';
                    string cmd = str[5..];

                    if (_ParseCommands(cmd)) // actual command starts at str[5]
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
                        SendSysMessage(CypherStrings.CmdInvalid, cmd);
                        SendFailed();
                    }

                    return true;
                default:
                    return false;
            }
        }

        public override void SendSysMessage(string str, bool escapeCharacters)
        {
            if (!hadAck)
                SendAck();

            StringBuilder msg = new("m");
            msg.Append(echo, 0, 4);
            string body = str;

            if (escapeCharacters)
                body.Replace("|", "||");

            int pos, lastpos;

            for (lastpos = 0, pos = body.IndexOf('\n', lastpos); pos != -1; lastpos = pos + 1, pos = body.IndexOf('\n', lastpos))
            {
                StringBuilder line = msg;
                line.Append(body, lastpos, pos - lastpos);
                Send(line.ToString());
            }

            msg.Append(body, lastpos, pos - lastpos);
            Send(msg.ToString());
        }

        public override bool IsHumanReadable()
        {
            return humanReadable;
        }

        private void Send(string msg)
        {
            ChatPkt chat = new();
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
    }

    public class ConsoleHandler : CommandHandler
    {
        public override bool IsAvailable(ChatCommandNode cmd)
        {
            return cmd._permission.AllowConsole;
        }

        public override bool HasPermission(RBACPermissions permission)
        {
            return true;
        }

        public override void SendSysMessage(string str, bool escapeCharacters)
        {
            SetSentErrorMessage(true);
            Log.outInfo(LogFilter.Server, str);
        }

        public override bool ParseCommands(string str)
        {
            if (str.IsEmpty())
                return false;

            // Console allows using commands both with and without leading indicator
            if (str[0] == '.' ||
                str[0] == '!')
                str = str[1..];

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

    public class RemoteAccessHandler : CommandHandler
    {
        private readonly Action<string> _reportToRA;

        public RemoteAccessHandler(Action<string> reportToRA) : base()
        {
            _reportToRA = reportToRA;
        }

        public override bool IsAvailable(ChatCommandNode cmd)
        {
            return cmd._permission.AllowConsole;
        }

        public override bool HasPermission(RBACPermissions permission)
        {
            return true;
        }

        public override void SendSysMessage(string str, bool escapeCharacters)
        {
            SetSentErrorMessage(true);
            _reportToRA(str);
        }

        public override bool ParseCommands(string str)
        {
            if (str.IsEmpty())
                return false;

            // Console allows using commands both with and without leading indicator
            if (str[0] == '.' ||
                str[0] == '!')
                str = str[1..];

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
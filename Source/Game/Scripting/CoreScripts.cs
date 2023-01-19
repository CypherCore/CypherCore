﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Chat;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
using Game.PvP;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Scripting
{
    public class ScriptObject
    {
        public ScriptObject(string name)
        {
            _name = name;
        }

        public string GetName() { return _name; }

        // Do not override this in scripts; it should be overridden by the various script type classes. It indicates
        // whether or not this script type must be assigned in the database.
        public virtual bool IsDatabaseBound() { return false; }

        public static T GetInstanceAI<T>(WorldObject obj) where T : class
        {
            InstanceMap instance = obj.GetMap().ToInstanceMap();
            if (instance != null && instance.GetInstanceScript() != null)
                return (T)Activator.CreateInstance(typeof(T), new object[] { obj });

            return null;
        }

        public static void ClearGossipMenuFor(Player player) { player.PlayerTalkClass.ClearMenus(); }
        // Using provided text, not from DB
        public static void AddGossipItemFor(Player player, GossipOptionNpc optionNpc, string text, uint sender, uint action)
        {
            player.PlayerTalkClass.GetGossipMenu().AddMenuItem(0, -1, optionNpc, text, 0, GossipOptionFlags.None, null, 0, 0, false, 0, "", null, null, sender, action);
        }
        // Using provided texts, not from DB
        public static void AddGossipItemFor(Player player, GossipOptionNpc optionNpc, string text, uint sender, uint action, string popupText, uint popupMoney, bool coded)
        {
            player.PlayerTalkClass.GetGossipMenu().AddMenuItem(0, -1, optionNpc, text, 0, GossipOptionFlags.None, null, 0, 0, coded, popupMoney, popupText, null, null, sender, action);
        }
        // Uses gossip item info from DB
        public static void AddGossipItemFor(Player player, uint gossipMenuID, uint gossipMenuItemID, uint sender, uint action)
        {
            player.PlayerTalkClass.GetGossipMenu().AddMenuItem(gossipMenuID, gossipMenuItemID, sender, action);
        }
        public static void SendGossipMenuFor(Player player, uint npcTextID, ObjectGuid guid) { player.PlayerTalkClass.SendGossipMenu(npcTextID, guid); }
        public static void SendGossipMenuFor(Player player, uint npcTextID, Creature creature) { if (creature) SendGossipMenuFor(player, npcTextID, creature.GetGUID()); }
        public static void CloseGossipMenuFor(Player player) { player.PlayerTalkClass.SendCloseGossip(); }

        string _name;
    }

    class GenericSpellScriptLoader<S> : SpellScriptLoader where S : SpellScript
    {
        public GenericSpellScriptLoader(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public override SpellScript GetSpellScript() { return (S)Activator.CreateInstance(typeof(S), _args); }

        object[] _args;
    }

    class GenericAuraScriptLoader<A> : AuraScriptLoader where A : AuraScript
    {
        public GenericAuraScriptLoader(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public override AuraScript GetAuraScript() { return (A)Activator.CreateInstance(typeof(A), _args); }

        object[] _args;
    }

    public class SpellScriptLoader : ScriptObject
    {
        public SpellScriptLoader(string name) : base(name)
        {
            Global.ScriptMgr.AddScript<SpellScriptLoader>(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Should return a fully valid SpellScript.
        public virtual SpellScript GetSpellScript() { return null; }
    }

    public class AuraScriptLoader : ScriptObject
    {
        public AuraScriptLoader(string name) : base(name)
        {
            Global.ScriptMgr.AddScript<AuraScriptLoader>(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Should return a fully valid AuraScript.
        public virtual AuraScript GetAuraScript() { return null; }
    }

    public class WorldScript : ScriptObject
    {
        protected WorldScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        // Called when the open/closed state of the world changes.
        public virtual void OnOpenStateChange(bool open) { }

        // Called after the world configuration is (re)loaded.
        public virtual void OnConfigLoad(bool reload) { }

        // Called before the message of the day is changed.
        public virtual void OnMotdChange(string newMotd) { }

        // Called when a world shutdown is initiated.
        public virtual void OnShutdownInitiate(ShutdownExitCode code, ShutdownMask mask) { }

        // Called when a world shutdown is cancelled.
        public virtual void OnShutdownCancel() { }

        // Called on every world tick (don't execute too heavy code here).
        public virtual void OnUpdate(uint diff) { }

        // Called when the world is started.
        public virtual void OnStartup() { }

        // Called when the world is actually shut down.
        public virtual void OnShutdown() { }
    }

    public class FormulaScript : ScriptObject
    {
        public FormulaScript(string name) : base(name) { }

        // Called after calculating honor.
        public virtual void OnHonorCalculation(float honor, uint level, float multiplier) { }

        // Called after gray level calculation.
        public virtual void OnGrayLevelCalculation(uint grayLevel, uint playerLevel) { }

        // Called after calculating experience color.
        public virtual void OnColorCodeCalculation(XPColorChar color, uint playerLevel, uint mobLevel) { }

        // Called after calculating zero difference.
        public virtual void OnZeroDifferenceCalculation(uint diff, uint playerLevel) { }

        // Called after calculating base experience gain.
        public virtual void OnBaseGainCalculation(uint gain, uint playerLevel, uint mobLevel) { }

        // Called after calculating experience gain.
        public virtual void OnGainCalculation(uint gain, Player player, Unit unit) { }

        // Called when calculating the experience rate for group experience.
        public virtual void OnGroupRateCalculation(float rate, uint count, bool isRaid) { }
    }

    public class MapScript<T> : ScriptObject where T : Map
    {
        public MapScript(string name, uint mapId) : base(name)
        {
            _mapEntry = CliDB.MapStorage.LookupByKey(mapId);

            if (_mapEntry == null)
                Log.outError(LogFilter.Scripts, "Invalid MapScript for {0}; no such map ID.", mapId);
        }

        // Gets the MapEntry structure associated with this script. Can return NULL.
        public MapRecord GetEntry() { return _mapEntry; }

        // Called when the map is created.
        public virtual void OnCreate(T map) { }

        // Called just before the map is destroyed.
        public virtual void OnDestroy(T map) { }

        // Called when a player enters the map.
        public virtual void OnPlayerEnter(T map, Player player) { }

        // Called when a player leaves the map.
        public virtual void OnPlayerLeave(T map, Player player) { }

        public virtual void OnUpdate(T obj, uint diff) { }

        MapRecord _mapEntry;
    }

    public class WorldMapScript : MapScript<Map>
    {
        public WorldMapScript(string name, uint mapId) : base(name, mapId)
        {
            if (GetEntry() != null && !GetEntry().IsWorldMap())
                Log.outError(LogFilter.Scripts, "WorldMapScript for map {0} is invalid.", mapId);

            Global.ScriptMgr.AddScript(this);
        }
    }

    public class InstanceMapScript : MapScript<InstanceMap>
    {
        public InstanceMapScript(string name, uint mapId) : base(name, mapId)
        {
            if (GetEntry() != null && !GetEntry().IsDungeon())
                Log.outError(LogFilter.Scripts, "InstanceMapScript for map {0} is invalid.", mapId);

            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Gets an InstanceScript object for this instance.
        public virtual InstanceScript GetInstanceScript(InstanceMap map) { return null; }
    }

    public class BattlegroundMapScript : MapScript<BattlegroundMap>
    {
        public BattlegroundMapScript(string name, uint mapId) : base(name, mapId)
        {
            if (GetEntry() != null && GetEntry().IsBattleground())
                Log.outError(LogFilter.Scripts, "BattlegroundMapScript for map {0} is invalid.", mapId);

            Global.ScriptMgr.AddScript(this);
        }
    }

    public class ItemScript : ScriptObject
    {
        public ItemScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when a player accepts a quest from the item.
        public virtual bool OnQuestAccept(Player player, Item item, Quest quest) { return false; }

        // Called when a player uses the item.
        public virtual bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId) { return false; }

        // Called when the item expires (is destroyed).
        public virtual bool OnExpire(Player player, ItemTemplate proto) { return false; }

        // Called when the item is destroyed.
        public virtual bool OnRemove(Player player, Item item) { return false; }

        // Called before casting a combat spell from this item (chance on hit spells of item template, can be used to prevent cast if returning false)
        public virtual bool OnCastItemCombatSpell(Player player, Unit victim, SpellInfo spellInfo, Item item) { return true; }
    }

    public class UnitScript : ScriptObject
    {
        public UnitScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public virtual void OnHeal(Unit healer, Unit reciever, ref uint gain) { }

        public virtual void OnDamage(Unit attacker, Unit victim, ref uint damage) { }

        // Called when DoT's Tick Damage is being Dealt
        public virtual void ModifyPeriodicDamageAurasTick(Unit target, Unit attacker, ref uint damage) { }

        // Called when Melee Damage is being Dealt
        public virtual void ModifyMeleeDamage(Unit target, Unit attacker, ref uint damage) { }

        // Called when Spell Damage is being Dealt
        public virtual void ModifySpellDamageTaken(Unit target, Unit attacker, ref int damage, SpellInfo spellInfo) { }
    }

    public class GenericCreatureScript<AI> : CreatureScript where AI : CreatureAI
    {
        public GenericCreatureScript(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public override CreatureAI GetAI(Creature me)
        {
            if (me.GetInstanceScript() != null)
                return GetInstanceAI<AI>(me);
            else
                return (AI)Activator.CreateInstance(typeof(AI), new object[] { me }.Combine(_args));
        }

        object[] _args;
    }

    public class CreatureScript : ScriptObject
    {
        public CreatureScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript<CreatureScript>(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when a CreatureAI object is needed for the creature.
        public virtual CreatureAI GetAI(Creature creature) { return null; }
    }

    public class GenericGameObjectScript<AI> : GameObjectScript where AI : GameObjectAI
    {
        public GenericGameObjectScript(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public override GameObjectAI GetAI(GameObject me)
        {
            if (me.GetInstanceScript() != null)
                return GetInstanceAI<AI>(me);
            else
                return (AI)Activator.CreateInstance(typeof(AI), new object[] { me }.Combine(_args));
        }

        object[] _args;
    }

    public class GameObjectScript : ScriptObject
    {
        public GameObjectScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when a GameObjectAI object is needed for the gameobject.
        public virtual GameObjectAI GetAI(GameObject go) { return null; }
    }

    public class GenericAreaTriggerScript<AI> : AreaTriggerEntityScript where AI : AreaTriggerAI
    {
        public GenericAreaTriggerScript(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public override AreaTriggerAI GetAI(AreaTrigger me)
        {
            if (me.GetInstanceScript() != null)
                return GetInstanceAI<AI>(me);
            else
                return (AI)Activator.CreateInstance(typeof(AI), new object[] { me }.Combine(_args));
        }

        object[] _args;
    }

    public class AreaTriggerScript : ScriptObject
    {
        public AreaTriggerScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when the area trigger is activated by a player.
        public virtual bool OnTrigger(Player player, AreaTriggerRecord trigger) { return false; }

        // Called when the area trigger is left by a player.
        public virtual bool OnExit(Player player, AreaTriggerRecord trigger) { return false; }
    }

    public class OnlyOnceAreaTriggerScript : AreaTriggerScript
    {
        public OnlyOnceAreaTriggerScript(string name) : base(name) { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger)
        {
            InstanceScript instance = player.GetInstanceScript();
            if (instance != null && instance.IsAreaTriggerDone(trigger.Id))
                return true;

            if (TryHandleOnce(player, trigger) && instance != null)
                instance.MarkAreaTriggerDone(trigger.Id);

            return true;
        }

        // returns true if the trigger was successfully handled, false if we should try again next time
        public virtual bool TryHandleOnce(Player player, AreaTriggerRecord trigger) { return false; }

        void ResetAreaTriggerDone(InstanceScript script, uint triggerId)
        {
            script.ResetAreaTriggerDone(triggerId);
        }

        void ResetAreaTriggerDone(Player player, AreaTriggerRecord trigger)
        {
            InstanceScript instance = player.GetInstanceScript();
            if (instance != null)
                ResetAreaTriggerDone(instance, trigger.Id);
        }
    }

    public class BattlefieldScript : ScriptObject
    {
        public BattlefieldScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        public virtual BattleField GetBattlefield(Map map) { return null; }
    }
    
    public class BattlegroundScript : ScriptObject
    {
        public BattlegroundScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Should return a fully valid Battlegroundobject for the type ID.
        public virtual Battleground GetBattleground() { return null; }
    }

    public class OutdoorPvPScript : ScriptObject
    {
        public OutdoorPvPScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Should return a fully valid OutdoorPvP object for the type ID.
        public virtual OutdoorPvP GetOutdoorPvP(Map map) { return null; }
    }

    public class WeatherScript : ScriptObject
    {
        public WeatherScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when the weather changes in the zone this script is associated with.
        public virtual void OnChange(Weather weather, WeatherState state, float grade) { }

        public virtual void OnUpdate(Weather obj, uint diff) { }
    }

    public class AuctionHouseScript : ScriptObject
    {
        public AuctionHouseScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        // Called when an auction is added to an auction house.
        public virtual void OnAuctionAdd(AuctionHouseObject ah, AuctionPosting auction) { }

        // Called when an auction is removed from an auction house.
        public virtual void OnAuctionRemove(AuctionHouseObject ah, AuctionPosting auction) { }

        // Called when an auction was succesfully completed.
        public virtual void OnAuctionSuccessful(AuctionHouseObject ah, AuctionPosting auction) { }

        // Called when an auction expires.
        public virtual void OnAuctionExpire(AuctionHouseObject ah, AuctionPosting auction) { }
    }

    public class ConditionScript : ScriptObject
    {
        public ConditionScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when a single condition is checked for a player.
        public virtual bool OnConditionCheck(Condition condition, ConditionSourceInfo sourceInfo) { return true; }
    }

    public class VehicleScript : ScriptObject
    {
        public VehicleScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        // Called after a vehicle is installed.
        public virtual void OnInstall(Vehicle veh) { }

        // Called after a vehicle is uninstalled.
        public virtual void OnUninstall(Vehicle veh) { }

        // Called when a vehicle resets.
        public virtual void OnReset(Vehicle veh) { }

        // Called after an accessory is installed in a vehicle.
        public virtual void OnInstallAccessory(Vehicle veh, Creature accessory) { }

        // Called after a passenger is added to a vehicle.
        public virtual void OnAddPassenger(Vehicle veh, Unit passenger, sbyte seatId) { }

        // Called after a passenger is removed from a vehicle.
        public virtual void OnRemovePassenger(Vehicle veh, Unit passenger) { }
    }

    public class DynamicObjectScript : ScriptObject
    {
        public DynamicObjectScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public virtual void OnUpdate(DynamicObject obj, uint diff) { }
    }

    public class TransportScript : ScriptObject
    {
        public TransportScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when a player boards the transport.
        public virtual void OnAddPassenger(Transport transport, Player player) { }

        // Called when a creature boards the transport.
        public virtual void OnAddCreaturePassenger(Transport transport, Creature creature) { }

        // Called when a player exits the transport.
        public virtual void OnRemovePassenger(Transport transport, Player player) { }

        // Called when a transport moves.
        public virtual void OnRelocate(Transport transport, uint mapId, float x, float y, float z) { }

        public virtual void OnUpdate(Transport obj, uint diff) { }
    }

    public class AchievementScript : ScriptObject
    {
        public AchievementScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when an achievement is completed.
        public virtual void OnCompleted(Player player, AchievementRecord achievement) { }
    }
    
    public class AchievementCriteriaScript : ScriptObject
    {
        public AchievementCriteriaScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when an additional criteria is checked.
        public virtual bool OnCheck(Player source, Unit target) { return false; }
    }

    public class PlayerScript : ScriptObject
    {
        public PlayerScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        // Called when a player kills another player
        public virtual void OnPVPKill(Player killer, Player killed) { }

        // Called when a player kills a creature
        public virtual void OnCreatureKill(Player killer, Creature killed) { }

        // Called when a player is killed by a creature
        public virtual void OnPlayerKilledByCreature(Creature killer, Player killed) { }

        // Called when a player's level changes (after the level is applied)
        public virtual void OnLevelChanged(Player player, byte oldLevel) { }

        // Called when a player's free talent points change (right before the change is applied)
        public virtual void OnFreeTalentPointsChanged(Player player, uint points) { }

        // Called when a player's talent points are reset (right before the reset is done)
        public virtual void OnTalentsReset(Player player, bool noCost) { }

        // Called when a player's money is modified (before the modification is done)
        public virtual void OnMoneyChanged(Player player, long amount) { }

        // Called when a player gains XP (before anything is given)
        public virtual void OnGiveXP(Player player, uint amount, Unit victim) { }

        // Called when a player's reputation changes (before it is actually changed)
        public virtual void OnReputationChange(Player player, uint factionId, int standing, bool incremental) { }

        // Called when a duel is requested
        public virtual void OnDuelRequest(Player target, Player challenger) { }

        // Called when a duel starts (after 3s countdown)
        public virtual void OnDuelStart(Player player1, Player player2) { }

        // Called when a duel ends
        public virtual void OnDuelEnd(Player winner, Player loser, DuelCompleteType type) { }

        // The following methods are called when a player sends a chat message.
        public virtual void OnChat(Player player, ChatMsg type, Language lang, string msg) { }

        public virtual void OnChat(Player player, ChatMsg type, Language lang, string msg, Player receiver) { }

        public virtual void OnChat(Player player, ChatMsg type, Language lang, string msg, Group group) { }

        public virtual void OnChat(Player player, ChatMsg type, Language lang, string msg, Guild guild) { }

        public virtual void OnChat(Player player, ChatMsg type, Language lang, string msg, Channel channel) { }

        // Both of the below are called on emote opcodes.
        public virtual void OnClearEmote(Player player) { }

        public virtual void OnTextEmote(Player player, uint textEmote, uint emoteNum, ObjectGuid guid) { }

        // Called in Spell.Cast.
        public virtual void OnSpellCast(Player player, Spell spell, bool skipCheck) { }

        // Called when a player logs in.
        public virtual void OnLogin(Player player) { }

        // Called when a player logs out.
        public virtual void OnLogout(Player player) { }

        // Called when a player is created.
        public virtual void OnCreate(Player player) { }

        // Called when a player is deleted.
        public virtual void OnDelete(ObjectGuid guid, uint accountId) { }

        // Called when a player delete failed
        public virtual void OnFailedDelete(ObjectGuid guid, uint accountId) { }

        // Called when a player is about to be saved.
        public virtual void OnSave(Player player) { }

        // Called when a player is bound to an instance
        public virtual void OnBindToInstance(Player player, Difficulty difficulty, uint mapId, bool permanent, byte extendState) { }

        // Called when a player switches to a new zone
        public virtual void OnUpdateZone(Player player, uint newZone, uint newArea) { }

        // Called when a player changes to a new map (after moving to new map)
        public virtual void OnMapChanged(Player player) { }

        // Called after a player's quest status has been changed
        public virtual void OnQuestStatusChange(Player player, uint questId) { }

        // Called when a player presses release when he died
        public virtual void OnPlayerRepop(Player player) { }

        // Called when a player completes a movie
        public virtual void OnMovieComplete(Player player, uint movieId) { }

        // Called when a player choose a response from a PlayerChoice
        public virtual void OnPlayerChoiceResponse(Player player, uint choiceId, uint responseId) { }
    }

    public class GuildScript : ScriptObject
    {
        public GuildScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return false; }

        // Called when a member is added to the guild.
        public virtual void OnAddMember(Guild guild, Player player, byte plRank) { }

        // Called when a member is removed from the guild.
        public virtual void OnRemoveMember(Guild guild, Player player, bool isDisbanding, bool isKicked) { }

        // Called when the guild MOTD (message of the day) changes.
        public virtual void OnMOTDChanged(Guild guild, string newMotd) { }

        // Called when the guild info is altered.
        public virtual void OnInfoChanged(Guild guild, string newInfo) { }

        // Called when a guild is created.
        public virtual void OnCreate(Guild guild, Player leader, string name) { }

        // Called when a guild is disbanded.
        public virtual void OnDisband(Guild guild) { }

        // Called when a guild member withdraws money from a guild bank.
        public virtual void OnMemberWitdrawMoney(Guild guild, Player player, ulong amount, bool isRepair) { }

        // Called when a guild member deposits money in a guild bank.
        public virtual void OnMemberDepositMoney(Guild guild, Player player, ulong amount) { }

        // Called when a guild member moves an item in a guild bank.
        public virtual void OnItemMove(Guild guild, Player player, Item pItem, bool isSrcBank, byte srcContainer, byte srcSlotId, bool isDestBank, byte destContainer, byte destSlotId) { }

        public virtual void OnEvent(Guild guild, byte eventType, ulong playerGuid1, ulong playerGuid2, byte newRank) { }

        public virtual void OnBankEvent(Guild guild, byte eventType, byte tabId, ulong playerGuid, uint itemOrMoney, ushort itemStackCount, byte destTabId) { }
    }

    public class GroupScript : ScriptObject
    {
        public GroupScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return false; }

        // Called when a member is added to a group.
        public virtual void OnAddMember(Group group, ObjectGuid guid) { }

        // Called when a member is invited to join a group.
        public virtual void OnInviteMember(Group group, ObjectGuid guid) { }

        // Called when a member is removed from a group.
        public virtual void OnRemoveMember(Group group, ObjectGuid guid, RemoveMethod method, ObjectGuid kicker, string reason) { }

        // Called when the leader of a group is changed.
        public virtual void OnChangeLeader(Group group, ObjectGuid newLeaderGuid, ObjectGuid oldLeaderGuid) { }

        // Called when a group is disbanded.
        public virtual void OnDisband(Group group) { }
    }

    public class AreaTriggerEntityScript : ScriptObject
    {
        public AreaTriggerEntityScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when a AreaTriggerAI object is needed for the areatrigger.
        public virtual AreaTriggerAI GetAI(AreaTrigger at) { return null; }
    }

    public class ConversationScript : ScriptObject
    {
        public ConversationScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when Conversation is created but not added to Map yet.
        public virtual void OnConversationCreate(Conversation conversation, Unit creator) { }

        // Called when player sends CMSG_CONVERSATION_LINE_STARTED with valid conversation guid
        public virtual void OnConversationLineStarted(Conversation conversation, uint lineId, Player sender) { }
    }

    public class SceneScript : ScriptObject
    {
        public SceneScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when a player start a scene
        public virtual void OnSceneStart(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate) { }

        // Called when a player receive trigger from scene
        public virtual void OnSceneTriggerEvent(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName) { }

        // Called when a scene is canceled
        public virtual void OnSceneCancel(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate) { }

        // Called when a scene is completed
        public virtual void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate) { }
    }

    public class QuestScript : ScriptObject
    {
        public QuestScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when a quest status change
        public virtual void OnQuestStatusChange(Player player, Quest quest, QuestStatus oldStatus, QuestStatus newStatus) { }

        // Called for auto accept quests when player closes quest UI after seeing initial quest details
        public virtual void OnAcknowledgeAutoAccept(Player player, Quest quest) { }

        // Called when a quest objective data change
        public virtual void OnQuestObjectiveChange(Player player, Quest quest, QuestObjective objective, int oldAmount, int newAmount) { }
    }

    public class WorldStateScript : ScriptObject
    {
        public WorldStateScript(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Called when worldstate changes value, map is optional
        public virtual void OnValueChange(int worldStateId, int oldValue, int newValue, Map map) { }
    }
}

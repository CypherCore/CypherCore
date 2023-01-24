// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
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
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAchievement;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Scripting.Interfaces.IAreaTriggerEntity;
using Game.Scripting.Interfaces.IAuctionHouse;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.IBattlefield;
using Game.Scripting.Interfaces.IBattleground;
using Game.Scripting.Interfaces.ICondition;
using Game.Scripting.Interfaces.IConversation;
using Game.Scripting.Interfaces.ICreature;
using Game.Scripting.Interfaces.IDynamicObject;
using Game.Scripting.Interfaces.IFormula;
using Game.Scripting.Interfaces.IGameObject;
using Game.Scripting.Interfaces.IGroup;
using Game.Scripting.Interfaces.IGuild;
using Game.Scripting.Interfaces.IItem;
using Game.Scripting.Interfaces.IMap;
using Game.Scripting.Interfaces.IOutdoorPvP;
using Game.Scripting.Interfaces.IQuest;
using Game.Scripting.Interfaces.IScene;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting.Interfaces.ITransport;
using Game.Scripting.Interfaces.IUnit;
using Game.Scripting.Interfaces.IVehicle;
using Game.Scripting.Interfaces.IWeather;
using Game.Scripting.Interfaces.IWorld;
using Game.Scripting.Interfaces.IWorldState;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Scripting
{
    public abstract class ScriptObject : IScriptObject
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

    abstract class GenericSpellScriptLoader<S> : SpellScriptLoader where S : SpellScript
    {
        public GenericSpellScriptLoader(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public override SpellScript GetSpellScript() { return (S)Activator.CreateInstance(typeof(S), _args); }

        object[] _args;
    }

    abstract class GenericAuraScriptLoader<A> : AuraScriptLoader where A : AuraScript
    {
        public GenericAuraScriptLoader(string name, object[] args) : base(name)
        {
            _args = args;
        }

        public override AuraScript GetAuraScript() { return (A)Activator.CreateInstance(typeof(A), _args); }

        object[] _args;
    }

    public abstract class SpellScriptLoader : ScriptObject, ISpellScriptLoaderGetSpellScript
    {
        public SpellScriptLoader(string name) : base(name)
        {
            Global.ScriptMgr.AddScript<SpellScriptLoader>(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Should return a fully valid SpellScript.
        public virtual SpellScript GetSpellScript() { return null; }
    }

    public abstract class AuraScriptLoader : ScriptObject, IAuraScriptLoaderGetAuraScript
    {
        public AuraScriptLoader(string name) : base(name)
        {
            Global.ScriptMgr.AddScript<AuraScriptLoader>(this);
        }

        public override bool IsDatabaseBound() { return true; }

        // Should return a fully valid AuraScript.
        public virtual AuraScript GetAuraScript() { return null; }
    }

    public abstract class ScriptObjectAutoAdd : ScriptObject
    {
        protected ScriptObjectAutoAdd(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }
    }

    public abstract class ScriptObjectAutoAddDBBound : ScriptObject
    {
        protected ScriptObjectAutoAddDBBound(string name) : base(name)
        {
            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound()
        {
            return true;
        }
    }

    #region Map Script Base types
    public abstract class MapScript<T> : ScriptObject where T : Map
    {
        public MapScript(string name, uint mapId) : base(name)
        {
            _mapEntry = CliDB.MapStorage.LookupByKey(mapId);

            if (_mapEntry == null)
                Log.outError(LogFilter.Scripts, "Invalid MapScript for {0}; no such map ID.", mapId);
        }

        // Gets the MapEntry structure associated with this script. Can return NULL.
        public MapRecord GetEntry() { return _mapEntry; }

        MapRecord _mapEntry;
    }

    public abstract class WorldMapScript : MapScript<Map>
    {
        public WorldMapScript(string name, uint mapId) : base(name, mapId)
        {
            if (GetEntry() != null && !GetEntry().IsWorldMap())
                Log.outError(LogFilter.Scripts, "WorldMapScript for map {0} is invalid.", mapId);

            Global.ScriptMgr.AddScript(this);
        }
    }

    public abstract class InstanceMapScript : MapScript<InstanceMap>
    {
        public InstanceMapScript(string name, uint mapId) : base(name, mapId)
        {
            if (GetEntry() != null && !GetEntry().IsDungeon())
                Log.outError(LogFilter.Scripts, "InstanceMapScript for map {0} is invalid.", mapId);

            Global.ScriptMgr.AddScript(this);
        }

        public override bool IsDatabaseBound() { return true; }
    }

    public abstract class BattlegroundMapScript : MapScript<BattlegroundMap>
    {
        public BattlegroundMapScript(string name, uint mapId) : base(name, mapId)
        {
            if (GetEntry() != null && GetEntry().IsBattleground())
                Log.outError(LogFilter.Scripts, "BattlegroundMapScript for map {0} is invalid.", mapId);

            Global.ScriptMgr.AddScript(this);
        }
    }
    #endregion


    public class QuestScript : ScriptObject, IQuestOnAckAutoAccept, IQuestOnQuestObjectiveChange, IQuestOnQuestStatusChange
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

}

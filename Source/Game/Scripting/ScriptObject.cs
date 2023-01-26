// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Chat;
using Game.Conditions;
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
using Game.Scripting.Interfaces.IBattlefield;
using Game.Scripting.Interfaces.IBattleground;
using Game.Scripting.Interfaces.ICondition;
using Game.Scripting.Interfaces.IConversation;
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
using Game.Scripting.Interfaces.ITransport;
using Game.Scripting.Interfaces.IUnit;
using Game.Scripting.Interfaces.IVehicle;
using Game.Scripting.Interfaces.IWeather;
using Game.Scripting.Interfaces.IWorld;
using Game.Scripting.Interfaces.IWorldState;
using Game.Spells;
using System;

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

}

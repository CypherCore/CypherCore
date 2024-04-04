// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.PvP;
using Game.Networking.Packets;
using Game.Scripting;
using System.Collections.Generic;

namespace Scripts.OutdoorPVP.HellfirePeninsula
{
    class HellfirePeninsulaPvP : OutdoorPvP
    {
        // how many towers are controlled
        uint m_AllianceTowersControlled;
        uint m_HordeTowersControlled;
        List<ObjectGuid> _controlZoneGUIDs = new();

        public HellfirePeninsulaPvP(Map map) : base(map)
        {
            m_TypeId = OutdoorPvPTypes.HellfirePeninsula;
            m_AllianceTowersControlled = 0;
            m_HordeTowersControlled = 0;

            ControlZoneHandlers[(uint)GameObjectIds.TowerS] = new HellfirePeninsulaControlZoneHandler(this);
            GetControlZoneTowerSouthHandler().SetFlagArtKitAlliance(65);
            GetControlZoneTowerSouthHandler().SetFlagArtKitHorde(64);
            GetControlZoneTowerSouthHandler().SetFlagArtKitNeutral(66);
            GetControlZoneTowerSouthHandler().SetTextCaptureAlliance(DefenseMessages.BrokenHillTakenAlliance);
            GetControlZoneTowerSouthHandler().SetTextCaptureHorde(DefenseMessages.BrokenHillTakenHorde);
            GetControlZoneTowerSouthHandler().SetWorldstateAlliance(WorldStateIds.UiTowerSA);
            GetControlZoneTowerSouthHandler().SetWorldstateHorde(WorldStateIds.UiTowerSH);
            GetControlZoneTowerSouthHandler().SetWorldstateNeutral(WorldStateIds.UiTowerSN);
            GetControlZoneTowerSouthHandler().SetKillCredit((uint)KillCreditIds.TowerS);

            ControlZoneHandlers[(uint)GameObjectIds.TowerN] = new HellfirePeninsulaControlZoneHandler(this);
            GetControlZoneTowerNorthHandler().SetFlagArtKitAlliance(62);
            GetControlZoneTowerNorthHandler().SetFlagArtKitHorde(61);
            GetControlZoneTowerNorthHandler().SetFlagArtKitNeutral(63);
            GetControlZoneTowerNorthHandler().SetTextCaptureAlliance(DefenseMessages.OverlookTakenAlliance);
            GetControlZoneTowerNorthHandler().SetTextCaptureHorde(DefenseMessages.OverlookTakenHorde);
            GetControlZoneTowerNorthHandler().SetWorldstateAlliance(WorldStateIds.UiTowerNA);
            GetControlZoneTowerNorthHandler().SetWorldstateHorde(WorldStateIds.UiTowerNH);
            GetControlZoneTowerNorthHandler().SetWorldstateNeutral(WorldStateIds.UiTowerNN);
            GetControlZoneTowerNorthHandler().SetKillCredit((uint)KillCreditIds.TowerN);

            ControlZoneHandlers[(uint)GameObjectIds.TowerW] = new HellfirePeninsulaControlZoneHandler(this);
            GetControlZoneTowerWestHandler().SetFlagArtKitAlliance(67);
            GetControlZoneTowerWestHandler().SetFlagArtKitHorde(68);
            GetControlZoneTowerWestHandler().SetFlagArtKitNeutral(69);
            GetControlZoneTowerWestHandler().SetTextCaptureAlliance(DefenseMessages.StadiumTakenAlliance);
            GetControlZoneTowerWestHandler().SetTextCaptureHorde(DefenseMessages.StadiumTakenHorde);
            GetControlZoneTowerWestHandler().SetWorldstateAlliance(WorldStateIds.UiTowerWA);
            GetControlZoneTowerWestHandler().SetWorldstateHorde(WorldStateIds.UiTowerWH);
            GetControlZoneTowerWestHandler().SetWorldstateNeutral(WorldStateIds.UiTowerWN);
            GetControlZoneTowerWestHandler().SetKillCredit((uint)KillCreditIds.TowerW);
        }

        public override bool SetupOutdoorPvP()
        {
            m_AllianceTowersControlled = 0;
            m_HordeTowersControlled = 0;

            // add the zones affected by the pvp buff
            for (int i = 0; i < Misc.BuffZones.Length; ++i)
                RegisterZone(Misc.BuffZones[i]);

            return true;
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            switch (go.GetEntry())
            {
                case 183514:
                    GetControlZoneTowerSouthHandler().SetFlagGuid(go.GetGUID());
                    break;
                case 182525:
                    GetControlZoneTowerNorthHandler().SetFlagGuid(go.GetGUID());
                    break;
                case 183515:
                    GetControlZoneTowerWestHandler().SetFlagGuid(go.GetGUID());
                    break;
                default:
                    break;
            }

            base.OnGameObjectCreate(go);
        }

        public override void HandlePlayerEnterZone(Player player, uint zone)
        {
            // add buffs
            if (player.GetTeam() == Team.Alliance)
            {
                if (m_AllianceTowersControlled >= 3)
                    player.CastSpell(player, SpellIds.AllianceBuff, true);
            }
            else
            {
                if (m_HordeTowersControlled >= 3)
                    player.CastSpell(player, SpellIds.HordeBuff, true);
            }
            base.HandlePlayerEnterZone(player, zone);
        }

        public override void HandlePlayerLeaveZone(Player player, uint zone)
        {
            // remove buffs
            if (player.GetTeam() == Team.Alliance)
                player.RemoveAurasDueToSpell(SpellIds.AllianceBuff);
            else
                player.RemoveAurasDueToSpell(SpellIds.HordeBuff);

            base.HandlePlayerLeaveZone(player, zone);
        }

        public override void Update(uint diff)
        {
            base.Update(diff);

            if (m_AllianceTowersControlled == 3)
                TeamApplyBuff(BattleGroundTeamId.Alliance, SpellIds.AllianceBuff, SpellIds.HordeBuff);
            else if (m_HordeTowersControlled == 3)
                TeamApplyBuff(BattleGroundTeamId.Horde, SpellIds.HordeBuff, SpellIds.AllianceBuff);
            else
            {
                TeamCastSpell(BattleGroundTeamId.Alliance, -(int)SpellIds.AllianceBuff);
                TeamCastSpell(BattleGroundTeamId.Horde, -(int)SpellIds.HordeBuff);
            }

            SetWorldState(WorldStateIds.CountA, (int)m_AllianceTowersControlled);
            SetWorldState(WorldStateIds.CountH, (int)m_HordeTowersControlled);
        }

        public override void SendRemoveWorldStates(Player player)
        {
            InitWorldStates initWorldStates = new();
            initWorldStates.MapID = player.GetMapId();
            initWorldStates.AreaID = player.GetZoneId();
            initWorldStates.SubareaID = player.GetAreaId();
            initWorldStates.AddState(WorldStateIds.DisplayA, 0);
            initWorldStates.AddState(WorldStateIds.DisplayH, 0);
            initWorldStates.AddState(WorldStateIds.CountH, 0);
            initWorldStates.AddState(WorldStateIds.CountA, 0);

            foreach (var pair in ControlZoneHandlers)
            {
                HellfirePeninsulaControlZoneHandler handler = pair.Value as HellfirePeninsulaControlZoneHandler;
                initWorldStates.AddState(handler.GetWorldStateNeutral(), 0);
                initWorldStates.AddState(handler.GetWorldStateHorde(), 0);
                initWorldStates.AddState(handler.GetWorldStateAlliance(), 0);
            }

            player.SendPacket(initWorldStates);
        }

        public override void HandleKillImpl(Player player, Unit killed)
        {
            if (!killed.IsPlayer())
                return;

            // need to check if player is inside an capture zone
            bool isInsideCaptureZone = false;
            foreach (ObjectGuid guid in _controlZoneGUIDs)
            {
                GameObject gameObject = GetMap().GetGameObject(guid);
                if (gameObject != null)
                {
                    var insidePlayerGuids = gameObject.GetInsidePlayers();
                    if (!insidePlayerGuids.Empty())
                    {
                        if (insidePlayerGuids.Contains(player.GetGUID()))
                        {
                            isInsideCaptureZone = true;
                            break;
                        }
                    }
                }
            }

            if (isInsideCaptureZone)
            {
                if (player.GetTeam() == Team.Alliance && killed.ToPlayer().GetTeam() != Team.Alliance)
                    player.CastSpell(player, SpellIds.AlliancePlayerKillReward, true);
                else if (player.GetTeam() == Team.Horde && killed.ToPlayer().GetTeam() != Team.Horde)
                    player.CastSpell(player, SpellIds.HordePlayerKillReward, true);
            }
        }

        public uint GetAllianceTowersControlled()
        {
            return m_AllianceTowersControlled;
        }

        public void SetAllianceTowersControlled(uint count)
        {
            m_AllianceTowersControlled = count;
        }

        public uint GetHordeTowersControlled()
        {
            return m_HordeTowersControlled;
        }

        public void SetHordeTowersControlled(uint count)
        {
            m_HordeTowersControlled = count;
        }

        HellfirePeninsulaControlZoneHandler GetControlZoneTowerNorthHandler() { return ControlZoneHandlers[(uint)GameObjectIds.TowerN] as HellfirePeninsulaControlZoneHandler; }
        HellfirePeninsulaControlZoneHandler GetControlZoneTowerSouthHandler() { return ControlZoneHandlers[(uint)GameObjectIds.TowerS] as HellfirePeninsulaControlZoneHandler; }
        HellfirePeninsulaControlZoneHandler GetControlZoneTowerWestHandler() { return ControlZoneHandlers[(uint)GameObjectIds.TowerW] as HellfirePeninsulaControlZoneHandler; }
    }

    class HellfirePeninsulaControlZoneHandler : OutdoorPvPControlZoneHandler
    {
        ObjectGuid _flagGuid;
        uint _textCaptureAlliance;
        uint _textCaptureHorde;
        uint _flagArtKitNeutral;
        uint _flagArtKitHorde;
        uint _flagArtKitAlliance;
        int _worldstateNeutral;
        int _worldstateHorde;
        int _worldstateAlliance;
        uint _killCredit;

        public HellfirePeninsulaControlZoneHandler(HellfirePeninsulaPvP pvp) : base(pvp) { }

        public override void HandleProgressEventHorde(GameObject controlZone)
        {
            base.HandleProgressEventHorde(controlZone);

            controlZone.SetGoArtKit(1);
            controlZone.SendCustomAnim(0);
            GameObject flag = controlZone.GetMap().GetGameObject(_flagGuid);
            if (flag != null)
                flag.SetGoArtKit(_flagArtKitHorde);

            controlZone.GetMap().SetWorldStateValue(_worldstateHorde, 1, false);
            controlZone.GetMap().SetWorldStateValue(_worldstateAlliance, 0, false);
            controlZone.GetMap().SetWorldStateValue(_worldstateNeutral, 0, false);

            GetOutdoorPvPHP().SendDefenseMessage(Misc.BuffZones[0], _textCaptureHorde);

            var guidSet = controlZone.GetInsidePlayers();
            foreach (ObjectGuid guid in guidSet)
            {
                Player player = Global.ObjAccessor.GetPlayer(controlZone, guid);
                if (player != null && player.GetTeam() == Team.Horde)
                    player.KilledMonsterCredit(_killCredit);
            }
        }

        public override void HandleProgressEventAlliance(GameObject controlZone)
        {
            base.HandleProgressEventAlliance(controlZone);

            controlZone.SetGoArtKit(2);
            controlZone.SendCustomAnim(1);
            GameObject flag = controlZone.GetMap().GetGameObject(_flagGuid);
            if (flag != null)
                flag.SetGoArtKit(_flagArtKitAlliance);

            controlZone.GetMap().SetWorldStateValue(_worldstateHorde, 0, false);
            controlZone.GetMap().SetWorldStateValue(_worldstateAlliance, 1, false);
            controlZone.GetMap().SetWorldStateValue(_worldstateNeutral, 0, false);

            GetOutdoorPvPHP().SendDefenseMessage(Misc.BuffZones[0], _textCaptureAlliance);

            var guidSet = controlZone.GetInsidePlayers();
            foreach (ObjectGuid guid in guidSet)
            {
                Player player = Global.ObjAccessor.GetPlayer(controlZone, guid);
                if (player != null && player.GetTeam() == Team.Alliance)
                    player.KilledMonsterCredit(_killCredit);
            }
        }

        public override void HandleNeutralEventHorde(GameObject controlZone)
        {
            base.HandleNeutralEventHorde(controlZone);
            GetOutdoorPvPHP().SetHordeTowersControlled(GetOutdoorPvPHP().GetHordeTowersControlled() - 1);
        }

        public override void HandleNeutralEventAlliance(GameObject controlZone)
        {
            base.HandleNeutralEventAlliance(controlZone);
            GetOutdoorPvPHP().SetAllianceTowersControlled(GetOutdoorPvPHP().GetAllianceTowersControlled() - 1);
        }

        public override void HandleNeutralEvent(GameObject controlZone)
        {
            base.HandleNeutralEvent(controlZone);
            controlZone.SetGoArtKit(21);
            controlZone.SendCustomAnim(2);
            GameObject flag = controlZone.GetMap().GetGameObject(_flagGuid);
            if (flag != null)
                flag.SetGoArtKit(_flagArtKitNeutral);

            controlZone.GetMap().SetWorldStateValue(_worldstateHorde, 0, false);
            controlZone.GetMap().SetWorldStateValue(_worldstateAlliance, 0, false);
            controlZone.GetMap().SetWorldStateValue(_worldstateNeutral, 1, false);
        }

        public HellfirePeninsulaPvP GetOutdoorPvPHP()
        {
            return GetOutdoorPvP<HellfirePeninsulaPvP>();
        }

        public void SetFlagGuid(ObjectGuid guid) { _flagGuid = guid; }
        public void SetTextCaptureHorde(uint text) { _textCaptureHorde = text; }
        public void SetTextCaptureAlliance(uint text) { _textCaptureAlliance = text; }
        public void SetFlagArtKitNeutral(uint artKit) { _flagArtKitNeutral = artKit; }
        public void SetFlagArtKitHorde(uint artKit) { _flagArtKitHorde = artKit; }
        public void SetFlagArtKitAlliance(uint artKit) { _flagArtKitAlliance = artKit; }
        public void SetWorldstateNeutral(int id) { _worldstateNeutral = id; }
        public void SetWorldstateHorde(int id) { _worldstateHorde = id; }
        public void SetWorldstateAlliance(int id) { _worldstateAlliance = id; }
        public void SetKillCredit(uint credit) { _killCredit = credit; }

        public int GetWorldStateNeutral() { return _worldstateNeutral; }
        public int GetWorldStateHorde() { return _worldstateHorde; }
        public int GetWorldStateAlliance() { return _worldstateAlliance; }
    }

    [Script]
    class OutdoorPvP_hellfire_peninsula : OutdoorPvPScript
    {
        public OutdoorPvP_hellfire_peninsula() : base("outdoorpvp_hp") { }

        public override OutdoorPvP GetOutdoorPvP(Map map)
        {
            return new HellfirePeninsulaPvP(map);
        }
    }

    struct Misc
    {
        //  HP, citadel, ramparts, blood furnace, shattered halls, mag's lair
        public static uint[] BuffZones = { 3483, 3563, 3562, 3713, 3714, 3836 };
    }

    struct DefenseMessages
    {
        public const uint OverlookTakenAlliance = 14841; // '|cffffff00The Overlook has been taken by the Alliance!|r'
        public const uint OverlookTakenHorde = 14842; // '|cffffff00The Overlook has been taken by the Horde!|r'
        public const uint StadiumTakenAlliance = 14843; // '|cffffff00The Stadium has been taken by the Alliance!|r'
        public const uint StadiumTakenHorde = 14844; // '|cffffff00The Stadium has been taken by the Horde!|r'
        public const uint BrokenHillTakenAlliance = 14845; // '|cffffff00Broken Hill has been taken by the Alliance!|r'
        public const uint BrokenHillTakenHorde = 14846; // '|cffffff00Broken Hill has been taken by the Horde!|r'
    }

    struct SpellIds
    {
        public const uint AlliancePlayerKillReward = 32155;
        public const uint HordePlayerKillReward = 32158;
        public const uint AllianceBuff = 32071;
        public const uint HordeBuff = 32049;
    }

    enum TowerType
    {
        BrokenHill = 0,
        Overlook = 1,
        Stadium = 2,
        Num = 3
    }

    struct WorldStateIds
    {
        public const int DisplayA = 0x9ba;
        public const int DisplayH = 0x9b9;

        public const int CountH = 0x9ae;
        public const int CountA = 0x9ac;

        public const int UiTowerSA = 2483;
        public const int UiTowerSH = 2484;
        public const int UiTowerSN = 2485;

        public const int UiTowerNA = 2480;
        public const int UiTowerNH = 2481;
        public const int UiTowerNN = 2482;

        public const int UiTowerWA = 2471;
        public const int UiTowerWH = 2470;
        public const int UiTowerWN = 2472;
    }

    enum EventIds
    {
        HP_EVENT_TOWER_W_PROGRESS_HORDE = 11383,
        HP_EVENT_TOWER_W_PROGRESS_ALLIANCE = 11387,
        HP_EVENT_TOWER_W_NEUTRAL_HORDE = 11386,
        HP_EVENT_TOWER_W_NEUTRAL_ALLIANCE = 11385,

        HP_EVENT_TOWER_N_PROGRESS_HORDE = 11396,
        HP_EVENT_TOWER_N_PROGRESS_ALLIANCE = 11395,
        HP_EVENT_TOWER_N_NEUTRAL_HORDE = 11394,
        HP_EVENT_TOWER_N_NEUTRAL_ALLIANCE = 11393,

        HP_EVENT_TOWER_S_PROGRESS_HORDE = 11404,
        HP_EVENT_TOWER_S_PROGRESS_ALLIANCE = 11403,
        HP_EVENT_TOWER_S_NEUTRAL_HORDE = 11402,
        HP_EVENT_TOWER_S_NEUTRAL_ALLIANCE = 11401
    }

    enum GameObjectIds
    {
        TowerW = 182173,
        TowerN = 182174,
        TowerS = 182175
    }

    enum KillCreditIds
    {
        TowerS = 19032,
        TowerN = 19028,
        TowerW = 19029
    }
}

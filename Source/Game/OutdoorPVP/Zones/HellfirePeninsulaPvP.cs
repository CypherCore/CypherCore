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
using Game.Entities;
using Game.Maps;
using Game.Network.Packets;
using Game.Scripting;

namespace Game.PvP
{
    class HellfirePeninsulaPvP : OutdoorPvP
    {
        public HellfirePeninsulaPvP()
        {
            m_TypeId = OutdoorPvPTypes.HellfirePeninsula;
            m_AllianceTowersControlled = 0;
            m_HordeTowersControlled = 0;
        }

        public override bool SetupOutdoorPvP()
        {
            m_AllianceTowersControlled = 0;
            m_HordeTowersControlled = 0;
            SetMapFromZone(HPConst.BuffZones[0]);

            // add the zones affected by the pvp buff
            for (int i = 0; i < HPConst.BuffZones.Length; ++i)
                RegisterZone(HPConst.BuffZones[i]);

            AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPHPTowerType.BrokenHill));

            AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPHPTowerType.Overlook));

            AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPHPTowerType.Stadium));

            return true;
        }

        public override void HandlePlayerEnterZone(Player player, uint zone)
        {
            // add buffs
            if (player.GetTeam() == Team.Alliance)
            {
                if (m_AllianceTowersControlled >= 3)
                    player.CastSpell(player, OutdoorPvPHPSpells.AllianceBuff, true);
            }
            else
            {
                if (m_HordeTowersControlled >= 3)
                    player.CastSpell(player, OutdoorPvPHPSpells.HordeBuff, true);
            }
            base.HandlePlayerEnterZone(player, zone);
        }

        public override void HandlePlayerLeaveZone(Player player, uint zone)
        {
            // remove buffs
            if (player.GetTeam() == Team.Alliance)
                player.RemoveAurasDueToSpell(OutdoorPvPHPSpells.AllianceBuff);
            else
                player.RemoveAurasDueToSpell(OutdoorPvPHPSpells.HordeBuff);

            base.HandlePlayerLeaveZone(player, zone);
        }

        public override bool Update(uint diff)
        {
            bool changed = base.Update(diff);
            if (changed)
            {
                if (m_AllianceTowersControlled == 3)
                    TeamApplyBuff(TeamId.Alliance, OutdoorPvPHPSpells.AllianceBuff, OutdoorPvPHPSpells.HordeBuff);
                else if (m_HordeTowersControlled == 3)
                    TeamApplyBuff(TeamId.Horde, OutdoorPvPHPSpells.HordeBuff, OutdoorPvPHPSpells.AllianceBuff);
                else
                {
                    TeamCastSpell(TeamId.Alliance, -(int)OutdoorPvPHPSpells.AllianceBuff);
                    TeamCastSpell(TeamId.Horde, -(int)OutdoorPvPHPSpells.HordeBuff);
                }
                SendUpdateWorldState(OutdoorPvPHPWorldStates.Count_A, m_AllianceTowersControlled);
                SendUpdateWorldState(OutdoorPvPHPWorldStates.Count_H, m_HordeTowersControlled);
            }
            return changed;
        }

        public override void SendRemoveWorldStates(Player player)
        {
            player.SendUpdateWorldState(OutdoorPvPHPWorldStates.Display_A, 0);
            player.SendUpdateWorldState(OutdoorPvPHPWorldStates.Display_H, 0);
            player.SendUpdateWorldState(OutdoorPvPHPWorldStates.Count_H, 0);
            player.SendUpdateWorldState(OutdoorPvPHPWorldStates.Count_A, 0);

            for (int i = 0; i < (int)OutdoorPvPHPTowerType.Num; ++i)
            {
                player.SendUpdateWorldState(HPConst.Map_N[i], 0);
                player.SendUpdateWorldState(HPConst.Map_A[i], 0);
                player.SendUpdateWorldState(HPConst.Map_H[i], 0);
            }
        }

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            packet.AddState(OutdoorPvPHPWorldStates.Display_A, 1);
            packet.AddState(OutdoorPvPHPWorldStates.Display_H, 1);
            packet.AddState(OutdoorPvPHPWorldStates.Count_A, (int)m_AllianceTowersControlled);
            packet.AddState(OutdoorPvPHPWorldStates.Count_H, (int)m_HordeTowersControlled);

            foreach (var capture in m_capturePoints.Values)
                capture.FillInitialWorldStates(packet);
        }

        public override void HandleKillImpl(Player killer, Unit killed)
        {
            if (!killed.IsTypeId(TypeId.Player))
                return;

            if (killer.GetTeam() == Team.Alliance && killed.ToPlayer().GetTeam() != Team.Alliance)
                killer.CastSpell(killer, OutdoorPvPHPSpells.AlliancePlayerKillReward, true);
            else if (killer.GetTeam() == Team.Horde && killed.ToPlayer().GetTeam() != Team.Horde)
                killer.CastSpell(killer, OutdoorPvPHPSpells.HordePlayerKillReward, true);
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

        // how many towers are controlled
        uint m_AllianceTowersControlled;
        uint m_HordeTowersControlled;
    }

    class HellfirePeninsulaCapturePoint : OPvPCapturePoint
    {
        public HellfirePeninsulaCapturePoint(OutdoorPvP pvp, OutdoorPvPHPTowerType type) : base(pvp)
        {
            m_TowerType = (uint)type;

            var capturepoint = HPConst.CapturePoints[m_TowerType];
            var towerflag = HPConst.TowerFlags[m_TowerType];

            SetCapturePointData(capturepoint.entry, capturepoint.map, capturepoint.x, capturepoint.y, capturepoint.z, capturepoint.o, capturepoint.rot0,
                capturepoint.rot1, capturepoint.rot2, capturepoint.rot3);

            AddObject((uint)type, towerflag.entry, towerflag.map, towerflag.x, towerflag.y, towerflag.z, towerflag.o, towerflag.rot0, towerflag.rot1, towerflag.rot2, towerflag.rot3);
        }

        public override void ChangeState()
        {
            uint field = 0;
            switch (m_OldState)
            {
                case ObjectiveStates.Neutral:
                    field = HPConst.Map_N[m_TowerType];
                    break;
                case ObjectiveStates.Alliance:
                    field = HPConst.Map_A[m_TowerType];
                    uint alliance_towers = ((HellfirePeninsulaPvP)m_PvP).GetAllianceTowersControlled();
                    if (alliance_towers != 0)
                        ((HellfirePeninsulaPvP)m_PvP).SetAllianceTowersControlled(--alliance_towers);
                    break;
                case ObjectiveStates.Horde:
                    field = HPConst.Map_H[m_TowerType];
                    uint horde_towers = ((HellfirePeninsulaPvP)m_PvP).GetHordeTowersControlled();
                    if (horde_towers != 0)
                        ((HellfirePeninsulaPvP)m_PvP).SetHordeTowersControlled(--horde_towers);
                    break;
                case ObjectiveStates.NeutralAllianceChallenge:
                    field = HPConst.Map_N[m_TowerType];
                    break;
                case ObjectiveStates.NeutralHordeChallenge:
                    field = HPConst.Map_N[m_TowerType];
                    break;
                case ObjectiveStates.AllianceHordeChallenge:
                    field = HPConst.Map_A[m_TowerType];
                    break;
                case ObjectiveStates.HordeAllianceChallenge:
                    field = HPConst.Map_H[m_TowerType];
                    break;
            }

            // send world state update
            if (field != 0)
            {
                m_PvP.SendUpdateWorldState(field, 0);
                field = 0;
            }
            uint artkit = 21;
            uint artkit2 = HPConst.TowerArtKit_N[m_TowerType];
            switch (m_State)
            {
                case ObjectiveStates.Neutral:
                    field = HPConst.Map_N[m_TowerType];
                    break;
                case ObjectiveStates.Alliance:
                    {
                        field = HPConst.Map_A[m_TowerType];
                        artkit = 2;
                        artkit2 = HPConst.TowerArtKit_A[m_TowerType];
                        uint alliance_towers = ((HellfirePeninsulaPvP)m_PvP).GetAllianceTowersControlled();
                        if (alliance_towers < 3)
                            ((HellfirePeninsulaPvP)m_PvP).SetAllianceTowersControlled(++alliance_towers);
                        m_PvP.SendDefenseMessage(HPConst.BuffZones[0], HPConst.LangCapture_A[m_TowerType]);
                        break;
                    }
                case ObjectiveStates.Horde:
                    {
                        field = HPConst.Map_H[m_TowerType];
                        artkit = 1;
                        artkit2 = HPConst.TowerArtKit_H[m_TowerType];
                        uint horde_towers = ((HellfirePeninsulaPvP)m_PvP).GetHordeTowersControlled();
                        if (horde_towers < 3)
                            ((HellfirePeninsulaPvP)m_PvP).SetHordeTowersControlled(++horde_towers);
                        m_PvP.SendDefenseMessage(HPConst.BuffZones[0], HPConst.LangCapture_H[m_TowerType]);
                        break;
                    }
                case ObjectiveStates.NeutralAllianceChallenge:
                    field = HPConst.Map_N[m_TowerType];
                    break;
                case ObjectiveStates.NeutralHordeChallenge:
                    field = HPConst.Map_N[m_TowerType];
                    break;
                case ObjectiveStates.AllianceHordeChallenge:
                    field = HPConst.Map_A[m_TowerType];
                    artkit = 2;
                    artkit2 = HPConst.TowerArtKit_A[m_TowerType];
                    break;
                case ObjectiveStates.HordeAllianceChallenge:
                    field = HPConst.Map_H[m_TowerType];
                    artkit = 1;
                    artkit2 = HPConst.TowerArtKit_H[m_TowerType];
                    break;
            }

            Map map = Global.MapMgr.FindMap(530, 0);
            var bounds = map.GetGameObjectBySpawnIdStore().LookupByKey(m_capturePointSpawnId);
            foreach (var go in bounds)
                go.SetGoArtKit((byte)artkit);

            bounds = map.GetGameObjectBySpawnIdStore().LookupByKey(m_Objects[m_TowerType]);
            foreach (var go in bounds)
                go.SetGoArtKit((byte)artkit2);

            // send world state update
            if (field != 0)
                m_PvP.SendUpdateWorldState(field, 1);

            // complete quest objective
            if (m_State == ObjectiveStates.Alliance || m_State == ObjectiveStates.Horde)
                SendObjectiveComplete(HPConst.CreditMarker[m_TowerType], ObjectGuid.Empty);
        }

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            switch (m_State)
            {
                case ObjectiveStates.Alliance:
                case ObjectiveStates.AllianceHordeChallenge:
                    packet.AddState(HPConst.Map_N[m_TowerType], 0);
                    packet.AddState(HPConst.Map_A[m_TowerType], 1);
                    packet.AddState(HPConst.Map_H[m_TowerType], 0);
                    break;
                case ObjectiveStates.Horde:
                case ObjectiveStates.HordeAllianceChallenge:
                    packet.AddState(HPConst.Map_N[m_TowerType], 0);
                    packet.AddState(HPConst.Map_A[m_TowerType], 0);
                    packet.AddState(HPConst.Map_H[m_TowerType], 1);
                    break;
                case ObjectiveStates.Neutral:
                case ObjectiveStates.NeutralAllianceChallenge:
                case ObjectiveStates.NeutralHordeChallenge:
                default:
                    packet.AddState(HPConst.Map_N[m_TowerType], 1);
                    packet.AddState(HPConst.Map_A[m_TowerType], 0);
                    packet.AddState(HPConst.Map_H[m_TowerType], 0);
                    break;
            }
        }

        uint m_TowerType;
    }

    [Script]
    class OutdoorPvP_hellfire_peninsula : OutdoorPvPScript
    {
        public OutdoorPvP_hellfire_peninsula() : base("outdoorpvp_hp") { }

        public override OutdoorPvP GetOutdoorPvP()
        {
            return new HellfirePeninsulaPvP();
        }
    }

    struct HPConst
    {
        public static uint[] LangCapture_A = { DefenseMessages.BrokenHillTakenAlliance, DefenseMessages.OverlookTakenAlliance, DefenseMessages.StadiumTakenAlliance };

        public static uint[] LangCapture_H = { DefenseMessages.BrokenHillTakenHorde, DefenseMessages.OverlookTakenHorde, DefenseMessages.StadiumTakenHorde };

        public static uint[] Map_N = { 0x9b5, 0x9b2, 0x9a8 };

        public static uint[] Map_A = { 0x9b3, 0x9b0, 0x9a7 };

        public static uint[] Map_H = { 0x9b4, 0x9b1, 0x9a6 };

        public static uint[] TowerArtKit_A = { 65, 62, 67 };

        public static uint[] TowerArtKit_H = { 64, 61, 68 };

        public static uint[] TowerArtKit_N = { 66, 63, 69 };

        //  HP, citadel, ramparts, blood furnace, shattered halls, mag's lair
        public static uint[] BuffZones = { 3483, 3563, 3562, 3713, 3714, 3836 };

        public static uint[] CreditMarker = { 19032, 19028, 19029 };

        public static uint[] CapturePointEventEnter = { 11404, 11396, 11388 };

        public static uint[] CapturePointEventLeave = { 11403, 11395, 11387 };

        public static go_type[] CapturePoints =
        {
            new go_type(182175, 530, -471.462f, 3451.09f, 34.6432f, 0.174533f, 0.0f, 0.0f, 0.087156f, 0.996195f),      // 0 - Broken Hill
            new go_type(182174, 530, -184.889f, 3476.93f, 38.205f, -0.017453f, 0.0f, 0.0f, 0.008727f, -0.999962f),     // 1 - Overlook
            new go_type(182173, 530, -290.016f, 3702.42f, 56.6729f, 0.034907f, 0.0f, 0.0f, 0.017452f, 0.999848f)     // 2 - Stadium
        };

        public static go_type[] TowerFlags =
        {
            new go_type(183514, 530, -467.078f, 3528.17f, 64.7121f, 3.14159f, 0.0f, 0.0f, 1.0f, 0.0f),  // 0 broken hill
            new go_type(182525, 530, -187.887f, 3459.38f, 60.0403f, -3.12414f, 0.0f, 0.0f, 0.999962f, -0.008727f), // 1 overlook
            new go_type(183515, 530, -289.610f, 3696.83f, 75.9447f, 3.12414f, 0.0f, 0.0f, 0.999962f, 0.008727f) // 2 stadium
        };
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

    struct OutdoorPvPHPSpells
    {
        public const uint AlliancePlayerKillReward = 32155;
        public const uint HordePlayerKillReward = 32158;
        public const uint AllianceBuff = 32071;
        public const uint HordeBuff = 32049;
    }

    enum OutdoorPvPHPTowerType
    {
        BrokenHill = 0,
        Overlook = 1,
        Stadium = 2,
        Num = 3
    }

    struct OutdoorPvPHPWorldStates
    {
        public const uint Display_A = 0x9ba;
        public const uint Display_H = 0x9b9;

        public const uint Count_H = 0x9ae;
        public const uint Count_A = 0x9ac;
    }
}

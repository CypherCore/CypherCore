// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces.IOutdoorPvP;

namespace Game.PvP
{
    internal class HellfirePeninsulaPvP : OutdoorPvP
    {
        // how many towers are controlled
        private uint _AllianceTowersControlled;
        private uint _HordeTowersControlled;
        private readonly ulong[] _towerFlagSpawnIds = new ulong[(int)OutdoorPvPHPTowerType.Num];

        public HellfirePeninsulaPvP(Map map) : base(map)
        {
            _TypeId = OutdoorPvPTypes.HellfirePeninsula;
            _AllianceTowersControlled = 0;
            _HordeTowersControlled = 0;
        }

        public override bool SetupOutdoorPvP()
        {
            _AllianceTowersControlled = 0;
            _HordeTowersControlled = 0;

            // add the zones affected by the pvp buff
            for (int i = 0; i < HPConst.BuffZones.Length; ++i)
                RegisterZone(HPConst.BuffZones[i]);

            return true;
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            switch (go.GetEntry())
            {
                case 182175:
                    AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPHPTowerType.BrokenHill, go, _towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.BrokenHill]));

                    break;
                case 182174:
                    AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPHPTowerType.Overlook, go, _towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.Overlook]));

                    break;
                case 182173:
                    AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPHPTowerType.Stadium, go, _towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.Stadium]));

                    break;
                case 183514:
                    _towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.BrokenHill] = go.GetSpawnId();

                    break;
                case 182525:
                    _towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.Overlook] = go.GetSpawnId();

                    break;
                case 183515:
                    _towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.Stadium] = go.GetSpawnId();

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
                if (_AllianceTowersControlled >= 3)
                    player.CastSpell(player, OutdoorPvPHPSpells.AllianceBuff, true);
            }
            else
            {
                if (_HordeTowersControlled >= 3)
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
                if (_AllianceTowersControlled == 3)
                {
                    TeamApplyBuff(TeamId.Alliance, OutdoorPvPHPSpells.AllianceBuff, OutdoorPvPHPSpells.HordeBuff);
                }
                else if (_HordeTowersControlled == 3)
                {
                    TeamApplyBuff(TeamId.Horde, OutdoorPvPHPSpells.HordeBuff, OutdoorPvPHPSpells.AllianceBuff);
                }
                else
                {
                    TeamCastSpell(TeamId.Alliance, -(int)OutdoorPvPHPSpells.AllianceBuff);
                    TeamCastSpell(TeamId.Horde, -(int)OutdoorPvPHPSpells.HordeBuff);
                }

                SetWorldState(OutdoorPvPHPWorldStates.Count_A, (int)_AllianceTowersControlled);
                SetWorldState(OutdoorPvPHPWorldStates.Count_H, (int)_HordeTowersControlled);
            }

            return changed;
        }

        public override void SendRemoveWorldStates(Player player)
        {
            InitWorldStates initWorldStates = new();
            initWorldStates.MapID = player.GetMapId();
            initWorldStates.AreaID = player.GetZoneId();
            initWorldStates.SubareaID = player.GetAreaId();
            initWorldStates.AddState(OutdoorPvPHPWorldStates.Display_A, 0);
            initWorldStates.AddState(OutdoorPvPHPWorldStates.Display_H, 0);
            initWorldStates.AddState(OutdoorPvPHPWorldStates.Count_H, 0);
            initWorldStates.AddState(OutdoorPvPHPWorldStates.Count_A, 0);

            for (int i = 0; i < (int)OutdoorPvPHPTowerType.Num; ++i)
            {
                initWorldStates.AddState(HPConst.Map_N[i], 0);
                initWorldStates.AddState(HPConst.Map_A[i], 0);
                initWorldStates.AddState(HPConst.Map_H[i], 0);
            }

            player.SendPacket(initWorldStates);
        }

        public override void HandleKillImpl(Player killer, Unit killed)
        {
            if (!killed.IsTypeId(TypeId.Player))
                return;

            if (killer.GetTeam() == Team.Alliance &&
                killed.ToPlayer().GetTeam() != Team.Alliance)
                killer.CastSpell(killer, OutdoorPvPHPSpells.AlliancePlayerKillReward, true);
            else if (killer.GetTeam() == Team.Horde &&
                     killed.ToPlayer().GetTeam() != Team.Horde)
                killer.CastSpell(killer, OutdoorPvPHPSpells.HordePlayerKillReward, true);
        }

        public uint GetAllianceTowersControlled()
        {
            return _AllianceTowersControlled;
        }

        public void SetAllianceTowersControlled(uint count)
        {
            _AllianceTowersControlled = count;
        }

        public uint GetHordeTowersControlled()
        {
            return _HordeTowersControlled;
        }

        public void SetHordeTowersControlled(uint count)
        {
            _HordeTowersControlled = count;
        }
    }

    internal class HellfirePeninsulaCapturePoint : OPvPCapturePoint
    {
        private readonly ulong _flagSpawnId;

        private readonly uint _TowerType;

        public HellfirePeninsulaCapturePoint(OutdoorPvP pvp, OutdoorPvPHPTowerType type, GameObject go, ulong flagSpawnId) : base(pvp)
        {
            _TowerType = (uint)type;
            _flagSpawnId = flagSpawnId;

            _capturePointSpawnId = go.GetSpawnId();
            _capturePoint = go;
            SetCapturePointData(go.GetEntry());
        }

        public override void ChangeState()
        {
            uint field = 0;

            switch (OldState)
            {
                case ObjectiveStates.Neutral:
                    field = HPConst.Map_N[_TowerType];

                    break;
                case ObjectiveStates.Alliance:
                    field = HPConst.Map_A[_TowerType];
                    uint alliance_towers = ((HellfirePeninsulaPvP)PvP).GetAllianceTowersControlled();

                    if (alliance_towers != 0)
                        ((HellfirePeninsulaPvP)PvP).SetAllianceTowersControlled(--alliance_towers);

                    break;
                case ObjectiveStates.Horde:
                    field = HPConst.Map_H[_TowerType];
                    uint horde_towers = ((HellfirePeninsulaPvP)PvP).GetHordeTowersControlled();

                    if (horde_towers != 0)
                        ((HellfirePeninsulaPvP)PvP).SetHordeTowersControlled(--horde_towers);

                    break;
                case ObjectiveStates.NeutralAllianceChallenge:
                    field = HPConst.Map_N[_TowerType];

                    break;
                case ObjectiveStates.NeutralHordeChallenge:
                    field = HPConst.Map_N[_TowerType];

                    break;
                case ObjectiveStates.AllianceHordeChallenge:
                    field = HPConst.Map_A[_TowerType];

                    break;
                case ObjectiveStates.HordeAllianceChallenge:
                    field = HPConst.Map_H[_TowerType];

                    break;
            }

            // send world State update
            if (field != 0)
            {
                PvP.SetWorldState((int)field, 0);
                field = 0;
            }

            uint artkit = 21;
            uint artkit2 = HPConst.TowerArtKit_N[_TowerType];

            switch (State)
            {
                case ObjectiveStates.Neutral:
                    field = HPConst.Map_N[_TowerType];

                    break;
                case ObjectiveStates.Alliance:
                    {
                        field = HPConst.Map_A[_TowerType];
                        artkit = 2;
                        artkit2 = HPConst.TowerArtKit_A[_TowerType];
                        uint alliance_towers = ((HellfirePeninsulaPvP)PvP).GetAllianceTowersControlled();

                        if (alliance_towers < 3)
                            ((HellfirePeninsulaPvP)PvP).SetAllianceTowersControlled(++alliance_towers);

                        PvP.SendDefenseMessage(HPConst.BuffZones[0], HPConst.LangCapture_A[_TowerType]);

                        break;
                    }
                case ObjectiveStates.Horde:
                    {
                        field = HPConst.Map_H[_TowerType];
                        artkit = 1;
                        artkit2 = HPConst.TowerArtKit_H[_TowerType];
                        uint horde_towers = ((HellfirePeninsulaPvP)PvP).GetHordeTowersControlled();

                        if (horde_towers < 3)
                            ((HellfirePeninsulaPvP)PvP).SetHordeTowersControlled(++horde_towers);

                        PvP.SendDefenseMessage(HPConst.BuffZones[0], HPConst.LangCapture_H[_TowerType]);

                        break;
                    }
                case ObjectiveStates.NeutralAllianceChallenge:
                    field = HPConst.Map_N[_TowerType];

                    break;
                case ObjectiveStates.NeutralHordeChallenge:
                    field = HPConst.Map_N[_TowerType];

                    break;
                case ObjectiveStates.AllianceHordeChallenge:
                    field = HPConst.Map_A[_TowerType];
                    artkit = 2;
                    artkit2 = HPConst.TowerArtKit_A[_TowerType];

                    break;
                case ObjectiveStates.HordeAllianceChallenge:
                    field = HPConst.Map_H[_TowerType];
                    artkit = 1;
                    artkit2 = HPConst.TowerArtKit_H[_TowerType];

                    break;
            }

            Map map = Global.MapMgr.FindMap(530, 0);
            var bounds = map.GetGameObjectBySpawnIdStore().LookupByKey(_capturePointSpawnId);

            foreach (var go in bounds)
                go.SetGoArtKit(artkit);

            bounds = map.GetGameObjectBySpawnIdStore().LookupByKey(_flagSpawnId);

            foreach (var go in bounds)
                go.SetGoArtKit(artkit2);

            // send world State update
            if (field != 0)
                PvP.SetWorldState((int)field, 1);

            // complete quest objective
            if (State == ObjectiveStates.Alliance ||
                State == ObjectiveStates.Horde)
                SendObjectiveComplete(HPConst.CreditMarker[_TowerType], ObjectGuid.Empty);
        }
    }

    [Script]
    internal class OutdoorPvP_hellfire_peninsula : ScriptObjectAutoAddDBBound, IOutdoorPvPGetOutdoorPvP
    {
        public OutdoorPvP_hellfire_peninsula() : base("outdoorpvp_hp")
        {
        }

        public OutdoorPvP GetOutdoorPvP(Map map)
        {
            return new HellfirePeninsulaPvP(map);
        }
    }

    internal struct HPConst
    {
        public static uint[] LangCapture_A =
        {
            DefenseMessages.BrokenHillTakenAlliance, DefenseMessages.OverlookTakenAlliance, DefenseMessages.StadiumTakenAlliance
        };

        public static uint[] LangCapture_H =
        {
            DefenseMessages.BrokenHillTakenHorde, DefenseMessages.OverlookTakenHorde, DefenseMessages.StadiumTakenHorde
        };

        public static uint[] Map_N =
        {
            2485, 2482, 0x9a8
        };

        public static uint[] Map_A =
        {
            2483, 2480, 2471
        };

        public static uint[] Map_H =
        {
            2484, 2481, 2470
        };

        public static uint[] TowerArtKit_A =
        {
            65, 62, 67
        };

        public static uint[] TowerArtKit_H =
        {
            64, 61, 68
        };

        public static uint[] TowerArtKit_N =
        {
            66, 63, 69
        };

        //  HP, citadel, ramparts, blood furnace, shattered halls, mag's lair
        public static uint[] BuffZones =
        {
            3483, 3563, 3562, 3713, 3714, 3836
        };

        public static uint[] CreditMarker =
        {
            19032, 19028, 19029
        };

        public static uint[] CapturePointEventEnter =
        {
            11404, 11396, 11388
        };

        public static uint[] CapturePointEventLeave =
        {
            11403, 11395, 11387
        };
    }

    internal struct DefenseMessages
    {
        public const uint OverlookTakenAlliance = 14841;   // '|cffffff00The Overlook has been taken by the Alliance!|r'
        public const uint OverlookTakenHorde = 14842;      // '|cffffff00The Overlook has been taken by the Horde!|r'
        public const uint StadiumTakenAlliance = 14843;    // '|cffffff00The Stadium has been taken by the Alliance!|r'
        public const uint StadiumTakenHorde = 14844;       // '|cffffff00The Stadium has been taken by the Horde!|r'
        public const uint BrokenHillTakenAlliance = 14845; // '|cffffff00Broken Hill has been taken by the Alliance!|r'
        public const uint BrokenHillTakenHorde = 14846;    // '|cffffff00Broken Hill has been taken by the Horde!|r'
    }

    internal struct OutdoorPvPHPSpells
    {
        public const uint AlliancePlayerKillReward = 32155;
        public const uint HordePlayerKillReward = 32158;
        public const uint AllianceBuff = 32071;
        public const uint HordeBuff = 32049;
    }

    internal enum OutdoorPvPHPTowerType
    {
        BrokenHill = 0,
        Overlook = 1,
        Stadium = 2,
        Num = 3
    }

    internal struct OutdoorPvPHPWorldStates
    {
        public const int Display_A = 0x9ba;
        public const int Display_H = 0x9b9;

        public const int Count_H = 0x9ae;
        public const int Count_A = 0x9ac;
    }
}
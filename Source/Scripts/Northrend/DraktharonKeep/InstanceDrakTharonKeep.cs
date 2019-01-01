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

using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.Northrend.DraktharonKeep
{
    struct DTKDataTypes
    {
        // Encounter States/Boss Guids
        public const uint Trollgore = 0;
        public const uint Novos = 1;
        public const uint KingDred = 2;
        public const uint TharonJa = 3;

        // Additional Data
        //public const uint KingDredAchiev;

        public const uint TrollgoreInvaderSummoner1 = 4;
        public const uint TrollgoreInvaderSummoner2 = 5;
        public const uint TrollgoreInvaderSummoner3 = 6;

        public const uint NovosCrystal1 = 7;
        public const uint NovosCrystal2 = 8;
        public const uint NovosCrystal3 = 9;
        public const uint NovosCrystal4 = 10;
        public const uint NovosSummoner1 = 11;
        public const uint NovosSummoner2 = 12;
        public const uint NovosSummoner3 = 13;
        public const uint NovosSummoner4 = 14;

        public const int ActionCrystalHandlerDied = 15;
    }

    struct DTKCreatureIds
    {
        public const uint Trollgore = 26630;
        public const uint Novos = 26631;
        public const uint KingDred = 27483;
        public const uint TharonJa = 26632;

        // Trollgore
        public const uint DrakkariInvaderA = 27709;
        public const uint DrakkariInvaderB = 27753;
        public const uint DrakkariInvaderC = 27754;

        // Novos
        public const uint CrystalChannelTarget = 26712;
        public const uint CrystalHandler = 26627;
        public const uint HulkingCorpse = 27597;
        public const uint FetidTrollCorpse = 27598;
        public const uint RisenShadowcaster = 27600;

        // King Dred
        public const uint DrakkariGutripper = 26641;
        public const uint DrakkariScytheclaw = 26628;

        public const uint WorldTrigger = 22515;
    }

    struct DTKGameObjectIds
    {
        public const uint NovosCrystal1 = 189299;
        public const uint NovosCrystal2 = 189300;
        public const uint NovosCrystal3 = 189301;
        public const uint NovosCrystal4 = 189302;
    }

    [Script]
    class instance_drak_tharon_keep : InstanceMapScript
    {
        public instance_drak_tharon_keep() : base(nameof(instance_drak_tharon_keep), 600) { }

        class instance_drak_tharon_keep_InstanceScript : InstanceScript
        {
            public instance_drak_tharon_keep_InstanceScript(InstanceMap map) : base(map)
            {
                SetHeaders("DTK");
                SetBossNumber(4);
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case DTKCreatureIds.Trollgore:
                        TrollgoreGUID = creature.GetGUID();
                        break;
                    case DTKCreatureIds.Novos:
                        NovosGUID = creature.GetGUID();
                        break;
                    case DTKCreatureIds.KingDred:
                        KingDredGUID = creature.GetGUID();
                        break;
                    case DTKCreatureIds.TharonJa:
                        TharonJaGUID = creature.GetGUID();
                        break;
                    case DTKCreatureIds.WorldTrigger:
                        InitializeTrollgoreInvaderSummoner(creature);
                        break;
                    case DTKCreatureIds.CrystalChannelTarget:
                        InitializeNovosSummoner(creature);
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case DTKGameObjectIds.NovosCrystal1:
                        NovosCrystalGUIDs[0] = go.GetGUID();
                        break;
                    case DTKGameObjectIds.NovosCrystal2:
                        NovosCrystalGUIDs[1] = go.GetGUID();
                        break;
                    case DTKGameObjectIds.NovosCrystal3:
                        NovosCrystalGUIDs[2] = go.GetGUID();
                        break;
                    case DTKGameObjectIds.NovosCrystal4:
                        NovosCrystalGUIDs[3] = go.GetGUID();
                        break;
                    default:
                        break;
                }
            }

            void InitializeTrollgoreInvaderSummoner(Creature creature)
            {
                float y = creature.GetPositionY();
                float z = creature.GetPositionZ();

                if (z < 50.0f)
                    return;

                if (y < -650.0f && y > -660.0f)
                    TrollgoreInvaderSummonerGuids[0] = creature.GetGUID();
                else if (y < -660.0f && y > -670.0f)
                    TrollgoreInvaderSummonerGuids[1] = creature.GetGUID();
                else if (y < -675.0f && y > -685.0f)
                    TrollgoreInvaderSummonerGuids[2] = creature.GetGUID();
            }

            void InitializeNovosSummoner(Creature creature)
            {
                float x = creature.GetPositionX();
                float y = creature.GetPositionY();
                float z = creature.GetPositionZ();

                if (x < -374.0f && x > -379.0f && y > -820.0f && y < -815.0f && z < 60.0f && z > 58.0f)
                    NovosSummonerGUIDs[0] = creature.GetGUID();
                else if (x < -379.0f && x > -385.0f && y > -820.0f && y < -815.0f && z < 60.0f && z > 58.0f)
                    NovosSummonerGUIDs[1] = creature.GetGUID();
                else if (x < -374.0f && x > -385.0f && y > -827.0f && y < -820.0f && z < 60.0f && z > 58.0f)
                    NovosSummonerGUIDs[2] = creature.GetGUID();
                else if (x < -338.0f && x > -380.0f && y > -727.0f && y < 721.0f && z < 30.0f && z > 26.0f)
                    NovosSummonerGUIDs[3] = creature.GetGUID();
            }

            public override ObjectGuid GetGuidData(uint type)
            {
                switch (type)
                {
                    case DTKDataTypes.Trollgore:
                        return TrollgoreGUID;
                    case DTKDataTypes.Novos:
                        return NovosGUID;
                    case DTKDataTypes.KingDred:
                        return KingDredGUID;
                    case DTKDataTypes.TharonJa:
                        return TharonJaGUID;
                    case DTKDataTypes.TrollgoreInvaderSummoner1:
                    case DTKDataTypes.TrollgoreInvaderSummoner2:
                    case DTKDataTypes.TrollgoreInvaderSummoner3:
                        return TrollgoreInvaderSummonerGuids[type - DTKDataTypes.TrollgoreInvaderSummoner1];
                    case DTKDataTypes.NovosCrystal1:
                    case DTKDataTypes.NovosCrystal2:
                    case DTKDataTypes.NovosCrystal3:
                    case DTKDataTypes.NovosCrystal4:
                        return NovosCrystalGUIDs[type - DTKDataTypes.NovosCrystal1];
                    case DTKDataTypes.NovosSummoner1:
                    case DTKDataTypes.NovosSummoner2:
                    case DTKDataTypes.NovosSummoner3:
                    case DTKDataTypes.NovosSummoner4:
                        return NovosSummonerGUIDs[type - DTKDataTypes.NovosSummoner1];
                }

                return ObjectGuid.Empty;
            }

            ObjectGuid TrollgoreGUID;
            ObjectGuid NovosGUID;
            ObjectGuid KingDredGUID;
            ObjectGuid TharonJaGUID;

            ObjectGuid[] TrollgoreInvaderSummonerGuids = new ObjectGuid[3];
            ObjectGuid[] NovosCrystalGUIDs = new ObjectGuid[4];
            ObjectGuid[] NovosSummonerGUIDs = new ObjectGuid[4];
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_drak_tharon_keep_InstanceScript(map);
        }
    }
}

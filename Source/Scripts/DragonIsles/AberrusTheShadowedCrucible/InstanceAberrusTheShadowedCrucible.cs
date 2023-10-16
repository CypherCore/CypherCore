// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.DragonIsles.AberrusTheShadowedCrucible
{
    //uint EncounterCount = 9;

    struct DataTypes
    {
        // Encounters
        public const uint KazzaraTheHellforged = 0;
        public const uint TheAmalgamationChamber = 1;
        public const uint TheForgottenExperiments = 2;
        public const uint AssaultOfTheZaqali = 3;
        public const uint RashokTheElder = 4;
        public const uint ZskarnTheVigilantSteward = 5;
        public const uint Magmorax = 6;
        public const uint EchoOfNeltharion = 7;
        public const uint ScalecommanderSarkareth = 8;

        // Additional public const uint 
        public const uint KazzaraGate = 9;

        // Misc
        public const uint KazzaraIntroDone = 10;
    }

    struct CreatureIds
    {
        // public const uint Bosses
        public const uint BossKazzaraTheHellforged = 201261;

        public const uint BossEternalBlaze = 201773;
        public const uint BossEssenceOfShadow = 201774;
        public const uint BossShadowflameAmalgamation = 201934;

        public const uint BossNeldris = 200912;
        public const uint BossThadrion = 200913;
        public const uint BossRionthus = 200918;

        public const uint BossWarlordKagni = 199659;
        public const uint BossRashokTheElder = 201320;
        public const uint BossZskarnTheVigilantSteward = 202637;
        public const uint BossMagmorax = 201579;
        public const uint BossEchoOfNeltharion = 204223;
        public const uint BossScalecommanderSarkareth = 205319;

        // Misc
        public const uint SabellianAtAberrusEntrance = 201575;
        public const uint ScalecommanderSarkarethAtKazzara = 202416;
    }

    struct GameObjectIds
    {
        public const uint KazzaraDoor = 398742;
        public const uint KazzaraGate = 397996;
        public const uint InvisibleDoor = 398588;
    }

    struct MiscConst
    {
        public const int ActionStartKazzaraIntro = 0;

        public const uint SpellAberrusEntranceRpConversation3 = 403409; // Winglord Dezran, Sarkareth and Zskarn (Kazzara Summon)
    }

    [Script]
    class instance_aberrus_the_shadowed_crucible : InstanceMapScript
    {
        static ObjectData[] creatureData =
            {
            new(CreatureIds.BossKazzaraTheHellforged, DataTypes.KazzaraTheHellforged),
            new(CreatureIds.BossShadowflameAmalgamation, DataTypes.TheAmalgamationChamber),
            new(CreatureIds.BossRionthus, DataTypes.TheForgottenExperiments),
            new(CreatureIds.BossWarlordKagni, DataTypes.AssaultOfTheZaqali),
            new(CreatureIds.BossRashokTheElder, DataTypes.RashokTheElder),
            new(CreatureIds.BossZskarnTheVigilantSteward, DataTypes.ZskarnTheVigilantSteward),
            new(CreatureIds.BossMagmorax, DataTypes.Magmorax),
            new(CreatureIds.BossEchoOfNeltharion, DataTypes.EchoOfNeltharion),
            new(CreatureIds.BossScalecommanderSarkareth, DataTypes.ScalecommanderSarkareth)
        };

        static DoorData[] doorData =
        {
            new (GameObjectIds.KazzaraDoor, DataTypes.KazzaraTheHellforged, DoorType.Room)
        };

        static ObjectData[] objData =
        {
            new (GameObjectIds.KazzaraGate, DataTypes.KazzaraGate)
        };

        static DungeonEncounterData[] encounters =
        {
            new(DataTypes.KazzaraTheHellforged, 2688),
            new(DataTypes.TheAmalgamationChamber, 2687),
            new(DataTypes.TheForgottenExperiments, 2693),
            new(DataTypes.AssaultOfTheZaqali, 2682),
            new(DataTypes.RashokTheElder, 2680),
            new(DataTypes.ZskarnTheVigilantSteward, 2689),
            new(DataTypes.Magmorax, 2683),
            new(DataTypes.EchoOfNeltharion, 2684),
            new(DataTypes.ScalecommanderSarkareth, 2685)
        };

        public instance_aberrus_the_shadowed_crucible() : base(nameof(instance_aberrus_the_shadowed_crucible), 2569) { }

        class instance_aberrus_the_shadowed_crucible_InstanceMapScript : InstanceScript
        {
            byte _deadSunderedMobs;
            bool _kazzaraIntroDone;

            public instance_aberrus_the_shadowed_crucible_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("Aberrus");
                SetBossNumber(9);
                LoadObjectData(creatureData, objData);
                LoadDoorData(doorData);
                LoadDungeonEncounterData(encounters);

                _kazzaraIntroDone = false;
                _deadSunderedMobs = 0;
            }

            public override uint GetData(uint dataId)
            {
                switch (dataId)
                {
                    case DataTypes.KazzaraIntroDone:
                        return _kazzaraIntroDone ? 1 : 0u;
                    default:
                        break;
                }
                return 0;
            }

            public override void SetData(uint dataId, uint value)
            {
                switch (dataId)
                {
                    case DataTypes.KazzaraIntroDone:
                        _kazzaraIntroDone = true; // no need to pass value, it will never reset to false
                        break;
                    default:
                        break;
                }
            }

            public override void OnUnitDeath(Unit unit)
            {
                Creature creature = unit.ToCreature();
                if (creature == null)
                    return;

                if (creature.HasStringId("sundered_mob"))
                {
                    if (_deadSunderedMobs >= 6)
                        return;

                    _deadSunderedMobs++;
                    if (_deadSunderedMobs >= 6)
                    {
                        Creature sarkareth = creature.FindNearestCreature(CreatureIds.ScalecommanderSarkarethAtKazzara, 300.0f);
                        if (sarkareth == null)
                            return;

                        sarkareth.CastSpell(null, MiscConst.SpellAberrusEntranceRpConversation3);
                    }
                }
            }
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_aberrus_the_shadowed_crucible_InstanceMapScript(map);
        }
    }
}
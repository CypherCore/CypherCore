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
using Framework.Dynamic;
using Framework.GameMath;
using Game;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Movement;
using Game.Network.Packets;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Northrend.IcecrownCitadel
{
    struct GunshipTexts
    {
        // High Overlord Saurfang
        public const uint SaySaurfangIntro1 = 0;
        public const uint SaySaurfangIntro2 = 1;
        public const uint SaySaurfangIntro3 = 2;
        public const uint SaySaurfangIntro4 = 3;
        public const uint SaySaurfangIntro5 = 4;
        public const uint SaySaurfangIntro6 = 5;
        public const uint SaySaurfangIntroA = 6;
        public const uint SaySaurfangBoard = 7;
        public const uint SaySaurfangEnterSkybreaker = 8;
        public const uint SaySaurfangAxethrowers = 9;
        public const uint SaySaurfangRocketeers = 10;
        public const uint SaySaurfangMages = 11;
        public const byte SaySaurfangVictory = 12;
        public const byte SaySaurfangWipe = 13;

        // Muradin Bronzebeard
        public const uint SayMuradinIntro1 = 0;
        public const uint SayMuradinIntro2 = 1;
        public const uint SayMuradinIntro3 = 2;
        public const uint SayMuradinIntro4 = 3;
        public const uint SayMuradinIntro5 = 4;
        public const uint SayMuradinIntro6 = 5;
        public const uint SayMuradinIntro7 = 6;
        public const uint SayMuradinIntroH = 7;
        public const uint SayMuradinBoard = 8;
        public const uint SayMuradinEnterOrgrimmsHammer = 9;
        public const uint SayMuradinRifleman = 10;
        public const uint SayMuradinMortar = 11;
        public const uint SayMuradinSorcerers = 12;
        public const byte SayMuradinVictory = 13;
        public const byte SayMuradinWipe = 14;

        public const byte SayZafodRocketPackActive = 0;
        public const byte SayZafodRocketPackDisabled = 1;

        public const byte SayOverheat = 0;
    }

    struct GunshipEvents
    {
        // High Overlord Saurfang
        public const uint IntroH1 = 1;
        public const uint IntroH2 = 2;
        public const uint IntroSummonSkybreaker = 3;
        public const uint IntroH3 = 4;
        public const uint IntroH4 = 5;
        public const uint IntroH5 = 6;
        public const uint IntroH6 = 7;

        // Muradin Bronzebeard
        public const uint IntroA1 = 1;
        public const uint IntroA2 = 2;
        public const uint IntroSummonOrgrimsHammer = 3;
        public const uint IntroA3 = 4;
        public const uint IntroA4 = 5;
        public const uint IntroA5 = 6;
        public const uint IntroA6 = 7;
        public const uint IntroA7 = 8;

        public const uint KeepPlayerInCombat = 9;
        public const uint SummonMage = 10;
        public const uint Adds = 11;
        public const uint AddsBoardYell = 12;
        public const uint CheckRifleman = 13;
        public const uint CheckMortar = 14;
        public const uint Cleave = 15;

        public const uint Bladestorm = 16;
        public const uint WoundingStrike = 17;
    }

    struct GunshipSpells
    {
        // Applied On Friendly Transport Npcs
        public const uint FriendlyBossDamageMod = 70339;
        public const uint CheckForPlayers = 70332;
        public const uint GunshipFallTeleport = 67335;
        public const uint TeleportPlayersOnResetA = 70446;
        public const uint TeleportPlayersOnResetH = 71284;
        public const uint TeleportPlayersOnVictory = 72340;
        public const uint Achievement = 72959;
        public const uint AwardReputationBossKill = 73843;

        // Murading Bronzebeard
        // High Overlord Saurfang
        public const uint BattleFury = 69637;
        public const uint RendingThrow = 70309;
        public const uint Cleave = 15284;
        public const uint TasteOfBlood = 69634;

        // Applied On Enemy Npcs
        public const uint MeleeTargetingOnSkybreaker = 70219;
        public const uint MeleeTargetingOnOrgrimsHammer = 70294;

        // Gunship Hull
        public const uint ExplosionWipe = 72134;
        public const uint ExplosionVictory = 72137;

        // Hostile Npcs
        public const uint TeleportToEnemyShip = 70104;
        public const uint BattleExperience = 71201;
        public const uint Experienced = 71188;
        public const uint Veteran = 71193;
        public const uint Elite = 71195;
        public const uint AddsBerserk = 72525;

        // Skybreaker Sorcerer
        // Kor'Kron Battle-Mage
        public const uint ShadowChanneling = 43897;
        public const uint BelowZero = 69705;

        // Skybreaker Rifleman
        // Kor'Kron Axethrower
        public const uint Shoot = 70162;
        public const uint HurlAxe = 70161;
        public const uint BurningPitchA = 70403;
        public const uint BurningPitchH = 70397;
        public const uint BurningPitch = 69660;

        // Skybreaker Mortar Soldier
        // Kor'Kron Rocketeer
        public const uint RocketArtilleryA = 70609;
        public const uint RocketArtilleryH = 69678;
        public const uint BurningPitchDamageA = 70383;
        public const uint BurningPitchDamageH = 70374;

        // Skybreaker Marine
        // Kor'Kron Reaver
        public const uint DesperateResolve = 69647;

        // Skybreaker Sergeant
        // Kor'Kron Sergeant
        public const uint Bladestorm = 69652;
        public const uint WoundingStrike = 69651;

        //
        public const uint LockPlayersAndTapChest = 72347;
        public const uint OnSkybreakerDeck = 70120;
        public const uint OnOrgrimsHammerDeck = 70121;

        // Rocket Pack
        public const uint RocketPackDamage = 69193;
        public const uint RocketBurst = 69192;
        public const uint RocketPackUseable = 70348;

        // Alliance Gunship Cannon
        // Team.Horde Gunship Cannon
        public const uint Overheat = 69487;
        public const uint EjectAllPassengersBelowZero = 68576;
        public const uint EjectAllPassengersWipe = 50630;
    }

    struct GunshipMiscData
    {
        public const uint ItemGoblinRocketPack = 49278;

        public const byte PhaseCombat = 0;
        public const byte PhaseIntro = 1;

        public const uint MusicEncounter = 17289;

        public static Position SkybreakerAddsSpawnPos = new Position(15.91131f, 0.0f, 20.4628f, MathFunctions.PI);
        public static Position OrgrimsHammerAddsSpawnPos = new Position(60.728395f, 0.0f, 38.93467f, MathFunctions.PI);

        // Team.Horde encounter
        public static Position SkybreakerTeleportPortal = new Position(6.666975f, 0.013001f, 20.87888f, 0.0f);
        public static Position OrgrimsHammerTeleportExit = new Position(7.461699f, 0.158853f, 35.72989f, 0.0f);

        // Alliance encounter
        public static Position OrgrimsHammerTeleportPortal = new Position(47.550990f, -0.101778f, 37.61111f, 0.0f);
        public static Position SkybreakerTeleportExit = new Position(-17.55738f, -0.090421f, 21.18366f, 0.0f);

        public static SlotInfo[] SkybreakerSlotInfo =
        {
            new SlotInfo(CreatureIds.SkybreakerSorcerer, -9.479858f, 0.05663967f, 20.77026f, 4.729842f, 0 ),

            new SlotInfo(CreatureIds.SkybreakerSorcerer,  6.385986f,  4.978760f, 20.55417f, 4.694936f, 0 ),
            new SlotInfo(CreatureIds.SkybreakerSorcerer,  6.579102f, -4.674561f, 20.55060f, 1.553343f, 0 ),

            new SlotInfo(CreatureIds.SkybreakerRifleman,  -29.563900f, -17.95801f, 20.73837f, 4.747295f, 30 ),
            new SlotInfo(CreatureIds.SkybreakerRifleman,  -18.017210f, -18.82056f, 20.79150f, 4.747295f, 30 ),
            new SlotInfo(CreatureIds.SkybreakerRifleman,  -9.1193850f, -18.79102f, 20.58887f, 4.712389f, 30 ),
            new SlotInfo(CreatureIds.SkybreakerRifleman,  -0.3364258f, -18.87183f, 20.56824f, 4.712389f, 30 ),

            new SlotInfo(CreatureIds.SkybreakerRifleman,  -34.705810f, -17.67261f, 20.51523f, 4.729842f, 30 ),
            new SlotInfo(CreatureIds.SkybreakerRifleman,  -23.562010f, -18.28564f, 20.67859f, 4.729842f, 30 ),
            new SlotInfo(CreatureIds.SkybreakerRifleman,  -13.602780f, -18.74268f, 20.59622f, 4.712389f, 30 ),
            new SlotInfo(CreatureIds.SkybreakerRifleman,  -4.3350220f, -18.84619f, 20.58234f, 4.712389f, 30 ),

            new SlotInfo(CreatureIds.SkybreakerMortarSoldier,  -31.70142f, 18.02783f, 20.77197f, 4.712389f, 30 ),
            new SlotInfo(CreatureIds.SkybreakerMortarSoldier,  -9.368652f, 18.75806f, 20.65335f, 4.712389f, 30 ),

            new SlotInfo(CreatureIds.SkybreakerMortarSoldier,  -20.40851f, 18.40381f, 20.50647f, 4.694936f, 30 ),
            new SlotInfo(CreatureIds.SkybreakerMortarSoldier,  0.1585693f, 18.11523f, 20.41949f, 4.729842f, 30 ),

            new SlotInfo(CreatureIds.SkybreakerMarine, SkybreakerTeleportPortal, 0 ),
            new SlotInfo(CreatureIds.SkybreakerMarine, SkybreakerTeleportPortal, 0 ),

            new SlotInfo(CreatureIds.SkybreakerMarine, SkybreakerTeleportPortal, 0 ),
            new SlotInfo(CreatureIds.SkybreakerMarine, SkybreakerTeleportPortal, 0 ),

            new SlotInfo(CreatureIds.SkybreakerSergeant, SkybreakerTeleportPortal, 0 ),

            new SlotInfo(CreatureIds.SkybreakerSergeant, SkybreakerTeleportPortal, 0 )
        };
        public static SlotInfo[] OrgrimsHammerSlotInfo =
        {
            new SlotInfo(CreatureIds.KorKronBattleMage, 13.58548f, 0.3867192f, 34.99243f, 1.53589f, 0 ),

            new SlotInfo(CreatureIds.KorKronBattleMage, 47.29290f, -4.308941f, 37.55550f, 1.570796f, 0 ),
            new SlotInfo(CreatureIds.KorKronBattleMage, 47.34621f,  4.032004f, 37.70952f, 4.817109f, 0 ),

            new SlotInfo(CreatureIds.KorKronAxeThrower, -12.09280f, 27.65942f, 33.58557f, 1.53589f, 30 ),
            new SlotInfo(CreatureIds.KorKronAxeThrower, -3.170555f, 28.30652f, 34.21082f, 1.53589f, 30 ),
            new SlotInfo(CreatureIds.KorKronAxeThrower, 14.928040f, 26.18018f, 35.47803f, 1.53589f, 30 ),
            new SlotInfo(CreatureIds.KorKronAxeThrower, 24.703310f, 25.36584f, 35.97845f, 1.53589f, 30 ),

            new SlotInfo(CreatureIds.KorKronAxeThrower, -16.65302f, 27.59668f, 33.18726f, 1.53589f, 30 ),
            new SlotInfo(CreatureIds.KorKronAxeThrower, -8.084572f, 28.21448f, 33.93805f, 1.53589f, 30 ),
            new SlotInfo(CreatureIds.KorKronAxeThrower, 7.594765f, 27.41968f, 35.00775f, 1.53589f, 30 ),
            new SlotInfo(CreatureIds.KorKronAxeThrower, 20.763390f, 25.58215f, 35.75287f, 1.53589f, 30 ),

            new SlotInfo(CreatureIds.KorKronRocketeer, -11.44849f, -25.71838f, 33.64343f, 1.518436f, 30 ),
            new SlotInfo(CreatureIds.KorKronRocketeer, 12.30336f, -25.69653f, 35.32373f, 1.518436f, 30 ),

            new SlotInfo(CreatureIds.KorKronRocketeer, -0.05931854f, -25.46399f, 34.50592f, 1.518436f, 30 ),
            new SlotInfo(CreatureIds.KorKronRocketeer, 27.62149000f, -23.48108f, 36.12708f, 1.518436f, 30 ),

            new SlotInfo(CreatureIds.KorKronReaver, OrgrimsHammerTeleportPortal, 0 ),
            new SlotInfo(CreatureIds.KorKronReaver, OrgrimsHammerTeleportPortal, 0 ),

            new SlotInfo(CreatureIds.KorKronReaver, OrgrimsHammerTeleportPortal, 0 ),
            new SlotInfo(CreatureIds.KorKronReaver, OrgrimsHammerTeleportPortal, 0 ),

            new SlotInfo(CreatureIds.KorKronSergeant, OrgrimsHammerTeleportPortal, 0 ),

            new SlotInfo(CreatureIds.KorKronSergeant, OrgrimsHammerTeleportPortal, 0 )
        };

        public const uint MuradinExitPathSize = 10;
        public static Vector3[] MuradinExitPath =
        {
            new Vector3(8.130936f, -0.2699585f, 20.31728f ),
            new Vector3(6.380936f, -0.2699585f, 20.31728f ),
            new Vector3(3.507703f, 0.02986573f, 20.78463f ),
            new Vector3(-2.767633f, 3.743143f, 20.37663f ),
            new Vector3(-4.017633f, 4.493143f, 20.12663f ),
            new Vector3(-7.242224f, 6.856013f, 20.03468f ),
            new Vector3(-7.742224f, 8.606013f, 20.78468f ),
            new Vector3(-7.992224f, 9.856013f, 21.28468f ),
            new Vector3(-12.24222f, 23.10601f, 21.28468f ),
            new Vector3(-14.88477f, 25.20844f, 21.59985f )
        };

        public const uint SaurfangExitPathSize = 13;
        public static Vector3[] SaurfangExitPath =
        {
            new Vector3(30.43987f, 0.1475817f, 36.10674f ),
            new Vector3(21.36141f, -3.056458f, 35.42970f ),
            new Vector3(19.11141f, -3.806458f, 35.42970f ),
            new Vector3(19.01736f, -3.299440f, 35.39428f ),
            new Vector3(18.6747f, -5.862823f, 35.66611f ),
            new Vector3(18.6747f, -7.862823f, 35.66611f ),
            new Vector3(18.1747f, -17.36282f, 35.66611f ),
            new Vector3(18.1747f, -22.61282f, 35.66611f ),
            new Vector3(17.9247f, -24.36282f, 35.41611f ),
            new Vector3(17.9247f, -26.61282f, 35.66611f ),
            new Vector3(17.9247f, -27.86282f, 35.66611f ),
            new Vector3(17.9247f, -29.36282f, 35.66611f ),
            new Vector3(15.33203f, -30.42621f, 35.93796f )
        };
    }

    struct EncounterActions
    {
        public const int SpawnMage = 1;
        public const int SpawnAllAdds = 2;
        public const int ClearSlot = 3;
        public const int SetSlot = 4;
        public const int ShipVisits = 5;
    }

    enum PassengerSlots
    {
        // Freezing The Cannons
        FreezeMage = 0,

        // Channeling The Portal, Refilled With Adds That Board Player'S Ship
        Mage1 = 1,
        Mage2 = 2,

        // Rifleman
        Rifleman1 = 3,
        Rifleman2 = 4,
        Rifleman3 = 5,
        Rifleman4 = 6,

        // Additional Rifleman On 25 Man
        Rifleman5 = 7,
        Rifleman6 = 8,
        Rifleman7 = 9,
        Rifleman8 = 10,

        // Mortar
        Mortar1 = 11,
        Mortar2 = 12,

        // Additional Spawns On 25 Man
        Mortar3 = 13,
        Mortar4 = 14,

        // Marines
        Marine1 = 15,
        Marine2 = 16,

        // Additional Spawns On 25 Man
        Marine3 = 17,
        Marine4 = 18,

        // Sergeants
        Sergeant1 = 19,

        // Additional Spawns On 25 Man
        Sergeant2 = 20,

        Max
    }

    class SlotInfo
    {
        public SlotInfo(uint _entry, float x, float y, float z, float o, uint _cooldown)
        {
            Entry = _entry;
            TargetPosition = new Position(x, y, z, o);
            Cooldown = _cooldown;
        }
        public SlotInfo(uint _entry, Position pos, uint _cooldown)
        {
            Entry = _entry;
            TargetPosition = pos;
            Cooldown = _cooldown;
        }

        public uint Entry;
        public Position TargetPosition;
        public uint Cooldown;
    }

    class PassengerController
    {
        public PassengerController()
        {
            ResetSlots(Team.Horde);
        }

        public void SetTransport(Transport transport) { _transport = transport; }

        public void ResetSlots(Team team)
        {
            _transport = null;
            _spawnPoint = team == Team.Horde ? GunshipMiscData.OrgrimsHammerAddsSpawnPos : GunshipMiscData.SkybreakerAddsSpawnPos;
            _slotInfo = team == Team.Horde ? GunshipMiscData.OrgrimsHammerSlotInfo : GunshipMiscData.SkybreakerSlotInfo;
        }

        public bool SummonCreatures(PassengerSlots first, PassengerSlots last)
        {
            if (!_transport)
                return false;

            bool summoned = false;
            long now = Time.UnixTime;
            for (int i = (int)first; i <= (int)last; ++i)
            {
                if (_respawnCooldowns[i] > now)
                    continue;

                if (!_controlledSlots[i].IsEmpty())
                {
                    Creature current = ObjectAccessor.GetCreature(_transport, _controlledSlots[i]);
                    if (current && current.IsAlive())
                        continue;
                }
                Creature passenger = _transport.SummonPassenger(_slotInfo[i].Entry, SelectSpawnPoint(), TempSummonType.CorpseTimedDespawn, null, 15000);
                if (passenger)
                {
                    _controlledSlots[i] = passenger.GetGUID();
                    _respawnCooldowns[i] = 0L;
                    passenger.GetAI().SetData(EncounterActions.SetSlot, (uint)i);
                    summoned = true;
                }
            }

            return summoned;
        }

        public void ClearSlot(PassengerSlots slot)
        {
            _controlledSlots[(int)slot].Clear();
            _respawnCooldowns[(int)slot] = Time.UnixTime + _slotInfo[(int)slot].Cooldown;
        }

        public bool SlotsNeedRefill(PassengerSlots first, PassengerSlots last)
        {
            for (int i = (int)first; i <= (int)last; ++i)
                if (_controlledSlots[i].IsEmpty())
                    return true;

            return false;
        }

        Position SelectSpawnPoint()
        {
            float angle = RandomHelper.FRand(-MathFunctions.PI * 0.5f, MathFunctions.PI * 0.5f);
            return new Position(_spawnPoint.GetPositionX() + 2.0f * (float)Math.Cos(angle), _spawnPoint.GetPositionY() + 2.0f * (float)Math.Sin(angle),
                _spawnPoint.GetPositionZ(), _spawnPoint.GetOrientation());
        }

        Transport _transport;
        ObjectGuid[] _controlledSlots = new ObjectGuid[(int)PassengerSlots.Max];
        long[] _respawnCooldowns = new long[(int)PassengerSlots.Max];
        Position _spawnPoint;
        SlotInfo[] _slotInfo;
    }

    class DelayedMovementEvent : BasicEvent
    {
        public DelayedMovementEvent(Creature owner, Position dest)
        {
            _owner = owner;
            _dest = dest;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            if (!_owner.IsAlive())
                return true;

            _owner.GetMotionMaster().MovePoint(EventId.ChargePrepath, _owner, false);

            MoveSplineInit init = new MoveSplineInit(_owner);
            init.DisableTransportPathTransformations();
            init.MoveTo(_dest.GetPositionX(), _dest.GetPositionY(), _dest.GetPositionZ(), false);
            init.Launch();

            return true;
        }

        Creature _owner;
        Position _dest;
    }

    class ResetEncounterEvent : BasicEvent
    {
        public ResetEncounterEvent(Unit caster, uint spellId, ObjectGuid otherTransport)
        {
            _caster = caster;
            _spellId = spellId;
            _otherTransport = otherTransport;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            _caster.CastSpell(_caster, _spellId, true);
            _caster.GetTransport().AddObjectToRemoveList();

            Transport go = Global.ObjAccessor.FindTransport(_otherTransport);
            if (go)
                go.AddObjectToRemoveList();

            return true;
        }

        Unit _caster;
        uint _spellId;
        ObjectGuid _otherTransport;
    }

    class BattleExperienceEvent : BasicEvent
    {
        public BattleExperienceEvent(Creature creature)
        {
            _creature = creature;
            _level = 0;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            if (!_creature.IsAlive())
                return true;

            _creature.RemoveAurasDueToSpell(ExperiencedSpells[_level]);
            ++_level;

            _creature.CastSpell(_creature, ExperiencedSpells[_level], TriggerCastFlags.FullMask);
            if (_level < (_creature.GetMap().IsHeroic() ? 4 : 3))
            {
                _creature.m_Events.AddEvent(this, e_time + ExperiencedTimes[_level]);
                return false;
            }

            return true;
        }

        Creature _creature;
        int _level;

        public static uint[] ExperiencedSpells = { 0, GunshipSpells.Experienced, GunshipSpells.Veteran, GunshipSpells.Elite, GunshipSpells.AddsBerserk };
        public static uint[] ExperiencedTimes = { 100000, 70000, 60000, 90000, 0 };
    }

    class gunship_npc_AI : ScriptedAI
    {
        public gunship_npc_AI(Creature creature) : base(creature)
        {
            Instance = creature.GetInstanceScript();
            Slot = null;
            Index = 0xFFFFFFFF;

            BurningPitchId = Instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? GunshipSpells.BurningPitchA : GunshipSpells.BurningPitchH;
            me.setRegeneratingHealth(false);
        }

        public override void SetData(uint type, uint data)
        {
            if (type == EncounterActions.SetSlot && data < (int)PassengerSlots.Max)
            {
                SetSlotInfo(data);

                me.SetReactState(ReactStates.Passive);

                float x, y, z, o;
                Slot.TargetPosition.GetPosition(out x, out y, out z, out o);

                me.SetTransportHomePosition(Slot.TargetPosition);
                float hx = x, hy = y, hz = z, ho = o;
                me.GetTransport().CalculatePassengerPosition(ref hx, ref hy, ref hz, ref ho);
                me.SetHomePosition(hx, hy, hz, ho);

                me.GetMotionMaster().MovePoint(EventId.ChargePrepath, Slot.TargetPosition, false);

                MoveSplineInit init = new MoveSplineInit(me);
                init.DisableTransportPathTransformations();
                init.MoveTo(x, y, z, false);
                init.Launch();
            }
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            if (!me.IsAlive() || !me.IsInCombat())
                return;

            me.DeleteThreatList();
            me.CombatStop(true);
            me.GetMotionMaster().MoveTargetedHome();
        }

        public override void JustDied(Unit killer)
        {
            if (Slot != null)
            {
                Creature captain = me.FindNearestCreature(Instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? CreatureIds.IGBMuradinBrozebeard : CreatureIds.IGBHighOverlordSaurfang, 200.0f);
                if (captain)
                    captain.GetAI().SetData(EncounterActions.ClearSlot, Index);
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point)
                return;

            if (id == EventId.ChargePrepath && Slot != null)
            {
                me.SetFacingTo(Slot.TargetPosition.GetOrientation());
                me.m_Events.AddEvent(new BattleExperienceEvent(me), me.m_Events.CalculateTime(BattleExperienceEvent.ExperiencedTimes[0]));
                DoCast(me, GunshipSpells.BattleExperience, true);
                me.SetReactState(ReactStates.Aggressive);
            }
        }

        public override bool CanAIAttack(Unit target)
        {
            if (Instance.GetBossState(Bosses.GunshipBattle) != EncounterState.InProgress)
                return false;

            return target.HasAura(Instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? GunshipSpells.OnOrgrimsHammerDeck : GunshipSpells.OnSkybreakerDeck);
        }

        public void SetSlotInfo(uint index)
        {
            Index = index;
            Slot = ((Instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? GunshipMiscData.SkybreakerSlotInfo : GunshipMiscData.OrgrimsHammerSlotInfo)[Index]);
        }

        public bool SelectVictim()
        {
            if (Instance.GetBossState(Bosses.GunshipBattle) != EncounterState.InProgress)
            {
                EnterEvadeMode(EvadeReason.Other);
                return false;
            }

            if (!me.HasReactState(ReactStates.Passive))
            {
                Unit victim = me.SelectVictim();
                if (victim)
                    AttackStart(victim);
                return me.GetVictim();
            }
            else if (me.GetThreatManager().isThreatListEmpty())
            {
                EnterEvadeMode(EvadeReason.Other);
                return false;
            }

            return true;
        }

        public void TriggerBurningPitch()
        {
            if (Instance.GetBossState(Bosses.GunshipBattle) == EncounterState.InProgress &&
                !me.HasUnitState(UnitState.Casting) && !me.HasReactState(ReactStates.Passive) &&
                !me.GetSpellHistory().HasCooldown(BurningPitchId))
            {
                DoCastAOE(BurningPitchId, true);
                me.GetSpellHistory().AddCooldown(BurningPitchId, 0, TimeSpan.FromMilliseconds(RandomHelper.URand(3000, 4000)));
            }
        }

        public InstanceScript Instance;
        public SlotInfo Slot;
        public uint Index;
        public uint BurningPitchId;
    }

    [Script]
    class npc_gunship : NullCreatureAI
    {
        public npc_gunship(Creature creature) : base(creature)
        {
            _teamInInstance = (Team)creature.GetInstanceScript().GetData(DataTypes.TeamInInstance);
            _summonedFirstMage = false;
            _died = false;

            me.setRegeneratingHealth(false);
        }

        public override void DamageTaken(Unit source, ref uint damage)
        {
            if (damage >= me.GetHealth())
            {
                JustDied(null);
                damage = (uint)me.GetHealth() - 1;
                return;
            }

            if (_summonedFirstMage)
                return;

            if (me.GetTransport().GetEntry() != (_teamInInstance == Team.Horde ? GameObjectIds.TheSkybreaker_H : GameObjectIds.OrgrimsHammer_A))
                return;

            if (!me.HealthBelowPctDamaged(90, damage))
                return;

            _summonedFirstMage = true;
            Creature captain = me.FindNearestCreature(_teamInInstance == Team.Horde ? CreatureIds.IGBMuradinBrozebeard : CreatureIds.IGBHighOverlordSaurfang, 100.0f);
            if (captain)
                captain.GetAI().DoAction(EncounterActions.SpawnMage);
        }

        public override void JustDied(Unit killer)
        {
            if (_died)
                return;

            _died = true;

            bool isVictory = me.GetTransport().GetEntry() == GameObjectIds.TheSkybreaker_H || me.GetTransport().GetEntry() == GameObjectIds.OrgrimsHammer_A;
            InstanceScript instance = me.GetInstanceScript();
            instance.SetBossState(Bosses.GunshipBattle, isVictory ? EncounterState.Done : EncounterState.Fail);
            Creature creature = me.FindNearestCreature(me.GetEntry() == CreatureIds.OrgrimsHammer ? CreatureIds.TheSkybreaker : CreatureIds.OrgrimsHammer, 200.0f);
            if (creature)
            {
                instance.SendEncounterUnit(EncounterFrameType.Disengage, creature);
                creature.RemoveAurasDueToSpell(GunshipSpells.CheckForPlayers);
            }

            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
            me.RemoveAurasDueToSpell(GunshipSpells.CheckForPlayers);

            me.GetMap().SetZoneMusic(AreaIds.IcecrownCitadel, 0);
            List<Creature> creatures = new List<Creature>();
            me.GetCreatureListWithEntryInGrid(creatures, CreatureIds.MartyrStalkerIGBSaurfang, MapConst.SizeofGrids);
            foreach (var stalker in creatures)
            {
                stalker.RemoveAllAuras();
                stalker.DeleteThreatList();
                stalker.CombatStop(true);
            }


            uint explosionSpell = isVictory ? GunshipSpells.ExplosionVictory : GunshipSpells.ExplosionWipe;
            creatures.Clear();
            me.GetCreatureListWithEntryInGrid(creatures, CreatureIds.GunshipHull, 200.0f);
            foreach (var hull in creatures)
            {
                if (hull.GetTransport() != me.GetTransport())
                    continue;

                hull.CastSpell(hull, explosionSpell, TriggerCastFlags.FullMask);
            }

            creatures.Clear();
            me.GetCreatureListWithEntryInGrid(creatures, _teamInInstance == Team.Horde ? CreatureIds.HordeGunshipCannon : CreatureIds.AllianceGunshipCannon, 200.0f);
            foreach (var cannon in creatures)
            {
                if (isVictory)
                {
                    cannon.CastSpell(cannon, GunshipSpells.EjectAllPassengersBelowZero, TriggerCastFlags.FullMask);
                    cannon.RemoveVehicleKit();
                }
                else
                    cannon.CastSpell(cannon, GunshipSpells.EjectAllPassengersWipe, TriggerCastFlags.FullMask);
            }

            uint creatureEntry = CreatureIds.IGBMuradinBrozebeard;
            byte textId = isVictory ? GunshipTexts.SayMuradinVictory : GunshipTexts.SayMuradinWipe;
            if (_teamInInstance == Team.Horde)
            {
                creatureEntry = CreatureIds.IGBHighOverlordSaurfang;
                textId = isVictory ? GunshipTexts.SaySaurfangVictory : GunshipTexts.SaySaurfangWipe;
            }
            creature = me.FindNearestCreature(creatureEntry, 100.0f);
            if (creature)
                creature.GetAI().Talk(textId);

            if (isVictory)
            {
                Transport go = Global.ObjAccessor.FindTransport(instance.GetGuidData(Bosses.GunshipBattle));
                if (go)
                    go.EnableMovement(true);

                me.GetTransport().EnableMovement(true);
                Creature ship = me.FindNearestCreature(_teamInInstance == Team.Horde ? CreatureIds.OrgrimsHammer : CreatureIds.TheSkybreaker, 200.0f);
                if (ship)
                {
                    ship.CastSpell(ship, GunshipSpells.TeleportPlayersOnVictory, TriggerCastFlags.FullMask);
                    ship.CastSpell(ship, GunshipSpells.Achievement, TriggerCastFlags.FullMask);
                    ship.CastSpell(ship, GunshipSpells.AwardReputationBossKill, TriggerCastFlags.FullMask);
                }

                creatures.Clear();
                me.GetCreatureListWithEntryInGrid(creatures, CreatureIds.SkybreakerMarine, 200.0f);
                me.GetCreatureListWithEntryInGrid(creatures, CreatureIds.SkybreakerSergeant, 200.0f);
                me.GetCreatureListWithEntryInGrid(creatures, CreatureIds.KorKronReaver, 200.0f);
                me.GetCreatureListWithEntryInGrid(creatures, CreatureIds.KorKronSergeant, 200.0f);
                foreach (var obj in creatures)
                    obj.DespawnOrUnsummon(1);
            }
            else
            {
                uint teleportSpellId = _teamInInstance == Team.Horde ? GunshipSpells.TeleportPlayersOnResetH : GunshipSpells.TeleportPlayersOnResetA;
                me.m_Events.AddEvent(new ResetEncounterEvent(me, teleportSpellId, me.GetInstanceScript().GetGuidData(DataTypes.EnemyGunship)),
                    me.m_Events.CalculateTime(8000));
            }

            instance.SetBossState(Bosses.GunshipBattle, isVictory ? EncounterState.Done : EncounterState.Fail);
        }

        public override void SetGUID(ObjectGuid guid, int id = 0)
        {
            if (id != EncounterActions.ShipVisits)
                return;

            if (!_shipVisits.ContainsKey(guid))
                _shipVisits[guid] = 1;
            else
                ++_shipVisits[guid];
        }

        public override uint GetData(uint id)
        {
            if (id != EncounterActions.ShipVisits)
                return 0;

            uint max = 0;
            foreach (var count in _shipVisits.Values)
                max = Math.Max(max, count);

            return max;
        }

        Team _teamInInstance;
        Dictionary<ObjectGuid, uint> _shipVisits = new Dictionary<ObjectGuid, uint>();
        bool _summonedFirstMage;
        bool _died;
    }

    [Script]
    class npc_high_overlord_saurfang_igb : ScriptedAI
    {
        public npc_high_overlord_saurfang_igb(Creature creature)
            : base(creature)
        {
            _instance = creature.GetInstanceScript();

            _controller.ResetSlots(Team.Horde);
            _controller.SetTransport(creature.GetTransport());
            me.setRegeneratingHealth(false);
            me.m_CombatDistance = 70.0f;
        }

        public override void InitializeAI()
        {
            base.InitializeAI();

            _events.Reset();
            _firstMageCooldown = Time.UnixTime + 60;
            _axethrowersYellCooldown = 0L;
            _rocketeersYellCooldown = 0L;
        }

        public override void EnterCombat(Unit target)
        {
            _events.SetPhase(GunshipMiscData.PhaseCombat);
            DoCast(me, _instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? GunshipSpells.FriendlyBossDamageMod : GunshipSpells.MeleeTargetingOnOrgrimsHammer, true);
            DoCast(me, GunshipSpells.BattleFury, true);
            _events.ScheduleEvent(GunshipEvents.Cleave, RandomHelper.URand(2000, 10000));
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            if (!me.IsAlive())
                return;

            me.DeleteThreatList();
            me.CombatStop(true);
            me.GetMotionMaster().MoveTargetedHome();

            Reset();
        }

        public override void DoAction(int action)
        {
            if (action == Actions.EnemyGunshipTalk)
            {
                Creature muradin = me.FindNearestCreature(CreatureIds.IGBMuradinBrozebeard, 100.0f);
                if (muradin)
                    muradin.GetAI().DoAction(EncounterActions.SpawnAllAdds);

                Talk(GunshipTexts.SaySaurfangIntro5);
                _events.ScheduleEvent(GunshipEvents.IntroH5, 4000);
                _events.ScheduleEvent(GunshipEvents.IntroH6, 11000);
                _events.ScheduleEvent(GunshipEvents.KeepPlayerInCombat, 1);

                _instance.SetBossState(Bosses.GunshipBattle, EncounterState.InProgress);
                // Combat starts now
                Creature skybreaker = me.FindNearestCreature(CreatureIds.TheSkybreaker, 100.0f);
                if (skybreaker)
                    _instance.SendEncounterUnit(EncounterFrameType.Engage, skybreaker, 1);

                Creature orgrimsHammer = me.FindNearestCreature(CreatureIds.OrgrimsHammer, 100.0f);
                if (orgrimsHammer)
                {
                    _instance.SendEncounterUnit(EncounterFrameType.Engage, orgrimsHammer, 2);
                    orgrimsHammer.CastSpell(orgrimsHammer, GunshipSpells.CheckForPlayers, TriggerCastFlags.FullMask);
                }

                me.GetMap().SetZoneMusic(AreaIds.IcecrownCitadel, GunshipMiscData.MusicEncounter);
            }
            else if (action == EncounterActions.SpawnMage)
            {
                long now = Time.UnixTime;
                if (_firstMageCooldown > now)
                    _events.ScheduleEvent(GunshipEvents.SummonMage, (uint)(_firstMageCooldown - now) * Time.InMilliseconds);
                else
                    _events.ScheduleEvent(GunshipEvents.SummonMage, 1);
            }
            else if (action == EncounterActions.SpawnAllAdds)
            {
                _events.ScheduleEvent(GunshipEvents.Adds, 12000);
                _events.ScheduleEvent(GunshipEvents.CheckRifleman, 13000);
                _events.ScheduleEvent(GunshipEvents.CheckMortar, 13000);
                if (Is25ManRaid())
                    _controller.SummonCreatures(PassengerSlots.Mage1, PassengerSlots.Mortar4);
                else
                {
                    _controller.SummonCreatures(PassengerSlots.Mage1, PassengerSlots.Mage2);
                    _controller.SummonCreatures(PassengerSlots.Mortar1, PassengerSlots.Mortar2);
                    _controller.SummonCreatures(PassengerSlots.Rifleman1, PassengerSlots.Rifleman4);
                }
            }
            else if (action == Actions.ExitShip)
            {
                Position pos = new Position(GunshipMiscData.SaurfangExitPath[GunshipMiscData.SaurfangExitPathSize - 1].X, GunshipMiscData.SaurfangExitPath[GunshipMiscData.SaurfangExitPathSize - 1].Y, GunshipMiscData.SaurfangExitPath[GunshipMiscData.SaurfangExitPathSize - 1].Z);
                me.GetMotionMaster().MovePoint(EventId.ChargePrepath, pos, false);

                var path = GunshipMiscData.SaurfangExitPath;//, SaurfangExitPath + SaurfangExitPathSize);

                MoveSplineInit init = new MoveSplineInit(me);
                init.DisableTransportPathTransformations();
                init.MovebyPath(path, 0);
                init.Launch();

                me.DespawnOrUnsummon(18000);
            }
        }

        public override void SetData(uint type, uint data)
        {
            if (type == EncounterActions.ClearSlot)
            {
                _controller.ClearSlot((PassengerSlots)data);
                if (data == (uint)PassengerSlots.FreezeMage)
                    _events.ScheduleEvent(GunshipEvents.SummonMage, RandomHelper.URand(30000, 33500));
            }
        }

        public override void sGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            me.RemoveFlag64(UnitFields.NpcFlags, NPCFlags.Gossip);
            me.GetTransport().EnableMovement(true);
            _events.SetPhase(GunshipMiscData.PhaseIntro);
            _events.ScheduleEvent(GunshipEvents.IntroH1, 5000, 0, GunshipMiscData.PhaseIntro);
            _events.ScheduleEvent(GunshipEvents.IntroH2, 16000, 0, GunshipMiscData.PhaseIntro);
            _events.ScheduleEvent(GunshipEvents.IntroSummonSkybreaker, 24600, 0, GunshipMiscData.PhaseIntro);
            _events.ScheduleEvent(GunshipEvents.IntroH3, 29600, 0, GunshipMiscData.PhaseIntro);
            _events.ScheduleEvent(GunshipEvents.IntroH4, 39200, 0, GunshipMiscData.PhaseIntro);
        }

        public override void DamageTaken(Unit u, ref uint damage)
        {
            if (me.HealthBelowPctDamaged(65, damage) && !me.HasAura(GunshipSpells.TasteOfBlood))
                DoCast(me, GunshipSpells.TasteOfBlood, true);

            if (damage > me.GetHealth())
                damage = (uint)me.GetHealth() - 1;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && !_events.IsInPhase(GunshipMiscData.PhaseIntro) && _instance.GetBossState(Bosses.GunshipBattle) != EncounterState.InProgress)
                return;

            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case GunshipEvents.IntroH1:
                        Talk(GunshipTexts.SaySaurfangIntro1);
                        break;
                    case GunshipEvents.IntroH2:
                        Talk(GunshipTexts.SaySaurfangIntro2);
                        break;
                    case GunshipEvents.IntroSummonSkybreaker:
                        Global.TransportMgr.CreateTransport(GameObjectIds.TheSkybreaker_H, 0, me.GetMap());
                        break;
                    case GunshipEvents.IntroH3:
                        Talk(GunshipTexts.SaySaurfangIntro3);
                        break;
                    case GunshipEvents.IntroH4:
                        Talk(GunshipTexts.SaySaurfangIntro4);
                        break;
                    case GunshipEvents.IntroH5:
                        Creature muradin = me.FindNearestCreature(CreatureIds.IGBMuradinBrozebeard, 100.0f);
                        if (muradin)
                            muradin.GetAI().Talk(GunshipTexts.SayMuradinIntroH);
                        break;
                    case GunshipEvents.IntroH6:
                        Talk(GunshipTexts.SaySaurfangIntro6);
                        break;
                    case GunshipEvents.KeepPlayerInCombat:
                        if (_instance.GetBossState(Bosses.GunshipBattle) == EncounterState.InProgress)
                        {
                            _instance.DoCastSpellOnPlayers(GunshipSpells.LockPlayersAndTapChest);
                            _events.ScheduleEvent(GunshipEvents.KeepPlayerInCombat, RandomHelper.URand(5000, 8000));
                        }
                        break;
                    case GunshipEvents.SummonMage:
                        Talk(GunshipTexts.SaySaurfangMages);
                        _controller.SummonCreatures(PassengerSlots.FreezeMage, PassengerSlots.FreezeMage);
                        break;
                    case GunshipEvents.Adds:
                        Talk(GunshipTexts.SaySaurfangEnterSkybreaker);
                        _controller.SummonCreatures(PassengerSlots.Mage1, PassengerSlots.Mage2);
                        _controller.SummonCreatures(PassengerSlots.Marine1, Is25ManRaid() ? PassengerSlots.Marine4 : PassengerSlots.Marine2);
                        _controller.SummonCreatures(PassengerSlots.Sergeant1, Is25ManRaid() ? PassengerSlots.Sergeant2 : PassengerSlots.Sergeant1);
                        Transport orgrimsHammer = me.GetTransport();
                        if (orgrimsHammer)
                            orgrimsHammer.SummonPassenger(CreatureIds.TeleportPortal, GunshipMiscData.OrgrimsHammerTeleportPortal, TempSummonType.TimedDespawn, null, 21000);

                        Transport skybreaker = Global.ObjAccessor.FindTransport(_instance.GetGuidData(Bosses.GunshipBattle));
                        if (skybreaker)
                            skybreaker.SummonPassenger(CreatureIds.TeleportExit, GunshipMiscData.SkybreakerTeleportExit, TempSummonType.TimedDespawn, null, 23000);

                        _events.ScheduleEvent(GunshipEvents.AddsBoardYell, 6000);
                        _events.ScheduleEvent(GunshipEvents.Adds, 60000);
                        break;
                    case GunshipEvents.AddsBoardYell:
                        muradin = me.FindNearestCreature(CreatureIds.IGBMuradinBrozebeard, 200.0f);
                        if (muradin)
                            muradin.GetAI().Talk(GunshipTexts.SayMuradinBoard);
                        break;
                    case GunshipEvents.CheckRifleman:
                        if (_controller.SummonCreatures(PassengerSlots.Rifleman1, Is25ManRaid() ? PassengerSlots.Rifleman8 : PassengerSlots.Rifleman4))
                        {
                            if (_axethrowersYellCooldown < Time.UnixTime)
                            {
                                Talk(GunshipTexts.SaySaurfangAxethrowers);
                                _axethrowersYellCooldown = Time.UnixTime + 5;
                            }
                        }
                        _events.ScheduleEvent(GunshipEvents.CheckRifleman, 1000);
                        break;
                    case GunshipEvents.CheckMortar:
                        if (_controller.SummonCreatures(PassengerSlots.Mortar1, Is25ManRaid() ? PassengerSlots.Mortar4 : PassengerSlots.Mortar2))
                        {
                            if (_rocketeersYellCooldown < Time.UnixTime)
                            {
                                Talk(GunshipTexts.SaySaurfangRocketeers);
                                _rocketeersYellCooldown = Time.UnixTime + 5;
                            }
                        }
                        _events.ScheduleEvent(GunshipEvents.CheckMortar, 1000);
                        break;
                    case GunshipEvents.Cleave:
                        DoCastVictim(GunshipSpells.Cleave);
                        _events.ScheduleEvent(GunshipEvents.Cleave, RandomHelper.URand(2000, 10000));
                        break;
                    default:
                        break;
                }
            });

            if (me.IsWithinMeleeRange(me.GetVictim()))
                DoMeleeAttackIfReady();
            else if (me.isAttackReady())
            {
                DoCastVictim(GunshipSpells.RendingThrow);
                me.resetAttackTimer();
            }
        }

        public override bool CanAIAttack(Unit target)
        {
            return target.HasAura(GunshipSpells.OnOrgrimsHammerDeck) || !target.IsControlledByPlayer();
        }

        PassengerController _controller = new PassengerController();
        InstanceScript _instance;
        long _firstMageCooldown;
        long _axethrowersYellCooldown;
        long _rocketeersYellCooldown;
    }

    [Script]
    class npc_muradin_bronzebeard_igb : ScriptedAI
    {
        public npc_muradin_bronzebeard_igb(Creature creature)
            : base(creature)
        {
            _instance = creature.GetInstanceScript();

            _controller.ResetSlots(Team.Alliance);
            _controller.SetTransport(creature.GetTransport());
            me.setRegeneratingHealth(false);
            me.m_CombatDistance = 70.0f;
        }

        public override void InitializeAI()
        {
            base.InitializeAI();

            _events.Reset();
            _firstMageCooldown = Time.UnixTime + 60;
            _riflemanYellCooldown = 0L;
            _mortarYellCooldown = 0L;
        }

        public override void EnterCombat(Unit target)
        {
            _events.SetPhase(GunshipMiscData.PhaseCombat);
            DoCast(me, _instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Alliance ? GunshipSpells.FriendlyBossDamageMod : GunshipSpells.MeleeTargetingOnSkybreaker, true);
            DoCast(me, GunshipSpells.BattleFury, true);
            _events.ScheduleEvent(GunshipEvents.Cleave, RandomHelper.URand(2000, 10000));
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            if (!me.IsAlive())
                return;

            me.DeleteThreatList();
            me.CombatStop(true);
            me.GetMotionMaster().MoveTargetedHome();

            Reset();
        }

        public override void DoAction(int action)
        {
            if (action == Actions.EnemyGunshipTalk)
            {
                Creature muradin = me.FindNearestCreature(CreatureIds.IGBHighOverlordSaurfang, 100.0f);
                if (muradin)
                    muradin.GetAI().DoAction(EncounterActions.SpawnAllAdds);

                Talk(GunshipTexts.SayMuradinIntro6);
                _events.ScheduleEvent(GunshipEvents.IntroA6, 5000);
                _events.ScheduleEvent(GunshipEvents.IntroA7, 11000);
                _events.ScheduleEvent(GunshipEvents.KeepPlayerInCombat, 1);

                _instance.SetBossState(Bosses.GunshipBattle, EncounterState.InProgress);
                // Combat starts now
                Creature orgrimsHammer = me.FindNearestCreature(CreatureIds.OrgrimsHammer, 100.0f);
                if (orgrimsHammer)
                    _instance.SendEncounterUnit(EncounterFrameType.Engage, orgrimsHammer, 1);

                Creature skybreaker = me.FindNearestCreature(CreatureIds.TheSkybreaker, 100.0f);
                if (skybreaker)
                {
                    _instance.SendEncounterUnit(EncounterFrameType.Engage, skybreaker, 2);
                    skybreaker.CastSpell(skybreaker, GunshipSpells.CheckForPlayers, TriggerCastFlags.FullMask);
                }

                me.GetMap().SetZoneMusic(AreaIds.IcecrownCitadel, GunshipMiscData.MusicEncounter);
            }
            else if (action == EncounterActions.SpawnMage)
            {
                long now = Time.UnixTime;
                if (_firstMageCooldown < now)
                    _events.ScheduleEvent(GunshipEvents.SummonMage, (uint)(now - _firstMageCooldown) * Time.InMilliseconds);
                else
                    _events.ScheduleEvent(GunshipEvents.SummonMage, 1);
            }
            else if (action == EncounterActions.SpawnAllAdds)
            {
                _events.ScheduleEvent(GunshipEvents.Adds, 12000);
                _events.ScheduleEvent(GunshipEvents.CheckRifleman, 13000);
                _events.ScheduleEvent(GunshipEvents.CheckMortar, 13000);
                if (Is25ManRaid())
                    _controller.SummonCreatures(PassengerSlots.Mage1, PassengerSlots.Mortar4);
                else
                {
                    _controller.SummonCreatures(PassengerSlots.Mage1, PassengerSlots.Mage2);
                    _controller.SummonCreatures(PassengerSlots.Mortar1, PassengerSlots.Mortar2);
                    _controller.SummonCreatures(PassengerSlots.Rifleman1, PassengerSlots.Rifleman4);
                }
            }
        }

        public override void SetData(uint type, uint data)
        {
            if (type == EncounterActions.ClearSlot)
            {
                _controller.ClearSlot((PassengerSlots)data);
                if (data == (uint)PassengerSlots.FreezeMage)
                    _events.ScheduleEvent(GunshipEvents.SummonMage, RandomHelper.URand(30000, 33500));
            }
        }

        public override void sGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            me.RemoveFlag64(UnitFields.NpcFlags, NPCFlags.Gossip);
            me.GetTransport().EnableMovement(true);
            _events.SetPhase(GunshipMiscData.PhaseIntro);
            _events.ScheduleEvent(GunshipEvents.IntroA1, 5000);
            _events.ScheduleEvent(GunshipEvents.IntroA2, 10000, 0, GunshipMiscData.PhaseIntro);
            _events.ScheduleEvent(GunshipEvents.IntroSummonOrgrimsHammer, 28000, 0, GunshipMiscData.PhaseIntro);
            _events.ScheduleEvent(GunshipEvents.IntroA3, 33000, 0, GunshipMiscData.PhaseIntro);
            _events.ScheduleEvent(GunshipEvents.IntroA4, 39000, 0, GunshipMiscData.PhaseIntro);
            _events.ScheduleEvent(GunshipEvents.IntroA5, 45000, 0, GunshipMiscData.PhaseIntro);
        }

        public override void DamageTaken(Unit u, ref uint damage)
        {
            if (me.HealthBelowPctDamaged(65, damage) && me.HasAura(GunshipSpells.TasteOfBlood))
                DoCast(me, GunshipSpells.TasteOfBlood, true);

            if (damage >= me.GetHealth())
                damage = (uint)me.GetHealth() - 1;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && !_events.IsInPhase(GunshipMiscData.PhaseIntro) && _instance.GetBossState(Bosses.GunshipBattle) != EncounterState.InProgress)
                return;

            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case GunshipEvents.IntroA1:
                        Talk(GunshipTexts.SayMuradinIntro1);
                        break;
                    case GunshipEvents.IntroA2:
                        Talk(GunshipTexts.SayMuradinIntro2);
                        break;
                    case GunshipEvents.IntroSummonOrgrimsHammer:
                        Global.TransportMgr.CreateTransport(GameObjectIds.OrgrimsHammer_A, 0, me.GetMap());
                        break;
                    case GunshipEvents.IntroA3:
                        Talk(GunshipTexts.SayMuradinIntro3);
                        break;
                    case GunshipEvents.IntroA4:
                        Talk(GunshipTexts.SayMuradinIntro4);
                        break;
                    case GunshipEvents.IntroA5:
                        Talk(GunshipTexts.SayMuradinIntro5);
                        break;
                    case GunshipEvents.IntroA6:
                        Creature saurfang = me.FindNearestCreature(CreatureIds.IGBHighOverlordSaurfang, 100.0f);
                        if (saurfang)
                            saurfang.GetAI().Talk(GunshipTexts.SaySaurfangIntroA);
                        break;
                    case GunshipEvents.IntroA7:
                        Talk(GunshipTexts.SayMuradinIntro7);
                        break;
                    case GunshipEvents.KeepPlayerInCombat:
                        if (_instance.GetBossState(Bosses.GunshipBattle) == EncounterState.InProgress)
                        {
                            _instance.DoCastSpellOnPlayers(GunshipSpells.LockPlayersAndTapChest);
                            _events.ScheduleEvent(GunshipEvents.KeepPlayerInCombat, RandomHelper.URand(5000, 8000));
                        }
                        break;
                    case GunshipEvents.SummonMage:
                        Talk(GunshipTexts.SayMuradinSorcerers);
                        _controller.SummonCreatures(PassengerSlots.FreezeMage, PassengerSlots.FreezeMage);
                        break;
                    case GunshipEvents.Adds:
                        Talk(GunshipTexts.SayMuradinEnterOrgrimmsHammer);
                        _controller.SummonCreatures(PassengerSlots.Mage1, PassengerSlots.Mage2);
                        _controller.SummonCreatures(PassengerSlots.Marine1, Is25ManRaid() ? PassengerSlots.Marine4 : PassengerSlots.Marine2);
                        _controller.SummonCreatures(PassengerSlots.Sergeant1, Is25ManRaid() ? PassengerSlots.Sergeant2 : PassengerSlots.Sergeant1);

                        Transport skybreaker = me.GetTransport();
                        if (skybreaker)
                            skybreaker.SummonPassenger(CreatureIds.TeleportPortal, GunshipMiscData.SkybreakerTeleportPortal, TempSummonType.TimedDespawn, null, 21000);

                        Transport go = Global.ObjAccessor.FindTransport(_instance.GetGuidData(Bosses.GunshipBattle));
                        if (go)
                            go.SummonPassenger(CreatureIds.TeleportExit, GunshipMiscData.OrgrimsHammerTeleportExit, TempSummonType.TimedDespawn, null, 23000);

                        _events.ScheduleEvent(GunshipEvents.AddsBoardYell, 6000);
                        _events.ScheduleEvent(GunshipEvents.Adds, 60000);
                        break;
                    case GunshipEvents.AddsBoardYell:
                        saurfang = me.FindNearestCreature(CreatureIds.IGBHighOverlordSaurfang, 200.0f);
                        if (saurfang)
                            saurfang.GetAI().Talk(GunshipTexts.SaySaurfangBoard);
                        break;
                    case GunshipEvents.CheckRifleman:
                        if (_controller.SummonCreatures(PassengerSlots.Rifleman1, Is25ManRaid() ? PassengerSlots.Rifleman8 : PassengerSlots.Rifleman4))
                        {
                            if (_riflemanYellCooldown < Time.UnixTime)
                            {
                                Talk(GunshipTexts.SayMuradinRifleman);
                                _riflemanYellCooldown = Time.UnixTime + 5;
                            }
                        }
                        _events.ScheduleEvent(GunshipEvents.CheckRifleman, 1000);
                        break;
                    case GunshipEvents.CheckMortar:
                        if (_controller.SummonCreatures(PassengerSlots.Mortar1, Is25ManRaid() ? PassengerSlots.Mortar4 : PassengerSlots.Mortar2))
                        {
                            if (_mortarYellCooldown < Time.UnixTime)
                            {
                                Talk(GunshipTexts.SayMuradinMortar);
                                _mortarYellCooldown = Time.UnixTime + 5;
                            }
                        }
                        _events.ScheduleEvent(GunshipEvents.CheckMortar, 1000);
                        break;
                    case GunshipEvents.Cleave:
                        DoCastVictim(GunshipSpells.Cleave);
                        _events.ScheduleEvent(GunshipEvents.Cleave, RandomHelper.URand(2000, 10000));
                        break;
                    default:
                        break;
                }
            });

            if (me.IsWithinMeleeRange(me.GetVictim()))
                DoMeleeAttackIfReady();
            else if (me.isAttackReady())
            {
                DoCastVictim(GunshipSpells.RendingThrow);
                me.resetAttackTimer();
            }
        }

        public override bool CanAIAttack(Unit target)
        {
            if (_instance.GetBossState(Bosses.GunshipBattle) != EncounterState.InProgress)
                return false;

            return target.HasAura(GunshipSpells.OnSkybreakerDeck) || !target.IsControlledByPlayer();
        }

        PassengerController _controller = new PassengerController();
        InstanceScript _instance;
        long _firstMageCooldown;
        long _riflemanYellCooldown;
        long _mortarYellCooldown;
    }

    [Script]
    class npc_zafod_boombox : gunship_npc_AI
    {
        public npc_zafod_boombox(Creature creature)
            : base(creature) { }

        public override void Reset()
        {
            me.SetReactState(ReactStates.Passive);
        }

        public override void sGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            player.AddItem(GunshipMiscData.ItemGoblinRocketPack, 1);
            player.PlayerTalkClass.SendCloseGossip();
        }

        public override void UpdateAI(uint diff)
        {
            UpdateVictim();
        }
    }

    class npc_gunship_boarding_addAI : gunship_npc_AI
    {
        public npc_gunship_boarding_addAI(Creature creature) : base(creature)
        {
            me.m_CombatDistance = 80.0f;
            _usedDesperateResolve = false;
        }

        public override void SetData(uint type, uint data)
        {
            // detach from captain
            if (type == EncounterActions.SetSlot)
            {
                SetSlotInfo(data);

                me.SetReactState(ReactStates.Passive);

                me.m_Events.AddEvent(new DelayedMovementEvent(me, Slot.TargetPosition), me.m_Events.CalculateTime(3000 * (Index - (int)PassengerSlots.Marine1)));

                Creature captain = me.FindNearestCreature(Instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? CreatureIds.IGBMuradinBrozebeard : CreatureIds.IGBHighOverlordSaurfang, 200.0f);
                if (captain)
                    captain.GetAI().SetData(EncounterActions.ClearSlot, Index);
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point)
                return;

            if (id == EventId.ChargePrepath && Slot != null)
            {
                Position otherTransportPos = Instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? GunshipMiscData.OrgrimsHammerTeleportExit : GunshipMiscData.SkybreakerTeleportExit;
                float x, y, z, o;
                otherTransportPos.GetPosition(out x, out y, out z, out o);

                Transport myTransport = me.GetTransport();
                if (!myTransport)
                    return;

                Transport destTransport = Global.ObjAccessor.FindTransport(Instance.GetGuidData(Bosses.GunshipBattle));
                if (destTransport)
                    destTransport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);

                float angle = RandomHelper.URand(0, MathFunctions.PI * 2.0f);
                x += 2.0f * (float)Math.Cos(angle);
                y += 2.0f * (float)Math.Sin(angle);

                me.SetHomePosition(x, y, z, o);
                myTransport.CalculatePassengerOffset(ref x, ref y, ref z, ref o);
                me.SetTransportHomePosition(x, y, z, o);

                me.m_Events.AddEvent(new BattleExperienceEvent(me), me.m_Events.CalculateTime(BattleExperienceEvent.ExperiencedTimes[0]));
                DoCast(me, GunshipSpells.BattleExperience, true);
                DoCast(me, GunshipSpells.TeleportToEnemyShip, true);
                DoCast(me, Instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? GunshipSpells.MeleeTargetingOnOrgrimsHammer : GunshipSpells.MeleeTargetingOnSkybreaker, true);
                me.GetSpellHistory().AddCooldown(BurningPitchId, 0, TimeSpan.FromSeconds(3));

                List<Player> players = new List<Player>();
                var check = new UnitAuraCheck<Player>(true, Instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? GunshipSpells.OnOrgrimsHammerDeck : GunshipSpells.OnSkybreakerDeck);
                var searcher = new PlayerListSearcher(me, players, check);
                Cell.VisitWorldObjects(me, searcher, 200.0f);

                players.RemoveAll(player => me._IsTargetAcceptable(player) || !me.CanStartAttack(player, true));

                if (!players.Empty())
                {
                    players.Sort(new ObjectDistanceOrderPred(me));
                    foreach (var pl in players)
                        me.AddThreat(pl, 1.0f);

                    AttackStart(players.First());
                }

                me.SetReactState(ReactStates.Aggressive);
            }
        }
        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (_usedDesperateResolve)
                return;

            if (!me.HealthBelowPctDamaged(25, damage))
                return;

            _usedDesperateResolve = true;
            DoCast(me, GunshipSpells.DesperateResolve, true);
        }

        public override void UpdateAI(uint diff)
        {
            if (!SelectVictim())
            {
                TriggerBurningPitch();
                return;
            }

            if (!HasAttackablePlayerNearby())
                TriggerBurningPitch();

            DoMeleeAttackIfReady();
        }

        public override bool CanAIAttack(Unit target)
        {
            uint spellId = GunshipSpells.OnSkybreakerDeck;
            uint creatureEntry = CreatureIds.IGBMuradinBrozebeard;
            if (Instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde)
            {
                spellId = GunshipSpells.OnOrgrimsHammerDeck;
                creatureEntry = CreatureIds.IGBHighOverlordSaurfang;
            }

            return target.HasAura(spellId) || target.GetEntry() == creatureEntry;
        }

        public bool HasAttackablePlayerNearby()
        {
            List<Player> players = new List<Player>();
            var check = new UnitAuraCheck<Player>(true, Instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? GunshipSpells.OnOrgrimsHammerDeck : GunshipSpells.OnSkybreakerDeck);
            var searcher = new PlayerListSearcher(me, players, check);
            Cell.VisitWorldObjects(me, searcher, 200.0f);

            players.RemoveAll(player => !me._IsTargetAcceptable(player) || !me.CanStartAttack(player, true));

            return !players.Empty();
        }

        bool _usedDesperateResolve;
    }

    [Script]
    class npc_gunship_boarding_leader : npc_gunship_boarding_addAI
    {
        public npc_gunship_boarding_leader(Creature creature)
            : base(creature) { }

        public override void EnterCombat(Unit target)
        {
            base.EnterCombat(target);
            _events.ScheduleEvent(GunshipEvents.Bladestorm, RandomHelper.URand(13000, 18000));
            _events.ScheduleEvent(GunshipEvents.WoundingStrike, RandomHelper.URand(8000, 10000));
        }

        public override void UpdateAI(uint diff)
        {
            if (!SelectVictim())
            {
                TriggerBurningPitch();
                return;
            }

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting) || me.HasAura(GunshipSpells.Bladestorm))
                return;

            if (!HasAttackablePlayerNearby())
                TriggerBurningPitch();

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case GunshipEvents.Bladestorm:
                        DoCastAOE(GunshipSpells.Bladestorm);
                        _events.ScheduleEvent(GunshipEvents.Bladestorm, RandomHelper.URand(25000, 30000));
                        break;
                    case GunshipEvents.WoundingStrike:
                        DoCastVictim(GunshipSpells.WoundingStrike);
                        _events.ScheduleEvent(GunshipEvents.WoundingStrike, RandomHelper.URand(9000, 13000));
                        break;
                    default:
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_gunship_gunner : gunship_npc_AI
    {
        public npc_gunship_gunner(Creature creature)
            : base(creature)
        {
            creature.m_CombatDistance = 200.0f;
        }

        public override void AttackStart(Unit target)
        {
            me.Attack(target, false);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            base.MovementInform(type, id);
            if (type == MovementGeneratorType.Point && id == EventId.ChargePrepath)
                me.SetControlled(true, UnitState.Root);
        }

        public override void UpdateAI(uint diff)
        {
            if (!SelectVictim())
            {
                TriggerBurningPitch();
                return;
            }

            DoSpellAttackIfReady(me.GetEntry() == CreatureIds.SkybreakerRifleman ? GunshipSpells.Shoot : GunshipSpells.HurlAxe);
        }
    }

    [Script]
    class npc_gunship_rocketeer : gunship_npc_AI
    {
        public npc_gunship_rocketeer(Creature creature) : base(creature)
        {
            creature.m_CombatDistance = 200.0f;
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            base.MovementInform(type, id);
            if (type == MovementGeneratorType.Point && id == EventId.ChargePrepath)
                me.SetControlled(true, UnitState.Root);
        }

        public override void UpdateAI(uint diff)
        {
            if (!SelectVictim())
                return;

            if (me.HasUnitState(UnitState.Casting))
                return;

            uint spellId = me.GetEntry() == CreatureIds.SkybreakerMortarSoldier ? GunshipSpells.RocketArtilleryA : GunshipSpells.RocketArtilleryH;
            if (me.GetSpellHistory().HasCooldown(spellId))
                return;

            DoCastAOE(spellId, true);
            me.GetSpellHistory().AddCooldown(spellId, 0, TimeSpan.FromSeconds(9));
        }
    }

    [Script]
    class npc_gunship_mage : gunship_npc_AI
    {
        public npc_gunship_mage(Creature creature) : base(creature)
        {
            me.SetReactState(ReactStates.Passive);
        }

        public override void EnterEvadeMode(EvadeReason why) { }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point)
                return;

            if (id == EventId.ChargePrepath && Slot != null)
            {
                SlotInfo[] slots = Instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? GunshipMiscData.SkybreakerSlotInfo : GunshipMiscData.OrgrimsHammerSlotInfo;
                me.SetFacingTo(slots[Index].TargetPosition.GetOrientation());
                switch ((PassengerSlots)Index)
                {
                    case PassengerSlots.FreezeMage:
                        DoCastAOE(GunshipSpells.BelowZero);
                        break;
                    case PassengerSlots.Mage1:
                    case PassengerSlots.Mage2:
                        DoCastAOE(GunshipSpells.ShadowChanneling);
                        break;
                    default:
                        break;
                }

                me.SetControlled(true, UnitState.Root);
            }
        }

        public override void UpdateAI(uint diff)
        {
            UpdateVictim();
        }

        public override bool CanAIAttack(Unit target)
        {
            return true;
        }
    }

    /** @HACK This AI only resets MOVEMENTFLAG_ROOT on the vehicle.
          Currently the core always removes MOVEMENTFLAG_ROOT sent from client packets to prevent cheaters from freezing clients of other players
          but it actually is a valid flag - needs more research to fix both freezes and keep the flag as is (see WorldSession.ReadMovementInfo)

Example packet:
ClientToServer: CMSG_FORCE_MOVE_ROOT_ACK (0x00E9) Length: 67 ConnectionIndex: 0 Time: 03/04/2010 03:57:55.000 Number: 471326
Guid:
Movement Counter: 80
Movement Flags: OnTransport, Root (2560)
Extra Movement Flags: None (0)
Time: 52291611
Position: X: -396.0302 Y: 2482.906 Z: 249.86
Orientation: 1.468665
Transport GUID: Full: 0x1FC0000000000460 Type: MOTransport Low: 1120
Transport Position: X: -6.152398 Y: -23.49037 Z: 21.64464 O: 4.827727
Transport Time: 9926
Transport Seat: 255
Fall Time: 824
*/
    [Script]
    class npc_gunship_cannon : PassiveAI
    {
        public npc_gunship_cannon(Creature creature)
            : base(creature) { }

        public override void OnCharmed(bool apply) { }

        public override void PassengerBoarded(Unit passenger, sbyte seat, bool apply)
        {
            if (!apply)
            {
                me.SetControlled(false, UnitState.Root);
                me.SetControlled(true, UnitState.Root);
            }
        }
    }

    [Script]
    class spell_igb_rocket_pack : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(GunshipSpells.RocketPackDamage, GunshipSpells.RocketBurst);
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            if (GetTarget().moveSpline.Finalized())
                Remove(AuraRemoveMode.Expire);
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            SpellInfo damageInfo = Global.SpellMgr.GetSpellInfo(GunshipSpells.RocketPackDamage);
            GetTarget().CastCustomSpell(GunshipSpells.RocketPackDamage, SpellValueMod.BasePoint0, (int)(2 * (damageInfo.GetEffect(0).CalcValue() + aurEff.GetTickNumber() * aurEff.GetPeriod())), null, TriggerCastFlags.FullMask);
            GetTarget().CastSpell(null, GunshipSpells.RocketBurst, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
            OnEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_igb_rocket_pack_useable : AuraScript
    {
        public override bool Load()
        {
            return GetOwner().GetInstanceScript() != null;
        }

        bool CheckAreaTarget(Unit target)
        {
            return target.IsTypeId(TypeId.Player) && GetOwner().GetInstanceScript().GetBossState(Bosses.GunshipBattle) != EncounterState.Done;
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Creature owner = GetOwner().ToCreature();
            if (owner)
            {
                Player target = GetTarget().ToPlayer();
                if (target)
                    if (target.HasItemCount(GunshipMiscData.ItemGoblinRocketPack, 1))
                        Global.CreatureTextMgr.SendChat(owner, GunshipTexts.SayZafodRocketPackActive, target, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, Team.Other, false, target);
            }
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Creature owner = GetOwner().ToCreature();
            if (owner)
            {
                Player target = GetTarget().ToPlayer();
                if (target)
                    if (target.HasItemCount(GunshipMiscData.ItemGoblinRocketPack, 1))
                        Global.CreatureTextMgr.SendChat(owner, GunshipTexts.SayZafodRocketPackDisabled, target, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, Team.Other, false, target);
            }
        }

        public override void Register()
        {
            DoCheckAreaTarget.Add(new CheckAreaTargetHandler(CheckAreaTarget));
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_igb_on_gunship_deck : AuraScript
    {
        public override bool Load()
        {
            InstanceScript instance = GetOwner().GetInstanceScript();
            if (instance != null)
                _teamInInstance = (Team)instance.GetData(DataTypes.TeamInInstance);
            else
                _teamInInstance = 0;
            return true;
        }

        bool CheckAreaTarget(Unit unit)
        {
            return unit.IsTypeId(TypeId.Player);
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetSpellInfo().Id == (_teamInInstance == Team.Horde ? GunshipSpells.OnSkybreakerDeck : GunshipSpells.OnOrgrimsHammerDeck))
            {
                Creature gunship = GetOwner().FindNearestCreature(_teamInInstance == Team.Horde ? CreatureIds.OrgrimsHammer : CreatureIds.TheSkybreaker, 200.0f);
                if (gunship)
                    gunship.GetAI().SetGUID(GetTarget().GetGUID(), EncounterActions.ShipVisits);
            }
        }

        public override void Register()
        {
            DoCheckAreaTarget.Add(new CheckAreaTargetHandler(CheckAreaTarget));
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }

        Team _teamInInstance;
    }

    [Script]
    class spell_igb_periodic_trigger_with_power_cost : AuraScript
    {
        void HandlePeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), GetSpellInfo().GetEffect(0).TriggerSpell, (TriggerCastFlags.FullMask & ~TriggerCastFlags.IgnorePowerAndReagentCost));
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodicTick, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script]
    class spell_igb_cannon_blast : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Unit);
        }

        void CheckEnergy()
        {
            if (GetCaster().GetPower(PowerType.Energy) >= 100)
            {
                GetCaster().CastSpell(GetCaster(), GunshipSpells.Overheat, TriggerCastFlags.FullMask);
                Vehicle vehicle = GetCaster().GetVehicleKit();
                if (vehicle)
                {
                    Unit passenger = vehicle.GetPassenger(0);
                    if (passenger)
                        Global.CreatureTextMgr.SendChat(GetCaster().ToCreature(), GunshipTexts.SayOverheat, passenger);
                }
            }
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(CheckEnergy));
        }
    }

    [Script]
    class spell_igb_incinerating_blast : SpellScript
    {
        void StoreEnergy()
        {
            _energyLeft = (uint)GetCaster().GetPower(PowerType.Energy) - 10;
        }

        void RemoveEnergy()
        {
            GetCaster().SetPower(PowerType.Energy, 0);
        }

        void CalculateDamage(uint effIndex)
        {
            SetEffectValue((int)(GetEffectValue() + _energyLeft * _energyLeft * 8));
        }

        public override void Register()
        {
            OnCast.Add(new CastHandler(StoreEnergy));
            AfterCast.Add(new CastHandler(RemoveEnergy));
            OnEffectLaunchTarget.Add(new EffectHandler(CalculateDamage, 1, SpellEffectName.SchoolDamage));
        }

        uint _energyLeft;
    }

    [Script]
    class spell_igb_overheat : AuraScript
    {
        public override bool Load()
        {
            if (GetAura().GetAuraType() != AuraObjectType.Unit)
                return false;
            return GetUnitOwner().IsVehicle();
        }

        void SendClientControl(bool value)
        {
            Vehicle vehicle = GetUnitOwner().GetVehicleKit();
            if (vehicle)
            {
                Unit passenger = vehicle.GetPassenger(0);
                if (passenger)
                {
                    Player player = passenger.ToPlayer();
                    if (player)
                    {
                        ControlUpdate data = new ControlUpdate();
                        data.Guid = GetUnitOwner().GetGUID();
                        data.On = value;
                        player.SendPacket(data);
                    }
                }
            }
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            SendClientControl(false);
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            SendClientControl(true);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_igb_below_zero : SpellScript
    {
        void RemovePassengers(SpellMissInfo missInfo)
        {
            if (missInfo != SpellMissInfo.None)
                return;

            GetHitUnit().CastSpell(GetHitUnit(), GunshipSpells.EjectAllPassengersBelowZero, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            BeforeHit.Add(new BeforeHitHandler(RemovePassengers));
        }
    }

    [Script]
    class spell_igb_teleport_to_enemy_ship : SpellScript
    {
        void RelocateTransportOffset(uint effIndex)
        {
            Position dest = GetHitDest();
            Unit target = GetHitUnit();
            if (dest == null || !target || !target.GetTransport())
                return;

            float x, y, z, o;
            dest.GetPosition(out x, out y, out z, out o);
            target.GetTransport().CalculatePassengerOffset(ref x, ref y, ref z, ref o);
            target.m_movementInfo.transport.pos.Relocate(x, y, z, o);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(RelocateTransportOffset, 0, SpellEffectName.TeleportUnitsOld));
        }
    }

    [Script]
    class spell_igb_burning_pitch_selector : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            Team team = Team.Horde;
            InstanceScript instance = GetCaster().GetInstanceScript();
            if (instance != null)
                team = (Team)instance.GetData(DataTypes.TeamInInstance);

            targets.RemoveAll(target =>
            {
                Transport transport = target.GetTransport();
                if (transport)
                    return transport.GetEntry() != (team == Team.Horde ? GameObjectIds.OrgrimsHammer_H : GameObjectIds.TheSkybreaker_A);
                return true;
            });

            if (!targets.Empty())
            {
                WorldObject target = targets.SelectRandom();
                targets.Clear();
                targets.Add(target);
            }
        }

        void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), TriggerCastFlags.None);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_igb_burning_pitch : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetCaster().CastCustomSpell((uint)GetEffectValue(), SpellValueMod.BasePoint0, 8000, null, TriggerCastFlags.FullMask);
            GetHitUnit().CastSpell(GetHitUnit(), GunshipSpells.BurningPitch, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_igb_rocket_artillery : SpellScript
    {
        void SelectRandomTarget(List<WorldObject> targets)
        {
            if (!targets.Empty())
            {
                WorldObject target = targets.SelectRandom();
                targets.Clear();
                targets.Add(target);
            }
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), TriggerCastFlags.None);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(SelectRandomTarget, 0, Targets.UnitSrcAreaEntry));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_igb_rocket_artillery_explosion : SpellScript
    {
        void DamageGunship(uint effIndex)
        {
            InstanceScript instance = GetCaster().GetInstanceScript();
            if (instance != null)
                GetCaster().CastCustomSpell(instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? GunshipSpells.BurningPitchDamageH : GunshipSpells.BurningPitchDamageA, SpellValueMod.BasePoint0, 5000, null, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(DamageGunship, 0, SpellEffectName.TriggerMissile));
        }
    }

    [Script]
    class spell_igb_gunship_fall_teleport : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetInstanceScript() != null;
        }

        void SelectTransport(ref WorldObject target)
        {
            InstanceScript instance = target.GetInstanceScript();
            if (instance != null)
                target = Global.ObjAccessor.FindTransport(instance.GetGuidData(Bosses.GunshipBattle));
        }

        void RelocateDest(uint effIndex)
        {
            if (GetCaster().GetInstanceScript().GetData(DataTypes.TeamInInstance) == (uint)Team.Horde)
                GetHitDest().RelocateOffset(new Position(0.0f, 0.0f, 36.0f, 0.0f));
            else
                GetHitDest().RelocateOffset(new Position(0.0f, 0.0f, 21.0f, 0.0f));
        }

        public override void Register()
        {
            OnObjectTargetSelect.Add(new ObjectTargetSelectHandler(SelectTransport, 0, Targets.DestNearbyEntry));
            OnEffectLaunch.Add(new EffectHandler(RelocateDest, 0, SpellEffectName.TeleportUnitsOld));
        }
    }

    [Script]
    class spell_igb_check_for_players : SpellScript
    {
        public override bool Load()
        {
            _playerCount = 0;
            return GetCaster().IsTypeId(TypeId.Unit);
        }

        void CountTargets(List<WorldObject> targets)
        {
            _playerCount = (uint)targets.Count;
        }

        void TriggerWipe()
        {
            if (_playerCount == 0)
                GetCaster().ToCreature().GetAI().JustDied(null);
        }

        void TeleportPlayer(uint effIndex)
        {
            if (GetHitUnit().GetPositionZ() < GetCaster().GetPositionZ() - 10.0f)
                GetHitUnit().CastSpell(GetHitUnit(), GunshipSpells.GunshipFallTeleport, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitSrcAreaEntry));
            AfterCast.Add(new CastHandler(TriggerWipe));
            OnEffectHitTarget.Add(new EffectHandler(TeleportPlayer, 0, SpellEffectName.Dummy));
        }

        uint _playerCount;
    }

    [Script]
    class spell_igb_teleport_players_on_victory : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetInstanceScript() != null;
        }

        void FilterTargets(List<WorldObject> targets)
        {
            InstanceScript instance = GetCaster().GetInstanceScript();
            targets.RemoveAll(target => target.GetTransGUID() != instance.GetGuidData(DataTypes.EnemyGunship));
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEntry));
        }
    }

    [Script] // 71201 - Battle Experience - proc should never happen, handled in script
    class spell_igb_battle_experience_check : AuraScript
    {
        bool CheckProc(ProcEventInfo eventInfo)
        {
            return false;
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
        }
    }

    [Script]
    class achievement_im_on_a_boat : AchievementCriteriaScript
    {
        public achievement_im_on_a_boat() : base("achievement_im_on_a_boat") { }

        public override bool OnCheck(Player source, Unit target)
        {
            return target.GetAI() != null && target.GetAI().GetData(EncounterActions.ShipVisits) <= 2;
        }
    }
}

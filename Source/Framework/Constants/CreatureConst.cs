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
 */﻿

namespace Framework.Constants
{
    public enum CreatureLinkedRespawnType
    {
        CreatureToCreature,
        CreatureToGO,         // Creature is dependant on GO
        GOToGO,
        GOToCreature          // GO is dependant on creature
    }

    public enum AiReaction
    {
        Alert = 0,                               // pre-aggro (used in client packet handler)
        Friendly = 1,                               // (NOT used in client packet handler)
        Hostile = 2,                               // sent on every attack, triggers aggro sound (used in client packet handler)
        Afraid = 3,                               // seen for polymorph (when AI not in control of self?) (NOT used in client packet handler)
        Destory = 4                                // used on object destroy (NOT used in client packet handler)
    }

    public enum CreatureEliteType
    {
        Normal = 0,
        Elite = 1,
        RareElite = 2,
        WorldBoss = 3,
        Rare = 4,
        Unknown = 5                      // found in 2.2.3 for 2 mobs
    }

    [System.Flags]
    public enum UnitFlags : uint
    {
        ServerControlled = 0x01,
        NonAttackable = 0x02,
        RemoveClientControl = 0x04, // This is a legacy flag used to disable movement player's movement while controlling other units, SMSG_CLIENT_CONTROL replaces this functionality clientside now. CONFUSED and FLEEING flags have the same effect on client movement asDISABLE_MOVE_CONTROL in addition to preventing spell casts/autoattack (they all allow climbing steeper hills and emotes while moving)
        PvpAttackable = 0x08,
        Rename = 0x10,
        Preparation = 0x20,
        Unk6 = 0x40,
        NotAttackable1 = 0x80,
        ImmuneToPc = 0x100,
        ImmuneToNpc = 0x200,
        Looting = 0x400,
        PetInCombat = 0x800,
        Pvp = 0x1000,
        Silenced = 0x2000,
        CannotSwim = 0x4000,
        Unk15 = 0x8000,
        Unk16 = 0x10000,
        Pacified = 0x20000,
        Stunned = 0x40000,
        InCombat = 0x80000,
        TaxiFlight = 0x100000,
        Disarmed = 0x200000,
        Confused = 0x400000,
        Fleeing = 0x800000,
        PlayerControlled = 0x1000000,
        NotSelectable = 0x2000000,
        Skinnable = 0x4000000,
        Mount = 0x8000000,
        Unk28 = 0x10000000,
        Unk29 = 0x20000000,
        Sheathe = 0x40000000,
        Unk31 = 0x80000000
    }

    public enum UnitFlags2 : uint
    {
        FeignDeath = 0x01,
        Unk1 = 0x02,
        IgnoreReputation = 0x04,
        ComprehendLang = 0x08,
        MirrorImage = 0x10,
        InstantlyAppearModel = 0x20,
        ForceMove = 0x40,
        DisarmOffhand = 0x80,
        DisablePredStats = 0x100,
        AllowChangingTalents = 0x200,
        DisarmRanged = 0x400,
        RegeneratePower = 0x800,
        RestrictPartyInteraction = 0x1000,
        PreventSpellClick = 0x2000,
        AllowEnemyInteract = 0x4000,
        DisableTurn = 0x8000,
        Unk2 = 0x10000,
        PlayDeathAnim = 0x20000,
        AllowCheatSpells = 0x40000,
        NoActions = 0x800000
    }

    public enum UnitFlags3 : uint
    {
        Unk1 = 0x01,
    }

    public enum NPCFlags : ulong
    {
        None = 0x00,
        Gossip = 0x01,     // 100%
        QuestGiver = 0x02,     // 100%
        Unk1 = 0x04,
        Unk2 = 0x08,
        Trainer = 0x10,     // 100%
        TrainerClass = 0x20,     // 100%
        TrainerProfession = 0x40,     // 100%
        Vendor = 0x80,     // 100%
        VendorAmmo = 0x100,     // 100%, General Goods Vendor
        VendorFood = 0x200,     // 100%
        VendorPoison = 0x400,     // Guessed
        VendorReagent = 0x800,     // 100%
        Repair = 0x1000,     // 100%
        FlightMaster = 0x2000,     // 100%
        SpiritHealer = 0x4000,     // Guessed
        SpiritGuide = 0x8000,     // Guessed
        Innkeeper = 0x10000,     // 100%
        Banker = 0x20000,     // 100%
        Petitioner = 0x40000,     // 100% 0xc0000 = Guild Petitions, 0x40000 = Arena Team Petitions
        TabardDesigner = 0x80000,     // 100%
        BattleMaster = 0x100000,     // 100%
        Auctioneer = 0x200000,     // 100%
        StableMaster = 0x400000,     // 100%
        GuildBanker = 0x800000,     //
        SpellClick = 0x1000000,     //
        PlayerVehicle = 0x2000000,     // Players With Mounts That Have Vehicle Data Should Have It Set
        Mailbox = 0x4000000,     // Mailbox
        ArtifactPowerRespec = 0x8000000,     // Artifact Powers Reset
        Transmogrifier = 0x10000000,     // Transmogrification
        VaultKeeper = 0x20000000,     // Void Storage
        WildBattlePet = 0x40000000,     // Pet That Player Can Fight (Battle Pet)
        BlackMarket = 0x80000000,     // Black Market
        ItemUpgradeMaster = 0x100000000,
        GarrisonArchitect = 0x200000000,
        Steering = 0x400000000,
        ShipmentCrafter = 0x1000000000,
        GarrisonMissionNpc = 0x2000000000,
        TradeskillNpc = 0x4000000000,
        BlackMarketView = 0x8000000000,
        ContributionCollector = 0x40000000000,
    }

    public enum CreatureTypeFlags : uint
    {
        TameablePet = 0x01, // Makes the mob tameable (must also be a beast and have family set)
        GhostVisible = 0x02, // Creature are also visible for not alive player. Allow gossip interaction if npcflag allow?
        BossMob = 0x04, // Changes creature's visible level to "??" in the creature's portrait - Immune Knockback.
        DoNotPlayWoundParryAnimation = 0x08,
        HideFactionTooltip = 0x10,
        Unk5 = 0x20, // Sound related
        SpellAttackable = 0x40,
        CanInteractWhileDead = 0x80, // Player can interact with the creature if its dead (not player dead)
        HerbSkinningSkill = 0x100, // Can be looted by herbalist
        MiningSkinningSkill = 0x200, // Can be looted by miner
        DoNotLogDeath = 0x400, // Death event will not show up in combat log
        MountedCombatAllowed = 0x800, // Creature can remain mounted when entering combat
        CanAssist = 0x1000, // ? Can aid any player in combat if in range?
        IsPetBarUsed = 0x2000,
        MaskUID = 0x4000,
        EngineeringSkinningSkill = 0x8000, // Can be looted by engineer
        ExoticPet = 0x10000, // Can be tamed by hunter as exotic pet
        UseDefaultCollisionBox = 0x20000, // Collision related. (always using default collision box?)
        IsSiegeWeapon = 0x40000,
        CanCollideWithMissiles = 0x80000, // Projectiles can collide with this creature - interacts with TARGET_DEST_TRAJ
        HideNamePlate = 0x100000,
        DoNotPlayMountedAnimations = 0x200000,
        IsLinkAll = 0x400000,
        InteractOnlyWithCreator = 0x800000,
        DoNotPlayUnitEventSounds = 0x1000000,
        HasNoShadowBlob = 0x2000000,
        TreatAsRaidUnit = 0x4000000, //! Creature can be targeted by spells that require target to be in caster's party/raid
        ForceGossip = 0x8000000, // Allows the creature to display a single gossip option.
        DoNotSheathe = 0x10000000,
        DoNotTargetOnInteration = 0x20000000,
        DoNotRenderObjectName = 0x40000000,
        UnitIsQuestBoss = 0x80000000 // Not verified
    }

    public enum CreatureFlagsExtra : uint
    {
        InstanceBind = 0x01,       // Creature Kill Bind Instance With Killer And Killer'S Group
        Civilian = 0x02,       // Not Aggro (Ignore Faction/Reputation Hostility)
        NoParry = 0x04,       // Creature Can'T Parry
        NoParryHasten = 0x08,       // Creature Can'T Counter-Attack At Parry
        NoBlock = 0x10,       // Creature Can'T Block
        NoCrush = 0x20,       // Creature Can'T Do Crush Attacks
        NoXpAtKill = 0x40,       // Creature Kill Not Provide Xp
        Trigger = 0x80,       // Trigger Creature
        NoTaunt = 0x100,       // Creature Is Immune To Taunt Auras And Effect Attack Me
        NoMoveFlagsUpdate = 0x200, // Creature won't update movement flags
        Worldevent = 0x4000,       // Custom Flag For World Event Creatures (Left Room For Merging)
        Guard = 0x8000,       // Creature Is Guard
        NoCrit = 0x20000,       // Creature Can'T Do Critical Strikes
        NoSkillgain = 0x40000,       // Creature Won'T Increase Weapon Skills
        TauntDiminish = 0x80000,       // Taunt Is A Subject To Diminishing Returns On This Creautre
        AllDiminish = 0x100000,       // Creature Is Subject To All Diminishing Returns As Player Are
        NoPlayerDamageReq = 0x200000,       // creature does not need to take player damage for kill credit
        DungeonBoss = 0x10000000,        // Creature Is A Dungeon Boss (Set Dynamically, Do Not Add In Db)
        IgnorePathfinding = 0x20000000,        // creature ignore pathfinding
        ImmunityKnockback = 0x40000000,        // creature is immune to knockback effects

        DBAllowed = (InstanceBind | Civilian | NoParry | NoParryHasten | NoBlock | NoCrush | NoXpAtKill | 
            Trigger | NoTaunt | NoMoveFlagsUpdate | Worldevent | NoCrit | NoSkillgain | TauntDiminish | AllDiminish | Guard |
            IgnorePathfinding | NoPlayerDamageReq | ImmunityKnockback)
    }

    public enum CreatureType
    {
        Beast = 1,
        Dragonkin = 2,
        Demon = 3,
        Elemental = 4,
        Giant = 5,
        Undead = 6,
        Humanoid = 7,
        Critter = 8,
        Mechanical = 9,
        NotSpecified = 10,
        Totem = 11,
        NonCombatPet = 12,
        GasCloud = 13,
        WildPet = 14,
        Aberration = 15,

        MaskDemonOrUnDead = (1 << (Demon - 1)) | (1 << (Undead - 1)),
        MaskHumanoidOrUndead = (1 << (Humanoid - 1)) | (1 << (Undead - 1)),
        MaskMechanicalOrElemental = (1 << (Mechanical - 1)) | (1 << (Elemental - 1))
    }

    public enum CreatureFamily
    {
        None = 0,
        Wolf = 1,
        Cat = 2,
        Spider = 3,
        Bear = 4,
        Boar = 5,
        Crocolisk = 6,
        CarrionBird = 7,
        Crab = 8,
        Gorilla = 9,
        HorseCustom = 10, // Does Not Exist In Dbc But Used For Horse Like Beasts In Db
        Raptor = 11,
        Tallstrider = 12,
        Felhunter = 15,
        Voidwalker = 16,
        Succubus = 17,
        Doomguard = 19,
        Scorpid = 20,
        Turtle = 21,
        Imp = 23,
        Bat = 24,
        Hyena = 25,
        BirdOfPrey = 26,
        WindSerpent = 27,
        RemoteControl = 28,
        Felguard = 29,
        Dragonhawk = 30,
        Ravager = 31,
        WarpStalker = 32,
        Sporebat = 33,
        NetherRay = 34,
        Serpent = 35,
        Moth = 37,
        Chimaera = 38,
        Devilsaur = 39,
        Ghoul = 40,
        Silithid = 41,
        Worm = 42,
        Rhino = 43,
        Wasp = 44,
        CoreHound = 45,
        SpiritBeast = 46,
        WaterElemental = 49,
        Fox = 50,
        Monkey = 51,
        Dog = 52,
        Beetle = 53,
        ShaleSpider = 55,
        Zombie = 56,
        BeetleOld = 57,
        Silithid2 = 59,
        Wasp2 = 66,
        Hydra = 68,
        Felimp = 100,
        Voidlord = 101,
        Shivara = 102,
        Observer = 103,
        Wrathguard = 104,
        Infernal = 108,
        Fireelemental = 116,
        Earthelemental = 117,
        Crane = 125,
        Waterstrider = 126,
        Porcupine = 127,
        Quilen = 128,
        Goat = 129,
        Basilisk = 130,
        Direhorn = 138,
        Stormelemental = 145,
        Mtwaterelemental = 146,
        Torrorguard = 147,
        Abyssal = 148,
        Rylak = 149,
        Riverbeast = 150,
        Stag = 151
    }

    public enum InhabitType
    {
        Ground = 1,
        Water = 2,
        Air = 4,
        Root = 8,
        Anywhere = Ground | Water | Air | Root
    }

    public enum EvadeReason
    {
        NoHostiles,       // the creature's threat list is empty
        Boundary,          // the creature has moved outside its evade boundary
        NoPath,           // the creature was unable to reach its target for over 5 seconds
        SequenceBreak,    // this is a boss and the pre-requisite encounters for engaging it are not defeated yet
        Other
    }

    public enum GroupAIFlags
    {
        None = 0,          // No creature group behavior
        MembersAssistLeader = 0x01, // The member aggroes if the leader aggroes
        LeaderAssistsMember = 0x02, // The leader aggroes if the member aggroes
        MembersAssistMember = (MembersAssistLeader | LeaderAssistsMember), // every member will assist if any member is attacked
        IdleInFormation = 0x200, // The member will follow the leader when pathing idly
    }
}

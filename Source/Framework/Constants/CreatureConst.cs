// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Constants
{
    public enum CreatureLinkedRespawnType
    {
        CreatureToCreature = 0,
        CreatureToGO = 1,         // Creature is dependant on GO
        GOToGO = 2,
        GOToCreature = 3          // GO is dependant on creature
    }

    public enum AiReaction
    {
        Alert = 0,                               // pre-aggro (used in client packet handler)
        Friendly = 1,                               // (NOT used in client packet handler)
        Hostile = 2,                               // sent on every attack, triggers aggro sound (used in client packet handler)
        Afraid = 3,                               // seen for polymorph (when AI not in control of self?) (NOT used in client packet handler)
        Destory = 4                                // used on object destroy (NOT used in client packet handler)
    }

    public enum CreatureClassifications
    {
        Normal = 0,
        Elite = 1,
        RareElite = 2,
        Obsolete = 3,
        Rare = 4,
        Trivial = 5,
        MinusMob = 6
    }

    [Flags]
    public enum UnitFlags : uint
    {
        ServerControlled = 0x01,
        NonAttackable = 0x02, // not attackable, set when creature starts to cast spells with SPELL_EFFECT_SPAWN and cast time, removed when spell hits caster, original name is UNIT_FLAG_SPAWNING. Rename when it will be removed from all scripts
        RemoveClientControl = 0x04, // This is a legacy flag used to disable movement player's movement while controlling other units, SMSG_CLIENT_CONTROL replaces this functionality clientside now. CONFUSED and FLEEING flags have the same effect on client movement asDISABLE_MOVE_CONTROL in addition to preventing spell casts/autoattack (they all allow climbing steeper hills and emotes while moving)
        PlayerControlled = 0x08, //controlled by player, use _IMMUNE_TO_PC instead of _IMMUNE_TO_NPC
        Rename = 0x10,
        Preparation = 0x20,
        Unk6 = 0x40,
        NotAttackable1 = 0x80,
        ImmuneToPc = 0x100,
        ImmuneToNpc = 0x200,
        Looting = 0x400,
        PetInCombat = 0x800,
        PvpEnabling = 0x1000,
        ForceNameplate = 0x2000,
        CantSwim = 0x4000,
        CanSwim = 0x8000, // shows swim animation in water
        NonAttackable2 = 0x10000,
        Pacified = 0x20000,
        Stunned = 0x40000,
        InCombat = 0x80000,
        OnTaxi = 0x100000,
        Disarmed = 0x200000,
        Confused = 0x400000,
        Fleeing = 0x800000,
        Possessed = 0x1000000, // under direct client control by a player (possess or vehicle)
        Uninteractible = 0x2000000,
        Skinnable = 0x4000000,
        Mount = 0x8000000,
        Unk28 = 0x10000000,
        PreventEmotesFromChatText = 0x20000000, // Prevent automatically playing emotes from parsing chat text, for example "lol" in /say, ending message with ? or !, or using /yell
        Sheathe = 0x40000000,
        Immune = 0x80000000,

        Disallowed = (ServerControlled | NonAttackable | RemoveClientControl |
                                   PlayerControlled | Rename | Preparation | /* UNIT_FLAG_UNK_6 | */
                                   NotAttackable1 | Looting | PetInCombat | PvpEnabling |
                                   CantSwim | CanSwim | NonAttackable2 | Pacified | Stunned |
                                   InCombat | OnTaxi | Disarmed | Confused | Fleeing |
                                   Possessed | Skinnable | Mount | Unk28 |
                                   PreventEmotesFromChatText | Sheathe | Immune),

        Allowed = (0xFFFFFFFF & ~Disallowed)
    }

    public enum UnitFlags2 : uint
    {
        FeignDeath = 0x01,
        HideBody = 0x02,
        IgnoreReputation = 0x04,
        ComprehendLang = 0x08,
        MirrorImage = 0x10,
        DontFadeIn = 0x20,
        ForceMovement = 0x40,
        DisarmOffhand = 0x80,
        DisablePredStats = 0x100,
        AllowChangingTalents = 0x200,
        DisarmRanged = 0x400,
        RegeneratePower = 0x800,
        RestrictPartyInteraction = 0x1000,
        PreventSpellClick = 0x2000,
        InteractWhileHostile = 0x4000,
        CannotTurn = 0x8000,
        Unk2 = 0x10000,
        PlayDeathAnim = 0x20000,
        AllowCheatSpells = 0x40000,
        SuppressHighlightWhenTargetedOrMousedOver = 0x00080000,
        TreatAsRaidUnitForHelpfulSpells = 0x100000,
        LargeAoi = 0x00200000,
        GiganticAoi = 0x400000,
        NoActions = 0x800000,
        AiWillOnlySwimIfTargetSwims = 0x1000000,
        DontGenerateCombatLogWhenEngagedWithNpcs = 0x2000000,
        UntargetableByClient = 0x4000000,
        AttackerIgnoresMinimumRanges = 0x8000000,
        UninteractibleIfHostile = 0x10000000,
        Unsued11 = 0x20000000,
        InfiniteAoi = 0x40000000,
        Unused13 = 0x80000000,

        Disallowed = (/*FeignDeath |*/ IgnoreReputation | ComprehendLang |
            MirrorImage | ForceMovement | DisarmOffhand |
            DisablePredStats | AllowChangingTalents | DisarmRanged |
            /* UNIT_FLAG2_REGENERATE_POWER | */ RestrictPartyInteraction | CannotTurn |
            PreventSpellClick | /*InteractWhileHostile | */ /* Unk2 | */
            /* UNIT_FLAG2_PLAY_DEATH_ANIM | */ AllowCheatSpells | SuppressHighlightWhenTargetedOrMousedOver |
            TreatAsRaidUnitForHelpfulSpells | LargeAoi | GiganticAoi | NoActions |
            AiWillOnlySwimIfTargetSwims | DontGenerateCombatLogWhenEngagedWithNpcs | AttackerIgnoresMinimumRanges |
            UninteractibleIfHostile | Unsued11 | InfiniteAoi | Unused13),

        Allowed = (0xFFFFFFFF & ~Disallowed)
    }

    public enum UnitFlags3 : uint
    {
        Unk0 = 0x01,
        UnconsciousOnDeath = 0x02,   // Description Shows "Unconscious" In Unit Tooltip Instead Of "Dead"
        AllowMountedCombat = 0x04,
        GarrisonPet = 0x08,   // Special Garrison Pet Creatures That Display One Of Favorite Player Battle Pets - This Flag Allows Querying Name And Turns Off Default Battle Pet Behavior
        UiCanGetPosition = 0x10,   // Allows Lua Functions Like Unitposition To Always Get The Position Even For Npcs Or Non-Grouped Players
        AiObstacle = 0x20,
        AlternativeDefaultLanguage = 0x40,
        SuppressAllNpcFeedback = 0x80,   // Skips Playing Sounds On Left Clicking Npc For All Npcs As Long As Npc With This Flag Is Visible
        IgnoreCombat = 0x100,   // Same As SpellAuraIgnoreCombat
        SuppressNpcFeedback = 0x200,   // Skips Playing Sounds On Left Clicking Npc
        Unk10 = 0x400,
        Unk11 = 0x800,
        Unk12 = 0x1000,
        FakeDead = 0x2000,   // Title Show As Dead
        NoFacingOnInteractAndFastFacingChase = 0x4000,   // Causes The Creature To Both Not Change Facing On Interaction And Speeds Up Smooth Facing Changes While Attacking (Clientside)
        UntargetableFromUi = 0x8000,   // Cannot Be Targeted From Lua Functions Startattack, Targetunit, Petattack
        NoFacingOnInteractWhileFakeDead = 0x10000,   // Prevents Facing Changes While Interacting If Creature Has Flag FakeDead
        AlreadySkinned = 0x20000,
        SuppressAllNpcSounds = 0x40000,   // Skips Playing Sounds On Beginning And End Of Npc Interaction For All Npcs As Long As Npc With This Flag Is Visible
        SuppressNpcSounds = 0x80000,   // Skips Playing Sounds On Beginning And End Of Npc Interaction
        AllowInteractionWhileInCombat = 0x100000, //Allows using various NPC functions while in combat (vendor, gossip, questgiver)
        Unk21 = 0x200000,
        DontFadeOut = 0x400000,
        Unk23 = 0x800000,
        ForceHideNameplate = 0x1000000,
        Unk25 = 0x2000000,
        Unk26 = 0x4000000,
        Unk27 = 0x8000000,
        Unk28 = 0x10000000,
        Unk29 = 0x20000000,
        Unk30 = 0x40000000,
        Unk31 = 0x80000000,

        Disallowed = (Unk0 | /* UnconsciousOnDeath | */ /* AllowMountedCombat | */ GarrisonPet |
                                                                   /* UiCanGetPosition | */ /* AiObstacle | */ AlternativeDefaultLanguage | /* SuppressAllNpcFeedback | */
                                                                   IgnoreCombat | SuppressNpcFeedback | Unk10 | Unk11 |
                                                                   Unk12 | /*FakeDead |*/ /* NoFacingOnInteractAndFastFacingChase | */ /* UntargetableFromUi | */
                                                                   /* NoFacingOnInteractWhileFakeDead | */ AlreadySkinned | /* SuppressAllNpcSounds | */ /* SuppressNpcSounds | */
                                                                   AllowInteractionWhileInCombat | Unk21 | /* DontFadeOut | */ Unk23 |
                                                                   ForceHideNameplate | Unk25 | Unk26 | Unk27 |
                                                                   Unk28 | Unk29 | Unk30 | Unk31), // Skip
        Allowed = (0xffffffff & ~Disallowed) // Skip
    }

    public enum NPCFlags : uint
    {
        None = 0x00,
        Gossip = 0x01,     // 100%
        QuestGiver = 0x02,     // 100%
        AccountBanker = 0x04, //account banker
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
        SpiritHealer = 0x4000,     // 100%
        AreaSpiritHealer = 0x8000,     // 100%
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

        VendorMask = Vendor | VendorAmmo | VendorFood | VendorPoison | VendorReagent
    }

    public enum NPCFlags2
    {
        None = 0x00,
        ItemUpgradeMaster = 0x01,
        GarrisonArchitect = 0x02,
        Steering = 0x04,
        AreaSpiritHealerIndividual = 0x08,
        ShipmentCrafter = 0x10,
        GarrisonMissionNpc = 0x20,
        TradeskillNpc = 0x40,
        BlackMarketView = 0x80,
        GarrisonTalentNpc = 0x200,
        ContributionCollector = 0x400,
        FastSteeringAvoidsObstacles = 0x2000,
        AzeriteRespec = 0x4000,
        IslandsQueue = 0x8000,
        SuppressNpcSoundsExceptEndOfInteraction = 0x10000,
        PersonalTabardDesigner = 0x200000
    }

    [Flags]
    public enum CreatureTypeFlags : uint
    {
        Tameable = 0x00000001, // Makes The Mob Tameable (Must Also Be A Beast And Have Family Set)
        VisibleToGhosts = 0x00000002, // Creature Is Also Visible For Not Alive Player. Allows Gossip Interaction If Npcflag Allows?
        BossMob = 0x00000004, // Changes Creature'S Visible Level To "??" In The Creature'S Portrait
        DoNotPlayWoundAnim = 0x00000008,
        NoFactionTooltip = 0x00000010,
        MoreAudible = 0x00000020, // Sound Related
        SpellAttackable = 0x00000040,
        InteractWhileDead = 0x00000080, // Player Can Interact With The Creature If Creature Is Dead (Not If Player Is Dead)
        SkinWithHerbalism = 0x00000100, // Can Be Looted By Herbalist
        SkinWithMining = 0x00000200, // Can Be Looted By Miner
        NoDeathMessage = 0x00000400, // Death Event Will Not Show Up In Combat Log
        AllowMountedCombat = 0x00000800, // Creature Can Remain Mounted When Entering Combat
        CanAssist = 0x00001000, // ? Can Aid Any Player In Combat If In Range?
        NoPetBar = 0x00002000,
        MaskUid = 0x00004000,
        SkinWithEngineering = 0x00008000, // Can Be Looted By Engineer
        TameableExotic = 0x00010000, // Can Be Tamed By Hunter As Exotic Pet
        UseModelCollisionSize = 0x00020000, // Collision Related. (Always Using Default Collision Box?)
        AllowInteractionWhileInCombat = 0x00040000,
        CollideWithMissiles = 0x00080000, // Projectiles Can Collide With This Creature - Interacts With TargetDestTraj
        NoNamePlate = 0x00100000,
        DoNotPlayMountedAnimations = 0x00200000,
        LinkAll = 0x00400000,
        InteractOnlyWithCreator = 0x00800000,
        DoNotPlayUnitEventSounds = 0x01000000,
        HasNoShadowBlob = 0x02000000,
        TreatAsRaidUnit = 0x04000000, //! Creature Can Be Targeted By Spells That Require Target To Be In Caster'S Party/Raid
        ForceGossip = 0x08000000,   // Allows The Creature To Display A Single Gossip Option.
        DoNotSheathe = 0x10000000,
        DoNotTargetOnInteraction = 0x20000000,
        DoNotRenderObjectName = 0x40000000,
        QuestBoss = 0x80000000  // Not Verified
    }

    [Flags]
    public enum CreatureStaticFlags : uint
    {
        Mountable = 0x00000001, // Mountable
        NoXp = 0x00000002, // No Xp | CreatureFlagExtraNoXp
        NoLoot = 0x00000004, // No Loot
        Unkillable = 0x00000008, // Unkillable
        Tameable = 0x00000010, // Tameable | CreatureTypeFlagTameable
        ImmunePc = 0x00000020, // Immunepc | UnitFlagImmuneToPc
        ImmuneNpc = 0x00000040, // Immunenpc | UnitFlagImmuneToNpc
        CanWieldLoot = 0x00000080, // Canwieldloot
        Sessile = 0x00000100, // Sessile | Rooted Movementflag, Creature Is Permanently Rooted In Place
        Uninteractible = 0x00000200, // Uninteractible | UnitFlagUninteractible
        NoAutomaticRegen = 0x00000400, // No Automatic Regen | Creatures With That Flag Uses No UnitFlag2RegeneratePower
        DespawnInstantly = 0x00000800, // Despawn Instantly | Creature Instantly Disappear When Killed
        CorpseRaid = 0x00001000, // Corpseraid
        CreatorLoot = 0x00002000, // Creator Loot | Lootable Only By Creator (Engineering Dummies)
        NoDefense = 0x00004000, // No Defense
        NoSpellDefense = 0x00008000, // No Spell Defense
        RaidBossMob = 0x00010000, // Raid Boss Mob | CreatureTypeFlagBossMob
        CombatPing = 0x00020000, // Combat Ping
        Aquatic = 0x00040000, // Aquatic (Aka Water Only)
        Amphibious = 0x00080000, // Amphibious | Creatures Will Be Able To Enter And Leave Water But Can Only Move On The Ocean Floor When CanSwim Is Not Present
        NoMeleeFlee = 0x00100000, // No Melee (Flee) | Prevents Melee (Moves As-If Feared, Does Not Make Creature Passive)
        VisibleToGhosts = 0x00200000, // Visible To Ghosts | CreatureTypeFlagVisibleToGhosts
        PvpEnabling = 0x00400000, // Pvp Enabling | Old UnitFlagPvpEnabling, Now UnitBytes2OffsetPvpFlag From UnitFieldBytes2
        DoNotPlayWoundAnim = 0x00800000, // Do Not Play Wound Anim | CreatureTypeFlagDoNotPlayWoundAnim
        NoFactionTooltip = 0x01000000, // No Faction Tooltip | CreatureTypeFlagNoFactionTooltip
        IgnoreCombat = 0x02000000, // Ignore Combat | Actually Only Changes React State To Passive
        OnlyAttackTargetsThatArePvpEnabling = 0x04000000, // Only Attack Targets That Are Pvp Enabling
        CallsGuards = 0x08000000, // Calls Guards | Creature Will Summon A Guard If Player Is Within Its Aggro Range (Even If Creature Doesn't Attack Per Se)
        CanSwim = 0x10000000, // Can Swim | Unitflags 0x8000 UnitFlagCanSwim
        Floating = 0x20000000, // Floating <Don't Use> | Sets Disablegravity Movementflag On Spawn/Reset
        MoreAudible = 0x40000000, // More Audible: Caution, Expensive | CreatureTypeFlagMoreAudible
        LargeAoi = 0x80000000  // Large (Aoi): Caution, Expensive | Unitflags2 0x200000
    }

    [Flags]
    public enum CreatureStaticFlags2 : uint
    {
        NoPetScaling = 0x00000001, // No Pet Scaling
        ForceRaidCombat = 0x00000002, // Force Raid Combat
        LockTappersToRaidOnDeath = 0x00000004, // Lock Tappers To Raid On Death | Toggleable By 'Set "RaidLockOnDeath" Flag For Unit(S)' Action, CreatureFlagExtraInstanceBind
        NoHarmfulVertexColoring = 0x00000008, // No Harmful Vertex Coloring
        NoCrushingBlows = 0x00000010, // No Crushing Blows | CreatureFlagExtraNoCrushingBlows
        NoOwnerThreat = 0x00000020, // No Owner Threat
        NoWoundedSlowdown = 0x00000040, // No Wounded Slowdown
        UseCreatorBonuses = 0x00000080, // Use Creator Bonuses
        IgnoreFeignDeath = 0x00000100, // Ignore Feign Death | CreatureFlagExtraIgnoreFeignDeath
        IgnoreSanctuary = 0x00000200, // Ignore Sanctuary | Ignores SpellEffectSanctuary
        ActionTriggersWhileCharmed = 0x00000400, // Action Triggers While Charmed
        InteractWhileDead = 0x00000800, // Interact While Dead | CreatureTypeFlagInteractWhileDead
        NoInterruptSchoolCooldown = 0x00001000, // No Interrupt School Cooldown
        ReturnSoulShardToMasterOfPet = 0x00002000, // Return Soul Shard To Master Of Pet
        SkinWithHerbalism = 0x00004000, // Skin With Herbalism | CreatureTypeFlagSkinWithHerbalism
        SkinWithMining = 0x00008000, // Skin With Mining | CreatureTypeFlagSkinWithMining
        AlertContentTeamOnDeath = 0x00010000, // Alert Content Team On Death
        AlertContentTeamAt90PctHealth = 0x00020000, // Alert Content Team At 90% Health
        AllowMountedCombat = 0x00040000, // Allow Mounted Combat | CreatureTypeFlagAllowMountedCombat
        PvpEnablingOoc = 0x00080000, // Pvp Enabling Ooc
        NoDeathMessage = 0x00100000, // No Death Message | CreatureTypeFlagNoDeathMessage
        IgnorePathingFailure = 0x00200000, // Ignore Pathing Failure
        FullSpellList = 0x00400000, // Full Spell List
        DoesntReduceReputationForRaids = 0x00800000, // Doesn't Reduce Reputation For Raids
        IgnoreMisdirection = 0x01000000, // Ignore Misdirection
        HideBody = 0x02000000, // Hide Body | UnitFlag2HideBody
        SpawnDefensive = 0x04000000, // Spawn Defensive
        ServerOnly = 0x08000000, // Server Only
        NoCollision = 0x10000000, // No Collision
        PlayerCanHealOrBuff = 0x20000000, // Player Can Heal/Buff | CreatureTypeFlagCanAssist
        NoSkillGains = 0x40000000, // No Skill Gains | CreatureFlagExtraNoSkillGains
        NoPetBar = 0x80000000  // No Pet Bar | CreatureTypeFlagNoPetBar
    }

    [Flags]
    public enum CreatureStaticFlags3 : uint
    {
        NoDamageHistory = 0x00000001, // No Damage History
        DontPvpEnableOwner = 0x00000002, // Don't Pvp Enable Owner
        DontFadeIn = 0x00000004, // Don't Fade In | UnitFlag2DoNotFadeIn
        NonUniqueInCombatLog = 0x00000008, // Non-Unique In Combat Log
        SkinWithEngineering = 0x00000010, // Skin With Engineering | CreatureTypeFlagSkinWithEngineering
        NoAggroOnLeash = 0x00000020, // No Aggro On Leash
        NoFriendlyAreaAuras = 0x00000040, // No Friendly Area Auras
        ExtendedCorpseDuration = 0x00000080, // Extended Corpse Duration
        CantSwim = 0x00000100, // Can't Swim | UnitFlagCannotSwim
        TameableExotic = 0x00000200, // Tameable (Exotic) | CreatureTypeFlagTameableExotic
        GiganticAoi = 0x00000400, // Gigantic (Aoi): Caution, Expensive | Unitflags2 0x400000
        InfiniteAoi = 0x00000800, // Infinite (Aoi): Caution, Expensive | Unitflags2 0x40000000
        CannotPenetrateWater = 0x00001000, // Cannot Penetrate Water | Waterwalking
        NoNamePlate = 0x00002000, // No Name Plate | CreatureTypeFlagNoNamePlate
        ChecksLiquids = 0x00004000, // Checks Liquids: Caution, Expensive
        NoThreatFeedback = 0x00008000, // No Threat Feedback
        UseModelCollisionSize = 0x00010000, // Use Model Collision Size (Talk To A Programmer First) | CreatureTypeFlagUseModelCollisionSize
        AttackerIgnoresFacing = 0x00020000, // Attacker Ignores Facing
        AllowInteractionWhileInCombat = 0x00040000, // Allow Interaction While In Combat | CreatureTypeFlagAllowInteractionWhileInCombat
        SpellClickForPartyOnly = 0x00080000, // Spell Click For Party Only
        FactionLeader = 0x00100000, // Faction Leader
        ImmuneToPlayerBuffs = 0x00200000, // Immune To Player Buffs
        CollideWithMissiles = 0x00400000, // Collide With Missiles | CreatureTypeFlagCollideWithMissiles
        DoNotTap = 0x00800000, // Do Not Tap (Credit To Threat List)
        DisableDodgeParryAndBlockAnimations = 0x01000000, // Disable Dodge, Parry And Block Animations | CreatureTypeFlagDoNotPlayMountedAnimations
        CannotTurn = 0x02000000, // Cannot Turn | UnitFlag2CannotTurn
        EnemyCheckIgnoresLineOfSight = 0x04000000, // Enemy Check Ignores Line Of Sight
        ForeverCorpseDuration = 0x08000000, // Forever Corpse Duration (7 Days)
        PetsAttackWith3dPathing = 0x10000000, // Pets Attack With 3d Pathing (Kologarn)
        LinkAllFlag = 0x20000000, // Linkall Flag (Talk To A Programmer First) | CreatureTypeFlagLinkAll
        AiCanAutoTakeoffInCombat = 0x40000000, // Ai Can Auto Takeoff In Combat
        AiCanAutoLandInCombat = 0x80000000  // Ai Can Auto Land In Combat
    }

    [Flags]
    public enum CreatureStaticFlags4 : uint
    {
        NoBirthAnim = 0x00000001, // No Birth Anim
        TreatAsPlayerForDiminishingReturns = 0x00000002, // Treat As Player For Diminishing Returns
        TreatAsPlayerForPvpDebuffDuration = 0x00000004, // Treat As Player For Pvp Debuff Duration
        OnlyDisplayGossipForSummoner = 0x00000008, // Only Display Gossip For Summoner | CreatureTypeFlagInteractOnlyWithCreator
        NoDeathScream = 0x00000010, // No Death Scream | CreatureTypeFlagDoNotPlayUnitEventSounds
        CanBeHealedByEnemies = 0x00000020, // Can Be Healed By Enemies | CreatureTypeFlagHasNoShadowBlob
        DealsTripleDamageToPcControlledPets = 0x00000040, // Deals Triple Damage To Pc Controlled Pets
        NoNpcDamageBelow85Pct = 0x00000080, // No Npc Damage Below 85%
        ObeysTauntDiminishingReturns = 0x00000100, // Obeys Taunt Diminishing Returns | CreatureFlagExtraObeysTauntDiminishingReturns
        NoMeleeApproach = 0x00000200, // No Melee (Approach) | Prevents Melee (Chases Into Melee Range, Does Not Make Creature Passive)
        UpdateCreatureRecordWhenInstanceChangesDifficulty = 0x00000400, // Update Creature Record When Instance Changes Difficulty
        CannotDaze = 0x00000800, // Cannot Daze (Combat Stun)
        FlatHonorAward = 0x00001000, // Flat Honor Award
        IgnoreLosWhenCastingOnMe = 0x00002000, // Other Objects Can Ignore Line Of Sight Requirements When Casting Spells On Me
        GiveQuestKillCreditWhileOffline = 0x00004000, // Give Quest Kill Credit While Offline
        TreatAsRaidUnitForHelpfulSpells = 0x00008000, // Treat As Raid Unit For Helpful Spells (Instances Only) | CreatureTypeFlagTreatAsRaidUnit
        DontRepositionBecauseMeleeTargetIsTooClose = 0x00010000, // Don't Reposition Because Melee Target Is Too Close
        PetOrGuardianAiDontGoBehindTarget = 0x00020000, // Pet/Guardian Ai Don't Go Behind Target
        Minute5LootRollTimer = 0x00040000, // 5 Minute Loot Roll Timer
        ForceGossip = 0x00080000, // Force Gossip | CreatureTypeFlagForceGossip
        DontRepositionWithFriendsInCombat = 0x00100000, // Don't Reposition With Friends In Combat
        ManualSheathingControl = 0x00200000, // Manual Sheathing Control | CreatureTypeFlagDoNotSheathe
        AttackerIgnoresMinimumRanges = 0x00400000, // Attacker Ignores Minimum Ranges | Unitflags2 0x8000000
        SuppressInstanceWideReleaseInCombat = 0x00800000, // Suppress Instance Wide Release In Combat
        AiWillOnlySwimIfTargetSwims = 0x01000000, // Ai Will Only Swim If Target Swims | Unitflags2 0x1000000
        DontGenerateCombatLogWhenEngagedWithNpcs = 0x02000000, // Don't Generate Combat Log When Engaged With Npc's | Unitflags2 0x2000000
        AllowNpcCombatWhileUninteractible = 0x04000000, // Allow Npc Combat While Uninteractible
        PreferNpcsWhenSearchingForEnemies = 0x08000000, // Prefer Npcs When Searching For Enemies
        OnlyGenerateInitialThreat = 0x10000000, // Only Generate Initial Threat
        DoesntChangeTargetOnRightClick = 0x20000000, // Doesn't Change Target On Right Click | CreatureTypeFlagDoNotTargetOnInteraction
        HideNameInWorldFrame = 0x40000000, // Hide Name In World Frame | CreatureTypeFlagDoNotRenderObjectName
        QuestBoss = 0x80000000  // Quest Boss | CreatureTypeFlagQuestBoss
    }

    [Flags]
    public enum CreatureStaticFlags5 : uint
    {
        UntargetableByClient = 0x00000001, // Untargetable By Client | Unitflags2 0x4000000 UnitFlag2UntargetableByClient
        ForceSelfMounting = 0x00000002, // Force Self Mounting
        UninteractibleIfHostile = 0x00000004, // Uninteractible If Hostile | Unitflags2 0x10000000 UnitFlag2UninteractibleIfHostile
        DisablesXpAward = 0x00000008, // Disables Xp Award
        DisableAiPrediction = 0x00000010, // Disable Ai Prediction
        NoLeaveCombatStateRestore = 0x00000020, // No Leavecombat State Restore
        BypassInteractInterrupts = 0x00000040, // Bypass Interact Interrupts
        Degree240BackArc = 0x00000080, // 240 Degree Back Arc
        InteractWhileHostile = 0x00000100, // Interact While Hostile | Unitflags2 0x4000 UnitFlag2InteractWhileHostile
        DontDismissOnFlyingMount = 0x00000200, // Don't Dismiss On Flying Mount
        PredictivePowerRegen = 0x00000400, // Predictive Power Regen | CreatureTypeFlag2PredictivePowerRegen
        HideLevelInfoInTooltip = 0x00000800, // Hide Level Info In Tooltip | CreatureTypeFlag2HideLevelInfoInTooltip
        HideHealthBarUnderTooltip = 0x00001000, // Hide Health Bar Under Tooltip | CreatureTypeFlag2HideHealthBarUnderTooltip
        SuppressHighlightWhenTargetedOrMousedOver = 0x00002000, // Suppress Highlight When Targeted Or Moused Over | Unitflags2 0x80000 UnitFlag2SuppressHighlightWhenTargetedOrMousedOver
        AiPreferPathableTargets = 0x00004000, // Ai Prefer Pathable Targets
        FrequentAreaTriggerChecks = 0x00008000, // Frequent Area Trigger Checks (Expensive/Talk To A Programmer First)
        AssignKillCreditToEncounterList = 0x00010000, // Assign Kill Credit To Encounter List
        NeverEvade = 0x00020000, // Never Evade
        AiCantPathOnSteepSlopes = 0x00040000, // Ai Can't Path On Steep Slopes
        AiIgnoreLosToMeleeTarget = 0x00080000, // Ai Ignore Los To Melee Target
        NeverDisplayEmoteOrChatTextInAChatBubble = 0x00100000, // Never Display Emote Or Chat Text In A Chat Bubble | CreatureTypeflags2Unk4
        AiPetsCloseInOnUnpathableTarget = 0x00200000, // Ai Pets Close In On Unpathable Target
        PetOrGuardianAiDontGoBehindMe = 0x00400000, // Pet/Guardian Ai Don't Go Behind Me (Use On Target)
        NoDeathThud = 0x00800000, // No Death Thud | CreatureTypeFlag2NoDeathThud
        ClientLocalCreature = 0x01000000, // Client Local Creature
        CanDropLootWhileInAChallengeModeInstance = 0x02000000, // Can Drop Loot While In A Challenge Mode Instance
        HasSafeLocation = 0x04000000, // Has Safe Location
        NoHealthRegen = 0x08000000, // No Health Regen
        NoPowerRegen = 0x10000000, // No Power Regen
        NoPetUnitFrame = 0x20000000, // No Pet Unit Frame
        NoInteractOnLeftClick = 0x40000000, // No Interact On Left Click | CreatureTypeFlag2NoInteractOnLeftClick
        GiveCriteriaKillCreditWhenCharmed = 0x80000000  // Give Criteria Kill Credit When Charmed
    }

    [Flags]
    public enum CreatureStaticFlags6 : uint
    {
        DoNotAutoResummonThisCompanionCreature = 0x00000001, // Do Not Auto-Resummon This Companion Creature
        SmoothPhasingReplaceVisibleUnitIfAvailable = 0x00000002, // Smooth Phasing: Replace Visible Unit If Available (Ask Programmer First)
        IgnoreTheRealmCoalescingHidingCode = 0x00000004, // Ignore The Realm Coalescing Hiding Code (Always Show)
        TapsToFaction = 0x00000008, // Taps To Faction
        OnlyQuestGiverForSummoner = 0x00000010, // Only Questgiver For Summoner
        AiCombatReturnPrecise = 0x00000020, // Ai Combat Return Precise
        HomeRealmOnlyLoot = 0x00000040, // Home Realm Only Loot
        NoInteractResponse = 0x00000080, // No Interact Response | Tflag2Unk7
        NoInitialPower = 0x00000100, // No Initial Power
        DontCancelChannelOnMasterMounting = 0x00000200, // Don't Cancel Channel On Master Mounting
        CanToggleBetweenDeathAndPersonalLoot = 0x00000400, // Can Toggle Between Death And Personal Loot
        AlwaysStandOnTarget = 0x00000800, // Always, Always Tries To Stand Right On Top Of His Move To Target. Always!!
        UnconsciousOnDeath = 0x00001000, // Unconscious On Death
        DontReportToLocalDefenseChannelOnDeath = 0x00002000, // Don't Report To Local Defense Channel On Death
        PreferUnengagedMonstersWhenPickingATarget = 0x00004000, // Prefer Unengaged Monsters When Picking A Target
        UsePvpPowerAndResilienceWhenPlayersAttackThisCreature = 0x00008000, // Use Pvp Power And Resilience When Players Attack This Creature
        DontClearDebuffsOnLeaveCombat = 0x00010000, // Don't Clear Debuffs On Leave Combat
        PersonalLootHasFullSecurity = 0x00020000, // Personal Loot Has Full Security (Guaranteed Push/Mail Delivery)
        TripleSpellVisuals = 0x00040000, // Triple Spell Visuals
        UseGarrisonOwnerLevel = 0x00080000, // Use Garrison Owner Level
        ImmediateAoiUpdateOnSpawn = 0x00100000, // Immediate Aoi Update On Spawn
        UiCanGetPosition = 0x00200000, // Ui Can Get Position
        SeamlessTransferProhibited = 0x00400000, // Seamless Transfer Prohibited
        AlwaysUseGroupLootMethod = 0x00800000, // Always Use Group Loot Method
        NoBossKillBanner = 0x01000000, // No Boss Kill Banner
        ForceTriggeringPlayerLootOnly = 0x02000000, // Force Triggering Player Loot Only
        ShowBossFrameWhileUninteractable = 0x04000000, // Show Boss Frame While Uninteractable
        ScalesToPlayerLevel = 0x08000000, // Scales To Player Level
        AiDontLeaveMeleeForRangedWhenTargetGetsRooted = 0x10000000, // Ai Don't Leave Melee For Ranged When Target Gets Rooted
        DontUseCombatReachForChaining = 0x20000000, // Don't Use Combat Reach For Chaining
        DoNotPlayProceduralWoundAnim = 0x40000000, // Do Not Play Procedural Wound Anim
        ApplyProceduralWoundAnimToBase = 0x80000000, // Apply Procedural Wound Anim To Base | Tflag2Unk14
    }

    [Flags]
    public enum CreatureStaticFlags7
    {
        ImportantNpc = 0x00000001, // Important Npc
        ImportantQuestNpc = 0x00000002, // Important Quest Npc
        LargeNameplate = 0x00000004, // Large Nameplate
        TrivialPet = 0x00000008, // Trivial Pet (Ignored By Helpful Aoes)
        AiEnemiesDontBackupWhenIGetRooted = 0x00000010, // Ai Enemies Don't Backup When I Get Rooted
        NoAutomaticCombatAnchor = 0x00000020, // No Automatic Combat Anchor
        OnlyTargetableByCreator = 0x00000040, // Only Targetable By Creator
        TreatAsPlayerForIsPlayerControlled = 0x00000080, // 8.0.1 Flag - Treat As Player For Isplayercontrolled()
        GenerateNoThreatOrDamage = 0x00000100, // 8.0.1 Flag - Generate No Threat Or Damage
        InteractOnlyOnQuest = 0x00000200, // 8.0.1 Flag - Interact Only On Quest
        DisableKillCreditForOfflinePlayers = 0x00000400, // Disable Kill Credit For Offline Players
        AiAdditionalPathing = 0x00080000, // Ai Additional Pathing
    }

    [Flags]
    public enum CreatureStaticFlags8
    {
        ForceCloseInOnPathFailBehavior = 0x00000002, // Force Close In On Path Fail Behavior
        Use2dChasingCalculation = 0x00000020, // Use 2d Chasing Calculation
        UseFastClassicHeartbeat = 0x00000040, // Use Fast Classic Heartbeat
    }

    [Flags]
    public enum CreatureFlagsExtra : uint
    {
        InstanceBind = 0x01,       // Creature Kill Bind Instance With Killer And Killer'S Group
        Civilian = 0x02,       // Not Aggro (Ignore Faction/Reputation Hostility)
        NoParry = 0x04,       // Creature Can'T Parry
        NoParryHasten = 0x08,       // Creature Can'T Counter-Attack At Parry
        NoBlock = 0x10,       // Creature Can'T Block
        NoCrushingBlows = 0x20,       // Creature Can'T Do Crush Attacks
        NoXP = 0x40,       // creature kill does not provide XP
        Trigger = 0x80,       // Trigger Creature
        NoTaunt = 0x100,       // Creature Is Immune To Taunt Auras And 'attack me' effects
        Unused9 = 0x200,
        GhostVisibility = 0x400,       // creature will only be visible to dead players
        UseOffhandAttack = 0x800, // creature will use offhand attacks
        NoSellVendor = 0x1000,       // players can't sell items to this vendor
        CannotEnterCombat = 0x2000,         // creature is not allowed to enter combat
        Worldevent = 0x4000,       // Custom Flag For World Event Creatures (Left Room For Merging)
        Guard = 0x8000,       // Creature Is Guard
        IgnoreFeighDeath = 0x10000, // creature ignores feign death
        NoCrit = 0x20000,       // Creature Can'T Do Critical Strikes
        NoSkillGains = 0x40000,       // creature won't increase weapon skills
        ObeysTauntDiminishingReturns = 0x80000,       // Taunt is subject to diminishing returns on this creature
        AllDiminish = 0x100000,       // creature is subject to all diminishing returns as players are
        NoPlayerDamageReq = 0x200000,       // creature does not need to take player damage for kill credit
        Unused22 = 0x400000,
        Unused23 = 0x800000,
        Unused24 = 0x1000000,
        Unused25 = 0x2000000,
        Unused26 = 0x4000000,
        Unused27 = 0x8000000,
        DungeonBoss = 0x10000000,        // Creature Is A Dungeon Boss
        IgnorePathfinding = 0x20000000,        // creature ignore pathfinding
        ImmunityKnockback = 0x40000000,        // creature is immune to knockback effects
        Unused31 = 0x80000000,

        // Masks
        AllUnused = (Unused22 | Unused23 | Unused24 | Unused25 | Unused26 | Unused27 | Unused31),

        DBAllowed = (0xFFFFFFFF & ~(AllUnused | DungeonBoss))
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

        MaskDemonOrUndead = (1 << (Demon - 1)) | (1 << (Undead - 1)),
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
        Ray = 34,
        Serpent = 35,
        Moth = 37,
        Chimaera = 38,
        Devilsaur = 39,
        Ghoul = 40,
        Aqiri = 41,
        Worm = 42,
        Clefthoof = 43,
        Wasp = 44,
        CoreHound = 45,
        SpiritBeast = 46,
        WaterElemental = 49,
        Fox = 50,
        Monkey = 51,
        Hound = 52,
        Beetle = 53,
        ShaleBeast = 55,
        Zombie = 56,
        QaTest = 57,
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
        Rodent = 127,
        StoneHound = 128,
        Gruffhorn = 129,
        Basilisk = 130,
        Direhorn = 138,
        Stormelemental = 145,
        Torrorguard = 147,
        Abyssal = 148,
        Riverbeast = 150,
        Stag = 151,
        Mechanical = 154,
        Abomination = 155,
        Scalehide = 156,
        Oxen = 157,
        Feathermane = 160,
        Lizard = 288,
        Pterrordax = 290,
        Toad = 291,
        Carapid = 292,
        BloodBeast = 296,
        Camel = 298,
        Courser = 299,
        Mammoth = 300,
        Incubus = 302,
        LesserDragonkin = 303
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

    public enum SelectTargetMethod
    {
        Random = 0,  // just pick a random target
        MaxThreat,   // prefer targets higher in the threat list
        MinThreat,   // prefer targets lower in the threat list
        MaxDistance, // prefer targets further from us
        MinDistance  // prefer targets closer to us
    }

    [Flags]
    public enum GroupAIFlags
    {
        None = 0,          // No creature group behavior
        MembersAssistLeader = 0x01, // The member aggroes if the leader aggroes
        LeaderAssistsMember = 0x02, // The leader aggroes if the member aggroes
        MembersAssistMember = (MembersAssistLeader | LeaderAssistsMember), // every member will assist if any member is attacked
        IdleInFormation = 0x200, // The member will follow the leader when pathing idly
    }

    public enum CreatureChaseMovementType
    {
        Run,
        CanWalk,
        AlwaysWalk,

        Max
    }

    public enum CreatureRandomMovementType
    {
        Walk,
        CanRun,
        AlwaysRun,

        Max
    }

    public enum VendorInventoryReason
    {
        None = 0,
        Empty = 1
    }

    public enum VendorDataTypeFlags
    {
        Generic = 0x01,
        Ammo = 0x02,
        Food = 0x04,
        Poison = 0x08,
        Reagent = 0x10,
        Petition = 0x20,
    }
}

// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Game.AI
{
    [StructLayout(LayoutKind.Explicit)]
	public struct SmartAction
	{
		[FieldOffset(0)] public SmartActions type;

		[FieldOffset(4)] public Talk talk;

		[FieldOffset(4)] public SimpleTalk simpleTalk;

		[FieldOffset(4)] public Faction faction;

		[FieldOffset(4)] public MorphOrMount morphOrMount;

		[FieldOffset(4)] public Sound sound;

		[FieldOffset(4)] public Emote emote;

		[FieldOffset(4)] public Quest quest;

		[FieldOffset(4)] public QuestOffer questOffer;

		[FieldOffset(4)] public React react;

		[FieldOffset(4)] public RandomEmote randomEmote;

		[FieldOffset(4)] public Cast cast;

		[FieldOffset(4)] public CrossCast crossCast;

		[FieldOffset(4)] public SummonCreature summonCreature;

		[FieldOffset(4)] public ThreatPCT threatPCT;

		[FieldOffset(4)] public Threat threat;

		[FieldOffset(4)] public CastCreatureOrGO castCreatureOrGO;

		[FieldOffset(4)] public AutoAttack autoAttack;

		[FieldOffset(4)] public CombatMove combatMove;

		[FieldOffset(4)] public SetEventPhase setEventPhase;

		[FieldOffset(4)] public IncEventPhase incEventPhase;

		[FieldOffset(4)] public CastedCreatureOrGO castedCreatureOrGO;

		[FieldOffset(4)] public RemoveAura removeAura;

		[FieldOffset(4)] public Follow follow;

		[FieldOffset(4)] public RandomPhase randomPhase;

		[FieldOffset(4)] public RandomPhaseRange randomPhaseRange;

		[FieldOffset(4)] public KilledMonster killedMonster;

		[FieldOffset(4)] public SetInstanceData setInstanceData;

		[FieldOffset(4)] public SetInstanceData64 setInstanceData64;

		[FieldOffset(4)] public UpdateTemplate updateTemplate;

		[FieldOffset(4)] public CallHelp callHelp;

		[FieldOffset(4)] public SetSheath setSheath;

		[FieldOffset(4)] public ForceDespawn forceDespawn;

		[FieldOffset(4)] public InvincHP invincHP;

		[FieldOffset(4)] public IngamePhaseId ingamePhaseId;

		[FieldOffset(4)] public IngamePhaseGroup ingamePhaseGroup;

		[FieldOffset(4)] public SetData setData;

		[FieldOffset(4)] public MoveRandom moveRandom;

		[FieldOffset(4)] public Visibility visibility;

		[FieldOffset(4)] public SummonGO summonGO;

		[FieldOffset(4)] public Active active;

		[FieldOffset(4)] public Taxi taxi;

		[FieldOffset(4)] public WpStart wpStart;

		[FieldOffset(4)] public WpPause wpPause;

		[FieldOffset(4)] public WpStop wpStop;

		[FieldOffset(4)] public Item item;

		[FieldOffset(4)] public SetRun setRun;

		[FieldOffset(4)] public SetDisableGravity setDisableGravity;

		[FieldOffset(4)] public Teleport teleport;

		[FieldOffset(4)] public SetCounter setCounter;

		[FieldOffset(4)] public StoreTargets storeTargets;

		[FieldOffset(4)] public TimeEvent timeEvent;

		[FieldOffset(4)] public Movie movie;

		[FieldOffset(4)] public Equip equip;

		[FieldOffset(4)] public Flag flag;

		[FieldOffset(4)] public SetunitByte setunitByte;

		[FieldOffset(4)] public DelunitByte delunitByte;

		[FieldOffset(4)] public TimedActionList timedActionList;

		[FieldOffset(4)] public RandTimedActionList randTimedActionList;

		[FieldOffset(4)] public RandRangeTimedActionList randRangeTimedActionList;

		[FieldOffset(4)] public InterruptSpellCasting interruptSpellCasting;

		[FieldOffset(4)] public Jump jump;

		[FieldOffset(4)] public FleeAssist fleeAssist;

		[FieldOffset(4)] public EnableTempGO enableTempGO;

		[FieldOffset(4)] public MoveToPos moveToPos;

		[FieldOffset(4)] public SendGossipMenu sendGossipMenu;

		[FieldOffset(4)] public SetGoLootState setGoLootState;

		[FieldOffset(4)] public SendTargetToTarget sendTargetToTarget;

		[FieldOffset(4)] public SetRangedMovement setRangedMovement;

		[FieldOffset(4)] public SetHealthRegen setHealthRegen;

		[FieldOffset(4)] public SetRoot setRoot;

		[FieldOffset(4)] public GoState goState;

		[FieldOffset(4)] public CreatureGroup creatureGroup;

		[FieldOffset(4)] public Power power;

		[FieldOffset(4)] public GameEventStop gameEventStop;

		[FieldOffset(4)] public GameEventStart gameEventStart;

		[FieldOffset(4)] public ClosestWaypointFromList closestWaypointFromList;

		[FieldOffset(4)] public MoveOffset moveOffset;

		[FieldOffset(4)] public RandomSound randomSound;

		[FieldOffset(4)] public CorpseDelay corpseDelay;

		[FieldOffset(4)] public DisableEvade disableEvade;

		[FieldOffset(4)] public GroupSpawn groupSpawn;

		[FieldOffset(4)] public AuraType auraType;

		[FieldOffset(4)] public LoadEquipment loadEquipment;

		[FieldOffset(4)] public RandomTimedEvent randomTimedEvent;

		[FieldOffset(4)] public PauseMovement pauseMovement;

		[FieldOffset(4)] public RespawnData respawnData;

		[FieldOffset(4)] public AnimKit animKit;

		[FieldOffset(4)] public Scene scene;

		[FieldOffset(4)] public Cinematic cinematic;

		[FieldOffset(4)] public MovementSpeed movementSpeed;

		[FieldOffset(4)] public SpellVisualKit spellVisualKit;

		[FieldOffset(4)] public OverrideLight overrideLight;

		[FieldOffset(4)] public OverrideWeather overrideWeather;

		[FieldOffset(4)] public SetHover setHover;

		[FieldOffset(4)] public Evade evade;

		[FieldOffset(4)] public SetHealthPct setHealthPct;

		[FieldOffset(4)] public Conversation conversation;

		[FieldOffset(4)] public SetImmunePC setImmunePC;

		[FieldOffset(4)] public SetImmuneNPC setImmuneNPC;

		[FieldOffset(4)] public SetUninteractible setUninteractible;

		[FieldOffset(4)] public ActivateGameObject activateGameObject;

		[FieldOffset(4)] public AddToStoredTargets addToStoredTargets;

		[FieldOffset(4)] public BecomePersonalClone becomePersonalClone;

		[FieldOffset(4)] public TriggerGameEvent triggerGameEvent;

		[FieldOffset(4)] public DoAction doAction;

		[FieldOffset(4)] public Raw raw;

		#region Stucts

		public struct Talk
		{
			public uint textGroupId;
			public uint duration;
			public uint useTalkTarget;
		}

		public struct SimpleTalk
		{
			public uint textGroupId;
			public uint duration;
		}

		public struct Faction
		{
			public uint factionId;
		}

		public struct MorphOrMount
		{
			public uint creature;
			public uint model;
		}

		public struct Sound
		{
			public uint soundId;
			public uint onlySelf;
			public uint distance;
			public uint keyBroadcastTextId;
		}

		public struct Emote
		{
			public uint emoteId;
		}

		public struct Quest
		{
			public uint questId;
		}

		public struct QuestOffer
		{
			public uint questId;
			public uint directAdd;
		}

		public struct React
		{
			public uint state;
		}

		public struct RandomEmote
		{
			public uint emote1;
			public uint emote2;
			public uint emote3;
			public uint emote4;
			public uint emote5;
			public uint emote6;
		}

		public struct Cast
		{
			public uint spell;
			public uint castFlags;
			public uint triggerFlags;
			public uint targetsLimit;
		}

		public struct CrossCast
		{
			public uint spell;
			public uint castFlags;
			public uint targetType;
			public uint targetParam1;
			public uint targetParam2;
			public uint targetParam3;
		}

		public struct SummonCreature
		{
			public uint creature;
			public uint type;
			public uint duration;
			public uint storageID;
			public uint attackInvoker;
			public uint flags; // SmartActionSummonCreatureFlags
			public uint count;
		}

		public struct ThreatPCT
		{
			public uint threatINC;
			public uint threatDEC;
		}

		public struct CastCreatureOrGO
		{
			public uint quest;
			public uint spell;
		}

		public struct Threat
		{
			public uint threatINC;
			public uint threatDEC;
		}

		public struct AutoAttack
		{
			public uint attack;
		}

		public struct CombatMove
		{
			public uint move;
		}

		public struct SetEventPhase
		{
			public uint phase;
		}

		public struct IncEventPhase
		{
			public uint inc;
			public uint dec;
		}

		public struct CastedCreatureOrGO
		{
			public uint creature;
			public uint spell;
		}

		public struct RemoveAura
		{
			public uint spell;
			public uint charges;
			public uint onlyOwnedAuras;
		}

		public struct Follow
		{
			public uint dist;
			public uint angle;
			public uint entry;
			public uint credit;
			public uint creditType;
		}

		public struct RandomPhase
		{
			public uint phase1;
			public uint phase2;
			public uint phase3;
			public uint phase4;
			public uint phase5;
			public uint phase6;
		}

		public struct RandomPhaseRange
		{
			public uint phaseMin;
			public uint phaseMax;
		}

		public struct KilledMonster
		{
			public uint creature;
		}

		public struct SetInstanceData
		{
			public uint field;
			public uint data;
			public uint type;
		}

		public struct SetInstanceData64
		{
			public uint field;
		}

		public struct UpdateTemplate
		{
			public uint creature;
			public uint updateLevel;
		}

		public struct CallHelp
		{
			public uint range;
			public uint withEmote;
		}

		public struct SetSheath
		{
			public uint sheath;
		}

		public struct ForceDespawn
		{
			public uint delay;
			public uint forceRespawnTimer;
		}

		public struct InvincHP
		{
			public uint minHP;
			public uint percent;
		}

		public struct IngamePhaseId
		{
			public uint id;
			public uint apply;
		}

		public struct IngamePhaseGroup
		{
			public uint groupId;
			public uint apply;
		}

		public struct SetData
		{
			public uint field;
			public uint data;
		}

		public struct MoveRandom
		{
			public uint distance;
		}

		public struct Visibility
		{
			public uint state;
		}

		public struct SummonGO
		{
			public uint entry;
			public uint despawnTime;
			public uint summonType;
		}

		public struct Active
		{
			public uint state;
		}

		public struct Taxi
		{
			public uint id;
		}

		public struct WpStart
		{
			public uint run;
			public uint pathID;
			public uint repeat;
			public uint quest;

			public uint despawnTime;
			//public uint _reactState; DO NOT REUSE
		}

		public struct WpPause
		{
			public uint delay;
		}

		public struct WpStop
		{
			public uint despawnTime;
			public uint quest;
			public uint fail;
		}

		public struct Item
		{
			public uint entry;
			public uint count;
		}

		public struct SetRun
		{
			public uint run;
		}

		public struct SetDisableGravity
		{
			public uint disable;
		}

		public struct Teleport
		{
			public uint mapID;
		}

		public struct SetCounter
		{
			public uint counterId;
			public uint value;
			public uint reset;
		}

		public struct StoreTargets
		{
			public uint id;
		}

		public struct TimeEvent
		{
			public uint id;
			public uint min;
			public uint max;
			public uint repeatMin;
			public uint repeatMax;
			public uint chance;
		}

		public struct Movie
		{
			public uint entry;
		}

		public struct Equip
		{
			public uint entry;
			public uint mask;
			public uint slot1;
			public uint slot2;
			public uint slot3;
		}

		public struct Flag
		{
			public uint flag;
		}

		public struct SetunitByte
		{
			public uint byte1;
			public uint type;
		}

		public struct DelunitByte
		{
			public uint byte1;
			public uint type;
		}

		public struct TimedActionList
		{
			public uint id;
			public uint timerType;
			public uint allowOverride;
		}

		public struct RandTimedActionList
		{
			public uint actionList1;
			public uint actionList2;
			public uint actionList3;
			public uint actionList4;
			public uint actionList5;
			public uint actionList6;
		}

		public struct RandRangeTimedActionList
		{
			public uint idMin;
			public uint idMax;
		}

		public struct InterruptSpellCasting
		{
			public uint withDelayed;
			public uint spell_id;
			public uint withInstant;
		}

		public struct Jump
		{
			public uint SpeedXY;
			public uint SpeedZ;
			public uint Gravity;
			public uint UseDefaultGravity;
			public uint PointId;
			public uint ContactDistance;
		}

		public struct FleeAssist
		{
			public uint withEmote;
		}

		public struct EnableTempGO
		{
			public uint duration;
		}

		public struct MoveToPos
		{
			public uint pointId;
			public uint transport;
			public uint disablePathfinding;
			public uint contactDistance;
		}

		public struct SendGossipMenu
		{
			public uint gossipMenuId;
			public uint gossipNpcTextId;
		}

		public struct SetGoLootState
		{
			public uint state;
		}

		public struct SendTargetToTarget
		{
			public uint id;
		}

		public struct SetRangedMovement
		{
			public uint distance;
			public uint angle;
		}

		public struct SetHealthRegen
		{
			public uint regenHealth;
		}

		public struct SetRoot
		{
			public uint root;
		}

		public struct GoState
		{
			public uint state;
		}

		public struct CreatureGroup
		{
			public uint group;
			public uint attackInvoker;
		}

		public struct Power
		{
			public uint powerType;
			public uint newPower;
		}

		public struct GameEventStop
		{
			public uint id;
		}

		public struct GameEventStart
		{
			public uint id;
		}

		public struct ClosestWaypointFromList
		{
			public uint wp1;
			public uint wp2;
			public uint wp3;
			public uint wp4;
			public uint wp5;
			public uint wp6;
		}

		public struct MoveOffset
		{
			public uint PointId;
		}

		public struct RandomSound
		{
			public uint sound1;
			public uint sound2;
			public uint sound3;
			public uint sound4;
			public uint onlySelf;
			public uint distance;
		}

		public struct CorpseDelay
		{
			public uint timer;
			public uint includeDecayRatio;
		}

		public struct DisableEvade
		{
			public uint disable;
		}

		public struct GroupSpawn
		{
			public uint groupId;
			public uint minDelay;
			public uint maxDelay;
			public uint spawnflags;
		}

		public struct LoadEquipment
		{
			public uint id;
			public uint force;
		}

		public struct RandomTimedEvent
		{
			public uint minId;
			public uint maxId;
		}

		public struct PauseMovement
		{
			public uint movementSlot;
			public uint pauseTimer;
			public uint force;
		}

		public struct RespawnData
		{
			public uint spawnType;
			public uint spawnId;
		}

		public struct AnimKit
		{
			public uint animKit;
			public uint type;
		}

		public struct Scene
		{
			public uint sceneId;
		}

		public struct Cinematic
		{
			public uint entry;
		}

		public struct MovementSpeed
		{
			public uint movementType;
			public uint speedInteger;
			public uint speedFraction;
		}

		public struct SpellVisualKit
		{
			public uint spellVisualKitId;
			public uint kitType;
			public uint duration;
		}

		public struct OverrideLight
		{
			public uint zoneId;
			public uint areaLightId;
			public uint overrideLightId;
			public uint transitionMilliseconds;
		}

		public struct OverrideWeather
		{
			public uint zoneId;
			public uint weatherId;
			public uint intensity;
		}

		public struct SetHover
		{
			public uint enable;
		}

		public struct Evade
		{
			public uint toRespawnPosition;
		}

		public struct SetHealthPct
		{
			public uint percent;
		}

		public struct Conversation
		{
			public uint id;
		}

		public struct SetImmunePC
		{
			public uint immunePC;
		}

		public struct SetImmuneNPC
		{
			public uint immuneNPC;
		}

		public struct SetUninteractible
		{
			public uint uninteractible;
		}

		public struct ActivateGameObject
		{
			public uint gameObjectAction;
			public uint param;
		}

		public struct AddToStoredTargets
		{
			public uint id;
		}

		public struct BecomePersonalClone
		{
			public uint type;
			public uint duration;
		}

		public struct TriggerGameEvent
		{
			public uint eventId;
			public uint useSaiTargetAsGameEventSource;
		}

		public struct DoAction
		{
			public uint actionId;
		}

		public struct Raw
		{
			public uint param1;
			public uint param2;
			public uint param3;
			public uint param4;
			public uint param5;
			public uint param6;
			public uint param7;
		}

		#endregion
	}
}
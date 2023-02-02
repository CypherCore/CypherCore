// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scenarios;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    class ScenarioState : ServerPacket
    {
        public ScenarioState() : base(ServerOpcodes.ScenarioState, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(ScenarioID);
            _worldPacket.WriteInt32(CurrentStep);
            _worldPacket.WriteUInt32(DifficultyID);
            _worldPacket.WriteUInt32(WaveCurrent);
            _worldPacket.WriteUInt32(WaveMax);
            _worldPacket.WriteUInt32(TimerDuration);
            _worldPacket.WriteInt32(CriteriaProgress.Count);
            _worldPacket.WriteInt32(BonusObjectives.Count);
            _worldPacket.WriteInt32(PickedSteps.Count);
            _worldPacket.WriteInt32(Spells.Count);
            _worldPacket.WritePackedGuid(PlayerGUID);

            for (int i = 0; i < PickedSteps.Count; ++i)
                _worldPacket.WriteUInt32(PickedSteps[i]);

            _worldPacket.WriteBit(ScenarioComplete);
            _worldPacket.FlushBits();

            foreach (CriteriaProgressPkt progress in CriteriaProgress)
                progress.Write(_worldPacket);

            foreach (BonusObjectiveData bonusObjective in BonusObjectives)
                bonusObjective.Write(_worldPacket);

            foreach (ScenarioSpellUpdate spell in Spells)
                spell.Write(_worldPacket);
        }

        public int ScenarioID;
        public int CurrentStep = -1;
        public uint DifficultyID;
        public uint WaveCurrent;
        public uint WaveMax;
        public uint TimerDuration;
        public List<CriteriaProgressPkt> CriteriaProgress = new();
        public List<BonusObjectiveData> BonusObjectives = new();
        public List<uint> PickedSteps = new();
        public List<ScenarioSpellUpdate> Spells = new();
        public ObjectGuid PlayerGUID;
        public bool ScenarioComplete = false;
    }

    class ScenarioProgressUpdate : ServerPacket
    {
        public ScenarioProgressUpdate() : base(ServerOpcodes.ScenarioProgressUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            CriteriaProgress.Write(_worldPacket);
        }

        public CriteriaProgressPkt CriteriaProgress;
    }

    class ScenarioCompleted : ServerPacket
    {
        public ScenarioCompleted(uint scenarioId) : base(ServerOpcodes.ScenarioCompleted, ConnectionType.Instance)
        {
            ScenarioID = scenarioId;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(ScenarioID);
        }

        public uint ScenarioID;
    }

    class ScenarioVacate : ServerPacket
    {
        public ScenarioVacate() : base(ServerOpcodes.ScenarioVacate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(ScenarioID);
            _worldPacket.WriteInt32(Unk1);
            _worldPacket.WriteBits(Unk2, 2);
            _worldPacket.FlushBits();
        }

        public int ScenarioID;
        public int Unk1;
        public byte Unk2;
    }

    class QueryScenarioPOI : ClientPacket
    {
        public QueryScenarioPOI(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            var count = _worldPacket.ReadUInt32();
            for (var i = 0; i < count; ++i)
                MissingScenarioPOIs[i] = _worldPacket.ReadInt32();
        }

        public Array<int> MissingScenarioPOIs = new(50);
    }

    class ScenarioPOIs : ServerPacket
    {
        public ScenarioPOIs() : base(ServerOpcodes.ScenarioPois) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(ScenarioPOIDataStats.Count);

            foreach (ScenarioPOIData scenarioPOIData in ScenarioPOIDataStats)
            {
                _worldPacket.WriteInt32(scenarioPOIData.CriteriaTreeID);
                _worldPacket.WriteInt32(scenarioPOIData.ScenarioPOIs.Count);

                foreach (ScenarioPOI scenarioPOI in scenarioPOIData.ScenarioPOIs)
                {
                    _worldPacket.WriteInt32(scenarioPOI.BlobIndex);
                    _worldPacket.WriteInt32(scenarioPOI.MapID);
                    _worldPacket.WriteInt32(scenarioPOI.UiMapID);
                    _worldPacket.WriteInt32(scenarioPOI.Priority);
                    _worldPacket.WriteInt32(scenarioPOI.Flags);
                    _worldPacket.WriteInt32(scenarioPOI.WorldEffectID);
                    _worldPacket.WriteInt32(scenarioPOI.PlayerConditionID);
                    _worldPacket.WriteInt32(scenarioPOI.NavigationPlayerConditionID);
                    _worldPacket.WriteInt32(scenarioPOI.Points.Count);

                    foreach (var scenarioPOIBlobPoint in scenarioPOI.Points)
                    {
                        _worldPacket.WriteInt32((int)scenarioPOIBlobPoint.X);
                        _worldPacket.WriteInt32((int)scenarioPOIBlobPoint.Y);
                        _worldPacket.WriteInt32((int)scenarioPOIBlobPoint.Z);
                    }
                }
            }
        }

        public List<ScenarioPOIData> ScenarioPOIDataStats = new();
    }

    struct BonusObjectiveData
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(BonusObjectiveID);
            data.WriteBit(ObjectiveComplete);
            data.FlushBits();
        }

        public int BonusObjectiveID;
        public bool ObjectiveComplete;
    }

    class ScenarioSpellUpdate
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(SpellID);
            data.WriteBit(Usable);
            data.FlushBits();
        }

        public uint SpellID;
        public bool Usable = true;
    }

    struct ScenarioPOIData
    {
        public int CriteriaTreeID;
        public List<ScenarioPOI> ScenarioPOIs;
    }
}

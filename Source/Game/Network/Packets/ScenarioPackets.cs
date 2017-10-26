/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using Game.Scenarios;
using System.Collections.Generic;

namespace Game.Network.Packets
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
            _worldPacket.WriteUInt32(CriteriaProgress.Count);
            _worldPacket.WriteUInt32(BonusObjectives.Count);
            _worldPacket.WriteUInt32(PickedSteps.Count);
            _worldPacket.WriteUInt32(Spells.Count);

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

        public int ScenarioID { get; set; }
        public int CurrentStep { get; set; } = -1;
        public uint DifficultyID { get; set; }
        public uint WaveCurrent { get; set; }
        public uint WaveMax { get; set; }
        public uint TimerDuration { get; set; }
        public List<CriteriaProgressPkt> CriteriaProgress { get; set; } = new List<CriteriaProgressPkt>();
        public List<BonusObjectiveData> BonusObjectives { get; set; } = new List<BonusObjectiveData>();
        public List<uint> PickedSteps { get; set; } = new List<uint>();
        public List<ScenarioSpellUpdate> Spells { get; set; } = new List<ScenarioSpellUpdate>();
        public bool ScenarioComplete { get; set; } = false;
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

        public uint ScenarioID { get; set; }
    }

    class ScenarioBoot : ServerPacket
    {
        public ScenarioBoot() : base(ServerOpcodes.ScenarioBoot, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(ScenarioID);
            _worldPacket.WriteInt32(Unk1);
            _worldPacket.WriteBits(Unk2, 2);
            _worldPacket.FlushBits();
        }

        public int ScenarioID { get; set; }
        public int Unk1 { get; set; }
        public byte Unk2 { get; set; }
    }

    class QueryScenarioPOI : ClientPacket
    {
        public QueryScenarioPOI(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            var count = _worldPacket.ReadUInt32();
            for (var i = 0; i < count; ++i)
                MissingScenarioPOIs.Add(_worldPacket.ReadInt32());
        }

        public Array<int> MissingScenarioPOIs { get; set; } = new Array<int>(35);
    }

    class ScenarioPOIs : ServerPacket
    {
        public ScenarioPOIs() : base(ServerOpcodes.ScenarioPois) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(ScenarioPOIDataStats.Count);

            foreach (ScenarioPOIData scenarioPOIData in ScenarioPOIDataStats)
            {
                _worldPacket.WriteInt32(scenarioPOIData.CriteriaTreeID);
                _worldPacket.WriteUInt32(scenarioPOIData.ScenarioPOIs.Count);

                foreach (ScenarioPOI scenarioPOI in scenarioPOIData.ScenarioPOIs)
                {
                    _worldPacket.WriteInt32(scenarioPOI.BlobIndex);
                    _worldPacket.WriteInt32(scenarioPOI.MapID);
                    _worldPacket.WriteInt32(scenarioPOI.WorldMapAreaID);
                    _worldPacket.WriteInt32(scenarioPOI.Floor);
                    _worldPacket.WriteInt32(scenarioPOI.Priority);
                    _worldPacket.WriteInt32(scenarioPOI.Flags);
                    _worldPacket.WriteInt32(scenarioPOI.WorldEffectID);
                    _worldPacket.WriteInt32(scenarioPOI.PlayerConditionID);
                    _worldPacket.WriteUInt32(scenarioPOI.Points.Count);

                    foreach (var scenarioPOIBlobPoint in scenarioPOI.Points)
                    {
                        _worldPacket.WriteInt32(scenarioPOIBlobPoint.X);
                        _worldPacket.WriteInt32(scenarioPOIBlobPoint.Y);
                    }
                }
            }
        }

        public List<ScenarioPOIData> ScenarioPOIDataStats { get; set; } = new List<ScenarioPOIData>();
    }

    struct BonusObjectiveData
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(BonusObjectiveID);
            data.WriteBit(ObjectiveComplete);
            data.FlushBits();
        }

        public int BonusObjectiveID { get; set; }
        public bool ObjectiveComplete { get; set; }
    }

    class ScenarioSpellUpdate
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(SpellID);
            data.WriteBit(Usable);
            data.FlushBits();
        }

        public uint SpellID { get; set; }
        public bool Usable { get; set; } = true;
    }

    struct ScenarioPOIData
    {
        public int CriteriaTreeID { get; set; }
        public List<ScenarioPOI> ScenarioPOIs { get; set; }
    }
}

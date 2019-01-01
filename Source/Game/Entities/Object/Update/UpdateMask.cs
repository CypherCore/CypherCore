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
using Framework.IO;
using System.Collections;
using System;

namespace Game.Entities
{
    public class UpdateMask
    {
        public UpdateMask(uint valuesCount = 0)
        {
            _fieldCount = valuesCount;
            _blockCount = (valuesCount + 32 - 1) / 32;

            _mask = new BitArray((int)valuesCount, false);
        }

        public void SetCount(int valuesCount)
        {
            _fieldCount = (uint)valuesCount;
            _blockCount = (uint)(valuesCount + 32 - 1) / 32;

            _mask = new BitArray((int)valuesCount, false);
        }

        public uint GetCount() { return _fieldCount; }

        public virtual void AppendToPacket(ByteBuffer data)
        {
            data.WriteUInt8(_blockCount);
            var maskArray = new byte[_blockCount << 2];

            _mask.CopyTo(maskArray, 0);
            data.WriteBytes(maskArray);
        }

        public bool GetBit(int index)
        {
            return _mask.Get(index);
        }

        public void SetBit(int index)
        {
            _mask.Set(index, true);
        }

        void UnsetBit(int index)
        {
            _mask.Set(index, false);
        }

        public void Clear()
        {
            _mask.SetAll(false);
        }

        protected uint _fieldCount;
        protected uint _blockCount;
        protected BitArray _mask;
    }

    public class DynamicUpdateMask : UpdateMask
    {
        public DynamicUpdateMask(uint valuesCount) : base(valuesCount) { }

        public void EncodeDynamicFieldChangeType(DynamicFieldChangeType changeType, UpdateType updateType)
        {
            _updateType = updateType;
            DynamicFieldChangeType = (uint)(_blockCount | ((uint)(changeType & Entities.DynamicFieldChangeType.ValueAndSizeChanged) * ((3 - (int)updateType /*this part evaluates to 0 if update type is not VALUES*/) / 3)));
        }

        public override void AppendToPacket(ByteBuffer data)
        {
            data.WriteUInt16(DynamicFieldChangeType);
            if (DynamicFieldChangeType.HasAnyFlag((uint)Entities.DynamicFieldChangeType.ValueAndSizeChanged) && _updateType == UpdateType.Values)
                data.WriteUInt32(_fieldCount);

            var maskArray = new byte[_blockCount << 2];

            _mask.CopyTo(maskArray, 0);
            data.WriteBytes(maskArray);
        }

        public uint DynamicFieldChangeType;
        public UpdateType _updateType;
    }

    public enum DynamicFieldChangeType
    {
        Unchanged = 0,
        ValueChanged = 0x7FFF,
        ValueAndSizeChanged = 0x8000
    }
}

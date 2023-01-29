// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Game.Entities;

namespace Game.Movement
{
    // Transforms coordinates from global to Transport offsets
    public class TransportPathTransform
    {
        private readonly Unit _owner;
        private readonly bool _transformForTransport;

        public TransportPathTransform(Unit owner, bool transformForTransport)
        {
            _owner = owner;
            _transformForTransport = transformForTransport;
        }

        public Vector3 Calc(Vector3 input)
        {
            float x = input.X;
            float y = input.Y;
            float z = input.Z;

            if (_transformForTransport)
            {
                ITransport transport = _owner.GetDirectTransport();

                if (transport != null)
                {
                    float unused = 0.0f; // need reference
                    transport.CalculatePassengerOffset(ref x, ref y, ref z, ref unused);
                }
            }

            return new Vector3(x, y, z);
        }
    }
}
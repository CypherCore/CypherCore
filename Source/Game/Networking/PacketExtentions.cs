using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Networking
{
    public static class PacketExtentions
    {
        public static bool has_value<T>(this T obj) where T : new() 
        {
            return (obj != null);
        }
    }
}

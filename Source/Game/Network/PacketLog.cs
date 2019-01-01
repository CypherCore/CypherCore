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

using Framework.Configuration;
using Framework.Constants;
using System;
using System.IO;
using System.Net;
using System.Text;

public class PacketLog
{
    static object syncObj = new object();
    static string FullPath;

    static PacketLog()
    {
        string logsDir = AppContext.BaseDirectory + ConfigMgr.GetDefaultValue("LogsDir", "");
        string logname = ConfigMgr.GetDefaultValue("PacketLogFile", "");
        if (!string.IsNullOrEmpty(logname))
        {
            FullPath = logsDir + @"\" + logname;
            using (var writer = new BinaryWriter(File.Open(FullPath, FileMode.Create)))
            {
                writer.Write(Encoding.ASCII.GetBytes("PKT"));
                writer.Write((ushort)769);
                writer.Write(Encoding.ASCII.GetBytes("T"));
                writer.Write(Global.WorldMgr.GetRealm().Build);
                writer.Write(Encoding.ASCII.GetBytes("enUS"));
                writer.Write(new byte[40]);//SessionKey
                writer.Write((uint)Time.UnixTime);
                writer.Write(Time.GetMSTime());
                writer.Write(0);
            }
        }
    }

    public static void Write(byte[] data, uint opcode, IPAddress address, uint port, ConnectionType connectionType, bool isClientPacket)
    {
        if (!CanLog())
            return;

        lock (syncObj)
        {
            using (var writer = new BinaryWriter(File.Open(FullPath, FileMode.Append), Encoding.ASCII))
            {
                writer.Write(isClientPacket ? 0x47534d43 : 0x47534d53);
                writer.Write((uint)connectionType);
                writer.Write(Time.GetMSTime());

                writer.Write(20);
                byte[] SocketIPBytes = new byte[16];
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    Buffer.BlockCopy(address.GetAddressBytes(), 0, SocketIPBytes, 0, 4);
                else
                    Buffer.BlockCopy(address.GetAddressBytes(), 0, SocketIPBytes, 0, 16);

                writer.Write(data.Length + 4);
                writer.Write(SocketIPBytes);
                writer.Write(port);
                writer.Write(opcode);
                writer.Write(data);
            }
        }
    }

    public static bool CanLog()
    {
        return !string.IsNullOrEmpty(FullPath);
    }
}

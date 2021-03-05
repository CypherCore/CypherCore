﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
    private static object syncObj = new object();
    private static string FullPath;

    static PacketLog()
    {
        var logsDir = AppContext.BaseDirectory + ConfigMgr.GetDefaultValue("LogsDir", "");
        var logname = ConfigMgr.GetDefaultValue("PacketLogFile", "");
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

    public static void Write(byte[] data, uint opcode, IPEndPoint endPoint, ConnectionType connectionType, bool isClientPacket)
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
                var SocketIPBytes = new byte[16];
                if (endPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    Buffer.BlockCopy(endPoint.Address.GetAddressBytes(), 0, SocketIPBytes, 0, 4);
                else
                    Buffer.BlockCopy(endPoint.Address.GetAddressBytes(), 0, SocketIPBytes, 0, 16);

                var size = data.Length;
                if (isClientPacket)
                    size -= 2;

                writer.Write(size + 4);
                writer.Write(SocketIPBytes);
                writer.Write(endPoint.Port);
                writer.Write(opcode);

                if (isClientPacket)
                    writer.Write(data, 2, size);
                else
                    writer.Write(data, 0, size);
            }
        }
    }

    public static bool CanLog()
    {
        return !string.IsNullOrEmpty(FullPath);
    }
}

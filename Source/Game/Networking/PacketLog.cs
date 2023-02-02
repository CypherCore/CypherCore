// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Configuration;
using Framework.Constants;
using System;
using System.IO;
using System.Net;
using System.Text;

public class PacketLog
{
    static object syncObj = new();
    static string FullPath;

    static PacketLog()
    {
        string logsDir = AppContext.BaseDirectory + ConfigMgr.GetDefaultValue("LogsDir", "");
        string logname = ConfigMgr.GetDefaultValue("PacketLogFile", "");
        if (!string.IsNullOrEmpty(logname))
        {
            FullPath = logsDir + @"\" + logname;
            using var writer = new BinaryWriter(File.Open(FullPath, FileMode.Create));
            writer.Write(Encoding.ASCII.GetBytes("PKT"));
            writer.Write((ushort)769);
            writer.Write(Encoding.ASCII.GetBytes("T"));
            writer.Write(Global.WorldMgr.GetRealm().Build);
            writer.Write(Encoding.ASCII.GetBytes("enUS"));
            writer.Write(new byte[40]);//SessionKey
            writer.Write((uint)GameTime.GetGameTime());
            writer.Write(Time.GetMSTime());
            writer.Write(0);
        }
    }

    public static void Write(byte[] data, uint opcode, IPEndPoint endPoint, ConnectionType connectionType, bool isClientPacket)
    {
        if (!CanLog())
            return;

        lock (syncObj)
        {
            using var writer = new BinaryWriter(File.Open(FullPath, FileMode.Append), Encoding.ASCII);
            writer.Write(isClientPacket ? 0x47534d43 : 0x47534d53);
            writer.Write((uint)connectionType);
            writer.Write(Time.GetMSTime());

            writer.Write(20);
            byte[] SocketIPBytes = new byte[16];
            if (endPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                Buffer.BlockCopy(endPoint.Address.GetAddressBytes(), 0, SocketIPBytes, 0, 4);
            else
                Buffer.BlockCopy(endPoint.Address.GetAddressBytes(), 0, SocketIPBytes, 0, 16);

            int size = data.Length;
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

    public static bool CanLog()
    {
        return !string.IsNullOrEmpty(FullPath);
    }
}

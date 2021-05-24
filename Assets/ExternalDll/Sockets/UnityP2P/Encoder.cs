
using UnityEngine.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace UnityP2P
{
    public static class Encoder
    {
        public static byte[] GetObjectBytes(object obj)
        {
            byte[] toReturn;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, obj);
                toReturn = ms.ToArray();
            }
            return toReturn;
        }

        public static ClientPacket GetClientPacket(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                object toReturn = bf.Deserialize(ms);
                return (ClientPacket)toReturn;
            }
        }

        public static ServerPacket GetServerPacket(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                object toReturn = bf.Deserialize(ms);
                return (ServerPacket)toReturn;
            }
        }
    }
}
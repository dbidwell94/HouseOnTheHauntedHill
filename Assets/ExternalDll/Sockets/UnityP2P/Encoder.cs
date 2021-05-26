
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

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

        public static void GetClientPacket(byte[] data, out ClientPacket cp)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                try
                {
                    object toReturn = bf.Deserialize(ms);
                    cp = (ClientPacket)toReturn;
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    cp = null;
                }
            }
        }

        public static void GetServerPacket(byte[] data, out ServerPacket sp)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                try
                {
                    object toReturn = bf.Deserialize(ms);
                    sp = (ServerPacket)toReturn;
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    sp = null;
                }
            }
        }
    }
}
using System;
using UnityEngine;


namespace UnityP2P
{
    [Serializable]
    public class ClientPacket
    {
        public PacketDataType dataType;

        public object data;

        public string clientId;

        public ClientPacket(PacketDataType type, object data, string id)
        {
            dataType = type;
            this.data = data;
            this.clientId = id;
        }
    }

    [Serializable]
    public class ServerPacket
    {
        public PacketDataType dataType;
        public object data;

        public ServerPacket(PacketDataType type, object data)
        {
            dataType = type;
            this.data = data;
        }
    }

    [Serializable]
    public enum PacketDataType
    {
        Transform,
        BeginConnection,
        EndConnection
    }

    [Serializable]
    public class TransformData
    {
        SerializableVector3 position;

        public TransformData(Vector3 pos, Quaternion rot)
        {
            position = new SerializableVector3(pos);
        }
    }

    [Serializable]
    public class SerializableVector3
    {
        float x, y, z;

        float magnitude;
        public SerializableVector3(Vector3 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
            this.magnitude = v.magnitude;
        }
    }
}
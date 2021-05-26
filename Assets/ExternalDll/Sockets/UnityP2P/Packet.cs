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
        InstanciateObject,
        Message
    }

    [Serializable]
    public class TransformData
    {
        public SerializableVector3 position;

        public SerializableQuaternion quaternion;

        public TransformData(Vector3 pos, Quaternion rot)
        {
            position = new SerializableVector3(pos);
            quaternion = new SerializableQuaternion(rot);
        }

        public TransformData(Vector3 pos)
        {
            position = new SerializableVector3(pos);
            quaternion = new SerializableQuaternion(Quaternion.identity);
        }
    }

    [Serializable]
    public class SerializableVector3
    {
        public float x, y, z;
        public SerializableVector3(Vector3 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        public static explicit operator Vector3(SerializableVector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
    }

    [Serializable]
    public class SerializableQuaternion
    {
        public float x, y, z, w;
        public SerializableQuaternion(Quaternion q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public static explicit operator Quaternion(SerializableQuaternion q)
        {
            return new Quaternion(q.x, q.y, q.z, q.w);
        }
    }

    [Serializable]
    public class ObjectInstanciationRequest
    {
        public string objectId;
        public TransformData positionData;

        public ObjectInstanciationType objectType;

        public ObjectInstanciationRequest(string objectId, TransformData data, ObjectInstanciationType objType)
        {
            this.objectId = objectId;
            this.positionData = data;
            this.objectType = objType;
        }
    }

    [Serializable]
    public class ObjectMoveRequest
    {
        public string objectId;
        public TransformData transformData;

        public ObjectMoveRequest(string objectId, TransformData tData)
        {
            this.objectId = objectId;
            this.transformData = tData;
        }
    }

    [Serializable]
    public enum ObjectInstanciationType
    {
        Character,
        Room
    }
}
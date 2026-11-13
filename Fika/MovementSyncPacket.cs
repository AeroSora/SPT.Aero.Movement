using Fika.Core.Networking.LiteNetLib.Utils;
using UnityEngine;

namespace Aero.Movement.Fika
{
    public struct MovementSyncPacket : INetSerializable
    {
        public const byte TypeKick = 0;
        public const byte TypeDoor = 1;
        public const byte TypeVault = 2;

        public int NetId;
        public byte Type;
        public string DoorId;
        public Vector3 KickerPosition;
        public bool Reverse;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(Type);
            if (Type == TypeDoor)
            {
                writer.Put(DoorId ?? string.Empty);
                writer.Put(KickerPosition.x);
                writer.Put(KickerPosition.y);
                writer.Put(KickerPosition.z);
                writer.Put(Reverse);
            }
            else if (Type == TypeVault)
            {
                writer.Put(Reverse);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            Type = reader.GetByte();
            if (Type == TypeDoor)
            {
                DoorId = reader.GetString();
                KickerPosition = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
                Reverse = reader.GetBool();
            }
            else if (Type == TypeVault)
            {
                Reverse = reader.GetBool();
            }
        }
    }
}

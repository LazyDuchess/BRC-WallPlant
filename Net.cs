using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reptile;
using UnityEngine;
using SlopCrew.API;
using Microsoft.Extensions.DependencyInjection;

namespace WallPlant
{
    internal class NetDecal
    {
        public Characters Character
        {
            get
            {
                if (!UsingCustomCharacter)
                    return VanillaCharacter;
                if (!Plugin.IsCrewBoomInstalled())
                    return Characters.metalHead;
                return ReturnCrewBoomCharacter();

                Characters ReturnCrewBoomCharacter()
                {
                    if (!CrewBoom.CharacterDatabase.GetCharacterValueFromGuid(CrewBoomGUID, out var character))
                        return Characters.metalHead;
                    return character;
                }
            }
        }
        public bool UsingCustomCharacter = false;
        public Characters VanillaCharacter = Characters.metalHead;
        public Guid CrewBoomGUID;
        public Vector3 Point;
        public Vector3 Normal;
        public float Size = 1.5f;

        public NetDecal(Characters character, Vector3 point, Vector3 normal, float size)
        {
            UsingCustomCharacter = false;
            if (character > Characters.MAX)
            {
                VanillaCharacter = Characters.metalHead;
                if (Plugin.IsCrewBoomInstalled())
                    CrewBoomStuff();
            }
            Point = point;
            Normal = normal;
            Size = Mathf.Clamp(size, 0.1f, 15f);

            void CrewBoomStuff()
            {
                if (CrewBoomAPI.CrewBoomAPIDatabase.GetUserGuidForCharacter((int)character, out var guid))
                {
                    UsingCustomCharacter = true;
                    CrewBoomGUID = guid;
                }
            }
        }

        private NetDecal()
        {
        }

        public static NetDecal Read(BinaryReader reader)
        {
            var netDecal = new NetDecal();
            var result = netDecal.Deserialize(reader);
            if (result)
                return netDecal;
            return null;
        }

        public bool Deserialize(BinaryReader reader)
        {
            var version = reader.ReadByte();
            if (version > 0)
            {
                Debug.LogError("Got a wallplant decal from a future version, ignoring.");
                return false;
            }
            UsingCustomCharacter = !reader.ReadBoolean();
            if (!UsingCustomCharacter)
                VanillaCharacter = (Characters)reader.ReadInt32();
            else
            {
                CrewBoomGUID = Guid.Parse(reader.ReadString());
            }
            var affectedLayers = (LayerMask)reader.ReadInt32();
            Size = Mathf.Clamp(reader.ReadSingle(), 0.1f, 15f);
            Point = ReadVector3(reader);
            Normal = Vector3.Normalize(ReadVector3(reader));
            reader.Close();
            return true;
        }

        private static Vector3 ReadVector3(BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            return new Vector3(x, y, z);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)0);
            writer.Write(!UsingCustomCharacter);
            if (!UsingCustomCharacter)
                writer.Write((int)VanillaCharacter);
            else
                writer.Write(CrewBoomGUID.ToString());
            writer.Write((int)WallPlantAbility.WallPlantLayerMask);
            writer.Write(Size);
            WriteVector3(writer, Point);
            WriteVector3(writer, Normal);
        }

        private static void WriteVector3(BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }
    }
    internal static class Net
    {
        public static void Initialize()
        {
            if (!Plugin.IsSlopCrewInstalled())
                return;
            APIManager.API.OnCustomPacketReceived += OnDecalReceived;
            StageManager.OnStagePostInitialization += OnStagePostInitialization;
        }

        private static void OnStagePostInitialization()
        {
            Debug.Log($"SlopCrew connected postinit: {APIManager.API.Connected}");
            Debug.Log($"SlopCrew Playa Count: {APIManager.API.PlayerCount}");
        }

        public static void SendDecal(Characters character, Vector3 point, Vector3 normal, float size, LayerMask affectedLayers)
        {
            if (!Plugin.IsSlopCrewInstalled())
                return;
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            var decal = new NetDecal(character, point, normal, size);
            decal.Serialize(writer);
            writer.Flush();
            var data = ms.ToArray();
            APIManager.API.SendCustomPacket($"{PluginInfo.PLUGIN_GUID}-Decal", data);
            writer.Close();
        }

        public static void OnDecalReceived(uint player, string packetId, byte[] data)
        {
            if (packetId != $"{PluginInfo.PLUGIN_GUID}-Decal")
                return;
            var slopPlayas = SlopCrew.Plugin.Plugin.Host.Services.GetRequiredService<SlopCrew.Plugin.PlayerManager>();
            if (!slopPlayas.Players.TryGetValue(player, out var _))
                return;
            var ms = new MemoryStream(data);
            var reader = new BinaryReader(ms);
            var netDecal = NetDecal.Read(reader);
            if (netDecal == null)
            {
                reader.Close();
                return;
            }
            var decal = Decal.Create(netDecal.Point, netDecal.Normal, netDecal.Size, WallPlantAbility.WallPlantLayerMask);
            decal.SetTexture(GraffitiDatabase.GetGraffitiTexture(netDecal.Character));
            decal.AnimateSpray();
            reader.Close();
        }

        private static Vector3 ReadVector3(BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            return new Vector3(x, y, z);
        }
    }
}

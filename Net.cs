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
    internal static class Net
    {
        public static void Initialize()
        {
            if (!Plugin.IsSlopCrewInstalled())
                return;
            APIManager.API.OnCustomPacketReceived += OnDecalReceived;
        }

        public static void SendDecal(Characters character, Vector3 point, Vector3 normal, float size, LayerMask affectedLayers)
        {
            if (!Plugin.IsSlopCrewInstalled())
                return;
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            // version
            writer.Write((byte)0);
            var isBaseCharacter = character < Characters.MAX;
            if (!Plugin.IsCrewBoomInstalled())
                isBaseCharacter = true;
            writer.Write(isBaseCharacter);
            if (isBaseCharacter)
                writer.Write((int)character);
            else
            {
                if (!CrewBoomAPI.CrewBoomAPIDatabase.GetUserGuidForCharacter((int)character, out var guid))
                {
                    writer.Close();
                    return;
                }
                writer.Write(guid.ToString());
            }
            writer.Write((int)affectedLayers);
            writer.Write(size);
            WriteVector3(writer, point);
            WriteVector3(writer, normal);
            writer.Flush();
            var data = ms.ToArray();
            APIManager.API.SendCustomPacket($"{PluginInfo.PLUGIN_GUID}-Decal", data);
            writer.Close();
        }

        private static void WriteVector3(BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
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
            var version = reader.ReadByte();
            if (version > 0)
            {
                Debug.LogError("Got a wallplant decal from a future version, ignoring.");
                return;
            }
            var isBaseCharacter = reader.ReadBoolean();
            var character = Characters.metalHead;
            if (isBaseCharacter)
                character = (Characters)reader.ReadInt32();
            else
            {
                if (Plugin.IsCrewBoomInstalled())
                {
                    var characterGuid = reader.ReadString();
                    if (Guid.TryParse(characterGuid, out Guid guid))
                    {
                        if (!CrewBoom.CharacterDatabase.GetCharacterValueFromGuid(guid, out character))
                            character = Characters.metalHead;
                    }
                }
            }
            var affectedLayers = (LayerMask)reader.ReadInt32();
            var size = reader.ReadSingle();
            size = Mathf.Clamp(size, 0.1f, 10f);
            var point = ReadVector3(reader);
            var normal = Vector3.Normalize(ReadVector3(reader));
            var decal = Decal.Create(point, normal, size, affectedLayers);
            decal.SetTexture(GraffitiDatabase.GetGraffitiTexture(character));
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

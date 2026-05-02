using Reptile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BombRushMP.Plugin;

namespace WallPlant
{
    internal class NetDecalList
    {
        public int DecalLimit = 0;
        public List<NetDecal> Decals = new List<NetDecal>();

        public static NetDecalList MakeFromCurrentDecals()
        {
            var list = new NetDecalList();
            list.Decals = new List<NetDecal>(Net.CurrentDecals);
            list.DecalLimit = WallPlantSettings.MaxGraffiti;
            return list;
        }
        
        public static NetDecalList Read(BinaryReader reader)
        {
            var decalList = new NetDecalList();
            var result = decalList.Deserialize(reader);
            if (result)
                return decalList;
            return null;
        }

        public void Serialize(BinaryWriter writer)
        {
            //version
            writer.Write((byte)0);
            writer.Write(DecalLimit);
            writer.Write(Decals.Count);
            foreach(var decal in Decals)
            {
                decal.Serialize(writer);
            }
        }

        public bool Deserialize(BinaryReader reader)
        {
            var version = reader.ReadByte();
            if (version > 0)
            {
                Debug.Log("Got Decal list from the future, ignoring.");
                return false;
            }
            DecalLimit = reader.ReadInt32();
            var decalAmount = Mathf.Clamp(reader.ReadInt32(), 0, WallPlantSettings.MaxGraffiti);
            for(var i = 0; i < decalAmount; i++)
            {
                var decal = NetDecal.Read(reader);
                if (decal != null)
                    Decals.Add(decal);
                else
                {
                    return false;
                }
            }
            return true;
        }

        public void ApplyToWorld()
        {
            for(var i = Decals.Count - 1; i >= 0; i--)
            {
                var netDecal = Decals[i];
                Decal decal = Decal.Create(netDecal.Point, netDecal.Normal, netDecal.Size, WallPlantAbility.WallPlantLayerMask);
                Net.BindNetDecal(netDecal, decal);
                decal.SetTexture(GraffitiDatabase.GetGraffitiTexture(netDecal.Character));
                decal.SetCompleted();
            }
        }
    }
    internal class NetDecal
    {
        public Characters Character
        {
            get
            {
                if (!UsingCustomCharacter)
                    return VanillaCharacter;
                if (!Plugin.CrewBoomInstalled)
                    return Characters.metalHead;
                return ReturnCrewBoomCharacter();

                Characters ReturnCrewBoomCharacter()
                {
                    if (!CrewBoomAPI.CrewBoomAPIDatabase.IsInitialized)
                        return Characters.metalHead;
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
            VanillaCharacter = character;
            if (character > Characters.MAX)
            {
                VanillaCharacter = Characters.metalHead;
                if (Plugin.CrewBoomInstalled)
                    CrewBoomStuff();
            }
            Point = point;
            Normal = normal;
            Size = Mathf.Clamp(size, 0.1f, 15f);

            void CrewBoomStuff()
            {
                if (!CrewBoomAPI.CrewBoomAPIDatabase.IsInitialized)
                    return;
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
        public static List<NetDecal> CurrentDecals = new List<NetDecal>();
        public static List<NetDecalList> ReceivedDecalLists = new List<NetDecalList>();
        public static bool ReceivedDecalList = true;
        public static bool DecalRequestSent = false;
        public static float CurrentDecalListWaitTime = 0f;
        public static int DecalRequestCurrentAttempt = 1;
        public const int DecalRequestAttempts = 3;
        public const float DecalListWaitTime = 1f;
        public const float DecalListWaitTimeReceivedPackets = 1f;

        private const string PacketId = "LazyDuchess-WallPlant";
        private const byte DecalListId = 0;
        private const byte DecalListRequestId = 1;
        private const byte DecalId = 2;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Initialize()
        {
            if (!Plugin.AllCityNetworkInstalled)
                return;
            StageManager.OnStagePostInitialization += OnStagePostInitialization;
            ClientController.RegisterCustomPacketHandler(PacketId, PacketHandler);
        }

        private static void PacketHandler(ushort playerId, byte[] data)
        {
            if (ClientController.Instance.LocalID == playerId) return;
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(ms))
                {
                    var packetId = reader.ReadByte();
                    switch (packetId)
                    {
                        case DecalListId:
                            OnDecalListReceived(reader);
                            break;

                        case DecalListRequestId:
                            OnDecalListRequestReceived(playerId, reader);
                            break;

                        case DecalId:
                            OnDecalReceived(reader);
                            break;
                    }
                }
            }
        }

        public static void OnDecalListReceived(BinaryReader reader)
        {
            if (DecalManager.Instance == null)
                return;
            if (ReceivedDecalList)
                return;
            Log("Received a decal list packet.");
            var decalList = NetDecalList.Read(reader);
            if (decalList == null)
            {
                reader.Close();
                return;
            }
            Log($"Decal list contains {decalList.Decals.Count} decals.");
            if (decalList.DecalLimit >= WallPlantSettings.MaxGraffiti || (decalList.Decals.Count < decalList.DecalLimit))
            {
                Log($"Applying the decal list we just received! Contains {decalList.Decals.Count} decals.");
                ReceivedDecalList = true;
                decalList.ApplyToWorld();
                reader.Close();
                ReceivedDecalLists.Clear();
                return;
            }
            Log("Adding the decal list we just received to our candidates.");
            ReceivedDecalLists.Add(decalList);
        }

        public static void OnDecalListRequestReceived(ushort playerId, BinaryReader reader)
        {
            if (DecalManager.Instance == null)
                return;
            if (!ReceivedDecalList)
                return;
            Log($"Player {playerId} is requesting a decal list. Sending.");
            var currentDecalList = NetDecalList.MakeFromCurrentDecals();
            SendDecalList(currentDecalList);
        }

        public static void OnDecalReceived(BinaryReader reader)
        {
            if (WallPlantSettings.MaxGraffiti <= 0)
                return;
            if (DecalManager.Instance == null)
                return;
            Log($"Got a decal. Currently at {CurrentDecals.Count} decals.");
            var netDecal = NetDecal.Read(reader);
            if (netDecal == null)
            {
                reader.Close();
                return;
            }
            var decal = Decal.Create(netDecal.Point, netDecal.Normal, netDecal.Size, WallPlantAbility.WallPlantLayerMask);
            BindNetDecal(netDecal, decal);
            decal.SetTexture(GraffitiDatabase.GetGraffitiTexture(netDecal.Character));
            decal.AnimateSpray();
        }

        private static void OnStagePostInitialization()
        {
            CurrentDecals.Clear();
            ReceivedDecalLists.Clear();
            ReceivedDecalList = false;
            CurrentDecalListWaitTime = 0f;
            DecalRequestSent = false;
            DecalRequestCurrentAttempt = 1;
            Plugin.Instance.StopAllCoroutines();
            Plugin.Instance.StartCoroutine(OnJoinNewStage());
        }

        private static IEnumerator OnJoinNewStage()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            if (!ClientController.Instance.Connected)
                yield return null;
            Log($"Requesting decals for this stage. There are {ClientController.Instance.Players.Count} players including ourselves.");
            RequestDecalList();
            DecalRequestSent = true;
        }

        private static void Log(string message)
        {
            if (!WallPlantSettings.DebugNetworking)
                return;
            Debug.Log($"[WALLPLANT NETWORKING] {message}");
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Update()
        {
            if (ReceivedDecalList)
                return;
            if (!DecalRequestSent)
                return;
            CurrentDecalListWaitTime += Core.dt;
            var waitTime = DecalListWaitTime;
            if (ReceivedDecalLists.Count > 0)
                waitTime = DecalListWaitTimeReceivedPackets;
            if (CurrentDecalListWaitTime >= waitTime)
            {
                if (DecalRequestCurrentAttempt < DecalRequestAttempts && ClientController.Instance.Connected)
                {
                    Log("Wait for decal list expired. Trying again.");
                    CurrentDecalListWaitTime = 0f;
                    DecalRequestCurrentAttempt++;
                    RequestDecalList();
                    return;
                }
                Log($"Wait for decal list expired after {DecalRequestCurrentAttempt} attempts");
                ReceivedDecalList = true;
                NetDecalList largestDecalList = null;
                var largestDecalListAmount = 0;
                foreach(var decalList in ReceivedDecalLists)
                {
                    if (largestDecalList == null)
                    {
                        largestDecalList = decalList;
                        largestDecalListAmount = decalList.Decals.Count;
                    }
                    else
                    {
                        if (decalList.Decals.Count > largestDecalListAmount)
                        {
                            largestDecalList = decalList;
                            largestDecalListAmount = decalList.Decals.Count;
                        }
                    }
                }
                if (largestDecalList != null)
                {
                    Log($"Applying a decal list! Contains {largestDecalListAmount} decals.");
                    largestDecalList.ApplyToWorld();
                }
                Log("Didn't receive any decal list.");
                ReceivedDecalLists.Clear();
            }
        }

        public static void RequestDecalList()
        {
            if (!Plugin.AllCityNetworkInstalled)
                return;
            Log("Sending request decal list packet.");
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            writer.Write(DecalListRequestId);
            writer.Write(WallPlantSettings.MaxGraffiti);
            writer.Flush();
            var data = ms.ToArray();
            ClientController.Instance.BroadcastCustomPacket(data, PacketId);
            writer.Close();
        }

        public static void SendDecalList(NetDecalList decalList)
        {
            if (!Plugin.AllCityNetworkInstalled)
                return;
            Log("Sending a decal list packet");
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            writer.Write(DecalListId);
            decalList.Serialize(writer);
            writer.Flush();
            var data = ms.ToArray();
            ClientController.Instance.BroadcastCustomPacket(data, PacketId);
            writer.Close();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static NetDecal SendDecal(Characters character, Vector3 point, Vector3 normal, float size, LayerMask affectedLayers)
        {
            if (!Plugin.AllCityNetworkInstalled)
                return null;
            Log($"Sending a decal. Currently have {CurrentDecals.Count} decals.");
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            writer.Write(DecalId);
            var decal = new NetDecal(character, point, normal, size);
            decal.Serialize(writer);
            writer.Flush();
            var data = ms.ToArray();
            ClientController.Instance.BroadcastCustomPacket(data, PacketId);
            writer.Close();
            return decal;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void BindNetDecal(NetDecal netDecal, Decal decal)
        {
            CurrentDecals.Insert(0, netDecal);
            decal.OnDestroyCallback += () => CurrentDecals.Remove(netDecal);
        }
    }
}

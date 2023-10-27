using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using CommonAPI;
using Reptile;
using UnityEngine;
using CrewBoomAPI;

namespace WallPlant
{
    internal static class GraffitiDatabase
    {
        private static Dictionary<string, List<Texture2D>> CustomGraffiti = new Dictionary<string, List<Texture2D>>();
        public static void Initialize()
        {
            var graffitiDir = Path.Combine(Paths.ConfigPath, Plugin.Name, "Graffiti");

            if (!Directory.Exists(graffitiDir))
            {
                Directory.CreateDirectory(graffitiDir);
                Directory.CreateDirectory(Path.Combine(graffitiDir, "Global"));
                WriteReadme(Path.Combine(graffitiDir, "Readme.txt"));
            }

            var folders = Directory.GetDirectories(graffitiDir);

            foreach(var folder in folders)
            {
                var characterID = Path.GetFileName(folder).ToLowerInvariant();
                var images = Directory.GetFiles(folder, "*.png");

                foreach (var image in images)
                {
                    try
                    {
                        var imageBytes = File.ReadAllBytes(image);
                        var tex = new Texture2D(2, 2);
                        tex.LoadImage(imageBytes);
                        tex.wrapMode = TextureWrapMode.Clamp;

                        if (!CustomGraffiti.TryGetValue(characterID, out List<Texture2D> texList))
                            texList = new List<Texture2D>();

                        texList.Add(tex);
                        CustomGraffiti[characterID] = texList;
                        Plugin.Instance.GetLogger().LogInfo($"Loaded custom graffiti {Path.GetFileName(image)} for character {characterID}");
                    }
                    catch (Exception e)
                    {
                        Plugin.Instance.GetLogger().LogError($"Problem loading graffiti {Path.GetFileName(image)}: {e}");
                    }
                }
            }
        }

        static void WriteReadme(string filename)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Create subfolders here and put PNG images inside them to replace the Graffiti Plant graffiti for each character, using their internal names. For CrewBoom characters, create a folder with their GUID as the name.");
            sb.AppendLine($"For instance, if you create a folder named \"{Characters.frank}\" and add 2 PNG images with any filename inside, when you Graffiti plant as {Characters.frank} a random image will be picked from that folder to place.");
            sb.AppendLine("A \"Global\" folder was automatically created for you. PNG images placed in this folder will override the graffiti for every character, unless they have their own subfolder here.");
            sb.AppendLine("Possible character internal names are as follows:");
            var allCharacters = Enum.GetValues(typeof(Characters));
            foreach(Characters character in allCharacters)
            {
                if (character != Characters.MAX && character != Characters.NONE)
                    sb.AppendLine(character.ToString());
            }
            File.WriteAllText(filename, sb.ToString());
        }

        public static Texture GetGraffitiTexture(Player player)
        {
            var characterID = player.character.ToString().ToLowerInvariant();

            if (player.character > Characters.MAX && CrewBoomAPIDatabase.IsInitialized)
            {
                if (CrewBoomAPIDatabase.GetUserGuidForCharacter((int)player.character, out Guid crewBoomGUID))
                    characterID = crewBoomGUID.ToString().ToLowerInvariant();
            }
            
            List<Texture2D> graffitiList;

            if (!CustomGraffiti.TryGetValue(characterID, out graffitiList))
            {
                if (!CustomGraffiti.TryGetValue("global", out graffitiList))
                    graffitiList = null;
            }

            if (graffitiList == null)
            {
                var graffInfo = AssetAPI.GetGraffitiArtInfo();
                var graff = graffInfo.FindByCharacter(player.character);
                return graff.graffitiMaterial.mainTexture;
            }

            return graffitiList[UnityEngine.Random.Range(0, graffitiList.Count)];
        }
    }
}

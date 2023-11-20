using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using CrewBoomAPI;
using Reptile;
using UnityEngine;

namespace WallPlant
{
	internal static class GraffitiDatabase
	{
		public static void Initialize()
		{
			string text = Path.Combine(Paths.ConfigPath, Plugin.Name, "Graffiti");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
				Directory.CreateDirectory(Path.Combine(text, "Global"));
				GraffitiDatabase.WriteReadme(Path.Combine(text, "Readme.txt"));
			}
			foreach (string text2 in Directory.GetDirectories(text))
			{
				string text3 = Path.GetFileName(text2).ToLowerInvariant();
				foreach (string text4 in Directory.GetFiles(text2, "*.png"))
				{
					try
					{
						byte[] array = File.ReadAllBytes(text4);
						Texture2D texture2D = new Texture2D(2, 2);
						texture2D.LoadImage(array);
						texture2D.wrapMode = TextureWrapMode.Clamp;
						List<Texture2D> list;
						if (!GraffitiDatabase.CustomGraffiti.TryGetValue(text3, out list))
						{
							list = new List<Texture2D>();
						}
						list.Add(texture2D);
						GraffitiDatabase.CustomGraffiti[text3] = list;
						Plugin.Instance.GetLogger().LogInfo("Loaded custom graffiti " + Path.GetFileName(text4) + " for character " + text3);
					}
					catch (Exception ex)
					{
						Plugin.Instance.GetLogger().LogError(string.Format("Problem loading graffiti {0}: {1}", Path.GetFileName(text4), ex));
					}
				}
			}
		}

		private static void WriteReadme(string filename)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Create subfolders here and put PNG images inside them to replace the Graffiti Plant graffiti for each character, using their internal names. For CrewBoom characters, create a folder with their GUID as the name.");
			stringBuilder.AppendLine(string.Format("For instance, if you create a folder named \"{0}\" and add 2 PNG images with any filename inside, when you Graffiti plant as {1} a random image will be picked from that folder to place.", Characters.frank, Characters.frank));
			stringBuilder.AppendLine("A \"Global\" folder was automatically created for you. PNG images placed in this folder will override the graffiti for every character, unless they have their own subfolder here.");
			stringBuilder.AppendLine("Possible character internal names are as follows:");
			foreach (object obj in Enum.GetValues(typeof(Characters)))
			{
				Characters characters = (Characters)obj;
				if (characters != Characters.MAX && characters != Characters.NONE)
				{
					stringBuilder.AppendLine(characters.ToString());
				}
			}
			File.WriteAllText(filename, stringBuilder.ToString());
		}

		public static Texture GetGraffitiTexture(Player player)
		{
			string text = player.character.ToString().ToLowerInvariant();
			if (Plugin.IsCrewBoomInstalled())
			{
				Guid guid;
				if (player.character > Characters.MAX && CrewBoomAPIDatabase.IsInitialized && CrewBoomAPIDatabase.GetUserGuidForCharacter((int)player.character, out guid))
				{
					text = guid.ToString().ToLowerInvariant();
				}
			}
			List<Texture2D> list;
			if (!GraffitiDatabase.CustomGraffiti.TryGetValue(text, out list) && !GraffitiDatabase.CustomGraffiti.TryGetValue("global", out list))
			{
				list = null;
			}
			if (list == null)
			{
				return Plugin.GetGraffitiArtInfo().FindByCharacter(player.character).graffitiMaterial.mainTexture;
			}
			return list[UnityEngine.Random.Range(0, list.Count)];
		}

		private static Dictionary<string, List<Texture2D>> CustomGraffiti = new Dictionary<string, List<Texture2D>>();
	}
}

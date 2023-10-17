# BRC-WallPlant
BepInEx plugin that adds a Wall Plant trick to Bomb Rush Cyberfunk.

![wallplant](https://github.com/LazyDuchess/BRC-WallPlant/assets/42678262/d93f8bd3-50f2-42f0-8ac4-e8e0c39713f4)

# Usage
Simply hit the jump button as you hit a wall in mid-air to trigger the trick. By default, you will be able to air dash and air boost again after hitting a wall plant, and every wall plant you do in a row without landing/grinding/wallrunning/etc. beforehand will get weaker. You can do up to 4 in a row.

# Configuration
A documented BepInEx configuration file will be created the first time you run the mod, called **"com.LazyDuchess.BRC.WallPlant.cfg"**. You can use r2modman to more easily edit it. You can edit most aspects of the mod such as the physics, limits and penalties.

# Building from source
You will need to provide a publicized version of the **"Assembly-CSharp.dll"** file which can be found in your "Bomb Rush Cyberfunk_Data/Managed" folder. To publicize it, you can use the [BepInEx.AssemblyPublicizer](https://github.com/BepInEx/BepInEx.AssemblyPublicizer) tool, and paste the result into **"lib/Assembly-CSharp-publicized.dll"** in this project's root directory.

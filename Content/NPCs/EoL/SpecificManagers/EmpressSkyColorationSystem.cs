﻿using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace WoTE.Content.NPCs.EoL
{
    public class EmpressSkyColorationSystem : ModSystem
    {
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            backgroundColor = Color.Lerp(backgroundColor, Color.Silver, EmpressSky.Opacity * 0.15f);
            tileColor = Color.Lerp(tileColor, Color.Lavender, EmpressSky.Opacity * 0.25f);
        }
    }
}

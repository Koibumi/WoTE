﻿using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace WoTE.Content.NPCs.EoL
{
    public class EmpressSky : CustomSky
    {
        private bool skyActive;

        private static int totalSpawnedRainParticles;

        public struct RainParticle
        {
            public bool Active;

            public float Scale;

            public Vector2 Position;

            public Vector2 Velocity;

            public void Update()
            {
                if (!Active)
                    return;

                Position += Velocity;
                if (Collision.SolidCollision(Position, 4, 4))
                    Active = false;
            }

            public static void SpawnNew(Vector2 spawnPosition, Vector2 velocity, float scale)
            {
                totalSpawnedRainParticles++;
                RainParticles[totalSpawnedRainParticles % RainParticles.Length] = new()
                {
                    Active = true,
                    Position = spawnPosition,
                    Velocity = velocity,
                    Scale = scale
                };
            }
        }

        private static Color mistColor;

        private static Color cloudColor;

        private static Color moonColor;

        private static Color moonBackglowColor;

        public static Color BackgroundTint
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether rain effects should be created or not.
        /// </summary>
        public static bool ShouldRain => !Main.dayTime && !Main.gameMenu;

        /// <summary>
        /// The position of the moon in screen space.
        /// </summary>
        public static Vector2 MoonScreenPosition => Main.ScreenSize.ToVector2() * new Vector2(0.5f, 0.115f);

        /// <summary>
        /// The set of all active rain particles.
        /// </summary>
        public static readonly RainParticle[] RainParticles = new RainParticle[2048];

        /// <summary>
        /// The opacity of this sky.
        /// </summary>
        public static new float Opacity
        {
            get;
            set;
        }

        /// <summary>
        /// The identifier key for this sky.
        /// </summary>
        public const string SkyKey = "WoTE:EmpressSky";

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (ModContent.GetInstance<WoTE>()?.Assets?.IsDisposed ?? true)
                return;

            EmpressPaletteSet palette = Main.dayTime ? EmpressPalettes.DaytimePaletteSet : EmpressPalettes.Default;
            if (EmpressOfLight.Myself is not null)
                palette = EmpressOfLight.Myself.As<EmpressOfLight>().Palette;

            moonColor = palette.MoonColor;
            moonBackglowColor = palette.MoonBackglowColor;
            mistColor = palette.MistColor;
            cloudColor = palette.CloudColor;
            BackgroundTint = palette.BackgroundTint;

            Matrix backgroundMatrix = Main.BackgroundViewMatrix.TransformationMatrix;
            Vector3 translationDirection = new(1f, Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f, 1f);
            backgroundMatrix.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation * translationDirection;

            // Prevent drawing beyond the back layer.
            if (maxDepth >= float.MaxValue && minDepth < float.MaxValue)
            {
                Texture2D sky = ModContent.Request<Texture2D>("WoTE/Content/NPCs/EoL/SpecificManagers/EmpressSky").Value;

                Vector2 screenSize = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height + 200f);
                Rectangle skyRectangle = new(0, 0, (int)screenSize.X, (int)screenSize.Y);
                Main.spriteBatch.Draw(sky, skyRectangle, BackgroundTint * Opacity);

                if (!Main.dayTime)
                {
                    Texture2D moon = ModContent.Request<Texture2D>("WoTE/Content/NPCs/EoL/SpecificManagers/TheMoonFromInfernum").Value;
                    Vector2 moonDrawPosition = MoonScreenPosition;

                    Texture2D bloom = MiscTexturesRegistry.BloomCircleSmall.Value;

                    Color weakBloomColor = Color.Lerp(moonColor, Color.White, 0.5f);
                    Main.spriteBatch.Draw(bloom, moonDrawPosition, null, weakBloomColor with { A = 0 } * Opacity * 0.6f, 0f, bloom.Size() * 0.5f, 2f, 0, 0f);
                    Main.spriteBatch.Draw(bloom, moonDrawPosition, null, weakBloomColor with { A = 0 } * Opacity * 0.31f, 0f, bloom.Size() * 0.5f, 3.3f, 0, 0f);
                    Main.spriteBatch.Draw(bloom, moonDrawPosition, null, moonBackglowColor * Opacity * 0.15f, 0f, bloom.Size() * 0.5f, 6f, 0, 0f);
                    Main.spriteBatch.Draw(moon, moonDrawPosition, null, moonColor * Opacity, 0f, moon.Size() * 0.5f, 0.11f, 0, 0f);
                }

                // Draw clouds.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, backgroundMatrix);
                DrawClouds();

                // Return to standard drawing.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, backgroundMatrix);
            }

            else
            {
                // Draw mist.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, backgroundMatrix);
                DrawMist();

                // Return to standard drawing.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, backgroundMatrix);
            }

            if (Main.gameMenu)
                DrawCustomRain();
        }

        private static void DrawClouds()
        {
            Texture2D clouds = ModContent.Request<Texture2D>("WoTE/Content/NPCs/EoL/SpecificManagers/CloudTexture").Value;
            ManagedShader cloudShader = ShaderManager.GetShader("WoTE.CloudBackgroundShader");
            cloudShader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 1, SamplerState.PointWrap);
            cloudShader.TrySetParameter("baseTextureSize", clouds.Size());
            cloudShader.Apply();

            Vector2 screenSize = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Rectangle cloudsRectangle = new(0, 0, Main.screenWidth, (int)(screenSize.Y * 0.3f));

            Main.spriteBatch.Draw(clouds, cloudsRectangle, cloudColor * Opacity);
        }

        private static void DrawMist()
        {
            Texture2D clouds = ModContent.Request<Texture2D>("WoTE/Content/NPCs/EoL/SpecificManagers/CloudTexture").Value;
            ManagedShader mistShader = ShaderManager.GetShader("WoTE.MistBackgroundShader");
            Vector2 screenSize = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height + 200f);
            Rectangle mistRectangle = new(0, (int)(screenSize.Y * 0.25f), Main.screenWidth, (int)(screenSize.Y * 0.7f));

            if (Main.gameMenu)
            {
                mistRectangle.Y -= 600;
                mistRectangle.Height += 600;
            }

            mistShader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 1, SamplerState.PointWrap);
            mistShader.SetTexture(TextureAssets.Extra[ExtrasID.QueenSlimeGradient], 2, SamplerState.LinearWrap);
            mistShader.TrySetParameter("dewAppearanceCutoffThreshold", 0.99f);
            mistShader.TrySetParameter("baseTextureSize", mistRectangle.Size() * 0.5f);
            mistShader.TrySetParameter("worldOffset", (Main.gameMenu ? new Vector2(0f, 3200f) : Main.screenPosition) / clouds.Size() * 0.05f);
            mistShader.TrySetParameter("twinkleSpeed", 3f);
            mistShader.Apply();

            Main.spriteBatch.Draw(clouds, mistRectangle, mistColor * Opacity * 0.15f);
        }

        public override void Update(GameTime gameTime)
        {
            // Disable ambient sky objects like wyverns and eyes appearing in front of the background.
            if (Opacity >= 0.5f)
                SkyManager.Instance["Ambience"].Deactivate();

            bool underground = Main.screenPosition.Y >= Main.worldSurface * 16f;
            bool fadeIn = skyActive && !underground;
            Opacity = Utilities.Saturate(Opacity + fadeIn.ToDirectionInt() * 0.015f);

            for (int i = 0; i < RainParticles.Length; i++)
                RainParticles[i].Update();

            float coverage = Main.gameMenu ? 2750f : 1300f;
            if (skyActive && Main.LocalPlayer.Center.Y >= 3000f && ShouldRain)
            {
                for (int i = 0; i < 2; i++)
                {
                    float rainScaleInterpolant = Main.rand.NextFloat();
                    float rainScale = MathHelper.Lerp(0.6f, 1.4f, rainScaleInterpolant);
                    Vector2 rainVelocity = Vector2.UnitY.RotatedBy(0.15f) * MathHelper.Lerp(34f, 60f, rainScale) * (Main.gameMenu ? 0.45f : 1f);
                    Vector2 rainSpawnPosition = (Main.gameMenu ? Vector2.Zero : Main.LocalPlayer.Center) + new Vector2(Main.rand.NextFloatDirection() * coverage, -1050f);
                    RainParticle.SpawnNew(rainSpawnPosition, rainVelocity, rainScale);
                }
            }
        }

        public override void OnLoad()
        {
            On_Main.DrawRain += DrawCustomRainWrapper;
        }

        private void DrawCustomRainWrapper(On_Main.orig_DrawRain orig, Main self)
        {
            if (Opacity > 0f && !Main.gameMenu)
                DrawCustomRain();

            orig(self);
        }

        public static void DrawCustomRain()
        {
            Texture2D rain = TextureAssets.Rain.Value;
            for (int i = 0; i < RainParticles.Length; i++)
            {
                Rectangle frame = new(i % 3 * 4, 0, 2, i % 3 * 10 + 20);
                if (!RainParticles[i].Active)
                    continue;

                var drawPosition = RainParticles[i].Position - (Main.gameMenu ? Vector2.Zero : Main.screenPosition);
                Main.spriteBatch.Draw(rain, drawPosition, frame, Color.Wheat * Opacity * 0.2f, RainParticles[i].Velocity.ToRotation() + MathHelper.PiOver2, frame.Size() * 0.5f, RainParticles[i].Scale, 0, 0f);
            }
        }

        #region Boilerplate

        public override void Deactivate(params object[] args) => skyActive = false;

        public override void Reset() => skyActive = false;

        public override bool IsActive() => skyActive || Opacity > 0f;

        public override void Activate(Vector2 position, params object[] args) => skyActive = true;

        public override float GetCloudAlpha() => 1f - Opacity;
        #endregion Boilerplate
    }
}

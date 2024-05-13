﻿using System;
using Luminance.Common.Easings;
using Luminance.Common.StateMachines;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTE.Content.NPCs.EoL.Projectiles;
using WoTE.Content.Particles;

namespace WoTE.Content.NPCs.EoL
{
    public partial class EmpressOfLight : ModNPC
    {
        /// <summary>
        /// Whether the Empress is in phase 1 but would like to enter phase 2.
        /// </summary>
        public bool EnterPhase2AfterNextAttack => Phase <= 0 && NPC.life <= NPC.lifeMax * Phase2LifeRatio;

        /// <summary>
        /// Whether the Empress is currently in phase 2 or not.
        /// </summary>
        public bool Phase2
        {
            get => Phase >= 1;
            set => Phase = value ? Math.Max(1, Phase) : 0;
        }

        /// <summary>
        /// How long the Empress spends disappearing during her second phase transition.
        /// </summary>
        public static int Phase2Transition_DisappearTime => Utilities.SecondsToFrames(0f);

        /// <summary>
        /// How long the Empress spends invisible as the rain pours during her second phase transition.
        /// </summary>
        public static int Phase2Transition_StayInvisibleTime => Utilities.SecondsToFrames(11f);

        /// <summary>
        /// The life ratio at which the Emperss transitions to her second phase.
        /// </summary>
        public static float Phase2LifeRatio => 0.6f;

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_Phase2Transition()
        {
            StateMachine.RegisterTransition(EmpressAIType.Phase2Transition, EmpressAIType.OrbitReleasedTerraprismas, false, () =>
            {
                return AITimer >= 1000000;
            });
            StateMachine.ApplyToAllStatesExcept(state =>
            {
                StateMachine.RegisterTransition(state, EmpressAIType.Phase2Transition, false, () => EnterPhase2AfterNextAttack && CurrentState != EmpressAIType.ButterflyBurstDashes);
            }, EmpressAIType.Phase2Transition, EmpressAIType.Die);

            StateMachine.RegisterStateBehavior(EmpressAIType.Phase2Transition, DoBehavior_Phase2Transition);
        }

        /// <summary>
        /// Performs the Empress' second phase transition state.
        /// </summary>
        public void DoBehavior_Phase2Transition()
        {
            if (Main.mouseRight && Main.mouseRightRelease)
                AITimer = 0;

            float maxZPosition = MathHelper.Lerp(5f, 1.1f, Utilities.Sin01(MathHelper.TwoPi * AITimer / 60f).Cubed());
            ZPosition = EasingCurves.Cubic.Evaluate(EasingType.InOut, Utilities.InverseLerp(0f, 60f, AITimer)) * maxZPosition;
            if (ZPosition >= 0.8f)
                NPC.velocity *= 0.9f;
            else
                NPC.SmoothFlyNear(Target.Center - Vector2.UnitY * 270f, ZPosition * 0.1f, 1f - ZPosition * 0.15f);
            NPC.rotation = MathHelper.Lerp(NPC.rotation, 0f, 0.3f);

            float appearanceInterpolant = Utilities.InverseLerpBump(0f, 0.4f, 0.5f, 0.55f, (AITimer - Phase2Transition_DisappearTime) / (float)Phase2Transition_StayInvisibleTime).Squared();
            if (Main.netMode != NetmodeID.MultiplayerClient && ZPosition >= 2f && AITimer % 3 == 0 && appearanceInterpolant >= 0.5f)
            {
                Vector2 moonlightPosition = NPC.Center + (MathHelper.TwoPi * AITimer / 30f).ToRotationVector2() * Main.rand.NextFloat(1200f, 1300f) * new Vector2(1f, 0.6f);
                Vector2 moonlightVelocity = moonlightPosition.SafeDirectionTo(NPC.Center).RotatedBy(MathHelper.PiOver2) * 32f;
                Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), moonlightPosition, moonlightVelocity, ModContent.ProjectileType<ConvergingMoonlight>(), 0, 0f);
            }

            for (int i = 0; i < appearanceInterpolant * 16f; i++)
            {
                float pixelScale = Main.rand.NextFloat(1f, 5f);
                Vector2 pixelSpawnPosition = NPC.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(900f, 1256f);
                Vector2 pixelVelocity = pixelSpawnPosition.SafeDirectionTo(NPC.Center).RotatedBy(MathHelper.PiOver4) * Main.rand.NextFloat(12f, 30f) / pixelScale;
                Color pixelBloomColor = Utilities.MulticolorLerp(Main.rand.NextFloat(), Color.Yellow, Color.HotPink, Color.Violet, Color.DeepSkyBlue) * 0.6f;

                BloomPixelParticle bloom = new(pixelSpawnPosition, pixelVelocity, Color.White, pixelBloomColor, Main.rand.Next(150, 210), Vector2.One * pixelScale, () => NPC.Center);
                bloom.Spawn();
            }

            LeftHandFrame = EmpressHandFrame.HandPressedToChest;
            RightHandFrame = EmpressHandFrame.HandPressedToChest;
            NPC.dontTakeDamage = true;
            NPC.ShowNameOnHover = false;
            Phase2 = true;
            IdealDrizzleVolume = StandardDrizzleVolume + Utilities.InverseLerp(0f, 120f, AITimer - Phase2Transition_DisappearTime) * 0.3f;

            DashAfterimageInterpolant = MathHelper.Lerp(DashAfterimageInterpolant, Utilities.InverseLerp(0f, 120f, AITimer), 0.055f);

            // God.
            // This ensures that Noxus' apparent position isn't as responsive to camera movements if he's in the background, giving a pseudo-parallax visual.
            // Idea is basically Noxus going
            // "Oh? You moved 30 pixels in this direction? Well I'm in the background bozo so I'm gonna follow you and go in the same direction by, say, 27 pixels. This will make it look like I only moved 3 pixels"
            // This obviously doesn't work in multiplayer, and as such it does not run there.
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                float parallax = 1f - MathF.Pow(2f, ZPosition * -1.5f);
                Vector2 targetOffset = Target.velocity;
                if (NPC.HasPlayerTarget)
                {
                    Player playerTarget = Main.player[NPC.TranslatedTargetIndex];
                    targetOffset = playerTarget.position - playerTarget.oldPosition;
                }
                NPC.position += targetOffset * Utilities.Saturate(parallax);
            }
        }
    }
}

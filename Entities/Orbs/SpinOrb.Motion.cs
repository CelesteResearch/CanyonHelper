using System;
using Celeste.Mods.CanyonHelper.Entities.Orbs.Particles;
using Celeste.Game.Content.Dialogs;
using Celeste.Core.Mathematics.Easing;
using Celeste.Formats.Map.Entities.Structure;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Pooling;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Entities.Birds;
using Celeste.Game.UI.Dialogue;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Content.Atlas;
using Celeste.Mods.CanyonHelper.Modules;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Presentation.Hud.Tutorials.Birds;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.CanyonHelper.Entities.Orbs
{
    public partial class SpinOrb
    {
        public override void Update()
        {
            base.Update();
            OriginalLevelSceneRoot level = Scene as OriginalLevelSceneRoot;
            if (playerEntity != null)
            {
                if (playerEntity.Dead)
                {
                    ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Stop(moveSfx);
                }

                if (tutorialGui != null)
                {
                    if (shouldShowTutorial)
                    {
                        tutorialGui.Open =
                            (((ISourceVectorEnvironment)engine).VectorArithmetic.Length(playerEntity.Position -
                                this.Position) < 64);
                    }
                    else
                    {
                        tutorialGui.Open = false;
                    }
                }
            }

            if (playerInOrb)
            {
                if (playerEntity != null)
                {
                    Vector2 targetPos = Position + Vector2.UnitY * 6.5f;
                    targetPos = new Vector2((float)Math.Round(targetPos.X),
                        (float)Math.Round(targetPos.Y)); //round to prevent garbage collision
                    playerEntity.Position = targetPos;
                    playerEntity.Visible = false;
                    playerEntity.Speed = Vector2.Zero;
                }

                if (playerEntity.Input.GrabCheck && !playerEntity.Input.Jump.Check)
                {
                    currentRotateSpeed = slowRotateSpeed;
                }
                else if (playerEntity.Input.Jump.Check && !playerEntity.Input.GrabCheck)
                {
                    currentRotateSpeed = fastRotateSpeed;
                }
                else if (playerEntity.Input.Jump.Check && playerEntity.Input.GrabCheck)
                {
                    currentRotateSpeed = 0;
                }
                else
                {
                    currentRotateSpeed = normalRotateSpeed;
                }

                sprite.Rotation = currentRotateAngle + spriteRotationOffset;
                if (rotateCounterclockwise)
                {
                    currentRotateAngle -= currentRotateSpeed * engine.DeltaTime;
                }
                else
                {
                    currentRotateAngle += currentRotateSpeed * engine.DeltaTime;
                }
            }
            else
            {
                if (useDelay > 0)
                {
                    useDelay -= engine.DeltaTime;
                    outline.Visible = true;
                    if (useDelay < 0.8f && !debrisShaken)
                    {
                        debrisShaken = true;
                        ISourceAudioEventReference sound =
                            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                                "event:/game/04_cliffside/arrowblock_reform_begin", debris[0].Position);
                        foreach (SpinOrbDebris debrie in debris)
                        {
                            debrie.StartShaking();
                        }
                    }

                    if (useDelay < 0.65f && !debrisReturned)
                    {
                        debrisReturned = true;
                        foreach (SpinOrbDebris debrie in debris)
                        {
                            debrie.ReturnHome(0.5f);
                        }
                    }

                    if (useDelay < 0.15f)
                    {
                        outline.Visible = false;
                        Visible = true;
                        sprite.Scale = Vector2.One * ((0.15f - useDelay) / 0.15f);
                    }
                }
                else if (useDelay < 0)
                {
                    useDelay = 0;
                    Collidable = true;
                    level.Displacement.AddBurst(Center, 0.35f, 8f, 48f, 0.25f, null, null);
                    level.Particles.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.Player.P_Split, 32, Center,
                        Vector2.One * 2f);
                    ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                        "event:/game/04_cliffside/greenbooster_reappear", Position);
                    foreach (SpinOrbDebris debrie in debris)
                    {
                        debrie.RemoveSelf();
                    }

                    debris = new List<SpinOrbDebris>();
                }
                else
                {
                    outline.Visible = false;
                    if (level.Clock.OnInterval(0.1f, engine))
                    {
                        level.ParticlesFG.Emit(SpinOrbGlowParticles, 1, Position, Vector2.One * 5f);
                        //level.ParticlesBG.Emit(BadelineBoost.P_Ambience, 1, Center, Vector2.One);
                    }
                }
            }
        }
    }
}

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
        private void OnPlayerDashed(Vector2 direction)
        {
            if (playerInOrb)
            {
                shouldShowTutorial = false;
                sprite.Play("idle", true);
                playerInOrb = false;
                if (playerEntity != null)
                {
                    playerEntity.Machine.State = 11;
                    playerEntity.Visible = true;
                    playerEntity.Speed = Vector2Geometry.AngleToVector(currentRotateAngle, launchSpeed);
                    playerEntity.Machine.State = 0;
                    playerEntity.RefillDash();
                    playerEntity.RefillStamina();
                }

                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Stop(moveSfx);
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/char/badeline/booster_throw",
                    Position);
                OriginalLevelSceneRoot level = Scene as OriginalLevelSceneRoot;
                level.Displacement.AddBurst(Center, 0.35f, 8f, 48f, 0.25f, null, null);
                level.Particles.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.Player.P_Split, 96, Center,
                    Vector2.One * 12f);
                for (int i = -4; i < 5; i++)
                {
                    ISourceRandom random =
                        ((ISourceRandomProcessEnvironment)engine).RandomProcess.Factory.CreateUnseeded();
                    SpinOrbStarBurst starBurst = engine.EntityPools.Create<SpinOrbStarBurst>().Init(Center,
                        Vector2Geometry.AngleToVector(currentRotateAngle + i * ((float)Math.PI) / 72, 1f));
                    ((OriginalLevelSceneRoot)Scene).Scheduler.QueueSourceEntity(starBurst);
                }

                currentRotateAngle = initialAngle;
                sprite.Rotation = initialAngle + spriteRotationOffset;
                Visible = false;
                Collidable = false;
                BreakParticles();
            }
        }

        private void BreakParticles()
        {
            debrisShaken = false;
            debrisReturned = false;
            Vector2 center = base.Center;
            ISourceRandom random = ((ISourceRandomProcessEnvironment)engine).RandomProcess.Factory.CreateUnseeded();
            ((OriginalLevelSceneRoot)Scene).Particles.Emit(
                ((IOriginalGameplayProcess)engine).ParticleTypes.MoveBlock.P_Break, 6, center, Vector2.One * 8f,
                SourceRandomMath.Range(random, 0f, ((float)Math.PI * 2)));

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = new Vector2(SourceRandomMath.Range(random, -4f, 4f),
                    SourceRandomMath.Range(random, -4f, 4f));
                offset = ((ISourceVectorEnvironment)engine).VectorArithmetic.Normalize(offset);
                SpinOrbDebris d = engine.EntityPools.Create<SpinOrbDebris>()
                    .Init(Position + (offset / 2), Center, Position + offset);
                debris.Add(d);
                ((OriginalLevelSceneRoot)Scene).Scheduler.QueueSourceEntity(d);
                d = null;
            }
        }
    }
}

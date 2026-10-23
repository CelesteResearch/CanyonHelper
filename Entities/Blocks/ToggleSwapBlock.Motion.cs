using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Pooling;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Core.Rendering.States;
using Celeste.Mods.CanyonHelper.Modules;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.CanyonHelper.Entities.Blocks
{
    public partial class ToggleSwapBlock
    {
        private static readonly string sourcePlayerRegistryKey =
            typeof(global::Celeste.Simulation.RoomPlayerSession).FullName;

        private IEnumerator TriggeredMove()
        {
            if (moving || stopped)
            {
                if (stopped)
                {
                    StartShaking(0.25f);
                }

                yield break;
            }

            moving = true;

            if (nodeIndex < nodes.Length - 1 && nodeIndex != 0)
            {
                if (!returning)
                {
                    nodeIndex++;
                }
                else
                {
                    nodeIndex--;
                }
            }
            else if (nodeIndex == 0)
            {
                if (returning)
                {
                    returning = false;
                }

                nodeIndex++;
            }
            else
            {
                if (stopAtEnd)
                {
                    nodeIndex = -1;
                    stopped = true;
                    StartShaking(0.5f);
                    yield break;
                }
                else if (oscillate)
                {
                    nodeIndex--;
                    returning = true;
                }
                else
                {
                    nodeIndex = 0;
                }
            }

            middleRed.Play("moving");
            moveSfx = ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                "event:/game/05_mirror_temple/swapblock_move", Center);

            Vector2 targetPosition = nodes[nodeIndex];
            Vector2 startPosition = Position;
            //Start moving there
            dirVector = targetPosition - startPosition;
            Vector2 lerpVector = dirVector * travelSpeed;

            RoomPlayerSession player =
                level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(sourcePlayerRegistryKey);

            while (Position != targetPosition)
            {
                MoveTo(Vector2.Lerp(startPosition, targetPosition, lerp), lerpVector);
                lerp = ScalarMath.Approach(lerp, 1, travelSpeed * engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                yield return null;
            }

            lerp = 0;
            (Scene as OriginalLevelSceneRoot).Displacement.AddBurst(Center, 0.2f, 0f, 16f, 1f, null, null);
            moving = false;
            middleRed.Play("idle");
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Stop(moveSfx, true);
            moveSfx = null;
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                "event:/game/05_mirror_temple/swapblock_move_end",
                Center);
            RecalculateLaserColor();
        }

        public override void Update()
        {
            base.Update();

            RoomPlayerSession player =
                level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(sourcePlayerRegistryKey);
            Vector2 swapCancel = Vector2.One;

            if (player != null)
            {
                if (player.Machine.State == 2)
                {
                    if (player.Data.Direction.X != 0 && player.Input.GrabCheck &&
                        SourceEntityCollision.CheckAt(player, this,
                            player.Position + Vector2.UnitX * Math.Sign(player.Data.Direction.X)) &&
                        Math.Sign(dirVector.X) == Math.Sign(player.Data.Direction.X))
                    {
                        player.Machine.State = 1;
                        player.Speed = Vector2.Zero;
                    }

                    if (SourceEntityCollision.CheckAt(player, this, player.Position + Vector2.UnitY))
                    {
                        if (lerp > 0)
                        {
                            if (player.Data.Direction.X != 0f &&
                                Math.Sign(dirVector.X) == Math.Sign(player.Data.Direction.X))
                            {
                                player.Data.Motion.Normal.Speed.X = swapCancel.X = 0f;
                                if (lerp >= 0.8)
                                    player.Machine.State = 0;
                            }

                            if (player.Data.Direction.Y != 0f &&
                                Math.Sign(dirVector.Y) == Math.Sign(player.Data.Direction.Y))
                            {
                                player.Data.Motion.Normal.Speed.Y = swapCancel.Y = 0f;
                                if (lerp >= 0.8)
                                    player.Machine.State = 0;
                            }
                        }
                    }

                    player.Data.Motion.Normal.Speed.X = player.Data.Motion.Normal.Speed.X * swapCancel.X;
                    player.Data.Motion.Normal.Speed.Y = player.Data.Motion.Normal.Speed.Y * swapCancel.Y;
                }
            }
        }

        private void OnPlayerDashed(Vector2 direction)
        {
            Add(new SimulationCoroutine(engine, TriggeredMove(), true));
        }
    }
}

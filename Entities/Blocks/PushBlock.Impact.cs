using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Debris;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;

namespace Celeste.Mods.CanyonHelper.Entities.Blocks
{
    public partial class PushBlock
    {
        private bool OnDashed(Vector2 from)
        {
            int j = 0;
            Vector2 startPos = TopLeft;
            int addPos = -1;
            while (j < Width)
            {
                if (from.X < Position.X) //from left
                {
                    startPos = TopRight;
                    addPos = 1;
                }

                if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid",
                        startPos + new Vector2(addPos, j)) || SourceSceneCollision.CheckPoint(level.Scheduler.Entities,
                        "Celeste.Platform", startPos + new Vector2(addPos, j)))
                {
                    return false;
                }

                j += 4;
            }

            if (delayBetweenImpactEffect <= 0)
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/general/wall_break_stone",
                    Position);

            if (!(Left + 1 < level.Bounds.Left || Right + 1 > level.Bounds.Right))
            {
                if (from.X < Position.X) //If from the left
                {
                    if (delayBetweenImpactEffect <= 0)
                    {
                        for (int i = 1; i < 7; i++)
                        {
                            level.Scheduler.QueueSourceEntity(engine.EntityPools.Create<DebrisState>()
                                .Init(TopLeft + new Vector2(-1, i * 4), breakDebrisTileset)
                                .BlastFrom(TopRight - TopCenter + from));
                        }

                        delayBetweenImpactEffect = maxDelay;
                    }

                    speedX = 320;
                }
                else //If from the right
                {
                    if (delayBetweenImpactEffect <= 0)
                    {
                        for (int i = 1; i < 7; i++)
                        {
                            level.Scheduler.QueueSourceEntity(engine.EntityPools.Create<DebrisState>()
                                .Init(TopRight + new Vector2(1, i * 4), breakDebrisTileset)
                                .BlastFrom(legacy ? (TopLeft - TopCenter - from) : (TopLeft - TopCenter + from)));
                        }

                        delayBetweenImpactEffect = maxDelay;
                    }

                    speedX = -320;
                }
            }

            return true;
        }

        private bool OnDashedTop(Vector2 from)
        {
            int j = 0;
            Vector2 startPos = TopLeft;
            int addPos = -1;
            while (j < Width)
            {
                if (from.Y < Position.Y) //from the top
                {
                    startPos = BottomLeft;
                    addPos = 1;
                }

                if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid",
                        startPos + new Vector2(j, addPos)) || SourceSceneCollision.CheckPoint(level.Scheduler.Entities,
                        "Celeste.Platform", startPos + new Vector2(j, addPos)))
                {
                    return false;
                }

                j += 4;
            }

            if (delayBetweenImpactEffect <= 0)
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/general/wall_break_stone",
                    Position);
            if (from.Y < Position.Y) //If from the top
            {
                if (delayBetweenImpactEffect <= 0)
                {
                    for (int i = 1; i < 7; i++)
                    {
                        level.Scheduler.QueueSourceEntity(engine.EntityPools.Create<DebrisState>()
                            .Init(TopLeft + new Vector2(i * 4, -2), breakDebrisTileset)
                            .BlastFrom(TopCenter - Vector2.UnitY));
                    }

                    delayBetweenImpactEffect = maxDelay;
                }

                speedY = 320;
            }
            else //If from the bottom
            {
                if (delayBetweenImpactEffect <= 0)
                {
                    for (int i = 1; i < 7; i++)
                    {
                        level.Scheduler.QueueSourceEntity(engine.EntityPools.Create<DebrisState>()
                            .Init(BottomLeft + new Vector2(i * 4, 2), breakDebrisTileset)
                            .BlastFrom(BottomCenter + Vector2.UnitY));
                    }

                    delayBetweenImpactEffect = maxDelay;
                }

                speedY = -320;
            }

            return true;
        }
    }
}

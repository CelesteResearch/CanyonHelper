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
        private void LandParticles()
        {
            int num = 0;
            while (num <= Width)
            {
                if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid",
                        BottomLeft + new Vector2(num, 1f)))
                {
                    level.ParticlesFG.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.FallingBlock.P_FallDustA, 1,
                        new Vector2(Left + num, Bottom), Vector2.One * 4f, (float)-Math.PI / 2);
                    float direction;
                    if (num < Width / 2f)
                    {
                        direction = (float)Math.PI;
                    }
                    else
                    {
                        direction = 0f;
                    }

                    level.ParticlesFG.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.FallingBlock.P_LandDust, 1,
                        new Vector2(Left + num, Bottom), Vector2.One * 4f, direction);
                }

                num += 4;
            }
        }

        private void ScrapeParticles(Vector2 dir)
        {
            if (dir.X != 0f)
            {
                int x = 0;
                while ((legacy && x < Width) || x <= Width)
                {
                    Vector2 bottomPos = new Vector2(Left + x, Bottom + 1);
                    if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid", bottomPos))
                    {
                        level.ParticlesFG.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.ZipMover.P_Scrape,
                            bottomPos);
                    }

                    Vector2 topPos = new Vector2(Left + x, Top - 1);
                    if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid", topPos))
                    {
                        level.ParticlesFG.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.ZipMover.P_Scrape,
                            topPos);
                    }

                    x += 8;
                }
            }
            else
            {
                int y = 0;
                while ((legacy && y < Height) || y <= Height)
                {
                    Vector2 leftPos = new Vector2(Left - 1, Top + y);
                    if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid", leftPos))
                    {
                        level.ParticlesFG.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.ZipMover.P_Scrape,
                            leftPos);
                    }

                    Vector2 rightPos = new Vector2(Right + 1, Top + y);
                    if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid", rightPos))
                    {
                        level.ParticlesFG.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.ZipMover.P_Scrape,
                            rightPos);
                    }

                    y += 8;
                }
            }
        }
    }
}

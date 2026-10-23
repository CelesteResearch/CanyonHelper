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
        public override void Update()
        {
            base.Update();
            ApplyGravityCheck();
            if (delayBetweenImpactEffect > 0)
            {
                delayBetweenImpactEffect -= engine.DeltaTime;
            }
            else
            {
                delayBetweenImpactEffect = 0;
            }

            if (applyGravity)
            {
                if (gravity < 160)
                {
                    gravity += 480 * engine.DeltaTime;
                }
                else
                {
                    gravity = 160;
                }
            }
            else
            {
                gravity = 0;
            }

            if (level.Clock.OnInterval(0.03f, engine))
            {
                if (speedY > 0f)
                {
                    ScrapeParticles(Vector2.UnitY);
                }
                else if (speedY < 0f)
                {
                    ScrapeParticles(-Vector2.UnitY);
                }

                if (speedX > 0f)
                {
                    ScrapeParticles(Vector2.UnitX);
                }
                else if (speedX < 0f)
                {
                    ScrapeParticles(-Vector2.UnitX);
                }
            }

            if (legacy)
                MoveHExactCollideSolids((int)Math.Floor(speedX * engine.DeltaTime), false, null);
            else
                MoveHCollideSolids(speedX * engine.DeltaTime, false);
            if (speedX != 0)
            {
                speedX = Approach(speedX, 0, 960 * engine.DeltaTime);
                if (Left < level.Bounds.Left || Right > level.Bounds.Right)
                    speedX = 0;
            }

            if (SourceTrackedEntityCollision.CheckAt(level.Scheduler.Entities, "Celeste.SolidTiles", this,
                    Position + Vector2.UnitX * Math.Sign(speedX) * 256f) || CheckCollisionX(speedX))
            {
                Approach(speedX, 24f, 500f * engine.DeltaTime * 0.25f);
            }

            if (legacy)
                MoveVExactCollideSolids((int)Math.Floor((speedY + (applyGravity ? gravity : 0f)) * engine.DeltaTime),
                    false,
                    null);
            else
                MoveVCollideSolids((speedY + gravity) * engine.DeltaTime, false);
            if (speedY != 0)
            {
                if (Top < level.Bounds.Top)
                    speedY = 0;
                if (Top > level.Bounds.Bottom)
                    RemoveSelf();
                speedY = Approach(speedY, 0, 960 * engine.DeltaTime);
            }

            if (SourceTrackedEntityCollision.CheckAt(level.Scheduler.Entities, "Celeste.SolidTiles", this,
                    Position + Vector2.UnitY * Math.Sign(speedY) * 256f) || CheckCollisionY(speedY))
            {
                Approach(speedY, 24f, 500f * engine.DeltaTime * 0.25f);
            }

            //Is on edge? Slide off!
            if (speedX == 0 && !applyGravity)
            {
                int amt = 0;
                if (!SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid", BottomRight))
                    amt++;
                if (!SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid", BottomLeft))
                    amt++;
                if (amt == 1 && (Left < level.Bounds.Left || Right > level.Bounds.Right))
                {
                    if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid", BottomLeft) &&
                        !SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid",
                            BottomLeft + new Vector2(4, 1)))
                    {
                        speedX = 150f;
                    }
                    else if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid", BottomRight) &&
                             !SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid",
                                 BottomRight + new Vector2(-4, 1)))
                    {
                        speedX = -150f;
                    }
                }
            }
        }

        private void ApplyGravityCheck()
        {
            touchingGround = CheckCollisionY(1, true);
            touchingCeiling = CheckCollisionY(-1, true);
            touchingLeftWall = CheckCollisionX(-1, true);
            touchingRightWall = CheckCollisionX(1, true);

            if (touchingGround && !lastTouchingGround)
            {
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/general/fallblock_impact",
                    BottomCenter);
                StartShaking(0.2f);
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                    RumbleLength.Medium);
                level.DirectionalShake(Vector2.UnitY, 0.3f);
                LandParticles();
            }

            lastTouchingGround = touchingGround;

            if (touchingGround)
            {
                applyGravity = false;
                return;
            }

            if (stickyTop && touchingCeiling)
            {
                applyGravity = false;
                return;
            }

            if (stickyLeft && touchingLeftWall)
            {
                applyGravity = false;
                return;
            }

            if (stickyRight && touchingRightWall)
            {
                applyGravity = false;
                return;
            }

            applyGravity = true;
        }

        private bool CheckCollisionX(float amount, bool ignoreLevelBounds = false)
        {
            if (!ignoreLevelBounds)
            {
                if (Left + amount * engine.DeltaTime < level.Bounds.Left ||
                    Right + amount * engine.DeltaTime > level.Bounds.Right)
                    return true;
            }

            int i = 2;
            while (i < Height)
            {
                if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid",
                        (amount < 0 ? TopLeft : TopRight) + new Vector2(amount * engine.DeltaTime, i)))
                {
                    foreach (OriginalDashSwitchState dSwitch in dashSwitches)
                    {
                        if (SourceEntityCollision.Check(this, dSwitch))
                        {
                            dSwitch.OnDashCollide(null, Vector2.UnitX * Math.Sign(speedX));
                            return false;
                        }
                    }

                    return true;
                }

                i += 4;
            }

            return false;
        }

        private bool CheckCollisionY(float amount, bool ignoreLevelBounds = false)
        {
            if (!ignoreLevelBounds)
            {
                if (amount < 0) //if moving up
                {
                    if (Top < level.Bounds.Top)
                        return true;
                }
            }

            int i = 0;
            while (i < Width)
            {
                if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid",
                        (amount < 0 ? TopLeft : BottomLeft) + new Vector2(i, amount * engine.DeltaTime)) ||
                    SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Platform",
                        (amount < 0 ? TopLeft : BottomLeft) + new Vector2(i, amount * engine.DeltaTime)))
                {
                    return true;
                }

                i += 4;
            }

            if (SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Solid",
                    (amount < 0 ? TopLeft : BottomLeft) + new Vector2(31.5f, amount * engine.DeltaTime)) ||
                SourceSceneCollision.CheckPoint(level.Scheduler.Entities, "Celeste.Platform",
                    (amount < 0 ? TopLeft : BottomLeft) + new Vector2(31.5f, amount * engine.DeltaTime)))
            {
                return true;
            }

            return false;
        }

        private float Approach(float value, float target, float step) => ScalarMath.Approach(value, target, step,
            ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
    }
}

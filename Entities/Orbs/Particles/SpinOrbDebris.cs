using System;
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

namespace Celeste.Mods.CanyonHelper.Entities.Orbs.Particles
{
    [RuntimeTypeAlias("Celeste.Mod.CanyonHelper.SpinOrb+Debris")]
    public sealed class SpinOrbDebris : SourceActorEntityState
    {
        private SpriteImage sprite;
        private Vector2 home;
        private Vector2 speed;

        private bool shaking;
        private bool returning;

        private float returnEase;
        private float returnDuration;
        private SimpleCurve returnCurve;

        private bool firstHit;

        private float alpha;

        private SourceCollisionHandler onCollideH;
        private SourceCollisionHandler onCollideV;
        private float spin;

        private readonly ISourceEngineContext engine;

        public SpinOrbDebris(ISourceEngineContext engine) : base(new PixelActor(null, Vector2.Zero), engine,
            ((ISourcePlatformTargetMotionEnvironment)engine).TargetMotionPolicy)
        {
            this.engine = engine;
            Tags = new Celeste.Scene.Scheduling.SceneTagSet(((IOriginalGameTagsEnvironment)engine).Tags.TransitionUpdate
                .Value);
            Collider = new HitboxColliderGeometry(4f, 4f, -2f, -2f);
            Add(sprite = new SpriteImage(((ISourceRandomEnvironment)engine).Random.Choose(
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.GetAtlasSubtextures(
                    "objects/canyon/spinorb/debris"))));
            sprite.CenterOrigin();
            sprite.FlipX = ((ISourceRandomEnvironment)engine).Random.NextDouble() < 0.5f;
            onCollideH = delegate(PixelCollision c) { speed.X = -speed.X * 0.5f; };
            onCollideV = delegate(PixelCollision c)
            {
                if (firstHit || speed.Y > 50f)
                {
                    ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/general/debris_stone",
                        Position,
                        "debris_velocity", ScalarMath.ClampedMap(speed.Y, 0f, 600f, 0f, 1f));
                }

                if (speed.Y > 0f && speed.Y < 40f)
                {
                    speed.Y = 0f;
                }
                else
                {
                    speed.Y = -speed.Y * 0.25f;
                }

                firstHit = false;
            };
        }

        protected override void OnSquish(PixelCollision data)
        {
        }

        public SpinOrbDebris Init(Vector2 position, Vector2 center, Vector2 returnTo)
        {
            Collidable = true;
            Position = position;
            speed = ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(position - center,
                60f + SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, 60f));
            home = returnTo;
            sprite.Position = Vector2.Zero;
            sprite.Rotation = SourceRandomMath.NextAngle(((ISourceRandomEnvironment)engine).Random);
            returning = false;
            shaking = false;
            sprite.Scale.X = 1f;
            sprite.Scale.Y = 1f;
            sprite.Color = PackedColor.White;
            alpha = 1f;
            firstHit = false;
            spin = SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, 3.49065852f, 10.4719753f) *
                   ((ISourceRandomEnvironment)engine).Random.Choose(1, -1);
            return this;
        }

        public override void Update()
        {
            base.Update();
            if (!returning)
            {
                if (Collidable)
                {
                    speed.X = ScalarMath.Approach(speed.X, 0f, engine.DeltaTime * 100f,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                    if (!OnGround(1))
                    {
                        speed.Y = speed.Y + 400f * engine.DeltaTime;
                    }

                    MoveH(speed.X * engine.DeltaTime, onCollideH);
                    MoveV(speed.Y * engine.DeltaTime, onCollideV);
                }

                if (shaking && ((OriginalLevelSceneRoot)Scene).Clock.OnInterval(0.05f, engine))
                {
                    sprite.X = (-1 + ((ISourceRandomEnvironment)engine).Random.Next(3));
                    sprite.Y = (-1 + ((ISourceRandomEnvironment)engine).Random.Next(3));
                }
            }
            else
            {
                Position = returnCurve.GetPoint(((ISourceEasingEnvironment)engine).EasingArithmetic.CubeOut(returnEase),
                    ((ISourceCurveEnvironment)engine).CurveArithmetic);
                returnEase = ScalarMath.Approach(returnEase, 1f, engine.DeltaTime / returnDuration,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                sprite.Scale = Vector2.One * (1f + returnEase * 0.5f);
            }

            if ((Scene as OriginalLevelSceneRoot).Transitioning)
            {
                alpha = ScalarMath.Approach(alpha, 0f, engine.DeltaTime * 4f,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                sprite.Color = PackedColor.White * alpha;
            }

            sprite.Rotation += spin * ScalarMath.ClampedMap(Math.Abs(speed.Y), 50f, 150f, 0f, 1f) * engine.DeltaTime;
        }

        public void StopMoving()
        {
            Collidable = false;
        }

        public void StartShaking()
        {
            shaking = true;
        }

        public void ReturnHome(float duration)
        {
            if (Scene != null)
            {
                var camera = (base.Scene as OriginalLevelSceneRoot).Camera;
                if (X < camera.X)
                {
                    X = camera.X - 8f;
                }

                if (Y < camera.Y)
                {
                    Y = camera.Y - 8f;
                }

                if (X > camera.X + 320f)
                {
                    X = camera.X + 320f + 8f;
                }

                if (Y > camera.Y + 180f)
                {
                    Y = camera.Y + 180f + 8f;
                }
            }

            returning = true;
            returnEase = 0f;
            returnDuration = duration;
            Vector2 vector = ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(home - Position);
            Vector2 control = (Position + home) / 2f + new Vector2(vector.Y, -vector.X) *
                (SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, 16f) + 16f) *
                (((ISourceRandomEnvironment)engine).Random.Choose(-1, 1));
            returnCurve = new SimpleCurve(Position, home, control);
        }
    }
}

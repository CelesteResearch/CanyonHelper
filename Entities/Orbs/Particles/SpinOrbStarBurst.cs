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
    [RuntimeTypeAlias("Celeste.Mod.CanyonHelper.SpinOrb+StarBurst")]
    public sealed class SpinOrbStarBurst : SourceActorEntityState
    {
        private SpriteImage sprite;
        private Vector2 speed;
        private float spin;
        private float alpha;

        private readonly ISourceEngineContext engine;

        public SpinOrbStarBurst(ISourceEngineContext engine) : base(new PixelActor(null, Vector2.Zero), engine,
            ((ISourcePlatformTargetMotionEnvironment)engine).TargetMotionPolicy)
        {
            this.engine = engine;
            Add(sprite = new SpriteImage(((ISourceRandomEnvironment)engine).Random.Choose(
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.GetAtlasSubtextures(
                    "objects/canyon/spinorb/starBurst"))));
            sprite.CenterOrigin();
            sprite.FlipX = ((ISourceRandomEnvironment)engine).Random.NextDouble() < 0.5f;
        }

        public SpinOrbStarBurst Init(Vector2 position, Vector2 direction)
        {
            Depth = -8499;
            Position = position;
            direction = ((ISourceVectorEnvironment)engine).VectorArithmetic.Normalize(direction);
            speed = direction * SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, 90f, 120f);
            sprite.Position = Vector2.Zero;
            sprite.Rotation = SourceRandomMath.NextAngle(((ISourceRandomEnvironment)engine).Random);
            sprite.Color = PackedColor.White;
            alpha = 1;
            spin = SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, 3.49065852f, 10.4719753f) *
                   ((ISourceRandomEnvironment)engine).Random.Choose(1, -1);
            return this;
        }

        protected override void OnSquish(PixelCollision data)
        {
        }

        public override void Update()
        {
            base.Update();
            sprite.Rotation += spin * engine.DeltaTime;
            Position += speed * engine.DeltaTime;
            alpha -= engine.DeltaTime * 2;
            sprite.Scale.X = 1f;
            sprite.Scale.Y = 1f;
            sprite.Color = PackedColor.White * alpha;
            if (alpha <= 0)
            {
                RemoveSelf();
            }
        }
    }
}

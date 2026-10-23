using System;
using Celeste.Graphics.Rendering.Vertices;
using Celeste.Scene.Tracking.Attributes;
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
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.CanyonHelper.ToggleBlockLaser")]
    public sealed class ToggleBlockLaser : SceneEntityState
    {
        private SourceVertexPositionColor[] fade;
        AtlasEntry laserSprite;
        float angle;
        public PackedColor color;

        public Vector2 start;
        public Vector2 end;

        float timer;
        float sineTimer = 0;

        private readonly ISourceEngineContext engine;

        public ToggleBlockLaser(ISourceEngineContext engine) : base(((ISourceEntitySourceDataEnvironment)engine)
            .EntitySourceDataRegistration)
        {
            this.engine = engine;
            timer = 0;
            fade = new SourceVertexPositionColor[24];

            laserSprite =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas["objects/canyon/toggleblock/laser"];
            Depth = 8999;
            timer = SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random);
        }

        public ToggleBlockLaser Init(Vector2 currentNode, Vector2 nextNode)
        {
            start = currentNode;
            end = nextNode;
            angle = (float)Math.Atan2(nextNode.Y - currentNode.Y, nextNode.X - currentNode.X);
            return this;
        }

        public override void Update()
        {
            base.Update();
            timer += engine.DeltaTime * 4f;
            sineTimer += engine.DeltaTime * 12f;
        }

        public override void Render(ISourceRenderContext context)
        {
            float scale = 0.5f * (0.5f + ((float)Math.Sin(timer) + 1f) * 0.25f);
            //Draw.TextureBannerV(laserSprite, start, Vector2.Zero, new Vector2(((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic.Distance(start, end) / 128f, 1), angle, color * scale, SourceSpriteEffects.None, 5, 2, 1);
            SourceSineTextureDrawing.Horizontal((ISourceDrawScope)context, laserSprite, start, Vector2.Zero,
                new Vector2(
                    ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic.Distance(start, end) /
                    128f, 1.5f), angle, color * scale, SourceSpriteEffects.None, sineTimer, 1, 1, 0.04f);
        }
    }
}

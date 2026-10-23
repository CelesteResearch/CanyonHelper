using System;
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
    [RuntimeTypeAlias("Celeste.Mod.CanyonHelper.ToggleBlockNode")]
    public sealed class ToggleBlockNode : SceneEntityState
    {
        AtlasEntry nodeTexture;
        AtlasEntry nodeCrystalTexture;
        public PackedColor color = PackedColor.Red;
        SineWaveState sine;

        float positionOffset = 0;

        private readonly ISourceEngineContext engine;

        public ToggleBlockNode(ISourceEngineContext engine) : base(((ISourceEntitySourceDataEnvironment)engine)
            .EntitySourceDataRegistration)
        {
            this.engine = engine;
            nodeTexture =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas["objects/canyon/toggleblock/node"];
            nodeCrystalTexture =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[
                    "objects/canyon/toggleblock/nodeCrystal"];
            Depth = 8999;
            Add(sine = new SineWaveState(engine, 0.6f, 0f));
            //sine.Randomize();
        }

        public override void Update()
        {
            base.Update();
            positionOffset = sine.Value - 2;
        }

        public ToggleBlockNode Init(Vector2 position)
        {
            Position = position;
            return this;
        }

        public override void Render(ISourceRenderContext context)
        {
            SourceAtlasDrawing.DrawCentered(context, this, nodeCrystalTexture,
                Position + (Vector2.UnitY * positionOffset),
                PackedColor.White);
            SourceAtlasDrawing.DrawCentered(context, this, nodeTexture, Position + (Vector2.UnitY * positionOffset),
                color);
        }
    }
}

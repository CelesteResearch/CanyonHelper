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
        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            SourceAtlasDrawing.DrawCentered(context, this, texture, Position, PackedColor.White);
            if (stickyTop)
                SourceAtlasDrawing.DrawCentered(context, this, stickyTextureArray[0], Position, PackedColor.White, 1,
                    0);
            if (stickyBottom)
                SourceAtlasDrawing.DrawCentered(context, this, stickyTextureArray[1], Position, PackedColor.White, 1,
                    (float)Math.PI);
            if (stickyLeft)
                SourceAtlasDrawing.DrawCentered(context, this, stickyTextureArray[2], Position, PackedColor.White, 1,
                    -(float)Math.PI / 2f);
            if (stickyRight)
                SourceAtlasDrawing.DrawCentered(context, this, stickyTextureArray[3], Position, PackedColor.White, 1,
                    (float)Math.PI / 2f);
        }
    }
}

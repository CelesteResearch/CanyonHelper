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
        void RecalculateLaserColor()
        {
            for (int i = 0; i < laserCount; i++)
            {
                if (lasers[i].start == nodes[nodeIndex] + new Vector2(Width, Height) / 2 &&
                    lasers[i].end == nodes[GetNextNode(nodeIndex)] + new Vector2(Width, Height) / 2)
                {
                    lasers[i].color = onColor;
                }
                else if (lasers[i].end == nodes[nodeIndex] + new Vector2(Width, Height) / 2 &&
                         lasers[i].start == nodes[GetNextNode(nodeIndex)] + new Vector2(Width, Height) / 2)
                {
                    lasers[i].color = onColor;
                }
                else
                {
                    lasers[i].color = offColor;
                }

                if (stopAtEnd)
                {
                    if (i == laserCount - 1)
                    {
                        lasers[i].color = endColor;
                    }
                }
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            int nextNode;
            if (!oscillate && nodeIndex == nodes.Length - 1)
            {
                nextNode = 0;
            }
            else if (!oscillate)
            {
                nextNode = nodeIndex + 1;
            }
            else
            {
                if (nodeIndex == nodes.Length - 1)
                {
                    nextNode = nodeIndex - 1;
                }
                else if (nodeIndex != 0)
                {
                    nextNode = nodeIndex + (returning ? -1 : 1);
                }
                else
                {
                    nextNode = 1;
                }
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                if (stopAtEnd && i == nodes.Length - 1)
                {
                    nodeTextures[i].color = endColor;
                }
                else if (stopAtEnd && (i == 0 || (oscillate && i == nodes.Length - 2)))
                {
                    nodeTextures[i].color = offColor;
                }
                else
                {
                    if (moving || stopped)
                    {
                        nodeTextures[i].color = offColor;
                    }
                    else
                    {
                        nodeTextures[i].color = nextNode == i ? onColor : offColor;
                    }
                }
            }

            DrawBlockStyle(context, Position + Shake, Width, Height, nineSliceBlock, middleRed, PackedColor.White);
        }

        private void DrawBlockStyle(ISourceRenderContext context, Vector2 pos, float width, float height,
            AtlasEntry[,] ninSlice, Sprite middle, PackedColor color)
        {
            int num = (int)(width / 8f);
            int num2 = (int)(height / 8f);
            SourceAtlasDrawing.Draw(context, this, ninSlice[0, 0], pos + new Vector2(0f, 0f), Vector2.Zero, color);
            SourceAtlasDrawing.Draw(context, this, ninSlice[2, 0], pos + new Vector2(width - 8f, 0f), Vector2.Zero,
                color);
            SourceAtlasDrawing.Draw(context, this, ninSlice[0, 2], pos + new Vector2(0f, height - 8f), Vector2.Zero,
                color);
            SourceAtlasDrawing.Draw(context, this, ninSlice[2, 2], pos + new Vector2(width - 8f, height - 8f),
                Vector2.Zero,
                color);
            for (int i = 1; i < num - 1; i++)
            {
                SourceAtlasDrawing.Draw(context, this, ninSlice[1, 0], pos + new Vector2((float)(i * 8), 0f),
                    Vector2.Zero,
                    color);
                SourceAtlasDrawing.Draw(context, this, ninSlice[1, 2], pos + new Vector2((float)(i * 8), height - 8f),
                    Vector2.Zero, color);
            }

            for (int j = 1; j < num2 - 1; j++)
            {
                SourceAtlasDrawing.Draw(context, this, ninSlice[0, 1], pos + new Vector2(0f, (float)(j * 8)),
                    Vector2.Zero,
                    color);
                SourceAtlasDrawing.Draw(context, this, ninSlice[2, 1], pos + new Vector2(width - 8f, (float)(j * 8)),
                    Vector2.Zero, color);
            }

            for (int k = 1; k < num - 1; k++)
            {
                for (int l = 1; l < num2 - 1; l++)
                {
                    SourceAtlasDrawing.Draw(context, this, ninSlice[1, 1], pos + new Vector2((float)k, (float)l) * 8f,
                        Vector2.Zero, color);
                }
            }

            bool flag = middle != null;
            if (flag)
            {
                middle.Color = color;
                middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
                middle.Render(context);
            }
        }
    }
}

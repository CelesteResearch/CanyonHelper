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
    [RuntimeTypeAlias("Celeste.Mod.CanyonHelper.ToggleSwapBlock")]
    public partial class ToggleSwapBlock : SourceSolidEntityState
    {
        private readonly ISourceEngineContext engine;

        private OriginalLevelSceneRoot level
        {
            get { return (OriginalLevelSceneRoot)base.Scene; }
        }

        private DashListenerState dashListener;

        private ISourceAudioEventReference moveSfx;

        PackedColor offColor = PackedColor.DarkGray;
        PackedColor onColor = PackedColor.MediumPurple;
        PackedColor endColor = new PackedColor(0.65f, 0.4f, 0.4f);

        ToggleBlockLaser[] lasers;
        int laserCount;

        Sprite middleRed;

        float lerp;

        AtlasEntry[,] nineSliceBlock;
        Vector2[] nodes;

        ToggleBlockNode[] nodeTextures;

        int nodeIndex = 0;
        bool moving = false;

        float travelSpeed = 5f;
        bool oscillate = false;
        bool returning = false;
        bool stopAtEnd = false;

        Vector2 dirVector = Vector2.Zero;

        bool stopped = false;

        string customTexturePath;

        public ToggleSwapBlock(EntityData data, Vector2 offset, ISourceSolidWorld world) : base(data.Position + offset,
            data.Width, data.Height, false, world)
        {
            engine = world.EngineContext;
            Depth = -9999;

            nodes = new Vector2[data.Nodes.Length + 1];
            nodes[0] = Position;
            for (int i = 0; i < data.Nodes.Length; i++)
            {
                nodes[i + 1] = data.Nodes[i] + offset;
            }

            travelSpeed = data.Float("travelSpeed", 5f);
            oscillate = data.Bool("oscillate", false);
            stopAtEnd = data.Bool("stopAtEnd", false);
            customTexturePath = data.Attr("customTexturePath", "");

            Add(dashListener = new DashListenerState());
            Add(new LightOccludeState(0.2f));
            Add(middleRed = CanyonHelperModule.For(engine).Sprites.Create("toggleBlockLightRed"));
            dashListener.OnDash = OnPlayerDashed;

            AtlasEntry mtexture;

            if (customTexturePath.Length > 0)
            {
                mtexture = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[customTexturePath];
            }
            else
            {
                mtexture =
                    ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas
                        ["objects/canyon/toggleblock/block1"];
            }

            nineSliceBlock = new AtlasEntry[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    nineSliceBlock[i, j] = mtexture.GetSubtexture(new IntRect(i * 8, j * 8, 8, 8));
                }
            }

            if (oscillate || stopAtEnd || nodes.Length <= 2)
            {
                laserCount = nodes.Length - 1;
            }
            else
            {
                laserCount = nodes.Length;
            }

            lasers = new ToggleBlockLaser[laserCount];
            nodeTextures = new ToggleBlockNode[nodes.Length];
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (nodes.Length >= 2)
            {
                if (nodes.Length != 2)
                {
                    for (int i = 0; i < laserCount; i++)
                    {
                        level.Scheduler.QueueSourceEntity(lasers[i] = engine.EntityPools.Create<ToggleBlockLaser>()
                            .Init(nodes[i] + new Vector2(Width, Height) / 2,
                                nodes[GetNextNode(i)] + new Vector2(Width, Height) / 2));
                    }
                }
                else
                {
                    level.Scheduler.QueueSourceEntity(lasers[0] = engine.EntityPools.Create<ToggleBlockLaser>()
                        .Init(nodes[0] + new Vector2(Width, Height) / 2, nodes[1] + new Vector2(Width, Height) / 2));
                }

                for (int i = 0; i < nodes.Length; i++)
                {
                    level.Scheduler.QueueSourceEntity(nodeTextures[i] = engine.EntityPools.Create<ToggleBlockNode>()
                        .Init(nodes[i] + new Vector2(Width, Height) / 2));
                }

                RecalculateLaserColor();
            }
        }

        int GetNextNode(int node)
        {
            int nextNode;
            if (!oscillate && node == nodes.Length - 1)
            {
                nextNode = 0;
            }
            else if (!oscillate)
            {
                nextNode = node + 1;
            }
            else
            {
                if (node == nodes.Length - 1)
                {
                    nextNode = node - 1;
                }
                else if (node != 0)
                {
                    nextNode = node + (returning ? -1 : 1);
                }
                else
                {
                    nextNode = 1;
                }
            }

            return nextNode;
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            if (moveSfx != null)
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Stop(moveSfx, true);
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            if (moveSfx != null)
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Stop(moveSfx, true);
        }
    }
}

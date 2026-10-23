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
    [RuntimeTypeAlias("Celeste.Mod.CanyonHelper.PushBlock")]
    public partial class PushBlock : SourceSolidEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly AtlasEntry texture;
        private readonly AtlasEntry[] stickyTextures;
        private float speedX = 0;
        private float speedY = 0;
        private OriginalLevelSceneRoot level;
        private RoomPlayerSession playerEnt;

        private readonly bool stickyTop = false;
        private readonly bool stickyBottom = false;
        private readonly bool stickyLeft = false;
        private readonly bool stickyRight = false;

        private bool lastTouchingGround = true;
        private bool touchingGround;
        private bool touchingLeftWall;
        private bool touchingRightWall;
        private bool touchingCeiling;

        private readonly bool isTemple = false;
        private readonly char breakDebrisTileset = '5';

        private float gravity = 0;
        private bool applyGravity = false;

        private const float maxDelay = 0.1f;
        private float delayBetweenImpactEffect = maxDelay;

        private OriginalDashSwitchState[] dashSwitches;

        private readonly AtlasEntry[] stickyTextureArray = new AtlasEntry[4];

        private readonly bool legacy;
        private readonly string blockTexture;
        private readonly string gooTexture;

        public PushBlock(EntityData data, Vector2 offset, ISourceSolidWorld world) : base(data.Position + offset, 32,
            32,
            false, world)
        {
            engine = world.EngineContext;
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            // Added support for custom textures
            string customBlockTexture = data.Attr("customBlockTexture");
            string customGooTexture = data.Attr("customGooTexture");
            blockTexture = string.IsNullOrEmpty(customBlockTexture)
                ? "objects/canyon/pushblock/idle"
                : customBlockTexture;
            gooTexture = string.IsNullOrEmpty(customGooTexture)
                ? "objects/canyon/pushblock/stickyGoo"
                : customGooTexture;

            stickyTextures = atlas.GetAtlasSubtextures(gooTexture).ToArray();
            OnDashCollide = OnDashed;
            Collider.Center = Vector2.Zero;

            stickyTop = data.Bool("stickyTop", false);
            stickyBottom = data.Bool("stickyBottom", false);
            stickyLeft = data.Bool("stickyLeft", false);
            stickyRight = data.Bool("stickyRight", false);

            isTemple = data.Bool("isTemple", false);

            // Added a legacy option which is on for every placement before this update, and off by default for any future placements.
            legacy = data.Bool("legacy", !data.Has("legacy"));

            for (int i = 0; i < stickyTextureArray.Length; i++)
            {
                stickyTextureArray[i] = ((ISourceRandomEnvironment)engine).Random.Choose(stickyTextures);
            }

            string texturePath = blockTexture + (isTemple ? "Temple" : "");
            texture = atlas[texturePath];
            if (isTemple)
            {
                breakDebrisTileset = 'f';
            }

            // Override debris data if enabled.
            if (data.Bool("overrideDebris"))
            {
                breakDebrisTileset = data.Char("customDebrisFromTileset");
            }
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = (OriginalLevelSceneRoot)scene;
            dashSwitches = level.Scheduler.Entities.OfType<OriginalDashSwitchState>().ToArray();
        }

        private DashCollisionResult OnDashed(RoomPlayerSession player, Vector2 direction)
        {
            DashCollisionResult result;
            if (player.Left + 1 > Right || player.Right - 1 < Left) //If not on top/bottom
            {
                if (OnDashed(player.Center))
                {
                    result = DashCollisionResult.Rebound;
                    level.Session.IncrementCounter("pushBlocksHit");
                }
                else
                {
                    result = DashCollisionResult.NormalCollision;
                }
            }
            else
            {
                if (OnDashedTop(player.Center))
                {
                    result = DashCollisionResult.Rebound;
                    level.Session.IncrementCounter("pushBlocksHit");
                }
                else
                {
                    result = DashCollisionResult.NormalCollision;
                }
            }

            playerEnt = player;
            return result;
        }
    }
}

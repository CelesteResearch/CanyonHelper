using System;
using Celeste.Mods.CanyonHelper.Entities.Orbs.Particles;
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

namespace Celeste.Mods.CanyonHelper.Entities.Orbs
{
    [RuntimeTypeAlias("Celeste.Mod.CanyonHelper.SpinOrb")]
    public partial class SpinOrb : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private DashListenerState dashListener;
        private ISourceAudioEventReference moveSfx;
        private bool playerInOrb = false;
        private VertexLightState light;
        private Sprite sprite;
        private SceneEntityState outline;

        private const float normalRotateSpeed = ((float)Math.PI) * 1.5f;
        private const float fastRotateSpeed = ((float)Math.PI) * 3f;
        private const float slowRotateSpeed = ((float)Math.PI) * 0.75f;
        private float currentRotateSpeed = normalRotateSpeed;
        private const float launchSpeed = 350f;
        private const float maxUseDelay = 1.5f;
        private const float spriteRotationOffset = ((float)Math.PI / 2);

        // if set to false, rotate clockwise (default)
        private bool rotateCounterclockwise;

        List<SpinOrbDebris> debris = new List<SpinOrbDebris>();
        private bool debrisShaken = false;
        private bool debrisReturned = false; //Size 10 & fix stars not spawning in correct area

        private float initialAngle;
        private float currentRotateAngle;
        private float useDelay = 0;

        private RoomPlayerSession playerEntity;

        private BirdTutorialGuiState tutorialGui;
        bool shouldShowTutorial = false;

        ParticleTypePresentation SpinOrbGlowParticles = new ParticleTypePresentation
        {
            LifeMin = 0.4f,
            LifeMax = 0.6f,
            Size = 1f,
            SizeRange = 0f,
            DirectionRange = 6.28318548f,
            SpeedMin = 12f,
            SpeedMax = 18f,
            FadeMode = ParticleTypePresentation.FadeModes.Late,
            Color = SourceHexColor.HexToColor("c16de0"),
            Color2 = SourceHexColor.HexToColor("bc5ce0"),
            ColorMode = ParticleTypePresentation.ColorModes.Blink
        };

        public SpinOrb(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            rotateCounterclockwise = data.Bool("rotateCounterclockwise", false);
            // in degrees, 0° = right
            float initialRotateAngleDegrees = data.Float("initialAngle", 270);
            // convert to radians
            initialAngle = initialRotateAngleDegrees * (((float)Math.PI) / 180f);
            string spritePath = data.Attr("spritePath", "");

            Depth = -8500;
            Collider = new CircleColliderGeometry(((ISourceVectorEnvironment)engine).VectorArithmetic, 8.5f, -0.5f, 0f);
            Add(new PlayerColliderComponentState(OnPlayer, null, null));
            Add(light = new VertexLightState(engine, PackedColor.White, 1f, 16, 32));

            // backwards compat in case someone used sprites.xml to overwrite sprites previously
            if (spritePath.Equals(""))
            {
                Add(sprite = CanyonHelperModule.For(engine).Sprites.Create("spinorb"));
            }
            else
            {
                if (!spritePath.EndsWith("/")) spritePath += "/";
                Add(sprite = new Sprite(engine, ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas,
                    spritePath));
                sprite.Justify = new Vector2(0.5f, 0.5f);
                sprite.Add("idle", "idle", 0.2f, 0);
                sprite.Add("playerenter", "enter", 0.2f, new Chooser<string>("active"), 0, 1, 2);
                sprite.Add("active", "active", 0.2f, 0, 1, 2);
                sprite.Play("idle");
            }

            sprite.Rotation = initialAngle + spriteRotationOffset;

            Add(dashListener = new DashListenerState());
            dashListener.OnDash = OnPlayerDashed;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            //outline
            SpriteImage image =
                new SpriteImage(
                    ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas["objects/canyon/spinorb/outline"]);
            image.CenterOrigin();
            image.Color = PackedColor.White * 0.75f;
            outline = new SceneEntityState(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                Position);
            outline.Depth = 8999;
            outline.Visible = false;
            outline.Add(image);
            ((OriginalLevelSceneRoot)scene).Scheduler.QueueSourceEntity(outline);
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            if (moveSfx != null)
            {
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Stop(moveSfx, true);
            }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if ((Scene as OriginalLevelSceneRoot).Session.Level == "a-09")
            {
                tutorialGui = new BirdTutorialGuiState(engine, ((IOriginalGameTagsEnvironment)engine).Tags.HUD.Value,
                    ((IOriginalBirdProcess)engine).BirdTutorialResources, this, new Vector2(0f, -24f),
                    ((IOriginalFancyTextProcess)engine).Text.Dialog.Clean("ec_tutorial_speed"), new object[]
                    {
                        ((IOriginalFancyTextProcess)engine).Text.Dialog.Clean("tutorial_hold"),
                        BirdTutorialButton.Grab,
                        ((IOriginalFancyTextProcess)engine).Text.Dialog.Clean("tutorial_hold"),
                        BirdTutorialButton.Jump
                    });
                tutorialGui.Open = false;
                shouldShowTutorial = false;
                ((OriginalLevelSceneRoot)Scene).Scheduler.QueueSourceEntity(tutorialGui);
            }
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            if (!playerInOrb && useDelay <= 0)
            {
                shouldShowTutorial = true;
                sprite.Play("playerenter", false);
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/char/badeline/booster_begin",
                    Position);
                moveSfx = ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                    "event:/game/04_cliffside/arrowblock_move", Center);
                OriginalLevelSceneRoot level = Scene as OriginalLevelSceneRoot;
                level.Displacement.AddBurst(Center, 0.35f, 8f, 48f, 0.25f, null, null);
                level.Particles.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.Player.P_Split, 16, Center,
                    Vector2.One * 6f);
                useDelay = maxUseDelay;
                playerInOrb = true;
                player.Speed = Vector2.Zero;
                player.RefillDash();
                player.RefillStamina();
                currentRotateAngle = initialAngle; //in radians
                playerEntity = player;
            }
        }
    }
}

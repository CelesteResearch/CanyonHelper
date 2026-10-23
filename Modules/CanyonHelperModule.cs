using System;
using System.Numerics;
using System.Xml;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Registry;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Runtime.Events;
using Celeste.Everest.Runtime.Events.Player;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Doors.TempleGates;
using Celeste.Graphics.Content;
using Celeste.Mods.CanyonHelper.Entities.Blocks;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Presentation.Scene.Transitions;

namespace Celeste.Mods.CanyonHelper.Modules
{
    public sealed partial class CanyonHelperModule(ISourceEngineContext engine) : SourceEverestModule(engine)
    {
        private int pushblockCounter;
        public SpriteBank Sprites { get; private set; }

        public override void Load()
        {
            LoadAdditionalFamilies();
            ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadEntity += OnLoadEntity;
            ((IEverestGameEventsEnvironment)SourceEngine).GameEvents.OnLoadLevel += OnLoadLevel;
            EverestPlayerSourceEvents.OnDie += OnPlayerDie;
            EverestPlayerSourceEvents.OnSpawn += OnPlayerSpawn;
        }

        public override void Unload()
        {
            UnloadAdditionalFamilies();
            ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadEntity -= OnLoadEntity;
            ((IEverestGameEventsEnvironment)SourceEngine).GameEvents.OnLoadLevel -= OnLoadLevel;
            EverestPlayerSourceEvents.OnDie -= OnPlayerDie;
            EverestPlayerSourceEvents.OnSpawn -= OnPlayerSpawn;
        }

        public override void LoadContent(bool firstLoad)
        {
            var xml = new XmlDocument();
            using (var stream = ((IEverestContentRegistryEnvironment)SourceEngine).SourceContentRegistry
                   .Get("Graphics/CanyonSprites").Stream)
                xml.Load(stream);
            Sprites = new SpriteBank(SourceEngine,
                ((ISourceGameGraphicsEnvironment)SourceEngine).GameGraphics.GameAtlas, xml);
        }

        private bool Owns(OriginalLevelSceneRoot level) => ReferenceEquals(level.EngineContext, SourceEngine);

        private void OnPlayerDie(RoomPlayerSession player)
        {
            if (player.Scene is OriginalLevelSceneRoot level && Owns(level))
                level.Session.SetCounter("pushBlocksHit", pushblockCounter);
        }

        private void OnPlayerSpawn(RoomPlayerSession player)
        {
            if (player.Scene is OriginalLevelSceneRoot level && Owns(level))
                pushblockCounter = level.Session.GetCounter("pushBlocksHit");
        }

        private void OnLoadLevel(OriginalLevelSceneRoot level, PlayerIntroType playerIntro, bool isFromLoader)
        {
            if (!Owns(level) || level.Session.AreaSID != "exudias/4/EnchantedCanyon") return;
            pushblockCounter = level.Session.GetCounter("pushBlocksHit");
            if (level.Session.Level == "c-03" && level.Session.GetFlag("canyonHeartActive"))
                level.Scheduler.Entities.GetTrackedEntity<OriginalTempleGateState>("Celeste.TempleGate").RemoveSelf();
            var area = (OriginalAreaData)((IOriginalAreaKeyEnvironment)SourceEngine).AreaKeys
                .GetAreaBySID("exudias/4/EnchantedCanyon");
            if (level.Session.Level.StartsWith("c-") || level.Session.Level.StartsWith("b-"))
                area.Wipe = (scene, wipeIn, onComplete) => _ = new DropWipePresentation(scene, wipeIn, onComplete);
            else
                area.Wipe = (scene, wipeIn, onComplete) => _ = new WindWipePresentation(scene, wipeIn, onComplete);
        }

        private bool OnLoadEntity(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
        {
            if (!Owns(level) || data.Name != "canyon/pushblock") return false;
            level.Scheduler.QueueSourceEntity(new PushBlock(data, offset,
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, false)));
            return true;
        }
    }
}

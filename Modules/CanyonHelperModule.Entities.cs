using System;
using Celeste.Mods.CanyonHelper.Entities.Npcs;
using Celeste.Mods.CanyonHelper.Entities.Orbs;
using Celeste.Mods.CanyonHelper.Entities.Orbs.Particles;
using Celeste.Core.Maps;
using System.Linq;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.CanyonHelper.Entities.Blocks;
using Celeste.Simulation.Gameplay.Platforms.Collision;

namespace Celeste.Mods.CanyonHelper.Modules
{
    public sealed partial class CanyonHelperModule
    {
        private IDisposable laserPool;
        private IDisposable nodePool;
        private IDisposable starPool;
        private IDisposable debrisPool;

        public static CanyonHelperModule For(ISourceEngineContext engine) =>
            ((ISourceEverestModulesEnvironment)engine).Modules.OfType<CanyonHelperModule>().Single();

        private void LoadAdditionalFamilies()
        {
            laserPool = SourceEngine.EntityPools.RegisterScoped(() => new ToggleBlockLaser(SourceEngine));
            nodePool = SourceEngine.EntityPools.RegisterScoped(() => new ToggleBlockNode(SourceEngine));
            starPool = SourceEngine.EntityPools.RegisterScoped(() => new SpinOrbStarBurst(SourceEngine));
            debrisPool = SourceEngine.EntityPools.RegisterScoped(() => new SpinOrbDebris(SourceEngine));
            ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadEntity += OnLoadAdditionalEntity;
        }

        private void UnloadAdditionalFamilies()
        {
            ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadEntity -= OnLoadAdditionalEntity;
            debrisPool.Dispose();
            starPool.Dispose();
            nodePool.Dispose();
            laserPool.Dispose();
        }

        private bool OnLoadAdditionalEntity(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
        {
            if (!Owns(level)) return false;
            switch (data.Name)
            {
                case "canyon/toggleblock":
                    level.Scheduler.QueueSourceEntity(new ToggleSwapBlock(data, offset,
                        ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset,
                            false)));
                    return true;
                case "canyon/spinorb":
                    level.Scheduler.QueueSourceEntity(new SpinOrb(SourceEngine, data, offset));
                    return true;
                case "canyon/grannystart":
                    level.Scheduler.QueueSourceEntity(new GrannyStart(SourceEngine, data, offset,
                        new MapEntityId(room.Name, data.ID)));
                    return true;
                default:
                    return false;
            }
        }
    }
}

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Maps;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Entities.Npcs;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Particles;
using Celeste.Game.UI.Dialogue.Textboxes;
using Celeste.Graphics.Content;
using Celeste.Simulation;

namespace Celeste.Mods.CanyonHelper.Entities.Npcs
{
    [RuntimeTypeAlias("Celeste.Mod.CanyonHelper.GrannyStart")]
    public sealed class GrannyStart : OriginalNpcState
    {
        private static readonly string sourcePlayerRegistryKey =
            typeof(global::Celeste.Simulation.RoomPlayerSession).FullName;

        private MapEntityId id;

        private string dialog1;
        private string dialog2;

        private Vector2 scale = new Vector2(1, 1);
        private Vector2 indicatorOffset = Vector2.Zero;

        private SimulationCoroutine talkRoutine;

        public GrannyStart(ISourceEngineContext engine, EntityData data, Vector2 offset, MapEntityId id) : base(engine,
            data.Position + offset)
        {
            this.id = id;
            dialog1 = "EC_A_GRANNY_01";
            dialog2 = "EC_A_GRANNY_02";

            Add(Sprite = ((ISourceGameGraphicsEnvironment)Engine).GameGraphics.SpriteFactory.Create("granny"));
            Sprite.Scale.X = -1;
            Sprite.Play("idle");
        }

        public override void Added(object scene)
        {
            base.Added(scene);

            if ((scene as OriginalLevelSceneRoot).Session.GetFlag("DoNotTalk" + id))
                RemoveSelf();
        }

        public override void Update()
        {
            base.Update();

            RoomPlayerSession player =
                Level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(sourcePlayerRegistryKey);
            if (player != null)
            {
                if ((Scene as OriginalLevelSceneRoot).Session.GetFlag("canyonLevelStart"))
                {
                    Level.StartCutscene(OnTalkEnd);
                    if (talkRoutine == null)
                        Add(talkRoutine = new SimulationCoroutine(Engine, Talk(player)));
                }
            }
        }

        private void OnTalkEnd(OriginalLevelSceneRoot level)
        {
            RoomPlayerSession player =
                Level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(sourcePlayerRegistryKey);
            if (player != null)
            {
                player.Machine.Locked = false;
                player.Machine.State = 0;
            }

            (Scene as OriginalLevelSceneRoot).Session.SetFlag("canyonLevelStart", false);
            player.X = (float)Math.Round(Position.X - 16);
            player.Y = (float)Math.Round(Position.Y);
            (Scene as OriginalLevelSceneRoot).Session.SetFlag("DoNotTalk" + id);
            talkRoutine.Cancel();
            talkRoutine.RemoveSelf();
        }

        private IEnumerator Talk(RoomPlayerSession player)
        {
            yield return player.DummyWalkToExact(42 + (int)Level.LevelOffset.X);
            PlaySourceSound("event:/char/madeline/jump", player.BottomCenter);
            OriginalDust.Burst(Engine, player.BottomCenter, (float)-Math.PI / 2, 4, Process.ParticleTypes.Dust);
            player.Data.Motion.Normal.Speed.Y = -180f;
            yield return player.DummyWalkTo((int)Position.X - 52);
            yield return TextboxState.Say(dialog1, Engine);
            PlaySourceSound("event:/char/madeline/jump", player.BottomCenter);
            OriginalDust.Burst(Engine, player.BottomCenter, (float)-Math.PI / 2, 4, Process.ParticleTypes.Dust);
            player.Data.Motion.Normal.Speed.Y = -200f;
            yield return player.DummyWalkToExact((int)Position.X - 16, false, 1.2f);
            yield return TextboxState.Say(dialog2, Engine);
            Level.EndCutscene();
            OnTalkEnd(Level);
        }

        private IEnumerator JumpForSec(float time, RoomPlayerSession player)
        {
            float counter = 0;
            while (counter < time)
            {
                counter++;
                player.Jump();
                yield return null;
            }
        }
    }
}

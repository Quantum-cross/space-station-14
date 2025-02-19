using Content.Server.Chat.Commands;
using Content.Server.Chat.Systems;
using Content.Server.Destructible;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using ClientChatManager = Content.Client.Chat.Managers;
using ServerChatManager = Content.Server.Chat.Managers;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.PowerCell.Components;
using Robust.Client.Console;
using Robust.Server.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Speech.Accents
{
    [TestFixture]
    [TestOf(typeof(DamagedSiliconAccentComponent))]
    public sealed class DamagedSiliconAccentTest : RobustIntegrationTest
    {
        [Test]
        public async Task Test()
        {

            var originalMsg = "The quick brown fox jumps over the lazy dog";

            await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
            var server = pair.Server;
            var client = pair.Client;

            await server.WaitIdleAsync();


            var sLogger = server.ResolveDependency<ILogManager>().RootSawmill;
            var cLogger = server.ResolveDependency<ILogManager>().RootSawmill;


            EntityUid borg = default!;
            DamagedSiliconAccentSystem accentSys = null;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            // var mapManager = server.ResolveDependency<IMapManager>();
            // var clientChatManager = client.ResolveDependency<ClientChatManager.IChatManager>();
            // var serverChatManager = server.ResolveDependency<ServerChatManager.IChatManager>();
            var clientConsole = client.ResolveDependency<IClientConsoleHost>();
            DamageableSystem damageableSystem = default!;
            PowerCellSystem powerCellSystem = default!;
            BatterySystem batterySystem = default!;

            clientConsole.RegisterCommand(new SayCommand());

            var map = await pair.CreateTestMap();

            // Get player data
            var sPlayerMan = server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
            // var cPlayerMan = client.ResolveDependency<Robust.Client.Player.IPlayerManager>();
            if (client.Session == null)
                Assert.Fail("No player");
            var clientSession = client.Session!;
            var serverSession = sPlayerMan.GetSessionById(clientSession.UserId);

            DamagedSiliconAccentComponent accent = default!;

            await server.WaitIdleAsync();


            await server.WaitPost(() =>
            {
                damageableSystem = entitySystemManager.GetEntitySystem<DamageableSystem>();
                powerCellSystem = entitySystemManager.GetEntitySystem<PowerCellSystem>();
                batterySystem = entitySystemManager.GetEntitySystem<BatterySystem>();
            });

            await server.WaitIdleAsync();

            // var sChatSystem = server.ResolveDependency<Server.Chat.Systems.ChatSystem>();

            await server.WaitPost(() =>
            {
                var coordinates = map.MapCoords;
                // accentSys = entityManager.System<BrokenSiliconAccentSystem>();
                borg = entityManager.SpawnEntity("PlayerBorgBattery", coordinates);
                entityManager.RemoveComponent<DestructibleComponent>(borg);
                entityManager.RemoveComponent<MobThresholdsComponent>(borg);
                server.PlayerMan.SetAttachedEntity(serverSession, borg);
                Assert.That(entityManager.TryGetComponent(borg, out accent!));
            });


            await pair.RunTicksSync(5);

            for (var c = .15; c >= 0.0; c -= 0.03)
            {
                for (var i = 0; i <= 300; i += 25)
                {
                    var ev = new TransformSpeechEvent(borg, originalMsg);
                    await server.WaitPost(() =>
                    {
                        entityManager.TryGetComponent<PowerCellSlotComponent>(borg, out var powerCellSlotComp);
                        powerCellSystem.TryGetBatteryFromSlot(borg,
                            out var batteryEnt,
                            out var battery,
                            powerCellSlotComp);
                        batterySystem.SetCharge(batteryEnt!.Value, (float)(battery!.MaxCharge * c));
                        var damage = new DamageSpecifier();
                        damage.DamageDict.Add("Blunt", i);
                        entityManager.TryGetComponent<DamageableComponent>(borg, out var damageable);
                        damageableSystem.SetDamage(borg, damageable!, damage);
                        entityManager.EventBus.RaiseLocalEvent(borg, ev, true);
                    });

                    await server.WaitIdleAsync();
                    cLogger.Info($"D: {i}\tC: {c}\t'{ev.Message}'");
                }
            }


            await pair.CleanReturnAsync();
        }
    }
}

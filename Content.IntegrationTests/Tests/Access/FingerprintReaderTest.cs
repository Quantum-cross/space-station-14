using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.FingerprintReader;
using Content.Shared.Forensics.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Access
{
    [TestFixture]
    [TestOf(typeof(FingerprintReaderComponent))]
    public sealed class FingerprintReaderTest : InteractionTest
    {
        protected override string PlayerPrototype => "InteractionTestMob2";

        [TestPrototypes]
        private const string Prototypes = """
            - type: entity
              id: InteractionTestMob2
              parent: InteractionTestMob
              components:
              - type: Fingerprint

            - type: entity
              parent: BaseItem
              id: FingerprintItemDummy
              components:
              - type: FingerprintReader

            """;

        private Entity<FingerprintReaderComponent>? _sReader = null!;
        private Entity<FingerprintReaderComponent>? _cReader = null!;
        private string _playerFingerprint;

        [SetUp]
        public override async Task Setup()
        {
            await base.Setup();

            await SpawnTarget("FingerprintItemDummy");
            await RunTicks(1);

            Assert.That(TryComp<FingerprintReaderComponent>(out var sReadercomp), "No reader component");
            Assert.That(TryComp<FingerprintComponent>(Player, out var fingerprint),
                "Player has no fingerprint component");
            Assert.That(fingerprint?.Fingerprint, Is.Not.Null, "Player has no fingerprint");

            _playerFingerprint = fingerprint.Fingerprint;

            await Client.WaitAssertion(() =>
            {
                TryComp<FingerprintReaderComponent>(Target.Value, out var cReadercomp);
                _cReader = new Entity<FingerprintReaderComponent>(CTarget.Value, cReadercomp);
            });
            _sReader = new Entity<FingerprintReaderComponent>(STarget.Value, sReadercomp);

            await RunTicks(3);
        }

        [Test]
        public async Task TestFingerprintAccess()
        {
            var sFingerReaderSystem = SEntMan.System<FingerprintReaderSystem>();
            var cFingerReaderSystem = CEntMan.System<FingerprintReaderSystem>();

            // Check that the player is allowed with no fingerprints registered
            Assert.That(cFingerReaderSystem.IsAllowed(_cReader.Value, CPlayer),
                "No fingerprints allowed, but player denied");

            // Add a fingerprint
            sFingerReaderSystem.AddAllowedFingerprint(_sReader.Value, "ABC123");

            await RunTicks(3);

            // Check that the player is denied
            Assert.That(!cFingerReaderSystem.IsAllowed(_cReader.Value, CPlayer),
                "Player is allowed without fingerprint listed");

            // Add the player's fingerprint
            sFingerReaderSystem.AddAllowedFingerprint(_sReader.Value, _playerFingerprint);

            await RunTicks(3);

            // Check that the player is allowed
            Assert.That(cFingerReaderSystem.IsAllowed(_cReader.Value, CPlayer),
                "Player is NOT allowed with correct fingerprint");

            sFingerReaderSystem.RemoveAllowedFingerprint(_sReader.Value, "ABC123");

            // Check that the player is still allowed
            Assert.That(cFingerReaderSystem.IsAllowed(_cReader.Value, CPlayer),
                "Player is NOT allowed with correct fingerprint");

        }

        [Test]
        [Ignore("Designing a test case to pass in a future commit")]
        public async Task TestHidingFingerprintsToClient()
        {
            var sFingerReaderSystem = SEntMan.System<FingerprintReaderSystem>();
            var cFingerReaderSystem = CEntMan.System<FingerprintReaderSystem>();

            // Make sure that the client entity doesn't reflects the server entity with empty fingerprints
            Assert.That(_cReader.Value.Comp.AllowedFingerprints, Is.Empty,
                "Client got fingerprints when none are allowed");

            // Add some fingerprints
            sFingerReaderSystem.AddAllowedFingerprint(_sReader.Value, "ABC123");
            sFingerReaderSystem.AddAllowedFingerprint(_sReader.Value, "XYZ789");

            await RunTicks(3);

            Assert.That(_sReader.Value.Comp.AllowedFingerprints.Count, Is.EqualTo(2),
                "Server fingerprint reader should have 2 fingerprints");
            Assert.That(_cReader.Value.Comp.AllowedFingerprints, Is.Empty,
                "Client got other player fingerprints when it shouldn't have");

            // Add the player's fingerprint
            sFingerReaderSystem.AddAllowedFingerprint(_sReader.Value, _playerFingerprint);

            await RunTicks(3);

            Assert.That(_sReader.Value.Comp.AllowedFingerprints.Count, Is.EqualTo(2),
                "Server fingerprint reader should have 2 fingerprints");
            Assert.That(_cReader.Value.Comp.AllowedFingerprints.Count, Is.EqualTo(1),
                "Client got incorrect number of fingerprints with player fingerprint in allow list");
            Assert.That(cFingerReaderSystem.IsAllowed(_cReader.Value, CPlayer),
                "Client does not see player as allowed");

            // Spawn a second mob with a fingerprint
            var sMob2Uid = await SpawnEntity("InteractionTestMob2", new EntityCoordinates(SPlayer, 1, 0));
            var cMob2Uid = ToClient(sMob2Uid);
            var nMob2Uid = SEntMan.GetNetEntity(sMob2Uid);

            // Make sure the mob has a fingerprint that is different to the player mob
            Assert.That(TryComp<FingerprintComponent>(nMob2Uid, out var sMob2FingerprintComp),
                "Second mob doesn't have fingerprint component");
            Assert.That(sMob2FingerprintComp.Fingerprint, Is.Not.Null,
                "Second mob doesn't have fingerprint");
            var mob2Fingerprint = sMob2FingerprintComp.Fingerprint;
            Assert.That(mob2Fingerprint, Is.Not.EqualTo(_playerFingerprint),
                "Second mob fingerprint is equal to the player fingerprint!");

            await RunTicks(3);

            // Attach to the second mob
            Server.PlayerMan.SetAttachedEntity(ServerSession, sMob2Uid);

            await RunTicks(5);

            Assert.That(Client.PlayerMan.LocalEntity, Is.EqualTo(sMob2Uid),
                "Client is not attached to the second mob");

            Assert.That(_cReader.Value.Comp.AllowedFingerprints, Is.Empty,
                "Client should not have any fingerprints after attach");
        }
    }
}

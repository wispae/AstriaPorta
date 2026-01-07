using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using AstriaPorta.Content;
using AstriaPorta.Util;
using Vintagestory.Common;

namespace AstriaPorta
{
    public class AstriaPortaModSystem : ModSystem
    {
        private GateDiagnostics diagnostics;

        public int eventHorizonShaderProgramRef;
        public int animatedTextureShaderProgramRef;
        public int eventHorizonTexRef;
        public int animatedTexRef;
        public LoadedTexture eventHorizonTex;
        public TextureAtlasPosition eventHorizonTexPos;
        public TextureAtlasPosition animatedTexPos;
        public IShaderProgram eventHorizonShaderProgram;
        public IShaderProgram animatedTextureShaderProgram;
        private ICoreClientAPI capi;
        private ICoreServerAPI sapi;
        private MeshData horizonMesh;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }

        public override double ExecuteOrder()
        {
            return 0.36d;
        }

        public override void Dispose()
        {
            StargateMeshHelper.Dispose();
        }

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            GateLogger.Initialize(Mod.Logger);

            ClassRegistry.legacyBlockEntityClassNames.TryAdd("BEStargate", "BEStargateMilkyway");
            RegisterBlocks(api);
            RegisterItems(api);

            StargateVolumeManager.Initialize();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;

            RegisterCommands(api);
#if DEBUG
            Mod.Logger.Debug("Started server-side modsystem");
#endif
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;

            api.Event.BlockTexturesLoaded += OnClientAssetsLoaded;
        }

        private void OnClientAssetsLoaded()
        {
            capi.Event.ReloadTextures += CreateExternalTextures;
            capi.Event.ReloadShader += RegisterShaderPrograms;

            InitializeConfigurations(capi);

            CreateExternalTextures();
            RegisterShaderPrograms();

            InitializeMeshes(capi);
        }

        private void CreateExternalTextures()
        {
            AssetLocation horizonTexLocation = new AssetLocation("astriaporta", "block/gates/vortex");
            bool success = capi.BlockTextureAtlas.GetOrInsertTexture(horizonTexLocation, out eventHorizonTexRef, out eventHorizonTexPos);
        }

        private bool RegisterShaderPrograms()
        {
            eventHorizonShaderProgram = capi.Shader.NewShaderProgram();
            eventHorizonShaderProgram.AssetDomain = "astriaporta";
            eventHorizonShaderProgram.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
            eventHorizonShaderProgram.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
            eventHorizonShaderProgram.ClampTexturesToEdge = true;

            eventHorizonShaderProgramRef = capi.Shader.RegisterFileShaderProgram("eventhorizon", eventHorizonShaderProgram);

#if DEBUG
            capi.Logger.Notification("Loaded Shaderprogram for event horizon.");
#endif

            return eventHorizonShaderProgram.Compile();
        }

        private void RegisterItems(ICoreAPI api)
        {
            api.RegisterCollectibleBehaviorClass("GateAddressHolder", typeof(BehaviorGateAddressHolder));
            api.RegisterItemClass("ItemCartoucheLocator", typeof(ItemCartoucheLocator));
            api.RegisterItemClass("ItemStargateDebugTablet", typeof(ItemStargateDebugTablet));
        }

        private void RegisterBlocks(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockRandomizerOrientable", typeof(BlockRandomizerOrientable));
            api.RegisterBlockClass("BlockStargate", typeof(BlockStargate));
            api.RegisterBlockClass("BlockDialHomeDevice", typeof(BlockDialHomeDevice));
            api.RegisterBlockClass("BlockMultiblockStargate", typeof(BlockMultiblockStargate));

            api.RegisterBlockEntityClass("BERandomizerOrientable", typeof(BlockEntityBlockRandomizerOrientable));
            // api.RegisterBlockEntityClass("BEStargate", typeof(BlockEntityStargate));
            api.RegisterBlockEntityClass("BEStargateMilkyway", typeof(BlockEntityStargateMilkyway));
            api.RegisterBlockEntityClass("BEStargatePegasus", typeof(BlockEntityStargatePegasus));
            api.RegisterBlockEntityClass("BEDialHomeDevice", typeof(BlockEntityDialHomeDevice));

            api.RegisterBlockBehaviorClass("MultiblockStargate", typeof(BlockBehaviorMultiblockStargate));
        }

        private void InitializeConfigurations(ICoreAPI api)
        {
            var baseSoundConfig = new StargateSoundLocationConfiguration
            {
                ActiveSoundLocation = new("sounds/environment/underwater.ogg"),
                RotateSoundLocation = new("sounds/block/quern.ogg"),
            };
            var milkywayConfig = InitializeSoundConfiguration(api, baseSoundConfig, "astriaporta:stargate-milkyway-north");
            var pegasusConfig = InitializeSoundConfiguration(api, baseSoundConfig, "astriaporta:stargate-pegasus-north");

            StargateSoundManagerClient.InitializeLocations(EnumStargateType.Milkyway, milkywayConfig);
            StargateSoundManagerClient.InitializeLocations(EnumStargateType.Pegasus, pegasusConfig);
        }

        private StargateSoundLocationConfiguration InitializeSoundConfiguration(ICoreAPI api, StargateSoundLocationConfiguration baseConfig, AssetLocation fromBlock)
        {
            var gateBlock = api.World.GetBlock(fromBlock);
            if (gateBlock == null) return baseConfig;

            var soundConfig = gateBlock.Attributes["gateSounds"].AsObject<StargateSoundLocationConfiguration>();
            if (soundConfig == null) return baseConfig;

            soundConfig.ActiveSoundLocation ??= baseConfig.ActiveSoundLocation;
            soundConfig.BreakSoundLocation ??= baseConfig.BreakSoundLocation;
            soundConfig.EnterSoundLocation ??= baseConfig.EnterSoundLocation;
            soundConfig.FailSoundLocation ??= baseConfig.FailSoundLocation;
            soundConfig.LockSoundLocation ??= baseConfig.LockSoundLocation;
            soundConfig.ReleaseSoundLocation ??= baseConfig.ReleaseSoundLocation;
            soundConfig.RotateSoundLocation ??= baseConfig.RotateSoundLocation;
            soundConfig.RotateStartSoundLocation ??= baseConfig.RotateStartSoundLocation;
            soundConfig.VortexSoundLocation ??= baseConfig.VortexSoundLocation;
            soundConfig.WarningSoundLocation ??= baseConfig.WarningSoundLocation;
            soundConfig.VortexSoundDelay ??= baseConfig.VortexSoundDelay;

            return soundConfig;
        }

        private void InitializeMeshes(ICoreClientAPI api)
        {
            StargateMeshHelper.Initialize(api);
        }

        private void RegisterCommands(ICoreServerAPI api)
        {
            diagnostics = new GateDiagnostics(api);
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;

            api.ChatCommands.Create("sectoraddress")
                .RequiresPrivilege(Privilege.gamemode)
                .WithDescription("Calculates the local sector gate address")
                .RequiresPlayer()
                .HandleWith(diagnostics.CalculateNearestAddress);

            api.ChatCommands.Create("nearestgate")
                .RequiresPrivilege(Privilege.gamemode)
                .WithDescription("Get the address of the nearest gate")
                .RequiresPlayer()
                .HandleWith(diagnostics.RetrieveClosestGate);

            api.ChatCommands.Create("gatelist")
                .RequiresPrivilege(Privilege.gamemode)
                .WithDescription("Retrieve list of known gates")
                .WithArgs(parsers.OptionalInt("page"))
                .HandleWith(diagnostics.DisplayGateList);

#if DEBUG
			api.ChatCommands.Create("addresstest")
				.RequiresPrivilege(Privilege.chat)
				.WithArgs(parsers.OptionalInt("address"))
				.HandleWith(diagnostics.RunGateAddressTests);

			api.ChatCommands.Create("targetglyph")
				.RequiresPrivilege(Privilege.chat)
				.WithArgs(parsers.Int("glyph"))
				.HandleWith(diagnostics.SeekActiveGlyph);

			api.ChatCommands.Create("manualdial")
				.RequiresPrivilege(Privilege.chat)
				.WithArgs(parsers.Word("address"))
				.HandleWith(diagnostics.DialAnimationManually);
#endif
        }
    }
}

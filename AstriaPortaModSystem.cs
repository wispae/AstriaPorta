using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using AstriaPorta.Content;
using AstriaPorta.Util;
using AstriaPorta.src.Block;
using Vintagestory.API.Util;
using AstriaPorta.Config;

namespace AstriaPorta
{
    public class AstriaPortaModSystem : ModSystem
	{
		private WorldGateManager gateManager;
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

		// Called on server and client
		// Useful for registering block/entity classes on both sides
		public override void Start(ICoreAPI api)
		{
			RegisterBlocks(api);
			RegisterItems(api);
		}

		public override void StartServerSide(ICoreServerAPI api)
		{
			base.StartServerSide(api);
			sapi = api;

			gateManager = WorldGateManager.GetNewInstance(api);
			gateManager.FlushRegisteredGates();

			RegisterCommands(api);
			Mod.Logger.Debug("Started server-side modsystem");
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
			base.StartClientSide(api);
			capi = api;

			api.Event.BlockTexturesLoaded += onClientAssetsLoaded;
		}

		private void onClientAssetsLoaded()
		{
			capi.Event.ReloadTextures += CreateExternalTextures;

			CreateExternalTextures();
			RegisterShaderPrograms();
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

			capi.Logger.Notification("Loaded Shaderprogram for event horizon.");

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
			api.RegisterBlockEntityClass("BEStargate", typeof(BlockEntityStargate));
			api.RegisterBlockEntityClass("BEDialHomeDevice", typeof(BlockEntityDialHomeDevice));

			api.RegisterBlockBehaviorClass("MultiblockStargate", typeof(BlockBehaviorMultiblockStargate));
		}

		private void RegisterWorldGenHooks(ICoreServerAPI api)
		{
			WorldGateManager gateManager = WorldGateManager.GetInstance(api);
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

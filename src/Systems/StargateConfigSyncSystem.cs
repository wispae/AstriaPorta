using AstriaPorta.Config;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.ServerMods;

namespace AstriaPorta.Systems
{
    public class StargateConfigSystem : ModSystem
    {
        private const string configChannelName = "astriaportaconfigchannel";
        private IServerNetworkChannel serverChannel;
        private IClientNetworkChannel clientChannel;

        public override double ExecuteOrder()
        {
            // need to load after GenStructures ModSystem
            return 0.35d;
        }

        public override void StartPre(ICoreAPI api)
        {
            setupNetworkChannels(api);
            loadConfiguration(api);

            if (api.Side == EnumAppSide.Server)
            {
                (api as ICoreServerAPI).Event.PlayerJoin += onPlayerJoined;
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            if (TerraGenConfig.DoDecorationPass)
            {
                api.Event.InitWorldGenerator(new Action(() => patchStructureParameters(api)), "standard");
            }
        }

        private void setupNetworkChannels(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Server)
            {
                serverChannel = (api as ICoreServerAPI).Network.GetChannel(configChannelName);
                if (serverChannel == null)
                {
                    serverChannel = (api as ICoreServerAPI).Network.RegisterChannel(configChannelName);
                }
                serverChannel.RegisterMessageType<StargateConfig>();
            }
            else
            {
                clientChannel = (api as ICoreClientAPI).Network.GetChannel(configChannelName);
                if (clientChannel == null)
                {
                    clientChannel = (api as ICoreClientAPI).Network.RegisterChannel(configChannelName);
                }
                clientChannel.RegisterMessageType<StargateConfig>();
                clientChannel.SetMessageHandler<StargateConfig>(onReceivedServerConfig);
            }
        }

        private void onPlayerJoined(IServerPlayer player)
        {
            Mod.Logger.Debug($"Sending server-side config file to player {player.PlayerName}");
            StargateConfig config = StargateConfig.Loaded;
            serverChannel.SendPacket(StargateConfig.Loaded, new IServerPlayer[1] { player });
        }

        private void loadConfiguration(ICoreAPI api)
        {
            string filename = Mod.Info.ModID + ".json";

            StargateConfig config;
            config = api.LoadModConfig<StargateConfig>(filename);
            if (config != null)
            {
                StargateConfig.Loaded = config;
            }

            api.StoreModConfig(StargateConfig.Loaded, filename);
        }

        private void onReceivedServerConfig(StargateConfig serverConfig)
        {
            Mod.Logger.Debug("Received config file from server, will use that instead");
            StargateConfig.Loaded = serverConfig;
        }

        private void patchStructureParameters(ICoreServerAPI sapi)
        {
            StargateConfig config = StargateConfig.Loaded;
            if (config == null) return;
            GenStructures structuresSystem = sapi.ModLoader.GetModSystem<GenStructures>();
            if (structuresSystem == null)
            {
                Mod.Logger.Warning("Couldn't find GenStructures modsystem, ignoring config settings related to structure generation");
                return;
            }

            try
            {

                Type structuresSystemType = typeof(GenStructures);
                WorldGenStructuresConfig structuresConfig = structuresSystemType.GetField("scfg", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(structuresSystem) as WorldGenStructuresConfig;

                if (config.EnableWorldGenGates)
                {
                    SetStructureGroupDistance(structuresConfig, config);
                } else
                {
                    DisableStargateStructures(structuresConfig);
                }
                
            } catch (Exception ex)
            {
                Mod.Logger.Debug(ex.Message);
            }
        }

        private void DisableStargateStructures(WorldGenStructuresConfig config)
        {
            WorldGenStructure structure;
            for (int i = 0; i < config.Structures.Length; i++)
            {
                structure = config.Structures[i];
                if (structure.Group == "stargatesurface" || structure.Group == "stargateunderground")
                {
                    structure.Chance = 0f;
                }
            }
        }

        private void SetStructureGroupDistance(WorldGenStructuresConfig structuresConfig, StargateConfig config)
        {
            WorldGenStructure structure;
            for (int i = 0; i < structuresConfig.Structures.Length; i++)
            {
                structure = structuresConfig.Structures[i];
                if (structure.Group == "stargatesurface")
                {
                    structure.MinGroupDistance = config.MinDistanceSurfaceGates;
                }
                else if (structure.Group == "stargateunderground")
                {
                    structure.MinGroupDistance = config.MinDistanceUndergroundGates;
                }
            }
        }
    }
}

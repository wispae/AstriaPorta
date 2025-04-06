using AstriaPorta.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace AstriaPorta.src.Systems
{
	public class StargateConfigSystem : ModSystem
	{
		private const string configChannelName = "astriaportaconfigchannel";
		private IServerNetworkChannel serverChannel;
		private IClientNetworkChannel clientChannel;

		public override void StartPre(ICoreAPI api)
		{
			base.StartPre(api);

			setupNetworkChannels(api);
			loadConfiguration(api);

			if (api.Side == EnumAppSide.Server)
			{
				(api as ICoreServerAPI).Event.PlayerJoin += onPlayerJoined;
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
			} else
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
			Mod.Logger.Notification($"Sending server-side config file to player {player.PlayerName}");
			StargateConfig config = StargateConfig.Loaded;
			serverChannel.SendPacket(StargateConfig.Loaded, new IServerPlayer[1] { player });
		}

		private void loadConfiguration(ICoreAPI api)
		{
			string filename = Mod.Info.ModID + ".json";

			StargateConfig config;
			config = api.LoadModConfig<StargateConfig>(filename);
			if (config == null)
			{
				api.StoreModConfig(StargateConfig.Loaded, filename);
			}
			else
			{
				StargateConfig.Loaded = config;
			}
		}

		private void onReceivedServerConfig(StargateConfig serverConfig)
		{
			Mod.Logger.Notification("Received config file from server, will use that instead");
			StargateConfig config = StargateConfig.Loaded;
			StargateConfig.Loaded = serverConfig;
		}
	}
}

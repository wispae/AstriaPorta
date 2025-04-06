using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using AstriaPorta.Content;
using Vintagestory.API.Config;
using AstriaPorta.src.Systems;

namespace AstriaPorta.Util
{
    public class GateDiagnostics
    {
        public ICoreServerAPI api;

        public GateDiagnostics(ICoreServerAPI api)
        {
            this.api = api;
        }

        public TextCommandResult CalculateNearestAddress(TextCommandCallingArgs args)
        {
            Console.WriteLine("Entered handler");
            IPlayer player = args.Caller.Player;
            Console.WriteLine("Retrieved Player");
            StargateAddress address = new StargateAddress();
            Console.WriteLine("Created stargate address");
            address.FromCoordinates((int)player.Entity.Pos.X, (int)player.Entity.Pos.Y, (int)player.Entity.Pos.Z, EnumAddressLength.Short);
            Console.WriteLine("set address from coordinates");

            string addressString = AddressDisplayString(address.AddressCoordinates);

            return TextCommandResult.Success(message: $"Local gate address: {addressString} at sector ( {address.AddressCoordinates.X} - {address.AddressCoordinates.Z} )");
        }

        public TextCommandResult RetrieveClosestGate(TextCommandCallingArgs args)
        {
			StargateManagerSystem gateManager = StargateManagerSystem.GetInstance(api);

            IPlayer player = args.Caller.Player;

            BlockPos closestCoordinates = gateManager.GetClosestGatePos((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z, player.Entity.Pos.Dimension);
            if (closestCoordinates.X == -1)
            {
                return TextCommandResult.Error("No gates found in this world");
            }

            string addressString = AddressDisplayString(closestCoordinates);

            int deltaX = api.WorldManager.MapSizeX / 2;
            int deltaZ = api.WorldManager.MapSizeZ / 2;
            return TextCommandResult.Success(message: $"Closest gate: {addressString} at coordinates ({closestCoordinates.X - deltaX} - {closestCoordinates.Y} - {closestCoordinates.Z - deltaZ})");
        }

        public TextCommandResult DisplayGateList(TextCommandCallingArgs args)
        {
			StargateManagerSystem gateManager = StargateManagerSystem.GetInstance(api);
            IPlayer player = args.Caller.Player;
            int gatesPerPage = 5;

            int pageNum = args.Parsers[0].IsMissing ? 1 : (int)args[0];

            List<AddressCoordinates> gateList = new List<AddressCoordinates>();

            if (gateList.Count / gatesPerPage + 1 < pageNum)
            {
                return TextCommandResult.Error($"Page number {pageNum} is invalid, maximum is {gateList.Count / gatesPerPage + 1}");
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Known gates:\n");

            for (int i = gatesPerPage * (pageNum - 1); i < gatesPerPage * pageNum && i < gateList.Count; i++)
            {
                sb.AppendLine(AddressDisplayString(gateList[i]));
            }
            sb.AppendLine($"\nPage {pageNum} of {gateList.Count / gatesPerPage + 1}");

            return TextCommandResult.Success(sb.ToString());
        }

#if DEBUG
		public TextCommandResult RunGateAddressTests(TextCommandCallingArgs args)
		{
			IPlayer player = args.Caller.Player;

			BlockPos playerPos = player.Entity.Pos.AsBlockPos;
			StargateAddress address = new StargateAddress();

			StringBuilder sb = new StringBuilder();

			int chunkX = playerPos.X / GlobalConstants.ChunkSize;
			int chunkZ = playerPos.Z / GlobalConstants.ChunkSize;
			sb.AppendLine($"Starting test from player chunk X{chunkX} Z{chunkZ}");
			address.FromCoordinates(playerPos.X, playerPos.Y, playerPos.Z);
			sb.AppendLine($"Coordinates transformed into address with glyphs {address.AddressCoordinates.Glyphs} ({address.ToString()})");
			sb.AppendLine($"The address has embedded coordinates: X{address.AddressCoordinates.X} Z{address.AddressCoordinates.Z}");
			sb.AppendLine($"Address bits are: {address.AddressBits}");
			sb.AppendLine("==============================================================");

			StargateAddress a2 = new StargateAddress();
			a2.FromGlyphs(address.AddressCoordinates.Glyphs);
			sb.AppendLine("Data for secondary address:");
			sb.AppendLine($"Glyphs transformed into address with glyphs {address.AddressCoordinates.Glyphs} ({address.ToString()})");
			sb.AppendLine($"The address has embedded coordinates: X{address.AddressCoordinates.X} Z{address.AddressCoordinates.Z}");
			sb.AppendLine($"Address bits are: {address.AddressBits}");
			sb.AppendLine("==============================================================");

			return TextCommandResult.Success(sb.ToString());
		}

        public TextCommandResult SeekActiveGlyph(TextCommandCallingArgs args)
        {
            IPlayer player = args.Caller.Player;
            int glyphPos = (int)args[0];

            BlockEntityStargate gate = FindGateNear(player.Entity.Pos.AsBlockPos);
            if (gate == null) return TextCommandResult.Error("No gate found in 10-block radius!");

            if (glyphPos >= gate.GateAddress.GlyphLength) return TextCommandResult.Error($"Max glyph length is {gate.GateAddress.GlyphLength - 1}");

            // gate.TargetGlyph = (byte)glyphPos;

            return TextCommandResult.Success($"Set glyph to {glyphPos}, ok");
        }

        public TextCommandResult DialAnimationManually(TextCommandCallingArgs args)
        {
            IPlayer player = args.Caller.Player;

            BlockEntityStargate gate = FindGateNear(player.Entity.Pos.AsBlockPos);
            if (gate == null) return TextCommandResult.Error("No gate found in 10-block radius!");

            StargateAddress address = FromStringAddress((string)args[0]);
            if (address == null) return TextCommandResult.Error("Could not parse address");

            gate.TryDial(address, EnumDialSpeed.Slow);
            return TextCommandResult.Success($"Dialing {address}, ok");
        }
#endif

        private StargateAddress FromStringAddress(string str)
        {
            string glyphs = "0123456789abcdefghijklmnopqrstuvwxyz";
            string[] parts = str.Split('-');
            if (parts.Length != 7) return null;

            byte[] addressBytes = new byte[7];

            try
            {
                for (int i = 0; i < 7; i++)
                {
                    for (int j = 0; j < glyphs.Length; j++)
                    {
                        if (glyphs[j] == parts[i][0])
                        {
                            addressBytes[i] = (byte)j;
                            break;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            StargateAddress address = new StargateAddress();
            address.FromGlyphs(addressBytes);
            // address.FromByteAddress(addressBytes);

            return address;
        }

        private BlockEntityStargate FindGateNear(BlockPos pos)
        {
            BlockPos minPos = new BlockPos(pos.dimension).Set(pos.X - 10, pos.Y - 10, pos.Z - 10);
            BlockPos maxPos = new BlockPos(pos.dimension).Set(pos.X + 10, pos.Y + 10, pos.Z + 10);
            BlockPos gatePos = null;
            api.World.BlockAccessor.SearchBlocks(minPos, maxPos, (b, bp) =>
            {
                if (b.Variant["gatetype"] != null)
                {
                    gatePos = bp;
                    return false;
                }
                return true;
            });

            if (gatePos == null)
            {
                return null;
            }

            BlockEntityStargate gate = api.World.BlockAccessor.GetBlockEntity<BlockEntityStargate>(gatePos);

            return gate;
        }

        private string AddressDisplayString(AddressCoordinates address)
        {
            if (address.Glyphs == null) return "";
            string addressString = address.Glyphs[0].ToString("d");
            for (int i = 1; i < address.Glyphs.Length; i++)
            {
                addressString += "-" + address.Glyphs[i].ToString("d");
            }

            return addressString;
        }

        private string AddressDisplayString(BlockPos pos)
        {
            StargateAddress address = new StargateAddress();
            address.FromCoordinates(pos.X, pos.Y, pos.Z);

            return AddressDisplayString(address.AddressCoordinates);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AstriaPorta.Content
{
	public class BlockStargate : BlockInteractable
	{
		WorldInteraction[] interactions;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

			if (api.Side != EnumAppSide.Client) return;
			ICoreClientAPI capi = api as ICoreClientAPI;

			interactions = ObjectCacheUtil.GetOrCreate(capi, "astriaporta:stargateInteractions", () =>
			{
				List<ItemStack> writeableStacklist = new List<ItemStack>();

				foreach (CollectibleObject obj in capi.World.Collectibles)
				{
					if (obj.FirstCodePart() == "paper")
					{
						List<ItemStack> stacks = obj.GetHandBookStacks(capi);
						if (stacks != null) writeableStacklist.AddRange(stacks);
					}
				}

				return new WorldInteraction[]
				{
					new WorldInteraction
					{
						ActionLangCode = "astriaporta:blockhelp-stargate-open-camouflage",
						MouseButton = EnumMouseButton.Right
					},
					new WorldInteraction
					{
						ActionLangCode = "astriaporta:blockhelp-stargate-copy-address",
						MouseButton = EnumMouseButton.Right,
						Itemstacks = writeableStacklist.ToArray(),
						GetMatchingStacks = (wi, bs, es) =>
						{
							if (wi.Itemstacks.Length == 0) return null;
							return wi.Itemstacks;
						}
					}
				};
			});
		}

		public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, BlockPatchAttributes attributes = null)
		{
			Console.WriteLine("Block try place for worldgen called...");

			return true;
		}

		public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
		{
			Console.WriteLine("CanPlaceBlock called");
			
			return true;
		}

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			return interactions;
		}

		public override EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
		{
			if (pos == null) return EnumBlockMaterial.Mantle;
			BlockEntityStargate gate = GetBlockEntity<BlockEntityStargate>(pos);
			if (gate == null) return EnumBlockMaterial.Mantle;

			if (gate.CanBreak) return EnumBlockMaterial.Metal;
			return EnumBlockMaterial.Mantle;
		}
	}
}

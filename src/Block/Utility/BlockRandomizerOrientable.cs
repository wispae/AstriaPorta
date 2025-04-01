using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AstriaPorta.Content
{
	public class BlockRandomizerOrientable : Block
	{
		public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
		{
			var stack = base.OnPickBlock(world, pos);

			var be = world.BlockAccessor.GetBlockEntity<BlockEntityBlockRandomizerOrientable>(pos);
			if (be != null)
			{
				stack.Attributes["chances"] = new FloatArrayAttribute(be.Chances);
				stack.Attributes.SetFloat("originalangle", be.OriginalAngle);
				be.Inventory.ToTreeAttributes(stack.Attributes);
			}

			return stack;
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			GetBlockEntity<BlockEntityBlockRandomizerOrientable>(blockSel.Position)?.OnInteract(byPlayer);
			return true;
		}

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			CustomBlockLayerHandler = true;
		}
	}
}

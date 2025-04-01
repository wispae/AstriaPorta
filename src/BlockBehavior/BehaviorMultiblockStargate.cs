using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AstriaPorta.Content
{
	public class  BlockBehaviorMultiblockStargate : BlockBehavior
	{
		int SizeX, SizeY, SizeZ;
		Vec3i ControllerPositionRel;

		public BlockBehaviorMultiblockStargate(Block block) : base(block) { }

		public override void Initialize(JsonObject properties)
		{
			base.Initialize(properties);

			SizeX = properties["sizex"].AsInt(3);
			SizeY = properties["sizey"].AsInt(3);
			SizeZ = properties["sizez"].AsInt(3);
			ControllerPositionRel = properties["cposition"].AsObject<Vec3i>(new Vec3i(1, 0, 1));
		}

		public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
		{
			bool blocked = false;

			IterateOverEach(blockSel.Position, (mpos) =>
			{
				if (mpos == blockSel.Position) return true;

				Block mblock = world.BlockAccessor.GetBlock(mpos);
				if (!mblock.IsReplacableBy(block))
				{
					blocked = true;
					return false;
				}

				return true;
			});

			if (blocked)
			{
				handling = EnumHandling.PreventDefault;
				failureCode = "notenoughspace";
				return false;
			}

			return true;
		}

		public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
		{
			handling = EnumHandling.PassThrough;

			IterateOverEach(pos, (mpos) =>
			{
				if (mpos == pos) return true;

				int dx = mpos.X - pos.X;
				int dy = mpos.Y - pos.Y;
				int dz = mpos.Z - pos.Z;

				string sdx = (dx < 0 ? "n" : (dx > 0 ? "p" : "")) + Math.Abs(dx);
				string sdy = (dy < 0 ? "n" : (dy > 0 ? "p" : "")) + Math.Abs(dy);
				string sdz = (dz < 0 ? "n" : (dz > 0 ? "p" : "")) + Math.Abs(dz);

				AssetLocation loc = new AssetLocation("astriaporta", "multiblockstargate-" + sdx + "-" + sdy + "-" + sdz);
				Block block = world.GetBlock(loc);

				if (block == null) throw new IndexOutOfRangeException("Not part of the 7x7 ring! ; " + loc.Path);

				world.BlockAccessor.SetBlock(block.Id, mpos);
				return true;
			});
		}

		public void IterateOverEach(BlockPos controllerPos, ActionConsumable<BlockPos> onBlock)
		{
			int x = controllerPos.X - ControllerPositionRel.X;
			int y = controllerPos.Y - ControllerPositionRel.Y;
			int z = controllerPos.Z - ControllerPositionRel.Z;
			int d = controllerPos.dimension;
			BlockPos tmpPos = new BlockPos(d);

			// top and bottom row x-direction
			for (int dx = 2; dx < (SizeX - 2); dx++)
			{
				tmpPos.Set(x + dx, y, z);
				if (!onBlock(tmpPos)) return;

				tmpPos.Add(0, SizeY - 1, 0);
				if (!onBlock(tmpPos)) return;
			}

			// top and bottom row z-direction
			for (int dz = 2; dz < (SizeZ - 2); dz++)
			{
				tmpPos.Set(x, y, z + dz);
				if (!onBlock(tmpPos)) return;

				tmpPos.Add(0, SizeY - 1, 0);
				if (!onBlock(tmpPos)) return;
			}

			// side walls
			for (int dy = 2; dy < (SizeY - 2); dy++)
			{
				tmpPos.Set(x, y + dy, z);
				if (!onBlock(tmpPos)) return;

				tmpPos.Set(x + SizeX - 1, y + dy, z + SizeZ - 1);
				if (!onBlock(tmpPos)) return;
			}

			// inner corners
			if (SizeZ == 1)
			{
				tmpPos.Set(x + 1, y + 1, z);
				if (!onBlock(tmpPos)) return;

				tmpPos.Set(x + 1, y + SizeY - 2, z);
				if (!onBlock(tmpPos)) return;

				tmpPos.Set(x + SizeX - 2, y + 1, z);
				if (!onBlock(tmpPos)) return;

				tmpPos.Set(x + SizeX - 2, y + SizeY - 2, z);
				if (!onBlock(tmpPos)) return;
			} else
			{
				tmpPos.Set(x, y + 1, z + 1);
				if (!onBlock(tmpPos)) return;

				tmpPos.Set(x, y + SizeY - 2, z + 1);
				if (!onBlock(tmpPos)) return;

				tmpPos.Set(x, y + 1, z + SizeZ - 2);
				if (!onBlock(tmpPos)) return;

				tmpPos.Set(x, y + SizeY - 2, z + SizeZ - 2);
				if (!onBlock(tmpPos)) return;
			}
		}

		public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
		{
			IterateOverEach(pos, (mpos) =>
			{
				if (mpos == pos) return true;

				Block mblock = world.BlockAccessor.GetBlock(mpos);
				if (mblock is BlockMultiblock)
				{
					world.BlockAccessor.SetBlock(0, mpos);
				}

				return true;
			});
		}
	}
}

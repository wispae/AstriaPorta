using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AstriaPorta.src.Block
{
	public class BlockMultiblockStargate : BlockMultiblock
	{
		public override AssetLocation GetRotatedBlockCode(int angle)
		{
			int angleIndex = ((angle / 90) % 4 + 4) % 4;
			if (angleIndex == 0) return Code;

			Vec3i offsetNew;
			switch (angleIndex)
			{
				case 1:
					offsetNew = new Vec3i(-Offset.Z, Offset.Y, Offset.X);
					break;
				case 2:
					offsetNew = new Vec3i(-Offset.X, Offset.Y, -Offset.Z);
					break;
				case 3:
					offsetNew = new Vec3i(Offset.Z, Offset.Y, -Offset.X);
					break;
				default:
					offsetNew = null;
					break;
			}

			return new AssetLocation(Code.Domain, "multiblockstargate" + OffsetToString(offsetNew.X) + OffsetToString(offsetNew.Y) + OffsetToString(offsetNew.Z));
		}

		private string OffsetToString(int x)
		{
			if (x == 0) return "-0";
			if (x < 0) return "-n" + (-x).ToString();
			return "-p" + x.ToString();
		}
	}
}

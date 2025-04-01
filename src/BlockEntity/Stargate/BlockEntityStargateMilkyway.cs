using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AstriaPorta.Content
{
	public class BlockEntityStargateMilkyway : BlockEntityStargate
	{
		protected override void InitializeRenderers(ICoreClientAPI capi)
		{
			if (!rendererInitialized || renderer == null)
			{
				renderer = new MilkywayGateRenderer(capi, Pos);
				renderer.orientation = Block.Shape.rotateY;
				horizonlight = new LightiningPointLight(new Vec3f(0.9f, 0.5f, 0.5f), Pos.AddCopy(0, 3, 0).ToVec3d());

				animUtil.InitializeAnimator("milkyway_chevron_animation", null, null, new Vec3f(0, Block.Shape.rotateY, 0));

				capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque);
				UpdateRendererState();
				UpdateChevronGlow(activeChevrons);
			}

			if (!horizonInitialized)
			{
				InitializeHorizonRenderer(capi);
			}
		}
	}
}

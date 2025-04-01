using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AstriaPorta.Content
{
	public abstract class StargateRenderManager
	{
		protected BEStargate gate;
		protected ICoreClientAPI capi;

		protected GateRenderer renderer;
		protected EventHorizonRenderer eventHorizonRenderer;

		protected bool rendererInitialized = false;
		protected bool horizonInitialized = false;
		protected bool horizonRegistered = false;

		public StargateRenderManager(BEStargate gate, ICoreClientAPI capi)
		{
			this.gate = gate;
			this.capi = capi;
		}

		public abstract void InitializeRenderer();

		public void InitializeHorizonRenderer()
		{
			AssetLocation horizonLoc = new AssetLocation("astriaporta", "gates/kawoosh2");
			IShaderProgram horizonLocProgram = capi.ModLoader.GetModSystem<AstriaPortaModSystem>().eventHorizonShaderProgram;
		}

		public void ActivateHorizon(bool isActivating = true)
		{
			if (eventHorizonRenderer == null) return;

			eventHorizonRenderer.t = 0;
			eventHorizonRenderer.activating = isActivating;
			eventHorizonRenderer.shouldRender = true;
			if (!horizonRegistered)
			{
				capi.Event.RegisterRenderer(eventHorizonRenderer, EnumRenderStage.Opaque);
				horizonRegistered = true;
			}
		}

		public void DeactivateHorizon()
		{
			if (eventHorizonRenderer == null) return;

			eventHorizonRenderer.shouldRender = false;
			capi.Event.UnregisterRenderer(eventHorizonRenderer, EnumRenderStage.Opaque);
			horizonRegistered = false;
		}

		public void UpdateRendererState()
		{
			if (renderer == null) return;

			renderer.ringRotation = gate.DialingManager.currentAngle;
		}

		internal void UpdateChevronGlow(int activeChevrons, EnumAddressLength length)
		{
			if (renderer == null) return;

			renderer.chevronGlow = new byte[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			byte padding = 0;

			for (int i = 1; i < 10; i++)
			{
				switch (length)
				{
					case EnumAddressLength.Short:
						{
							if (i == 4 || i == 5)
							{
								padding++;
								continue;
							}

							if (i <= (activeChevrons + padding))
							{
								renderer.chevronGlow[i - 1] = 200;
							}
							break;
						}
					case EnumAddressLength.Medium:
						{
							if (i == 5)
							{
								padding++;
								continue;
							}

							if (i <= (activeChevrons + padding))
							{
								renderer.chevronGlow[i - 1] = 200;
							}

							break;
						}
					case EnumAddressLength.Long:
						{
							if (i <= activeChevrons + padding) renderer.chevronGlow[i - 1] = 200;

							break;
						}
				}
			}
		}

		internal void DisposeRenderers()
		{
			if (renderer != null)
			{
				capi.Event.UnregisterRenderer(renderer, EnumRenderStage.Opaque);
			}
			if (eventHorizonRenderer != null)
			{
				capi.Event.UnregisterRenderer(renderer, EnumRenderStage.Opaque);
				horizonRegistered = false;
			}

			renderer?.Dispose();
			renderer = null;
			eventHorizonRenderer?.Dispose();
			eventHorizonRenderer = null;

			rendererInitialized = false;
			horizonInitialized = false;
		}
	}
}

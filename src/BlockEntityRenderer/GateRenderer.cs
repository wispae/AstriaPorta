using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace AstriaPorta.Content
{
	public abstract class GateRenderer : IRenderer
	{
		public float orientation = 0f;
		public float ringRotation = 0f;

		public byte[] chevronGlow = new byte[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

		public bool spinCW = true;
		public bool gateActive = false;
		public bool shouldRender = true;


		protected float glyphAngle;

		protected byte glyphCount = 39;

		protected Vec4f glowColor;


		public byte GlyphCount
		{
			get
			{
				return glyphCount;
			}
			set
			{
				glyphCount = value;
				glyphAngle = 360f / glyphCount;
			}
		}

		public Vec4f GlowColor
		{
			get
			{
				return glowColor;
			}
			set
			{
				glowColor = value;
			}
		}

		public double RenderOrder
		{
			get { return 0.5; }
		}

		public int RenderRange => 24;

		public abstract void OnRenderFrame(float delta, EnumRenderStage stage);
		public abstract void Dispose();
	}
}

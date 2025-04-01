using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Content
{
	public abstract class StargateDialingManager
	{
		private const float degPerS = 40f;

		// Physical state
		public float glyphAngle = 0f;
		public float previousAngle = 0f;
		public float currentAngle = 0f;
		public bool rotateCW = true;
		public bool isRotating = false;

		public byte activeChevrons = 0;

		// Internal state
		internal StargateAddress targetAddress;
		internal int currentAddressIndex;
		internal byte currentGlyph;
		internal byte nextGlyph;

		// Meta state
		protected BEStargate gate;
		protected int tickListenerId = -1;

		internal bool remoteNotified = false;

		public abstract event EventHandler OnActivationFailed;
		public abstract event EventHandler OnActivationSuccess;

		public int GlyphLength
		{
			get
			{
				switch (gate.StargateType)
				{
					case EnumStargateType.Milkyway: return 39;
					case EnumStargateType.Pegasus: return 39;
					case EnumStargateType.Destiny: return 39;
					default: return 36;
				}
			}
		}

		/// <summary>
		/// Calculates the next angle of the inner ring given
		/// a time since the last gametick in seconds
		/// </summary>
		/// <param name="delta"></param>
		/// <returns></returns>
		internal float NextAngle(float delta)
		{
			// CW: Normalize current glyph to pos 38
			// CC: Normalize current glyph to pos 0
			byte targetGlyph = targetAddress.AddressCoordinates.Glyphs[currentAddressIndex];
			previousAngle = currentAngle;

			if (currentGlyph != targetGlyph)
			{
				currentAngle += delta * (rotateCW ? -degPerS : degPerS);
				if (currentAngle < 0) currentAngle += 360;
				else if (currentAngle >= 360) currentAngle -= 360;

				nextGlyph = (byte)(currentGlyph + (rotateCW ? -1 : 1));
				if (nextGlyph > 250) nextGlyph = (byte)(GlyphLength - 1);
				else if (nextGlyph >= GlyphLength) nextGlyph = 0;

				float nextAngle = (nextGlyph * glyphAngle + 360f) % 360;
				float tPreviousAngle;
				float tCurrentAngle;
				float tNextAngle;

				if (rotateCW)
				{
					tPreviousAngle = 360f;
					tCurrentAngle = (currentAngle - previousAngle + 360f) % 360;
					tNextAngle = (nextAngle - previousAngle + 360) % 360;
				}
                else
                {
					tPreviousAngle = 0f;
					tCurrentAngle = (currentAngle + (360 - previousAngle)) % 360;
					tNextAngle = (nextAngle + (360 - previousAngle)) % 360;
				}

				if ((tCurrentAngle >= tNextAngle && tNextAngle > tPreviousAngle) || (tCurrentAngle <= tNextAngle && tNextAngle < tPreviousAngle))
				{
					currentGlyph = nextGlyph;

					if (currentGlyph == targetGlyph)
					{
						currentAngle = nextGlyph * glyphAngle;
						OnTargetGlyphReached();
					}
				}
			}
			else
			{
				OnTargetGlyphReached();
			}

			return currentAngle;
		}

		public abstract void OnTargetGlyphReached();

		public abstract void OnDialingSequenceCompletion();

		public abstract void OnActivationFailure(float delta);

		public abstract void OnGlyphActivationCompleted(float delta);
	}
}

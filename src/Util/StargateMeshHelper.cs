using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace AstriaPorta.Util
{
	public static class StargateMeshHelper
	{
		private static readonly int GLYPH_SHEET_HEIGHT = 6;
		private static readonly int GLYPH_SHEET_WIDTH = 6;
		private static readonly int CHEVRON_COUNT = 9;
		private static readonly int DESTINY_SHEET_WIDTH = 14;
		private static readonly int DESTINY_SHEET_HEIGHT = 3;
		private static readonly int DESTINY_GLYPH_COUNT = 36;

		private static MultiTextureMeshRef _milkywayChevronMeshRef;
        private static MultiTextureMeshRef _milkywayRingMeshRef;
        private static MultiTextureMeshRef _pegasusChevronMeshRef;
		private static MultiTextureMeshRef[] _pegasusGlyphMeshRefs;
		private static MultiTextureMeshRef _destinyGateMeshRef;
		private static MultiTextureMeshRef _activeDestinyGateMeshRef;

		private static MeshData[] _destinyGlyphMeshData;

		public static void Dispose()
		{
			_milkywayChevronMeshRef?.Dispose();
			_milkywayRingMeshRef?.Dispose();
			_pegasusChevronMeshRef?.Dispose();
			_destinyGateMeshRef?.Dispose();
			_activeDestinyGateMeshRef?.Dispose();
			_destinyGlyphMeshData = null;

			if (_pegasusChevronMeshRef != null)
			{
				foreach (var glyphMeshRef in _pegasusGlyphMeshRefs)
				{
					glyphMeshRef?.Dispose();
				}
			}
		}

		public static void Initialize(ICoreClientAPI capi)
		{
			GenDefaultHorizonMesh(capi);
			GenChevronMesh(capi, "milkyway");
			GenChevronMesh(capi, "pegasus");
			InitializePegasusGlyphs(capi);
			InitializeDestinyGlyphs(capi);
		}

		public static void InitializeDestinyGlyphs(ICoreClientAPI capi)
		{
			// initialize the meshdata for the glyphs.
			// no need to upload this yet as we want to
			// just add it to the big glyph mesh for efficiency
			if (_destinyGlyphMeshData != null)
			{
				return;
			}

            _destinyGlyphMeshData = new MeshData[DESTINY_GLYPH_COUNT];
            var glyphMesh = GenSingleGlyphMesh(capi, "destiny");

            float glyphSizeX = 0;
            float glyphSizeY = 0;
            float minUvX = 1f;
            float maxUvX = 0;
            float minUvY = 1f;
            float maxUvY = 0;
            for (int j = 0; j < glyphMesh.Uv.Length; j += 2)
            {
                if (glyphMesh.Uv[j] == 0 && glyphMesh.Uv[j + 1] == 0) continue;

                if (glyphMesh.Uv[j] > maxUvX)
                {
                    maxUvX = glyphMesh.Uv[j];
                }

                if (glyphMesh.Uv[j] < minUvX)
                {
                    minUvX = glyphMesh.Uv[j];
                }

                if (glyphMesh.Uv[j + 1] > maxUvY)
                {
                    maxUvY = glyphMesh.Uv[j + 1];
                }

                if (glyphMesh.Uv[j + 1] < minUvY)
                {
                    minUvY = glyphMesh.Uv[j + 1];
                }
            }

            // assume the texture atlas is square
            glyphSizeX = maxUvX - minUvX;
            glyphSizeY = maxUvY - minUvY;
			MeshData tempMesh;
			Matrixf transform = new();
			int i;

			for (var y = 0; y < DESTINY_SHEET_HEIGHT; y++)
			{
				for (var x = 0; x < DESTINY_SHEET_WIDTH; x++)
				{
					i = y * DESTINY_SHEET_WIDTH + x;
					if (i >= DESTINY_GLYPH_COUNT) return;

					tempMesh = glyphMesh.Clone();
					transform = transform.Identity()
						.Translate(.5f, .5f, 0f)
						.RotateZDeg((i + (i / 4) + 1) * -8f)
						.Translate(-.5f, -.5f, 0f);

					tempMesh.MatrixTransform(transform.Values);
                    _destinyGlyphMeshData[i] = tempMesh;

                    for (int j = 0; j < glyphMesh.Uv.Length; j += 2)
					{
						glyphMesh.Uv[j] += glyphSizeX;
					}
				}

				for (int j = 0; j < glyphMesh.Uv.Length; j += 2)
				{
					glyphMesh.Uv[j] -= DESTINY_SHEET_WIDTH * glyphSizeX;
					glyphMesh.Uv[j + 1] += glyphSizeY;
				}
			}
        }

		public static void InitializePegasusGlyphs(ICoreClientAPI capi)
		{
			if (_pegasusGlyphMeshRefs != null)
			{
				// this will caused issues if called when this is still in use
				foreach (var tempRef in _pegasusGlyphMeshRefs)
				{
					tempRef?.Dispose();
				}
			}

			_pegasusGlyphMeshRefs = new MultiTextureMeshRef[GLYPH_SHEET_HEIGHT * GLYPH_SHEET_WIDTH];
            var glyphMesh = GenSingleGlyphMesh(capi, "pegasus");

			float glyphSizeX = 0;
			float glyphSizeY = 0;
			float minUvX = 1f;
			float maxUvX = 0;
			float minUvY = 1f;
			float maxUvY = 0;
			for (int i = 0; i < glyphMesh.Uv.Length; i+=2)
			{
				if (glyphMesh.Uv[i] == 0 && glyphMesh.Uv[i + 1] == 0) continue;

				if (glyphMesh.Uv[i] > maxUvX)
				{
					maxUvX = glyphMesh.Uv[i];
				}
				
				if (glyphMesh.Uv[i] < minUvX)
				{
					minUvX = glyphMesh.Uv[i];
				}

				if (glyphMesh.Uv[i + 1] > maxUvY)
				{
					maxUvY = glyphMesh.Uv[i + 1];
				}
				
				if (glyphMesh.Uv[i + 1] < minUvY)
				{
					minUvY = glyphMesh.Uv[i + 1];
				}
			}

			// assume the texture atlas is square
			glyphSizeX = maxUvX - minUvX;
			glyphSizeY = maxUvY - minUvY;
			MultiTextureMeshRef glyphRef;

			for (var y = 0; y < GLYPH_SHEET_HEIGHT; y++)
			{
				for (var x = 0; x < GLYPH_SHEET_WIDTH; x++)
				{
					glyphRef = capi.Render.UploadMultiTextureMesh(glyphMesh);
					_pegasusGlyphMeshRefs[y * GLYPH_SHEET_WIDTH + x] = glyphRef;

                    for (int i = 0; i < glyphMesh.Uv.Length; i += 2)
                    {
                        glyphMesh.Uv[i] += glyphSizeX;
                    }
                }

				for (int i = 0; i < glyphMesh.Uv.Length; i+=2)
				{
					glyphMesh.Uv[i] -= GLYPH_SHEET_WIDTH * glyphSizeX;
					glyphMesh.Uv[i + 1] += glyphSizeY;
				}
			}
        }

		/// <summary>
		/// Returns a newly created or cached version of the single chevron mesh
		/// for the provided gate type
		/// </summary>
		/// <param name="capi"></param>
		/// <param name="gateType"></param>
		/// <returns></returns>
		public static MeshData GenChevronMesh(ICoreClientAPI capi, string gateType)
		{
			MeshData baseMesh = ObjectCacheUtil.GetOrCreate(capi, $"astriaporta{gateType}chevronmesh", () =>
			{
				Block block = capi.World.GetBlock(new AssetLocation($"astriaporta:stargate-{gateType}-north"));
				if (block == null) return null;

				ITesselatorAPI mesher = capi.Tesselator;

				MeshData mesh;
				AssetLocation chevronLight = new AssetLocation("astriaporta", $"shapes/block/gates/{gateType}_chevron.json");
				mesher.TesselateShape(block, Shape.TryGet(capi, chevronLight), out mesh);

				return mesh;
			});

			return baseMesh;
		}

		/// <summary>
		/// Returns a new or cached meshref to the single chevron mesh for the provided gate type
		/// </summary>
		/// <remarks>
		/// Only valid for "milkyway" and "pegasus" gates, "destiny" will return null
		/// </remarks>
		/// <param name="capi"></param>
		/// <param name="gateType"></param>
		/// <returns></returns>
		public static MultiTextureMeshRef GetChevronMeshRef(ICoreClientAPI capi, string gateType)
		{
			switch (gateType)
			{
				case "milkyway":
					if (_milkywayChevronMeshRef == null || _milkywayChevronMeshRef.Disposed)
					{
						_milkywayChevronMeshRef = capi.Render.UploadMultiTextureMesh(GenChevronMesh(capi, gateType));
					}
					return _milkywayChevronMeshRef;
				case "pegasus":
					if (_pegasusChevronMeshRef == null || _milkywayChevronMeshRef.Disposed)
					{
						_pegasusChevronMeshRef = capi.Render.UploadMultiTextureMesh(GenChevronMesh(capi, gateType));
					}
					return _pegasusChevronMeshRef;
				default:
					return null;
			}
		}

		public static MeshData GenDestinyGateMesh(ICoreClientAPI capi, bool chevronsActive)
		{
            var baseMesh = GenRingMesh(capi, "destiny").Clone();
            var chevronMesh = GenChevronMesh(capi, "destiny");

			if (chevronMesh == null)
			{
				if (baseMesh != null)
				{
					return baseMesh;
				}

				return null;
			}

			if (chevronsActive)
			{
				chevronMesh = chevronMesh.Clone();
				chevronMesh.SetVertexFlags(255);
			}

			MeshData tempMesh;
			Matrixf transform = new();

            for (int i = 0; i < CHEVRON_COUNT; i++)
			{
				transform = transform.Identity()
					.Translate(.5f, .5f, 0f)
					.RotateZDeg((i - 1) * 40f)
					.Translate(-.5f, -.5f, 0f);

				tempMesh = chevronMesh.Clone().MatrixTransform(transform.Values);
				baseMesh.AddMeshData(tempMesh);
			}

			return baseMesh;
        }

		public static MultiTextureMeshRef GetDestinyGateMeshRef(ICoreClientAPI capi, bool chevronsActive)
		{
            // we want to keep 2 meshes loaded in GPU for the destiny gates;
            // the active and inactive gates, since for destiny gates it's
            // either all or nothing for the chevrons. Then we just need to
            // assemble the large glyph mesh and upload that seperately
            // The base gate meshes can also be reused without issue across instances
            if (chevronsActive && _activeDestinyGateMeshRef != null && !_activeDestinyGateMeshRef.Disposed)
            {
                return _activeDestinyGateMeshRef;
            }
            else if (!chevronsActive && _destinyGateMeshRef != null && !_destinyGateMeshRef.Disposed)
            {
                return _destinyGateMeshRef;
            }

			var baseMesh = GenDestinyGateMesh(capi, chevronsActive);
			if (baseMesh == null) return null;
            if (chevronsActive)
            {
				_activeDestinyGateMeshRef = capi.Render.UploadMultiTextureMesh(baseMesh);
				return _activeDestinyGateMeshRef;
            }

			_destinyGateMeshRef = capi.Render.UploadMultiTextureMesh(baseMesh);
			return _destinyGateMeshRef;
        }

		/// <summary>
		/// Generates a single 0° offset top-side glyph for the given gate type
		/// </summary>
		/// <remarks>
		/// Assumes that a shape called "{gateType}_glyph.json" exists in the
		/// "shapes/block/gates/" folder
		/// </remarks>
		/// <param name="capi"></param>
		/// <param name="gateType"></param>
		/// <returns></returns>
		public static MeshData GenSingleGlyphMesh(ICoreClientAPI capi, string gateType)
		{
			MeshData baseMesh = ObjectCacheUtil.GetOrCreate(capi, $"astriaporta{gateType}glyphmesh", () =>
			{
				Block block = capi.World.GetBlock(new AssetLocation($"astriaporta:stargate-{gateType}-north"));
				if (block == null) return null;

				ITesselatorAPI mesher = capi.Tesselator;

				MeshData mesh;
				var glyphAsset = new AssetLocation("astriaporta", $"shapes/block/gates/{gateType}_glyph.json");
				mesher.TesselateShape(block, Shape.TryGet(capi, glyphAsset), out mesh);

				return mesh;
			});

			return baseMesh;
		}

        public static MeshData GenDestinyGlyphMesh(ICoreClientAPI capi, int[] activeGlyphs)
		{
			MeshData baseMesh = new MeshData(1, 1);

			MeshData tempMesh;
			Matrixf transform = new();
			for (int i = 0; i < DESTINY_GLYPH_COUNT; i++)
			{
				tempMesh = _destinyGlyphMeshData[i].Clone();

				if (activeGlyphs != null && activeGlyphs.Contains(i))
				{
					tempMesh.SetVertexFlags(255);
				}

				baseMesh.AddMeshData(tempMesh);
            }

			return baseMesh;
		}

		public static MultiTextureMeshRef GetPegasusGlyphMeshRef(ICoreClientAPI capi, int glyphIndex)
		{
			if (glyphIndex < 0) return null;

			if (_pegasusGlyphMeshRefs == null)
			{
				InitializePegasusGlyphs(capi);
			}

			if (glyphIndex >= _pegasusGlyphMeshRefs.Length) return null;

			return _pegasusGlyphMeshRefs[glyphIndex];
        }

        public static MeshData GenDefaultHorizonMesh(ICoreClientAPI capi)
        {
            return GenRoundHorizonMesh(capi, 2.385f, 39, 32, 0.485f, 3.3325f);
        }

        public static MeshData GenRingMesh(ICoreClientAPI capi, string gateType)
		{
			MeshData baseMesh = ObjectCacheUtil.GetOrCreate(capi, $"astriaporta{gateType}ringmesh", () =>
			{
				Block block = capi.World.GetBlock(new AssetLocation($"astriaporta:stargate-{gateType}-north"));
				if (block == null) return null;

				ITesselatorAPI mesher = capi.Tesselator;
				MeshData mesh;

				AssetLocation ring = new AssetLocation("astriaporta", $"shapes/block/gates/{gateType}_ring.json");
				mesher.TesselateShape(block, Shape.TryGet(capi, ring), out mesh);

				return mesh;
			});

			return baseMesh;
		}

		public static MeshData GenRoundHorizonMesh(ICoreClientAPI capi, float radius, int angles, int sectors, float offsetx, float offsety)
		{
			MeshData baseMesh;
			// caching ftw
			baseMesh = ObjectCacheUtil.TryGet<MeshData>(capi, "astriaportaeventhorizonmesh");
			if (baseMesh != null) return baseMesh;

			// Precalculate required vertex / index counts
			int vertexCount = angles * sectors + 1;
			int indexCount = 6 * angles * (sectors - 1) + 3 * angles;

			// Set the "withNormals" flag to false when using default shaders!
			baseMesh = new MeshData(vertexCount, indexCount, true, true, true, true);

			int color = BitConverter.ToInt32(new byte[] { 0, 0, 0, 255 });
			float x, y, z, uvx, uvy;
			float angle = 2 * MathF.PI / angles;
			z = 0.5f;

			// Generate vertices
			for (int i = 0; i < sectors; i++)
			{
				for (int j = 0; j < angles; j++)
				{
					// x E [-radius, radius]
					// y E [-radius, radius]
					// 
					//
					x = MathF.Cos(angle * j) * (radius / (float)(sectors)) * (float)(sectors - i);
					y = MathF.Sin(angle * j) * (radius / (float)(sectors)) * (float)(sectors - i);
					uvx = (x + radius) / (2 * radius);
					uvy = 1f - ((y + radius) / (2 * radius));

					x += offsetx;
					y += offsety;

					baseMesh.AddVertexWithFlags(x, y, z, uvx, uvy, color, 0);

					// add normal ( ! SKIP WHEN USING DEFAULT SHADERS / RENDERERS ! )
					if (i == 0) baseMesh.AddNormal(0f, 0f, 0f);
					else baseMesh.AddNormal(1f, 1f, 1f);
				}
			}
			// Center vertex
			baseMesh.AddVertexWithFlags(offsetx, offsety, z, 0.5f, 0.5f, color, 0);
			// Skip normal when using defualt!
			baseMesh.AddNormal(1f, 1f, 1f);

			// indices for all ring sectors, except innermost sector
			for (int i = 0; i < (sectors - 1); i++)
			{
				for (int j = 0; j < (angles - 1); j++)
				{
					baseMesh.AddIndex(j + i * angles);
					baseMesh.AddIndex(j + (i + 1) * angles);
					baseMesh.AddIndex(j + (i + 1) * angles + 1);

					baseMesh.AddIndex(j + i * angles);
					baseMesh.AddIndex(j + i * angles + 1);
					baseMesh.AddIndex(j + (i + 1) * angles + 1);
				}

				// connect the last vertices of the sectors with the
				// first vertices of the sector
				baseMesh.AddIndex(angles - 1 + i * angles);
				baseMesh.AddIndex(angles - 1 + (i + 1) * angles);
				baseMesh.AddIndex((i + 1) * angles);

				baseMesh.AddIndex(i * angles);
				baseMesh.AddIndex((i + 1) * angles);
				baseMesh.AddIndex(angles - 1 + i * angles);
			}

			// innermost sector, with all triangles converging
			// onto the center vertex
			for (int i = 0; i < (angles - 1); i++)
			{
				baseMesh.AddIndex(i + (sectors - 1) * angles);
				baseMesh.AddIndex(i + (sectors - 1) * angles + 1);
				baseMesh.AddIndex(sectors * angles);
			}
			baseMesh.AddIndex(sectors * angles - 1);
			baseMesh.AddIndex((sectors - 1) * angles);
			baseMesh.AddIndex(sectors * angles);

			// Update UV coordinates to position in the texture atlas
			baseMesh.SetTexPos(capi.ModLoader.GetModSystem<AstriaPortaModSystem>().eventHorizonTexPos);

			baseMesh = ObjectCacheUtil.GetOrCreate(capi, "astriaporta-eventhorizon", () => { return baseMesh; });

			return baseMesh;
		}
	}
}

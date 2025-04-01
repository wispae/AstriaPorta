using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace AstriaPorta.Util
{
	public static class StargateMeshHelper
	{
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

		public static MeshData GenDefaultHorizonMesh(ICoreClientAPI capi)
		{
			return GenRoundHorizonMesh(capi, 2.385f, 39, 32, 0.485f, 3.3325f);
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

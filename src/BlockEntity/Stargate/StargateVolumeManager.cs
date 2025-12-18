using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace AstriaPorta.Content
{
    public class StargateVolumeManager
    {
        private static Cuboidi vortexOffsetNorth;
        private static Cuboidi vortexOffsetEast;
        private static Cuboidi vortexOffsetSouth;
        private static Cuboidi vortexOffsetWest;

        private static Cuboidi irisOffsetNorthSouth;
        private static Cuboidi irisOffsetEastWest;

        private static Cuboidf tpAreaNorthSouth;
        private static Cuboidf tpAreaEastWest;

        private static StargateVolumeManager instance = new StargateVolumeManager();

        public static StargateVolumeManager Instance => instance;

        private StargateVolumeManager()
        {
        }

        public static void Initialize()
        {
            InitializeVortexOffsets();
            InitializeIrisOffsets();
            InitializeTpAreas();
        }

        public Cuboidi GetVortexVolume(float rotation)
        {
            switch (rotation)
            {
                case 0f:
                    return vortexOffsetNorth;
                case 90f:
                    return vortexOffsetWest;
                case 180f:
                    return vortexOffsetSouth;
                case 270f:
                    return vortexOffsetEast;
                default: return null;
            }
        }

        public Cuboidi GetIrisVolume(float rotation)
        {
            if (rotation % 180 == 0)
            {
                return irisOffsetNorthSouth;
            }

            return irisOffsetEastWest;
        }

        public static Cuboidf GetTeleportArea(float rotation)
        {
            if (rotation % 180 == 0)
            {
                return tpAreaNorthSouth;
            }

            return tpAreaEastWest;
        }

        private static void InitializeVortexOffsets()
        {
            vortexOffsetNorth = new Cuboidi(-1, 1, 1, 2, 5, 4);
            vortexOffsetSouth = new Cuboidi(-1, 1, 0, 2, 5, -3);
            vortexOffsetEast = new Cuboidi(0, 1, -1, -3, 5, 2);
            vortexOffsetWest = new Cuboidi(1, 1, -1, 4, 5, 2);
        }

        private static void InitializeIrisOffsets()
        {
            irisOffsetNorthSouth = new Cuboidi(-2, 1, 0, 2, 5, 0);
            irisOffsetEastWest = new Cuboidi(0, 1, -2, 0, 5, 2);
        }

        private static void InitializeTpAreas()
        {
            tpAreaNorthSouth = new Cuboidf(-2f, 0.5f, 0f, 3f, 6f, 1f);
            tpAreaEastWest = new Cuboidf(0f, 0.5f, -2f, 1f, 6f, 3f);
        }
    }
}

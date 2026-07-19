using AstriaPorta.Content;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace AstriaPorta.Util;

public class GateUtils
{
    /// <summary>
    /// Finds the closest gate to this DHD<br/>
    /// Expensive, use as little as possible
    /// </summary>
    /// <returns></returns>
    public static IStargate FindClosestGate(IBlockAccessor accessor, BlockPos position, Cuboidi searchArea)
    {
        if (searchArea == null)
            return null;

        List<IStargate> targetBlocks = new List<IStargate>();
        IStargate foundGate = null;

        for (int x = searchArea.MinX; x <= searchArea.MaxX; x++)
        {
            for (int z = searchArea.MinZ; z <= searchArea.MaxZ; z++)
            {
                for (int y = searchArea.MinY; y <= searchArea.MaxY; y++)
                {
                    foundGate = accessor.GetBlockEntity<StargateBase>(position.AddCopy(x, y, z));
                    if (foundGate != null)
                    {
                        targetBlocks.Add(foundGate);
                    }
                }
            }
        }

        foundGate = targetBlocks.Count > 0 ? targetBlocks[0] : null;
        foreach (var be in targetBlocks)
        {
            if (position.ManhattanDistance(be.Pos) < position.ManhattanDistance(foundGate.Pos))
            {
                foundGate = be;
            }
        }

        return foundGate;
    }
}

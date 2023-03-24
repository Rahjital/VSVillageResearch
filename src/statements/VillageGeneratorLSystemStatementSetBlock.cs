using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

using TCParser;

namespace VillageResearch
{
    public class StatementSetBlock : Statement
    {
        private string blockPaletteIdExpression;

        public StatementSetBlock(string blockPaletteIdExpression)
        {
            this.blockPaletteIdExpression = blockPaletteIdExpression;
        }

        public override StatementSelection Execute(Module module, StatementSelection selection)
        {
            int blockPaletteId = (int)MathParser.ParseString(blockPaletteIdExpression, module.variableContext);

            foreach(ShapePointData shapePointData in ShapeProcessor.IterateBlocks(selection))
            {
                int blockId = BlockPalette.GetBlockExpression(blockPaletteId, shapePointData);

                module.Grammar.LSystem.SetBlock(shapePointData.blockPos, blockId);
            }

            return selection;
        }
    }
}
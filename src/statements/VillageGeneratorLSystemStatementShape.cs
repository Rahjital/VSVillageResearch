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
    public class StatementShape : Statement
    {
        private Shape shape;

        public StatementShape(Shape shape)
        {
            this.shape = shape;
        }

        public override StatementSelection Execute(Module module, StatementSelection selection)
        {
            return new StatementSelection(shape, selection.position, selection.rotation, selection.size);
        }
    }
}
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
    public class StatementRotate : Statement
    {
        public enum Axis
        {
            X,
            Y,
            Z
        }

        private Axis axis;
        private string statement;

        public StatementRotate(Axis axis, string statement)
        {
            this.axis = axis;
            this.statement = statement;
        }

        public override StatementSelection Execute(Module module, StatementSelection selection)
        {
            float rotation = MathParser.ParseString(statement, module.variableContext);

            MathHelper.Axis helperAxis;

            switch (axis)
            {
                case Axis.X: helperAxis = MathHelper.Axis.X; break;
                case Axis.Y: helperAxis = MathHelper.Axis.Y; break;
                case Axis.Z: helperAxis = MathHelper.Axis.Z; break;
                default: throw new Exception("Unknown rotation axis in StatementRotate");
            }

            float[] newRotation = MathHelper.RotateMatrix(selection.rotation, helperAxis, rotation);

            return new StatementSelection(selection.shape,
                selection.position,
                newRotation,
                selection.size);
        }
    }
}
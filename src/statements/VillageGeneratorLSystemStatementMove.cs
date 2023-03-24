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
    public class StatementMove : Statement
    {
        private string xStatement;
        private string yStatement;
        private string zStatement;

        public StatementMove(string xStatement, string yStatement, string zStatement)
        {
            this.xStatement = xStatement;
            this.yStatement = yStatement;
            this.zStatement = zStatement;
        }

        public override StatementSelection Execute(Module module, StatementSelection selection)
        {
            Vec3f vector = new Vec3f((float)Math.Round(MathParser.ParseString(xStatement, module.variableContext)), 
                (float)Math.Round(MathParser.ParseString(yStatement, module.variableContext)), 
                (float)Math.Round(MathParser.ParseString(zStatement, module.variableContext)));

            Vec3f rotatedVector = MathHelper.RotateVector(vector, selection.rotation);

            return new StatementSelection(selection.shape,
                new Vec3f(selection.position.X + rotatedVector.X, selection.position.Y + rotatedVector.Y, selection.position.Z + rotatedVector.Z),
                selection.rotation,
                selection.size);
        }
    }
}
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
    public class StatementExpand : Statement
    {
        public enum Axis
        {
            X,
            Y,
            Z
        }

        public enum Direction
        {
            Positive,
            Negative,
            Both
        }

        private string statement;
        private Axis axis;
        private Direction direction;

        public StatementExpand(Axis axis, Direction direction, string statement)
        {
            this.statement = statement;
            this.axis = axis;
            this.direction = direction;
        }

        public override StatementSelection Execute(Module module, StatementSelection selection)
        {
            float sizeChange = MathParser.ParseString(statement, module.variableContext);

            float pos;
            float size;

            switch (axis)
            {
                case Axis.X: pos = selection.position.X; size = selection.size.X; break;
                case Axis.Y: pos = selection.position.Y; size = selection.size.Y; break;
                case Axis.Z: pos = selection.position.Z; size = selection.size.Z; break;
                default: throw new Exception("Unknown Axis in StatementExpand");
            }

            switch (direction)
            {
                case Direction.Both: size += (2 * sizeChange); break;
                case Direction.Positive: size += sizeChange; pos += (sizeChange / 2f); break;
                case Direction.Negative: size += sizeChange; pos -= (sizeChange / 2f); break;
                default: throw new Exception("Unknown Direction in StatementExpand");
            }

            Vec3f newSize;
            Vec3f newPosition;
            
            switch (axis)
            {
                case Axis.X: 
                    newSize = new Vec3f(size, selection.size.Y, selection.size.Z); 
                    newPosition = new Vec3f(pos, selection.position.Y, selection.position.Z);
                    break;
                case Axis.Y: 
                    newSize = new Vec3f(selection.size.X, size, selection.size.Z); 
                    newPosition = new Vec3f(selection.size.X, pos, selection.position.Z);
                    break;
                case Axis.Z: 
                    newSize = new Vec3f(selection.size.X, selection.size.Y, size); 
                    newPosition = new Vec3f(selection.position.X, selection.position.Y, pos);
                    break;
                default: throw new Exception("Unknown Axis in StatementExpand");
            }

            return new StatementSelection(selection.shape,
                newPosition,
                selection.rotation,
                newSize
            );
        }
    }
}
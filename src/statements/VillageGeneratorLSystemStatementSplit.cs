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
    public class StatementSplit : Statement
    {
        public enum SliceType
        {
            X,
            Y,
            Z
        }

        public struct Slice
        {
            public string size;
            public StatementList statementList;

            public Slice(string size, StatementList statementList)
            {
                this.size = size;
                this.statementList = statementList;
            }
        }

        private SliceType sliceType;
        private Slice[] slices;

        public StatementSplit(SliceType sliceType, params Slice[] slices)
        {
            this.sliceType = sliceType;
            this.slices = slices;
        }

        public override StatementSelection Execute(Module module, StatementSelection selection)
        {
            int totalSpace;
            int sliceStartDistance = 0;
            int sliceEndDistance = 0;

            switch (sliceType)
            {
                case SliceType.X: totalSpace = (int)selection.size.X; break;
                case SliceType.Y: totalSpace = (int)selection.size.Y; break;
                case SliceType.Z: totalSpace = (int)selection.size.Z; break;
                default: throw new Exception("Unknown slice type!");
            }

            int distanceOffset = totalSpace / 2;

			StatementList[] statementListHolder = new StatementList[slices.Length];
			StatementSelection[] statementSelectionHolder = new StatementSelection[slices.Length];

            for (int i = 0; i < slices.Length; i++)
            {
                int remainingSpace = totalSpace - (sliceStartDistance + sliceEndDistance);

                if (remainingSpace <= 0) { throw new Exception($"Module {module.Name} tried to split, but there's no space left for the #{i} slice!"); }

                bool isFromStart = i % 2 == 0;
                int sliceI = isFromStart ? (i / 2) : slices.Length - 1 - (i / 2);

                Slice currentSlice = slices[sliceI];

                int sliceSize = (int)Math.Round(MathParser.ParseString(currentSlice.size, module.variableContext));
                if (sliceSize > remainingSpace) { sliceSize = remainingSpace; }

                Vec3f position = selection.position.Clone();
                Vec3f size = selection.size.Clone();

                if (isFromStart)
                {
                    switch (sliceType)
                    {
                        case SliceType.X:
                            position.X = selection.position.X - distanceOffset + sliceStartDistance + sliceSize / 2;
                            size.X = sliceSize;
                            break;
                        case SliceType.Y:
                            position.Y = selection.position.Y - distanceOffset + sliceStartDistance + sliceSize / 2;
                            size.Y = sliceSize;
                            break;
                        case SliceType.Z:
                            position.Z = selection.position.Z - distanceOffset + sliceStartDistance + sliceSize / 2;
                            size.Z = sliceSize;
                            break;
                    }

                    sliceStartDistance += sliceSize;
                }
                else
                {
                    switch (sliceType)
                    {
                        case SliceType.X:
                            position.X = selection.position.X + (distanceOffset) - sliceEndDistance - sliceSize / 2;
                            size.X = sliceSize;
                            break;
                        case SliceType.Y:
                            position.Y = selection.position.Y + (distanceOffset) - sliceEndDistance - sliceSize / 2;
                            size.Y = sliceSize;
                            break;
                        case SliceType.Z:
                            position.Z = selection.position.Z + (distanceOffset) - sliceEndDistance - sliceSize / 2;
                            size.Z = sliceSize;
                            break;
                    }

                    sliceEndDistance += sliceSize;
                }

                if (currentSlice.statementList == null) { continue; }

                StatementSelection statementSelection = new StatementSelection(selection.shape, position, selection.rotation, size);

				// These two lines added to make things happen by line order in the debugger
				statementListHolder[isFromStart ? slices.Length - 1 - i : i] = currentSlice.statementList;
				statementSelectionHolder[isFromStart ? slices.Length - 1 - i : i] = statementSelection;

                //module.Grammar.AddStatementListData(module, currentSlice.statementList, statementSelection);
            }

			for (int i = 0; i < slices.Length; i++)
            {
				module.Grammar.AddStatementListData(module, statementListHolder[i], statementSelectionHolder[i]);
			}

            return selection;
        }
    }
}
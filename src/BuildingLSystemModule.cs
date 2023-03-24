using System;
using System.Collections.Generic;
using Vintagestory.Common;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using ProtoBuf;

using Cairo;

using TCParser;

namespace VillageResearch
{
	// -----
	public class ModuleVariableContext : IVariableContext
	{
		private Module module;

		public ModuleVariableContext(Module module)
		{
			this.module = module;
		}

		float IVariableContext.ResolveVariable(string name)
		{
			return module.GetVariable(name);
		}
	}

	public class Module
	{
		public Grammar Grammar {get; set;}
		public string Name {get; private set;}
		public int Id {get; set;}

		private Dictionary<string, float> variables = new Dictionary<string, float>();

		public ModuleVariableContext variableContext;

		public Module(string name, BlockPos startPos, BlockPos endPos, Dictionary<string, float> variables = null)
		{
			this.Name = name;
			this.variables = variables ?? new Dictionary<string, float>();

			SetDimensions(startPos, endPos);

			variableContext = new ModuleVariableContext(this);
		}

		public Module(string name, Vec3f position, Vec3f size, float[] rotation, Dictionary<string, float> variables = null)
		{
			this.Name = name;
			this.variables = variables ?? new Dictionary<string, float>();

			SetDimensions(position, size, rotation);

			variableContext = new ModuleVariableContext(this);
		}

		public void SetDimensions(BlockPos start, BlockPos end)
		{
			//Vec3f position = ((start.ToVec3f() + end.ToVec3f().Add(1f, 1f, 1f)) / 2).Add(-0.5f, -0.5f, -0.5f);
			Vec3f position = (start.ToVec3f() + end.ToVec3f().Add(1f, 1f, 1f)) / 2;
			Vec3f size = (end.ToVec3f().Add(1f, 1f, 1f) - start.ToVec3f());

			SetDimensions(position, size, null);
		}

		public void SetDimensions(Vec3f position, Vec3f size, float[] rotation)
		{
			SetVariable("positionX", position.X);
			SetVariable("positionY", position.Y);
			SetVariable("positionZ", position.Z);

			SetVariable("sizeX", size.X);
			SetVariable("sizeY", size.Y);
			SetVariable("sizeZ", size.Z);

			// Set rotation to passed rotation, or an identity matrix if none is passed
			SetVariable("rotation00", rotation != null ? rotation[0] : 1f);
			SetVariable("rotation01", rotation != null ? rotation[1] : 0f);
			SetVariable("rotation02", rotation != null ? rotation[2] : 0f);
			SetVariable("rotation10", rotation != null ? rotation[3] : 0f);
			SetVariable("rotation11", rotation != null ? rotation[4] : 1f);
			SetVariable("rotation12", rotation != null ? rotation[5] : 0f);
			SetVariable("rotation20", rotation != null ? rotation[6] : 0f);
			SetVariable("rotation21", rotation != null ? rotation[7] : 0f);
			SetVariable("rotation22", rotation != null ? rotation[8] : 1f);
		}

		public float[] GetRotationMatrix()
		{
			return new float[] {
				GetVariable("rotation00"), GetVariable("rotation01"), GetVariable("rotation02"),
				GetVariable("rotation10"), GetVariable("rotation11"), GetVariable("rotation12"),
				GetVariable("rotation20"), GetVariable("rotation21"), GetVariable("rotation22")
			};
		}

		public Dictionary<string, float> GetVariablesCopy()
		{
			return new Dictionary<string, float>(variables);
		}

		private bool GetSpecialVariable(string varName, out float result)
		{
			result = 0f;

			switch(varName)
			{
				case "width":
					result = Math.Abs(GetVariable("sizeX"));
					return true;
				case "height":
					result = Math.Abs(GetVariable("sizeY"));
					return true;
				case "length":
					result = Math.Abs(GetVariable("sizeZ"));
					return true;
			}

			return false;
		}

		public float GetVariable(string varName)
		{
			float result;

			if (GetSpecialVariable(varName, out result))
			{
				return result;
			}

			if (variables.TryGetValue(varName, out result))
			{
				return variables[varName];
			}
			
			throw new Exception($"Module {Name} does not have variable {varName} set!");
		}

		public void SetVariable(string varName, float value)
		{
			if (varName == null)
			{
				throw new Exception($"Trying to set variable {varName} to null in module {Name}!");
			}

			variables[varName] = value;
		}

		public IEnumerable<KeyValuePair<string, float>> ListVariables(bool includeSpecial = false)
		{
			foreach (KeyValuePair<string, float> kvPair in variables)
			{
				if (includeSpecial)
				{
					yield return kvPair;
				}
				else
				{
					switch (kvPair.Key)
					{
						case "positionX": case "positionY": case "positionZ":
						case "sizeX": case "sizeY": case "sizeZ":
						case "rotation00": case "rotation01": case "rotation02":
						case "rotation10": case "rotation11": case "rotation12":
						case "rotation20": case "rotation21": case "rotation22":
							break;

						default: yield return kvPair; break;
					}
				}
			}
		}

		public StatementSelection GetBaseSelection()
		{
			return new StatementSelection(Shape.Box, 
				new Vec3f(GetVariable("positionX"), GetVariable("positionY"), GetVariable("positionZ")),
				GetRotationMatrix(),
				new Vec3f(GetVariable("sizeX"), GetVariable("sizeY"), GetVariable("sizeZ")));
		}
	}
}
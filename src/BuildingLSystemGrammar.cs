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
	public class Grammar
	{
		public LinkedList<Module> Modules {get; private set;} = new LinkedList<Module>();
		public Statement CurrentStatement
		{
			get
			{
				if (statementListDataList.Count > 0)
				{
					StatementListData statementListData = statementListDataList[statementListDataList.Count - 1];

					return statementListData.StatementList.GetStatementAt(statementListData.CurrentStatementIndex);
				}

				return null;
			}
		}

		public Module CurrentModule	{ get => currentModuleNode?.Value ?? Modules.First?.Value; }

		private Dictionary<string, List<Rule>> rules = new Dictionary<string, List<Rule>>();
		List<StatementListData> statementListDataList = new List<StatementListData>();
		LinkedListNode<Module> currentModuleNode;
		private int currentModuleIndex;
		private bool finished;

		public BuildingLSystem LSystem {get; private set;}

		public Grammar(BuildingLSystem lSystem)
		{
			this.LSystem = lSystem;
		}

		public void SetAxiomModule(Module axiom)
		{
			currentModuleIndex = 0;

			Modules.Clear();
			Modules.AddLast(axiom);

			axiom.Grammar = this;
			axiom.Id = currentModuleIndex++;

			FetchModuleStatementList(axiom);
		}

		public void AddModuleRule(string moduleName, Rule rule)
		{
			if (!rules.ContainsKey(moduleName))
			{
				rules[moduleName] = new List<Rule>();
			}

			rules[moduleName].Add(rule);
		}

		public void Run()
		{
			// Keep evaluating until all modules fully execute
			int i = 0;

			LinkedListNode<Module> currentNode = Modules.First;
			LinkedListNode<Module> nextNode;

			while (Modules.Count > 0 && i < 1000)
			{
				currentNode = Modules.First;

				while (currentNode != null)
				{
					nextNode = currentNode.Next;

					Module module = currentNode.Value;

					List<Rule> moduleRules;

					if (!rules.TryGetValue(module.Name, out moduleRules))
					{
						throw new Exception($"Grammar has no rule for module {module.Name}!");
					}

					foreach (Rule rule in moduleRules)
					{
						if (rule.EvaluateAll(module))
						{
							break;
						}
					}

					Modules.Remove(currentNode);
					currentNode = nextNode;

					i++;
				}

				// TODO: change the limit from silent fail to error/exception?
			}
		}
		
		private class StatementListData
		{
			public StatementList StatementList {get; private set;}
			public int CurrentStatementIndex  {get; private set;} = 0;
			public StatementSelection StatementSelection {get; set;}
			public Module Module {get; private set;}

			public StatementListData(Module module, StatementList statementList, StatementSelection statementSelection)
			{
				Module = module;
				StatementList = statementList;
				StatementSelection = statementSelection;
			}

			public void IncrementStatementIndex()
			{
				CurrentStatementIndex++;
			}
		}

		public bool EvaluateNextStatement()
		{
			if (finished)
			{
				return false;
			}

			if (currentModuleNode == null)
			{
				currentModuleNode = Modules.First;
			}

			Module module = currentModuleNode.Value;

			// There can be zero statement lists if all conditions fail, or if an empty statement list gets ignored above
			if (statementListDataList.Count > 0)
			{
				int statementListIndex = statementListDataList.Count - 1;
				StatementListData statementListData = statementListDataList[statementListIndex];
				
				statementListData.StatementSelection = statementListData.StatementList.ExecuteAt(module, statementListData.StatementSelection, statementListData.CurrentStatementIndex);

				if (statementListData.CurrentStatementIndex < statementListData.StatementList.StatementCount - 1)
				{
					statementListData.IncrementStatementIndex();
				}
				else
				{
					statementListDataList.RemoveAt(statementListIndex);
				}
			}

			if (statementListDataList.Count == 0)
			{
				LinkedListNode<Module> nextModule = currentModuleNode.Next;
				Modules.Remove(currentModuleNode);
				currentModuleNode = nextModule ?? Modules.First;

				if (currentModuleNode != null)
				{
					FetchModuleStatementList(currentModuleNode.Value);
				}
			}

			finished = Modules.Count <= 0;
			return !finished;
		}

		public void FetchModuleStatementList(Module module)
		{
			if (statementListDataList.Count == 0)
			{
				List<Rule> moduleRules;

				if (!rules.TryGetValue(module.Name, out moduleRules))
				{
					throw new Exception($"Grammar has no rule for module {module.Name}!");
				}

				foreach (Rule rule in moduleRules)
				{
					if (rule.EvaluateCondition(module))
					{
						AddStatementListData(module, rule.GetStatementList(), module.GetBaseSelection());
						break;
					}
				}
			}
		}

		public void InsertModule(Module sourceModule, Module newModule)
		{
			LinkedListNode<Module> sourceModuleNode = Modules.Find(sourceModule);

			newModule.Grammar = this;
			newModule.Id = currentModuleIndex++;
			Modules.AddBefore(sourceModuleNode, newModule);
		}

		public void AddStatementListData(Module module, StatementList statementList, StatementSelection statementSelection)
		{
			if (statementList.StatementCount > 0)
			{
				statementListDataList.Add(new StatementListData(module, statementList, statementSelection));
			}
		}
	}
}
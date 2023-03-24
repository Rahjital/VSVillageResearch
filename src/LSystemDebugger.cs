using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using ProtoBuf;

using GuiExtensions;

using Cairo;

namespace VillageResearch
{
	public class LSystemDebugger : ModSystem
	{
		// Clientside
		private ICoreClientAPI capi;
		private IClientNetworkChannel clientChannel;

		private GuiDialogLSystemDebugger debuggerDialog;
		private int autoAdvanceDelayMs = -1;
		private long autoAdvanceListenerId = -1;

		public BuildingLSystem lSystem {get; private set;}

		public override void StartClientSide(ICoreClientAPI api)
		{
			capi = api;

			clientChannel = capi.Network.RegisterChannel("lsystemdebugger")
				.RegisterMessageType(typeof(MessageBlockCommit))
			;

			debuggerDialog = new GuiDialogLSystemDebugger(capi, this);

			capi.Input.RegisterHotKey("lsystemdebugger", "Village data visualisation GUI", GlKeys.Tilde, HotkeyType.DevTool, false, true);
			capi.Input.SetHotKeyHandler("lsystemdebugger", OnGuiHotkey);
		
			capi.RegisterCommand("dbuild", "[description]", "[syntax]", CmdGenBuildingDebug);
		}

		private void CmdGenBuildingDebug(int groupId, CmdArgs args)
		{
			if (lSystem is null)
			{
				lSystem = new BuildingLSystem(capi);
			}
			lSystem.Generate(capi.World.Player.Entity.Pos.AsBlockPos, args.PopWord() ?? "test", true);

			debuggerDialog.ClearAndRecompose();
			debuggerDialog.TryOpen();
		}

		public bool OnGuiHotkey(KeyCombination comb)
		{
			debuggerDialog.Toggle();
			return true;
		}

		public bool StepForward()
		{
			bool keepRunning = lSystem.TryStepForward();

			Dictionary<BlockPos, int> blockUpdates = lSystem.PopBlockUpdates();

			if (blockUpdates.Count > 0)
			{
				clientChannel.SendPacket<MessageBlockCommit>(new MessageBlockCommit() {
					Blocks = blockUpdates
				});
			}

			debuggerDialog.ClearAndRecompose();

			return keepRunning;
		}

		public void StepAuto()
		{
			if (autoAdvanceListenerId == -1)
			{
				autoAdvanceDelayMs = 350;
				autoAdvanceListenerId = capi.Event.RegisterGameTickListener((float delta) => 
					{
						if (!StepForward()) PauseStepAuto();
					}, 
					autoAdvanceDelayMs);
			}
		}

		public void PauseStepAuto()
		{
			if (autoAdvanceListenerId >= 0)
			{
				autoAdvanceDelayMs = -1;
				capi.Event.UnregisterGameTickListener(autoAdvanceListenerId);
				autoAdvanceListenerId = -1;
			}
		}
	}

	public class GuiDialogLSystemDebugger : GuiDialog
	{
		public override string ToggleKeyCombinationCode => "lsystemdebugger";
		public override bool PrefersUngrabbedMouse => false;

		private LSystemDebugger lSystemDebugger;
		private List<GuiElementDebuggerLine> moduleLines = new List<GuiElementDebuggerLine>();
		private Module selectedModule;
		private Vec4f primaryHighlightColour = new Vec4f(1, 1, 0, 0.2f);
		private Vec4f secondaryHighlightColour = new Vec4f(1, 1, 1, 0.1f);

		private bool recomposeDirty = true;

		public GuiDialogLSystemDebugger(ICoreClientAPI capi, LSystemDebugger lSystemDebugger) : base(capi)
		{
			this.lSystemDebugger = lSystemDebugger;

			SetupDialog();
		}

		public void SetupDialog(bool clear = false)
		{
			// Auto-sized dialog at the center of the screen
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithFixedOffset(-400, -300);

			if (clear)
			{
				SingleComposer.Clear(dialogBounds);
			}

			int modulesWidth = 200;
			int rulesWidth = 300;
			int modulesHeight = 300;
			int variablesHeight = 200;

			int moduleMargin = 16;

			// Background boundaries
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;

			// Play bar
			ElementBounds playBarBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0, GuiStyle.TitleBarHeight, 128, 48);
			playBarBounds.BothSizing = ElementSizing.FitToChildren;

			ElementBounds pauseButton = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 0, 0, 16, 16).WithFixedPadding(4, 4);
			ElementBounds advanceOneButton = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 0, 0, 16, 16).WithFixedPadding(4, 4);
			ElementBounds advancePlayButton = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 0, 0, 16, 16).WithFixedPadding(4, 4);
			ElementBounds advanceAllButton = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 0, 0, 16, 16).WithFixedPadding(4, 4);

			// Module area
			ElementBounds moduleFullBounds = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 0, 0, modulesWidth + rulesWidth + moduleMargin, modulesHeight + variablesHeight + moduleMargin).FixedUnder(playBarBounds);
			ElementBounds moduleListBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0, 0, modulesWidth, modulesHeight);
			ElementBounds moduleVariableBounds = ElementBounds.Fixed(EnumDialogArea.LeftBottom, 0, 0, modulesWidth, variablesHeight);
			ElementBounds rulesBounds = ElementBounds.Fixed(EnumDialogArea.RightTop, 0, 0, rulesWidth, modulesHeight + variablesHeight + moduleMargin);

			bgBounds.WithChildren(playBarBounds, moduleFullBounds);
			playBarBounds.WithChildren(pauseButton, advanceOneButton, advancePlayButton, advanceAllButton);
			moduleFullBounds.WithChildren(moduleListBounds, moduleVariableBounds, rulesBounds);

			SingleComposer = capi.Gui.CreateCompo("lsystemdebuggerdialog", dialogBounds)
				.AddShadedDialogBG(bgBounds, true)
				.AddDialogTitleBar("L-System Debugger", OnTitleBarClose)
						// Play bar
						.AddSmallButton("II", () => {lSystemDebugger.PauseStepAuto(); return true;}, pauseButton)
						.AddSmallButton("+1", () => {lSystemDebugger.StepForward(); return true;}, advanceOneButton.FixedRightOf(pauseButton, 16))
						.AddSmallButton(">", () => {lSystemDebugger.StepAuto(); return true;}, advancePlayButton.FixedRightOf(advanceOneButton, 16))
						.AddSmallButton(">>>", () => {return true;}, advanceAllButton.FixedRightOf(advancePlayButton, 16));

						// Module list
			SingleComposer
						.AddScrollableArea(moduleListBounds, (composer, scrollableBounds) => {
							ComposeModuleList(composer, scrollableBounds);
						}, key: "moduleListArea")
						// Module variable list
						.AddScrollableArea(moduleVariableBounds, (composer, scrollableBounds) => {
							ComposeVariableList(composer, scrollableBounds);
						}, key: "moduleVariableListArea")
						// Rule list
						.AddScrollableArea(rulesBounds, (composer, scrollableBounds) => {
							ComposeScriptList(composer, scrollableBounds);
						}, key: "moduleRuleListArea")
				.Compose()
			;

			SingleComposer.GetScrollableArea("moduleListArea").CalcTotalHeight();
			SingleComposer.GetScrollableArea("moduleVariableListArea").CalcTotalHeight();
			SingleComposer.GetScrollableArea("moduleRuleListArea").CalcTotalHeight();

			recomposeDirty = false;
		}

		public void ComposeModuleList(GuiElementContainer container, ElementBounds listBounds)
		{
			ElementBounds currentBounds = null;
			
			if (lSystemDebugger.lSystem?.Grammar?.Modules is null) return;

			int i = 0;

			moduleLines.Clear();

			bool selectedModuleExists = false;

			foreach (Module module in lSystemDebugger.lSystem.Grammar.Modules)
			{
				currentBounds = currentBounds != null ? 
					ElementBounds.Percentual(EnumDialogArea.CenterTop, 1, 1).WithFixedSize(0, 24).FixedUnder(currentBounds, 1) : 
					ElementBounds.Percentual(EnumDialogArea.CenterTop, 1, 1).WithFixedSize(0, 24);

				currentBounds.horizontalSizing = ElementSizing.Percentual;
				//currentBounds.verticalSizing = ElementSizing.FitToChildren; // Bleh, I really need this but it's broken :S
				// ElementSizing.FitToChildren crashes when a child has ElementSizing.Percentual, even when Percentual is horizontal and 
				// FitToChildren is vertical, so logically they shouldn't clash. Error is in buildBoundsFromChildren()
				currentBounds.verticalSizing = ElementSizing.Fixed;
				listBounds.WithChild(currentBounds);

				int j = i;
				GuiElementDebuggerLine debuggerLine = new GuiElementDebuggerLine(capi, (GuiElementDebuggerLine line) => {ModuleSelected(module, line, j); return true;}, currentBounds);
				container.Add(debuggerLine);
				moduleLines.Add(debuggerLine);

				if (selectedModule?.Id == module.Id)
				{
					debuggerLine.HighlightColor = primaryHighlightColour;
					selectedModuleExists = true;
				} 
				else if (lSystemDebugger.lSystem.Grammar.CurrentModule.Id == module.Id)
				{
					debuggerLine.HighlightColor = secondaryHighlightColour;
				}

				ElementBounds currentTextBounds = ElementBounds.Percentual(EnumDialogArea.LeftMiddle, 1, 1).WithFixedHeight(20);
				currentTextBounds.horizontalSizing = ElementSizing.Percentual;
				currentTextBounds.verticalSizing = ElementSizing.Fixed;
				currentBounds.WithChild(currentTextBounds);

				GuiElementDynamicText dynamicText = new GuiElementDynamicText(capi, $"{module.Name} (#{module.Id})", CairoFont.WhiteSmallText(), currentTextBounds);
				container.Add(dynamicText);

				i++;
			}

			if (!selectedModuleExists)
			{
				selectedModule = null;
			}
		}

		private void ModuleSelected(Module module, GuiElementDebuggerLine line, int lineIndex)
		{
			selectedModule = module;
			line.HighlightColor = primaryHighlightColour;
			UnhilightLinesInList(moduleLines, lineIndex);

			ClearAndRecompose();
		}

		private void UnhilightLinesInList(List<GuiElementDebuggerLine> lines, int skipLine = -1)
		{
			for (int i = 0; i < lines.Count; i++)
			{
				if (i != skipLine && lines[i].HighlightColor == primaryHighlightColour)
				{
					lines[i].HighlightColor = null;
				}
			}
		}

		public void ComposeVariableList(GuiElementContainer container, ElementBounds listBounds)
		{
			ElementBounds currentBounds = null;
			
			if (selectedModule is null && lSystemDebugger?.lSystem?.Grammar?.CurrentModule is null) return;

			Module scriptModule = selectedModule ?? lSystemDebugger.lSystem.Grammar.CurrentModule;

			bool initDone = false;

			CairoFont variableFont = CairoFont.WhiteSmallText().WithFontSize(14f);

			foreach (KeyValuePair<string, float> kvPair in scriptModule.ListVariables(false))
			{
				currentBounds = currentBounds != null ? 
					ElementBounds.Percentual(EnumDialogArea.CenterTop, 1, 1).WithFixedSize(0, 20).FixedUnder(currentBounds, 4) : 
					ElementBounds.Percentual(EnumDialogArea.CenterTop, 1, 1).WithFixedSize(0, 20);

				currentBounds.horizontalSizing = ElementSizing.Percentual;
				//currentBounds.verticalSizing = ElementSizing.FitToChildren; // Bleh, I really need this but it's broken :S
				// ElementSizing.FitToChildren crashes when a child has ElementSizing.Percentual, even when Percentual is horizontal and 
				// FitToChildren is vertical, so logically they shouldn't clash. Error is in buildBoundsFromChildren()
				currentBounds.verticalSizing = ElementSizing.Fixed;
				listBounds.WithChild(currentBounds);

				ElementBounds currentKeyBounds = currentBounds.FlatCopy();
				currentKeyBounds.Alignment = EnumDialogArea.LeftTop;
				currentKeyBounds.percentWidth = 0.6;

				GuiElementDynamicText dynamicTextKey = new GuiElementDynamicText(capi, $"{kvPair.Key}", variableFont, currentKeyBounds);
				container.Add(dynamicTextKey);

				ElementBounds currentValueBounds = currentBounds.FlatCopy();
				currentValueBounds.Alignment = EnumDialogArea.RightTop;
				currentValueBounds.percentWidth = 0.4;

				GuiElementScrollableTextInput textInput = new GuiElementScrollableTextInput(capi, currentValueBounds, (string val) => {if (initDone && float.TryParse(val, out float result)) {selectedModule.SetVariable(kvPair.Key, result);}}, CairoFont.WhiteSmallText());

				container.Add(textInput);
				textInput.LoadValue(kvPair.Value.ToString());
			}

			initDone = true;
		}

		public void ComposeScriptList(GuiElementContainer container, ElementBounds listBounds)
		{			
			if (lSystemDebugger?.lSystem?.Script is null || lSystemDebugger?.lSystem?.Grammar?.CurrentModule is null) return;

			Module scriptModule = selectedModule ?? lSystemDebugger.lSystem.Grammar.CurrentModule;

			ElementBounds currentBounds = null;
			int i = 0;

			CairoFont scriptFont = CairoFont.WhiteSmallText().WithFontSize(12f);
			CairoFont lineNumFont = CairoFont.WhiteSmallText().WithFontSize(10f).WithColor(new double[] {197 / 255.0, 137 / 255.0, 72 / 255.0, 0.7});

			int executingLine = lSystemDebugger.lSystem.Grammar.CurrentStatement?.SourceLine ?? -1;

			int currentLine = -1;
			int ruleNum = -1;
			int ruleLine = -1;
			bool foundRule = false;
			int braceCount = 0;

			int lineHeight = 16;

			foreach (string line in lSystemDebugger.lSystem.Script.Split('\n'))
			{
				currentLine++;

				string trimmedLine = line.TrimStart();

				if (!foundRule)
				{
					Match regexMatch = Regex.Match(trimmedLine, @"^\w+");

					foundRule = regexMatch.Success && regexMatch.Value == scriptModule.Name;
					ruleNum++;
					ruleLine = -1;

					if (!foundRule) continue;
				}

				ruleLine++;

				if (trimmedLine[0] == '{') braceCount++;
				if (trimmedLine[0] == '}' && --braceCount == 0) 
				{
					foundRule = false;
				}

				currentBounds = currentBounds != null ? 
					ElementBounds.Percentual(EnumDialogArea.RightTop, 1, 1).WithFixedSize(0, lineHeight).FixedUnder(currentBounds, ruleNum > 0 && ruleLine == 0 ? lineHeight + 1 : 1) : 
					ElementBounds.Percentual(EnumDialogArea.RightTop, 1, 1).WithFixedSize(0, lineHeight);

				currentBounds.horizontalSizing = ElementSizing.Percentual;
				//currentBounds.verticalSizing = ElementSizing.FitToChildren; // Bleh, I really need this but it's broken :S
				// ElementSizing.FitToChildren crashes when a child has ElementSizing.Percentual, even when Percentual is horizontal and 
				// FitToChildren is vertical, so logically they shouldn't clash. Error is in buildBoundsFromChildren()
				currentBounds.verticalSizing = ElementSizing.Fixed;
				listBounds.WithChild(currentBounds);

				int j = i;
				GuiElementDebuggerLine debuggerLine = new GuiElementDebuggerLine(capi, (GuiElementDebuggerLine line) => {return true;}, currentBounds);
				container.Add(debuggerLine);
				moduleLines.Add(debuggerLine);

				if (currentLine == executingLine)
				{
					debuggerLine.HighlightColor = primaryHighlightColour;
				}

				ElementBounds currentTextBounds = ElementBounds.Percentual(EnumDialogArea.RightMiddle, 1, 1).WithFixedSize(24, 16);
				currentTextBounds.horizontalSizing = ElementSizing.PercentualSubstractFixed;
				currentTextBounds.verticalSizing = ElementSizing.Fixed;
				currentBounds.WithChild(currentTextBounds);

				GuiElementDynamicText dynamicText = new GuiElementDynamicText(capi, line, scriptFont, currentTextBounds);
				container.Add(dynamicText);

				ElementBounds lineNumBounds = ElementBounds.FixedSize(24, 16).WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(4, 2);
				currentBounds.WithChild(lineNumBounds);

				GuiElementDynamicText lineNumText = new GuiElementDynamicText(capi, (currentLine + 1).ToString(), lineNumFont, lineNumBounds);
				container.Add(lineNumText);

				i++;
			}
		}

		public void ClearAndRecompose()
		{
			if (!recomposeDirty) {
				capi.Event.RegisterCallback((dt) => {
					SetupDialog(true);
					SingleComposer.ReCompose();
				}, 1);

				recomposeDirty = true;
			}
		}

		private void OnTitleBarClose()
		{
			TryClose();
		}
	}
}
﻿// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Numerics;
	using System.Runtime.CompilerServices;
	using Anamnesis.Posing;
	using CustomizePlus.Data;
	using CustomizePlus.Data.Configuration;
	using Dalamud.Interface;
	using Dalamud.Interface.Components;
	using Dalamud.Logging;
	using Dalamud.Utility;
	using ImGuiNET;

	public class EditInterface : WindowBase
	{
		protected override string Title => $"(WIP) Edit Scale: {this.BackupScale.ScaleName}";
		protected override string DrawTitle => $"{this.Title}###customize_plus_scale_edit_window{this.Index}"; //keep the same ID for all scale editor windows

		protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoScrollbar |
			(this.dirty ? ImGuiWindowFlags.UnsavedDocument : ImGuiWindowFlags.None);
        private int precision = 3;

        protected override bool LockCloseButton => this.dirty;

		protected BodyScale BackupScale { get; private set; }
		protected BodyScale WorkingScale { get; private set; }

		private bool dirty = false;

		private Dictionary<BoneData.BoneFamily, bool> boneGroups = new();

		private EditMode mode;
		private bool mirrorMode;
		private float windowHorz; //for formatting :V

		public static void Show(BodyScale scale)
		{
			EditInterface editWnd = Plugin.InterfaceManager.Show<EditInterface>();
			editWnd.mode = EditMode.Scale;

			if (scale == null)
			{
				editWnd.BackupScale = new BodyScale();
			}
			else
			{
				editWnd.BackupScale = scale;
			}

			editWnd.WorkingScale = new BodyScale(editWnd.BackupScale);
			editWnd.WorkingScale.UpdateBoneList();
			editWnd.UpdateBoneGroups(editWnd.WorkingScale);
		}
		
		protected override void DrawContents()
		{
			PluginConfiguration config = Plugin.ConfigurationManager.Configuration;
			this.windowHorz = ImGui.GetWindowWidth();

			if (!this.WorkingScale.SameNamesAs(this.BackupScale))
			{
				this.dirty = true;
			}

			ImGui.SetNextItemWidth(windowHorz / 4);
			RenderTextBox("Character Name", ref this.WorkingScale.CharacterName);

			ImGui.SameLine();

			RenderCheckBox("Enable", ref this.WorkingScale.BodyScaleEnabled);

			ImGui.SetNextItemWidth(windowHorz / 4);
			RenderTextBox("Scale Name", ref this.WorkingScale.ScaleName);

			ImGui.SameLine();

			RenderCheckBox("Automatic Mode", config.AutomaticEditMode, (b) => config.AutomaticEditMode = b);
			AppendTooltip($"Applies changes automatically without saving.");

			if (RenderCheckBox("Hrothgar", this.WorkingScale.InclHroth, this.WorkingScale.ToggleHrothgarFeatures))
			{
				this.UpdateBoneGroups(this.WorkingScale);
			}
			AppendTooltip("Show bones exclusive to hrothgar");

			ImGui.SameLine();

			if (RenderCheckBox("Viera", this.WorkingScale.InclViera, this.WorkingScale.ToggleVieraFeatures))
			{
				this.UpdateBoneGroups(this.WorkingScale);
			}
			AppendTooltip("Show bones exclusive to viera");


			ImGui.SameLine();

			if (RenderCheckBox("IVCS", this.WorkingScale.InclIVCS, this.WorkingScale.ToggleIVCSFeatures))
			{
				this.UpdateBoneGroups(this.WorkingScale);
			}
			AppendTooltip("Show bones added by the Illusio Vitae Custom Skeleton mod (NSFW)");

			ImGui.SameLine();

			RenderCheckBox("Mirror Mode", ref this.mirrorMode);
			AppendTooltip($"Bone changes will be reflected from left to right and vice versa");

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();
			ImGui.SetNextItemWidth(150);
			ImGui.SliderInt("Precision", ref precision, 1, 6);

			ImGui.Separator();

			if (ImGui.RadioButton("Position", mode == EditMode.Position))
				mode = EditMode.Position;

			ImGui.SameLine();
			if (ImGui.RadioButton("Rotation", mode == EditMode.Rotation))
				mode = EditMode.Rotation;

			ImGui.SameLine();
			if (ImGui.RadioButton("Scale", mode == EditMode.Scale))
				mode = EditMode.Scale;

            if (mode != EditMode.Scale)
			{
				ImGui.SameLine();
				ImGui.PushFont(UiBuilder.IconFont);
				ImGui.Text(FontAwesomeIcon.ExclamationTriangle.ToIconString());
				ImGui.PopFont();
				ImGui.SameLine();
				ImGui.Text($"{mode} is an advanced setting and might not look properly with some animations, use at your own risk.");
			}

			ImGui.Separator();

			RenderBoneRow("n_root");

			string col1Label = mode == EditMode.Rotation ? "Yaw" : "X";
			string col2Label = mode == EditMode.Rotation ? "Pitch" : "Y";
			string col3Label = mode == EditMode.Rotation ? "Roll" : "Z";
			string col4Label = mode == EditMode.Scale ? "All" : "N/A";

			ImGui.Separator();
			ImGui.BeginTable("Bones", 6, ImGuiTableFlags.SizingStretchSame);
			ImGui.TableNextColumn();
			ImGui.Text("Bones:");
			ImGui.TableNextColumn();
			ImGui.Text(col1Label);
			ImGui.TableNextColumn();
			ImGui.Text(col2Label);
			ImGui.TableNextColumn();
			ImGui.Text(col3Label);
			ImGui.TableNextColumn();
			ImGui.Text(col4Label);
			ImGui.TableNextColumn();
			ImGui.Text("Name");
			ImGui.EndTable();
			ImGui.Separator();

			ImGui.BeginChild("scrolling", new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56), false);

			IEnumerable<string> relevantBoneNames = BoneData.GetFilteredBoneCodenames(this.WorkingScale);

			foreach(var fam in this.WorkingScale.GetUniqueFamilies())
			{
				bool tempRef = this.boneGroups[fam];
				RenderArrowToggle($"##{fam}", ref tempRef);
				ImGui.SameLine();
				RenderLabel(fam.ToString());

				if (tempRef)
				{
					ImGui.Spacing();

					foreach(string codename in relevantBoneNames.Where(x => BoneData.GetBoneFamily(x) == fam))
					{
						RenderBoneRow(codename);
					}

					ImGui.Spacing();
				}
				this.boneGroups[fam] = tempRef;

				ImGui.Separator();
			}
			/*
			foreach(string codename in relevantBoneNames)
			{
				var familyType = BoneData.GetBoneFamily(codename);
				if (priorFam != familyType)
				{
					ImGui.Separator();
					ImGui.Spacing();
					ImGui.Separator();
					RenderLabel("\t" + familyType.ToString());
					ImGui.Separator();

					priorFam = familyType;
				}

				RenderBoneRow(codename);
			}
			*/

			ImGui.EndChild();

			ImGui.Separator();

			//----------------------------------

			if (ImGui.Button("Save") && this.dirty)
			{
				void Proceed()
				{
					AddToConfig();
					Plugin.ConfigurationManager.SaveConfiguration();
					Plugin.LoadConfig();
				}

				if ((this.BackupScale.InclHroth && !this.WorkingScale.InclHroth)
					|| (this.BackupScale.InclViera && !this.WorkingScale.InclViera)
					|| (this.BackupScale.InclIVCS && !this.WorkingScale.InclIVCS))
				{
					ConfirmationDialog.Show("Certain optional bones were turned off since the last save.\n"
						+ "Saving now will reset those bones to their default state.\n\nContinue?",
						Proceed, "Confirm Bone Deletion");
				}
				else
				{
					Proceed();
				}
			}
			AppendTooltip("Save changes and continue editing");

			ImGui.SameLine();

			if (ImGui.Button("Save and Close"))
			{
				if (!this.dirty)
				{
					this.Close();
				}
				else
				{
					void Proceed()
					{
						AddToConfig();
						Plugin.ConfigurationManager.SaveConfiguration();
						Plugin.LoadConfig();
						this.Close();
					}

					if ((this.BackupScale.InclHroth && !this.WorkingScale.InclHroth)
						|| (this.BackupScale.InclViera && !this.WorkingScale.InclViera)
						|| (this.BackupScale.InclIVCS && !this.WorkingScale.InclIVCS))
					{
						ConfirmationDialog.Show("Certain optional bones were turned off since the last save.\n"
							+ "Saving now will reset those bones to their default state.\n\nContinue?",
							Proceed, "Confirm Bone Deletion");
					}
					else
					{
						Proceed();
					}
				}
			}
			AppendTooltip("Save changes and stop editing");

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			ImGui.Text("Save with new scale/character name to create a copy.");

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			if (ImGui.Button("Revert") && this.dirty) {
				ConfirmationDialog.Show("Revert all unsaved work?", RevertAll);
			}
			AppendTooltip("Remove all pending changes, reverting to last save");

			ImGui.SameLine();

			if (ImGui.Button("Cancel")) {
				if (!this.dirty)
				{
					this.Close();
				}
				else
				{
					void Close()
					{
						this.RevertAll();
						this.Close();
					}
					ConfirmationDialog.Show("Revert unsaved work and exit editor?", Close);
				}
			}
			AppendTooltip("Close the editor without saving\n(reverting all unsaved changes)");
		}

		private void UpdateBoneGroups(BodyScale bs)
		{
			Dictionary<BoneData.BoneFamily, bool> output = new();

			foreach(BoneData.BoneFamily bf in bs.GetUniqueFamilies())
			{
				if (bf == BoneData.BoneFamily.Root) continue;

				if (this.boneGroups.TryGetValue(bf, out bool prior))
				{
					output[bf] = prior;
				}
				else
				{
					output[bf] = false;
				}
			}

			this.boneGroups = output;
		}

		private void AddToConfig()
		{
			PluginConfiguration config = Plugin.ConfigurationManager.Configuration;

			BodyScale pruned = this.WorkingScale.GetPrunedScale();

			if (config.BodyScales.Remove(this.WorkingScale))
			{
				config.BodyScales.Add(pruned);
			}
			else
			{
				config.BodyScales.Add(pruned);
				Plugin.ConfigurationManager.ToggleOffAllOtherMatching(pruned);
			}

			Plugin.ConfigurationManager.SaveConfiguration();

			this.BackupScale = new BodyScale(pruned);
			this.WorkingScale = new BodyScale(BackupScale);
			this.dirty = false;
		}

		private void UpdateCurrent(string boneName, BoneEditsContainer boneValue, bool autoMode = false)
		{
			PluginConfiguration config = Plugin.ConfigurationManager.Configuration;

			this.WorkingScale.Bones[boneName] = boneValue.DeepCopy();

			if (autoMode && config.BodyScales.Remove(this.WorkingScale))
			{
				config.BodyScales.Add(this.WorkingScale);
				Plugin.ConfigurationManager.SaveConfiguration();
				Plugin.LoadConfig(true);
			}
		}

		private void RevertAll()
		{
			var config = Plugin.ConfigurationManager.Configuration;

			this.WorkingScale = new BodyScale(this.BackupScale);
			this.WorkingScale.UpdateBoneList();

			if (config.BodyScales.Remove(this.WorkingScale))
			{
				config.BodyScales.Add(this.WorkingScale);
				Plugin.ConfigurationManager.SaveConfiguration();
				Plugin.LoadConfig(true);
			}
		}


		#region ImGui helper functions

		private bool RenderTextBox(string label, ref string value)
		{
			return ImGui.InputText(label, ref value, 1024);
		}

		private bool RenderCheckBox(string label, ref bool value)
		{
			return ImGui.Checkbox(label, ref value);
		}

		private bool RenderCheckBox(string label, in bool shown, Action<bool> toggle)
		{
			bool temp = shown;
			bool toggled = ImGui.Checkbox(label, ref temp);

			if (toggled)
			{
				toggle(temp);
			}

			return toggled;
		}

		private bool RenderArrowToggle(string label, ref bool value)
		{
			bool toggled = ImGui.ArrowButton(label, value ? ImGuiDir.Down : ImGuiDir.Right);

			if (toggled)
			{
				value = !value;
			}

			return value;
		}

		private void AppendTooltip(string text)
		{
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip(text);
			}
		}

		private bool RenderResetButton(string codename, ref Vector3 value)
		{
			bool output = ImGuiComponents.IconButton(codename, FontAwesomeIcon.Recycle);
			AppendTooltip($"Reset '{BoneData.GetBoneDisplayName(codename)}' to default {this.mode} values");

			if (output)
			{
				value = this.mode switch
				{
					EditMode.Scale => Vector3.One,
					_ => Vector3.Zero
				};
			}

			return output;
		}

		private bool RenderRevertButton(string codename, ref Vector3 value)
		{
			bool output = ImGuiComponents.IconButton(codename, FontAwesomeIcon.ArrowCircleLeft);
			AppendTooltip($"Revert '{BoneData.GetBoneDisplayName(codename)}' to last saved {this.mode} values");

			if (output)
			{
				value = this.mode switch
				{
					EditMode.Position => this.BackupScale.Bones[codename].Position,
					EditMode.Rotation => this.BackupScale.Bones[codename].Rotation,
					_ => this.BackupScale.Bones[codename].Scale
				};
			}

			return output;
		}

		private bool RenderDragBox(string label, ref Vector3 value)
		{
			float velocity = this.mode == EditMode.Rotation ? 1.0f : 0.001f;
			float minValue = this.mode == EditMode.Rotation ? -360.0f : -10.0f;
			float maxValue = this.mode == EditMode.Rotation ? 360.0f : 10.0f;

			float temp = this.mode switch
			{
				EditMode.Position => 0.0f,
				EditMode.Rotation => 0.0f,
				_ => value.X == value.Y && value.Y == value.Z ? value.X : 1.0f
			};
			

			if (ImGui.DragFloat(label, ref temp, velocity, minValue, maxValue, $"%.{precision}f"))
			{
				value = new Vector3(temp, temp, temp);
				return true;
			}
			return false;
		}

		private bool RenderTripleDragBoxes(string label, ref Vector3 value)
		{
			float velocity = mode == EditMode.Rotation ? 1.0f : 0.001f;
			float minValue = mode == EditMode.Rotation ? -360.0f : -10.0f;
			float maxValue = mode == EditMode.Rotation ? 360.0f : 10.0f;

			return ImGui.DragFloat3(label, ref value, velocity, minValue, maxValue, $"%.{precision}f");
		}

		private void RenderLabel(string? text, string tooltip = "")
		{
			ImGui.Text(text ?? "Unknown Bone");
			if (!tooltip.IsNullOrWhitespace())
			{
				AppendTooltip(tooltip);
			}
		}

		private void RenderBoneRow(string codename)
		{
			if (!this.WorkingScale.TryGetBone(codename, out var currentBoneEdits) || currentBoneEdits == null)
			{
				currentBoneEdits = new BoneEditsContainer();
				this.WorkingScale.Bones.Add(codename, currentBoneEdits);
			}

			string displayName = BoneData.GetBoneDisplayName(codename) ?? codename;

			bool flagUpdate = false;

			Vector3 whichBoneVector = mode switch
			{
				EditMode.Position => currentBoneEdits.Position,
				EditMode.Rotation => currentBoneEdits.Rotation,
				_ => currentBoneEdits.Scale
			};

			ImGui.PushID(codename);

			flagUpdate |= RenderResetButton(codename, ref whichBoneVector);

			ImGui.SameLine();

			flagUpdate |= RenderRevertButton(codename, ref whichBoneVector);

			ImGui.SameLine();

			ImGui.SetNextItemWidth(this.windowHorz * 3 / 6);
			flagUpdate |= RenderTripleDragBoxes($"##{displayName}", ref whichBoneVector);

			ImGui.SameLine();

			if (mode != EditMode.Scale)
				ImGui.BeginDisabled();

			ImGui.SetNextItemWidth(this.windowHorz / 7);
			flagUpdate |= RenderDragBox($"##{displayName}AllAxes", ref whichBoneVector);

			if (mode != EditMode.Scale)
				ImGui.EndDisabled();

			ImGui.SameLine();

			RenderLabel(displayName, codename);

			ImGui.PopID();

			if (flagUpdate)
			{
				this.dirty = true;

				currentBoneEdits.UpdateVector(mode, whichBoneVector);

				//this is really ugly, but I couldn't think of a better way to pull these values out
				//and be able to pass them back further below
				string mirrorName = String.Empty;
				BoneEditsContainer? mirrorEdits = null;
				if (this.mirrorMode)
				{
					mirrorName = BoneData.GetBoneMirror(codename);
					if (mirrorName != null
						&& this.WorkingScale.TryGetMirror(codename, out mirrorEdits)
						&& mirrorEdits != null)
					{
						//IVCS bones have their axes oriented differently, so they get reflected differently
						if (BoneData.IsIVCSBone(codename))
						{
							mirrorEdits = currentBoneEdits.ReflectIVCS();
						}
						else
						{
							mirrorEdits = currentBoneEdits.ReflectAcrossZPlane();
						}
					}
				}

				if (Plugin.ConfigurationManager.Configuration.AutomaticEditMode)
				{
					this.UpdateCurrent(codename, currentBoneEdits, true);

					if (this.mirrorMode && !mirrorName.IsNullOrEmpty() && mirrorEdits != null)
					{
						this.UpdateCurrent(mirrorName, mirrorEdits, true);
					}
				}
			}
		}

		#endregion

	}

	public enum EditMode
	{
		Position,
		Rotation,
		Scale
	}


}

﻿using Dalamud.Bindings.ImGui;
using Dalamud.Utility;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoDuty.Helpers
{
    using System;
    using Dalamud.Interface;
    using Dalamud.Interface.Utility.Raii;
    using IPC;
    using System.Numerics;

    internal static class ImGuiHelper
    {
        public static readonly Vector4 ExperimentalColor  = new(1, 0, 1, 1);
        public static readonly Vector4 ExperimentalColor2 = new(1, 0.6f, 0, 1);
        public static readonly Vector4 VersionColor       = new(0, 1, 1, 1);
        public static readonly Vector4 LinkColor          = new(0, 200, 238, 1);

        public static readonly Vector4 White         = new(1, 1, 1, 1);
        public static readonly Vector4 MaxLevelColor = new(0.5f, 1, 0.5f, 1);

        public static readonly Vector4 RoleTankColor       = new(0, 0.8f, 1, 1);
        public static readonly Vector4 RoleHealerColor     = new(0, 1, 0, 1);
        public static readonly Vector4 RoleDPSColor        = new(1, 0, 0, 1);
        public static readonly Vector4 RoleAllRounderColor = new(1, 1, 0.5f, 1);


        public const string idColor               = "<0.5,0.5,1>";
        public const string dutyColor             = "<0,1,0>";
        public const string pathFileColor         = "<0.8,0.8,0.8>";
        public const string pathFileColorNoUpdate = "<0,1,1>";

        public static void ColoredText(string text)
        {
            Match regex = RegexHelper.ColoredTextRegex().Match(text);
            ColoredText(regex, text);
        }

        public static void ColoredText(Match regex, string backupText)
        {
            void SameLine() => ImGui.SameLine(0, 0);

            if (regex.Success)
            {
                bool first = true;

                do
                {
                    bool nonColoredSet = false;

                    //Svc.Log.Debug(string.Join(" | ", regex.Groups.Values.Select(g=> g.Value)));

                    string nonColored = regex.Groups[1].Value;
                    if (!nonColored.IsNullOrEmpty())
                    {
                        if (!first)
                            SameLine();

                        first = false;
                        ImGui.Text(nonColored);
                        nonColoredSet = true;
                        //Svc.Log.Debug("non colored: " + nonColored);
                    }

                    string colorText   = regex.Groups[2].Value;
                    string coloredText = regex.Groups[3].Value;
                    if (!colorText.IsNullOrEmpty() && !coloredText.IsNullOrEmpty())
                    {
                        string[] split = colorText.Split(',');
                        if (split.Length >= 3)
                        {
                            if (float.TryParse(split[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float r))
                                if (float.TryParse(split[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float g))
                                    if (float.TryParse(split[2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float b))
                                    {
                                        float a = 1;
                                        if (split.Length == 4 && float.TryParse(split[3], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out a))
                                        {
                                        }

                                        if (nonColoredSet)
                                            SameLine();
                                        else if (!first)
                                            SameLine();

                                        first = false;

                                        Vector4 color = new(r, g, b, a);
                                        ImGui.TextColored(color, coloredText);

                                        //Svc.Log.Debug("colored: " + coloredText + " in: " + color);
                                    }
                        }
                    }

                    regex = regex.NextMatch();
                } while (regex.Success);
            }
            else
            {
                ImGui.Text(backupText);
            }
        }

        internal static void CenterNextElement(float percentWidth, float xIndent = 0)
        {
            var buttonWidth = ImGui.GetContentRegionAvail().X * percentWidth;
            ImGui.SetCursorPosX(xIndent + (ImGui.GetContentRegionAvail().X - buttonWidth) / 2f);
        }

        internal static void CenterNextElement(float width)
        {
            ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - width) / 2f);
        }

        internal static bool CenteredButton(string label, float percentWidth, float xIndent = 0)
        {
            var buttonWidth = ImGui.GetContentRegionAvail().X * percentWidth;
            ImGui.SetCursorPosX(xIndent + (ImGui.GetContentRegionAvail().X - buttonWidth) / 2f);
            return ImGui.Button(label, new(buttonWidth, 35f));
        }

        internal static void DrawIcon(FontAwesomeIcon icon)
        {
            using ImRaii.Font font = ImRaii.PushFont(UiBuilder.IconFont);
            ImGui.SetItemAllowOverlap();
            ImGui.Text(icon.ToIconString());
            ImGui.SameLine();
        }

        internal static ImRaii.IEndObject RequiresPlugin(ExternalPlugin plugin, string id, string? message = null, bool inline = false, bool write = true)
        {
            if (IPCSubscriber_Common.IsReady(plugin.GetExternalPluginData().name))
            {
                return new EndUnconditionally(() =>
                                              {
                                                  if (!write)
                                                      return;

                                                  if(inline)
                                                      ImGui.SameLine();
                                                  ImGui.Text($"{(inline ? "| " : "\t")}powered by ");
                                                  ImGui.SameLine(0, 1);
                                                  ImGui.TextColored(LinkColor, plugin.GetExternalPluginName());
                                              }, true);
            }
            else
            {
                ImRaii.IEndObject disabled = ImRaii.Disabled();

                ImGui.AlignTextToFramePadding();
                return new EndUnconditionally(() =>
                                              {
                                                  disabled.Dispose();

                                                  if (!write)
                                                      return;

                                                  ImGui.AlignTextToFramePadding();
                                                  if (inline)
                                                      ImGui.SameLine();
                                                  ImGui.Text(message ?? $"{(inline ? "| " : "\t")} requires ");
                                                  ImGui.SameLine(0,1);
                                                  ImGui.TextColored(LinkColor, plugin.GetExternalPluginName());

                                                  ImGui.SameLine(0, 5);
                                                  if (ImGui.Button($"Install##InstallExternalPlugin_{plugin}_{id}"))
                                                      PluginInstaller.InstallPlugin(plugin);
                                              }, true);
            }
        }


        //Straight from Dalamud
        private struct EndUnconditionally(Action endAction, bool success) : ImRaii.IEndObject
        {
            private Action EndAction { get; } = endAction;

            public bool Success { get; } = success;

            private bool Disposed { get; set; } = false;

            public void Dispose()
            {
                if (this.Disposed)
                    return;

                this.EndAction();
                this.Disposed = true;
            }
        }
    }
}

using System.Numerics;
using AutoDuty.Helpers;
using AutoDuty.IPC;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.EzSharedDataManager;
using ECommons.Funding;
using ECommons.ImGuiMethods;
using ECommons.Schedulers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Bindings.ImGui;

namespace AutoDuty.Windows;

using System;
using ECommons.Reflection;

public sealed class MainWindow : Window, IDisposable
{
    internal static string CurrentTabName = "";

    private static bool _showPopup = false;
    private static bool _nestedPopup = false;
    private static string _popupText = "";
    private static string _popupTitle = "";
    private static string openTabName = "";

    public MainWindow() : base(
        $"AutoDuty v0.0.0.{Plugin.Version}###Autoduty")
    {
        this.SizeConstraints = new WindowSizeConstraints
                               {
                                   MinimumSize = new Vector2(10, 10),
                                   MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
                               };

        this.TitleBarButtons.Add(new TitleBarButton { Icon        = FontAwesomeIcon.Cog, IconOffset                         = new Vector2(1, 1), Click          = _ => OpenTab("Config") });
        this.TitleBarButtons.Add(new TitleBarButton { ShowTooltip = () => ImGui.SetTooltip("Support erdelf on Ko-fi"), Icon = FontAwesomeIcon.Heart, IconOffset = new Vector2(1, 1), Click = _ => GenericHelpers.ShellStart("https://ko-fi.com/erdelf") });
    }

    internal static void SetCurrentTabName(string tabName)
    {
        if (CurrentTabName != tabName)
            CurrentTabName = tabName;
    }

    internal static void OpenTab(string tabName)
    {
        openTabName = tabName;
        _ = new TickScheduler(delegate
        {
            openTabName = "";
        }, 25);
    }

    public void Dispose()
    {
    }

    internal static void Start() => 
        ImGui.SameLine(0, 5);

    internal static void LoopsConfig()
    {
        using ImRaii.IEndObject _ = ImRaii.Disabled(ConfigurationMain.Instance.MultiBox && !ConfigurationMain.Instance.host);

        if ((AutoDuty.Configuration.UseSliderInputs  && ImGui.SliderInt("Times", ref AutoDuty.Configuration.LoopTimes, 0, 100)) || 
            (!AutoDuty.Configuration.UseSliderInputs && ImGui.InputInt("Times", ref AutoDuty.Configuration.LoopTimes, 1)))
        {
            if (AutoDuty.Configuration.AutoDutyModeEnum == AutoDutyMode.Playlist)
                Plugin.PlaylistCurrentEntry?.count = AutoDuty.Configuration.LoopTimes;

            Configuration.Save();
        }
    }

    internal static void StopResumePause()
    {
        using (ImRaii.Disabled(!Plugin.states.HasFlag(PluginState.Looping) && !Plugin.states.HasFlag(PluginState.Navigating) && RepairHelper.State != ActionState.Running && GotoHelper.State != ActionState.Running && GotoInnHelper.State != ActionState.Running && GotoBarracksHelper.State != ActionState.Running && GCTurninHelper.State != ActionState.Running && ExtractHelper.State != ActionState.Running && DesynthHelper.State != ActionState.Running))
        {
            if (ImGui.Button($"Stop###Stop2"))
            {
                StopAndReset();
                return;
            }
            ImGui.SameLine(0, 5);
        }

        using (ImRaii.Disabled((!Plugin.states.HasFlag(PluginState.Looping) && !Plugin.states.HasFlag(PluginState.Navigating) && RepairHelper.State != ActionState.Running && GotoHelper.State != ActionState.Running && GotoInnHelper.State != ActionState.Running && GotoBarracksHelper.State != ActionState.Running && GCTurninHelper.State != ActionState.Running && ExtractHelper.State != ActionState.Running && DesynthHelper.State != ActionState.Running) || Plugin.CurrentTerritoryContent == null))
        {
            if (Plugin.Stage == Stage.Paused)
            {
                if (ImGui.Button("Resume"))
                {
                    Plugin.taskManager.StepMode = false;
                    Plugin.Stage = Plugin.previousStage;
                    Plugin.states &= ~PluginState.Paused;
                }
            }
            else
            {
                if (ImGui.Button("Pause")) Plugin.Stage = Stage.Paused;
            }
        }
    }

    private static void StopAndReset()
    {
        Plugin.playlistIndex = 0;
        Plugin.Stage = Stage.Stopped;
    }

    internal static void GotoAndActions()
    {
        if(Plugin.states.HasFlag(PluginState.Other))
        {
            if(ImGui.Button("Stop###Stop1"))
                StopAndReset();
            ImGui.SameLine(0,5);
        }

        using (ImRaii.Disabled(Plugin.states.HasFlag(PluginState.Looping) || Plugin.states.HasFlag(PluginState.Navigating)))
        {
            using (ImRaii.Disabled(AutoDuty.Configuration is { OverrideOverlayButtons: true, GotoButton: false }))
            {
                using (ImRaii.Disabled(Plugin.states.HasFlag(PluginState.Other)))
                {
                    if (ImGui.Button("Goto"))
                    {
                        ImGui.OpenPopup("GotoPopup");
                    }   
                }
            }

            if (ImGui.BeginPopup("GotoPopup"))
            {
                if (ImGui.Selectable("Barracks")) GotoBarracksHelper.Invoke();
                if (ImGui.Selectable("Inn")) GotoInnHelper.Invoke();
                if (ImGui.Selectable("GCSupply")) GotoHelper.Invoke(PlayerHelper.GetGrandCompanyTerritoryType(PlayerHelper.GetGrandCompany()), [GCTurninHelper.GCSupplyLocation], 0.25f, 3f);
                if (ImGui.Selectable("Flag Marker")) MapHelper.MoveToMapMarker();
                if (ImGui.Selectable("Summoning Bell")) SummoningBellHelper.Invoke(AutoDuty.Configuration.PreferredSummoningBellEnum);
                if (ImGui.Selectable("Apartment")) GotoHousingHelper.Invoke(Housing.Apartment);
                if (ImGui.Selectable("Personal Home")) GotoHousingHelper.Invoke(Housing.Personal_Home);
                if (ImGui.Selectable("FC Estate")) GotoHousingHelper.Invoke(Housing.FC_Estate);

                if (ImGui.Selectable("Triple Triad Trader")) GotoHelper.Invoke(TripleTriadCardSellHelper.GoldSaucerTerritoryType, TripleTriadCardSellHelper.TripleTriadCardVendorLocation);
                ImGui.EndPopup();
            }



            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(AutoDuty.Configuration is { AutoGCTurnin: false, OverrideOverlayButtons: false } || !AutoDuty.Configuration.TurninButton))
            {
                using (ImRaii.Disabled(Plugin.states.HasFlag(PluginState.Other)))
                {
                    if (ImGui.Button("TurnIn"))
                    {
                        if (AutoRetainer_IPCSubscriber.IsEnabled)
                            GCTurninHelper.Invoke();
                        else
                            ShowPopup("Missing Plugin", "GC Turnin Requires AutoRetainer plugin. Get @ https://love.puni.sh/ment.json");
                    }
                    if (AutoRetainer_IPCSubscriber.IsEnabled)
                        ToolTip("Click to Goto GC Turnin and Invoke AutoRetainer's GC Turnin");
                    else
                        ToolTip("GC Turnin Requires AutoRetainer plugin. Get @ https://love.puni.sh/ment.json");
                }
            }
            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(AutoDuty.Configuration is { AutoDesynth: false, OverrideOverlayButtons: false } || !AutoDuty.Configuration.DesynthButton))
            {
                using (ImRaii.Disabled(Plugin.states.HasFlag(PluginState.Other)))
                {
                    if (ImGui.Button("Desynth"))
                        DesynthHelper.Invoke();
                    ToolTip("Click to Desynth all Items in Inventory");
                    
                }
            }
            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(AutoDuty.Configuration is { AutoExtract: false, OverrideOverlayButtons: false } || !AutoDuty.Configuration.ExtractButton))
            {
                using (ImRaii.Disabled(Plugin.states.HasFlag(PluginState.Other)))
                {
                    if (ImGui.Button("Extract"))
                    {
                        if (QuestManager.IsQuestComplete(66174))
                            ExtractHelper.Invoke();
                        else
                            ShowPopup("Missing Quest Completion", "Materia Extraction requires having completed quest: Forging the Spirit");
                    }
                    if (QuestManager.IsQuestComplete(66174))
                        ToolTip("Click to Extract Materia");
                    else
                        ToolTip("Materia Extraction requires having completed quest: Forging the Spirit");
                }
            }
            
            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(AutoDuty.Configuration is { AutoRepair: false, OverrideOverlayButtons: false } || !AutoDuty.Configuration.RepairButton))
            {
                using (ImRaii.Disabled(Plugin.states.HasFlag(PluginState.Other)))
                {
                    if (ImGui.Button("Repair"))
                    {
                        if (InventoryHelper.CanRepair(100))
                            RepairHelper.Invoke();
                        //else
                            //ShowPopup("", "");
                    }
                    //if ()
                        ToolTip("Click to Repair");
                    //else
                        //ToolTip("");
                    
                }
            }
            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(AutoDuty.Configuration is { AutoEquipRecommendedGear: false, OverrideOverlayButtons: false } || !AutoDuty.Configuration.EquipButton))
            {
                using (ImRaii.Disabled(Plugin.states.HasFlag(PluginState.Other)))
                {
                    if (ImGui.Button("Equip"))
                    {
                        AutoEquipHelper.Invoke();
                        //else
                        //ShowPopup("", "");
                    }

                    //if ()
                    ToolTip("Click to Equip Gear");
                    //else
                    //ToolTip("");
                }
            }

            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(AutoDuty.Configuration is { AutoOpenCoffers: false, OverrideOverlayButtons: false } || !AutoDuty.Configuration.CofferButton))
            {
                using (ImRaii.Disabled(Plugin.states.HasFlag(PluginState.Other)))
                {
                    if (ImGui.Button("Coffers")) 
                        CofferHelper.Invoke();
                    ToolTip("Click to open coffers");
                }
            }
            ImGui.SameLine(0, 5);

            using (ImRaii.Disabled(!(AutoDuty.Configuration.TripleTriadRegister || AutoDuty.Configuration.TripleTriadSell) && (!AutoDuty.Configuration.OverrideOverlayButtons || !AutoDuty.Configuration.TTButton)))
            {
                using (ImRaii.Disabled(Plugin.states.HasFlag(PluginState.Other)))
                {
                    if (ImGui.Button("Triple Triad"))
                        ImGui.OpenPopup("TTPopup");
                    
                }
            }

            if (ImGui.BeginPopup("TTPopup"))
            {
                if (ImGui.Selectable("Register TT Cards"))
                    TripleTriadCardUseHelper.Invoke();
                if (ImGui.Selectable("Sell TT Cards")) 
                    TripleTriadCardSellHelper.Invoke();
                ImGui.EndPopup();
            }
        }
    }

    internal static void ToolTip(string text)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
            ImGuiEx.Text(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
    }

    internal static void ShowPopup(string popupTitle, string popupText, bool nested = false)
    {
        _popupTitle = popupTitle;
        _popupText = popupText;
        _showPopup = true;
        _nestedPopup = nested;
    }

    internal static void DrawPopup(bool nested = false)
    {
        if (!_showPopup || (_nestedPopup && !nested) || (!_nestedPopup && nested)) return;

        if (!ImGui.IsPopupOpen($"{_popupTitle}###Popup"))
            ImGui.OpenPopup($"{_popupTitle}###Popup");

        Vector2 textSize = ImGui.CalcTextSize(_popupText);
        ImGui.SetNextWindowSize(new Vector2(textSize.X + 25, textSize.Y + 100));
        if (ImGui.BeginPopupModal($"{_popupTitle}###Popup", ref _showPopup, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove))
        {
            ImGuiEx.TextCentered(_popupText);
            ImGui.Spacing();
            if (ImGuiHelper.CenteredButton("OK", .5f, 15))
            {
                _showPopup = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private static void KofiLink()
    {
        OpenTab(CurrentTabName);
        if (EzThrottler.Throttle("KofiLink", 15000))
            _ = new TickScheduler(delegate
                                  {
                                      GenericHelpers.ShellStart("https://ko-fi.com/erdelf");
                                  }, 500);
    }

    //ECommons
    static uint ColorNormal
    {
        get
        {
            Vector4 vector1 = ImGuiEx.Vector4FromRGB(0x022594);
            Vector4 vector2 = ImGuiEx.Vector4FromRGB(0x940238);

            uint    gen  = GradientColor.Get(vector1, vector2).ToUint();
            uint[]? data = EzSharedData.GetOrCreate<uint[]>("ECommonsPatreonBannerRandomColor", [gen]);
            if (!GradientColor.IsColorInRange(data[0].ToVector4(), vector1, vector2)) 
                data[0] = gen;
            return data[0];
        }
    }

    public static void EzTabBar(string id, string? KoFiTransparent, string openTabName, ImGuiTabBarFlags flags, params (string name, Action function, Vector4? color, bool child)[] tabs)
    {
        ImGui.BeginTabBar(id, flags);


        bool valid = (BossMod_IPCSubscriber.IsEnabled  || AutoDuty.Configuration.UsingAlternativeBossPlugin)     &&
                     (VNavmesh_IPCSubscriber.IsEnabled || AutoDuty.Configuration.UsingAlternativeMovementPlugin) &&
                     (BossMod_IPCSubscriber.IsEnabled  || AutoDuty.Configuration.UsingAlternativeRotationPlugin);

        if (!valid)
            openTabName = "Info";

        foreach ((string name, Action function, Vector4? color, bool child) in tabs)
        {
            if (name.IsNullOrEmpty()) 
                continue;
            if (color != null) 
                ImGui.PushStyleColor(ImGuiCol.Tab, color.Value);
            
            if ((valid || name == "Info") && ImGui.BeginTabItem(name, openTabName == name ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
            {
                if (color != null) 
                    ImGui.PopStyleColor();
                if (child) 
                    ImGui.BeginChild(name + "child");

                if(!valid)
                {
                    ImGui.NewLine();
                    ImGui.TextColored(EzColor.Red, "You need to do the basic setup below. Enjoy");
                }

                function();

                if (child) 
                    ImGui.EndChild();
                ImGui.EndTabItem();
            }
            else
            {
                if (color != null) 
                    ImGui.PopStyleColor();
            }
        }
        if (KoFiTransparent != null) 
            PatreonBanner.RightTransparentTab();
        
        ImGui.EndTabBar();
    }

    private static readonly (string, Action, Vector4?, bool)[] tabList =
    [
        ("Main", MainTab.Draw, null, false), 
        ("Build", BuildTab.Draw, null, false), 
        ("Paths", PathsTab.Draw, null, false), 
        ("Config", ConfigTab.Draw, null, false), 
        ("Info", InfoTab.Draw, null, false), 
        ("Logs", LogTab.Draw, null, false),
        ("Stats", StatsTab.Draw, null, false),
        ("Support AutoDuty", KofiLink, ImGui.ColorConvertU32ToFloat4(ColorNormal), false)
    ];

    public override void Draw()
    {
        DrawPopup();

        if(DalamudHelper.IsOnStaging())
        {
            ImGui.TextColored(GradientColor.Get(ImGuiHelper.ExperimentalColor, ImGuiHelper.ExperimentalColor2, 500), "NOT SUPPORTED ON STAGING.");
            ImGui.Text("Please type in \"/xlbranch\" and pick Release, then restart the game.");

            if (!ImGui.CollapsingHeader("Use despite staging. Support will not be given##stagingHeader"))
                return;
        }

        EzTabBar("MainTab", null, openTabName, ImGuiTabBarFlags.None, tabList);
    }
}

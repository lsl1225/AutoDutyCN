namespace AutoDuty.Configurations;

using Dalamud.Bindings.ImGui;
using Data;
using ECommons;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.IPC.Subscribers.AutoRetainer;
using ECommons.IPC.Subscribers.RotationSolverReborn;
using Helpers;
using Multibox;
using Newtonsoft.Json;
using Serilog.Events;
using System.Collections.Generic;
using System.Numerics;
using Windows;

[JsonObject(MemberSerialization.OptOut)]
public class ConfigurationProfileV2
{
    public ConfigurationProfileV2()
    {
        this.Log        = new LogConfig(this);
        this.Overlay    = new OverlayConfig(this);
        this.DutyConfig = new DutyConfigConfig(this);
        this.Loop       = new LoopConfig(this);
        this.Meta       = new MetaConfig(this);
    }

    public MetaConfig Meta { get; set; }

    [JsonObject(MemberSerialization.OptOut)]
    public class MetaConfig(ConfigurationProfileV2 config)
    {
        [JsonIgnore]
        private readonly ConfigurationProfileV2 config = config;

        public Dictionary<uint, Dictionary<string, JobWithRole>?> PathSelectionsByPath { get; set; } = [];

        public AutoDutyMode AutoDutyModeEnum
        {
            get;
            set
            {
                field                          = value;
                Plugin.CurrentTerritoryContent = null;
                MainTab.DutySelected           = null;
                Plugin.LevelingModeEnum        = LevelingMode.None;
            }
        } = AutoDutyMode.Looping;

        public int LoopTimes { get; set; } = 1;

        public DutyMode dutyModeEnum = DutyMode.Support;

        [JsonIgnore]
        public DutyMode DutyModeEnum
        {
            get => this.AutoDutyModeEnum switch
            {
                AutoDutyMode.Playlist => Plugin.PlaylistCurrentEntry?.DutyMode ?? this.dutyModeEnum,
                AutoDutyMode.Looping or _ => field
            };
            set
            {
                field                          = value;
                Plugin.CurrentTerritoryContent = null;
                MainTab.DutySelected           = null;
                Plugin.LevelingModeEnum        = LevelingMode.None;
            }
        }

        public bool Unsynced                       { get; set; } = false;
        public bool HideUnavailableDuties          { get; set; } = false;
        public bool PreferTrustOverSupportLeveling { get; set; } = false;
        public bool SquadronAssignLowestMembers    { get; set; } = true;

        public bool ShowMainWindowOnStartup { get; set; } = false;
    }

    public LogConfig Log { get; set; }

    [JsonObject(MemberSerialization.OptOut)]
    public class LogConfig(ConfigurationProfileV2 config)
    {
        [JsonIgnore]
        private readonly ConfigurationProfileV2 config = config;

        public bool          AutoScroll    { get; set; } = true;
        public LogEventLevel LogEventLevel { get; set; } = LogEventLevel.Debug;
    }

    public OverlayConfig Overlay { get; set; }

    [JsonObject(MemberSerialization.OptOut)]
    public class OverlayConfig(ConfigurationProfileV2 config)
    {
        [JsonIgnore]
        private readonly ConfigurationProfileV2 config = config;

        public bool ShowOverlay
        {
            get;
            set
            {
                field                  = value;
                Plugin.Overlay?.IsOpen = value;
            }
        } = true;

        public bool HideOverlayWhenStopped
        {
            get;
            set
            {
                field = value;
                if (Plugin.Overlay != null)
                    SchedulerHelper.ScheduleAction("LockOverlaySetter", () => Plugin.Overlay.IsOpen = !value || Plugin.States.HasAnyFlag(PluginState.Looping, PluginState.Navigating), () => Plugin.Overlay != null);
            }
        } = false;

        public bool LockOverlay
        {
            get;
            set
            {
                field = value;
                if (value)
                    SchedulerHelper.ScheduleAction("LockOverlaySetter", () =>
                                                                        {
                                                                            if (!Plugin.Overlay.Flags.HasFlag(ImGuiWindowFlags.NoMove))
                                                                                Plugin.Overlay.Flags |= ImGuiWindowFlags.NoMove;
                                                                        }, () => Plugin.Overlay != null);
                else
                    SchedulerHelper.ScheduleAction("LockOverlaySetter", () =>
                                                                        {
                                                                            if (Plugin.Overlay.Flags.HasFlag(ImGuiWindowFlags.NoMove))
                                                                                Plugin.Overlay.Flags -= ImGuiWindowFlags.NoMove;
                                                                        }, () => Plugin.Overlay != null);
            }
        } = false;

        public bool OverlayNoBG
        {
            get;
            set
            {
                field = value;
                if (value)
                    SchedulerHelper.ScheduleAction("OverlayNoBGSetter", () =>
                                                                        {
                                                                            if (!Plugin.Overlay.Flags.HasFlag(ImGuiWindowFlags.NoBackground))
                                                                                Plugin.Overlay.Flags |= ImGuiWindowFlags.NoBackground;
                                                                        }, () => Plugin.Overlay != null);
                else
                    SchedulerHelper.ScheduleAction("OverlayNoBGSetter", () =>
                                                                        {
                                                                            if (Plugin.Overlay.Flags.HasFlag(ImGuiWindowFlags.NoBackground))
                                                                                Plugin.Overlay.Flags -= ImGuiWindowFlags.NoBackground;
                                                                        }, () => Plugin.Overlay != null);
            }
        } = false;

        public bool OverlayAnchorBottom    { get; set; } = false;
        public bool ShowDutyLoopText       { get; set; } = true;
        public bool ShowActionText         { get; set; } = true;
        public bool UseSliderInputs        { get; set; } = false;
    }

    public DutyConfigConfig DutyConfig { get; set; }

    [JsonObject(MemberSerialization.OptOut)]
    public class DutyConfigConfig
    {
        [JsonIgnore]
        private readonly ConfigurationProfileV2 config;

        public DutyConfigConfig(ConfigurationProfileV2 config)
        {
            this.config = config;
            this.Stuck  = new StuckConfig(this);
        }

        public bool           AutoExitDuty                  { get; set; } = true;
        public bool           OnlyExitWhenDutyDone          { get; set; } = false;
        public bool           AutoManageRotationPluginState { get; set; } = true;
        public RotationPlugin RotationPlugin                { get; set; } = RotationPlugin.All;

        public WrathConfig Wrath { get; set; } = new();

        [JsonObject(MemberSerialization.OptOut)]
        public class WrathConfig
        {
            public bool                                AutoSetupJobs    { get; set; } = true;
            public WrathCombo.API.Enum.DPSRotationMode TargetingTank    { get; set; } = WrathCombo.API.Enum.DPSRotationMode.Highest_Max;
            public WrathCombo.API.Enum.DPSRotationMode TargetingNonTank { get; set; } = WrathCombo.API.Enum.DPSRotationMode.Lowest_Current;
        }

        public RSRConfig RSR { get; set; } = new();

        [JsonObject(MemberSerialization.OptOut)]
        public class RSRConfig
        {
            public RotationSolverRebornIPC.TargetHostileType TargetHostileType    { get; set; } = RotationSolverRebornIPC.TargetHostileType.AllTargetsCanAttack;
            public RotationSolverRebornIPC.TargetingType     TargetingTypeTank    { get; set; } = RotationSolverRebornIPC.TargetingType.HighHP;
            public RotationSolverRebornIPC.TargetingType     TargetingTypeNonTank { get; set; } = RotationSolverRebornIPC.TargetingType.LowHP;
        }

        public bool AutoManageBossModAISettings { get; set; } = true;

        public BossModConfig BossMod { get; set; } = new();

        [JsonObject(MemberSerialization.OptOut)]
        public class BossModConfig
        {
            public bool UpdatePresetsAutomatically { get; set; } = true;

            public bool MaxDistanceToTargetRoleBased
            {
                get;
                set
                {
                    field = value;
                    if (value)
                        SchedulerHelper.ScheduleAction("MaxDistanceToTargetRoleBasedBMRoleChecks", BMRoleChecks, () => PlayerHelper.IsReady);
                }
            } = true;

            public float MaxDistanceToTargetFloat    { get; set; } = 2.6f;
            public float MaxDistanceToTargetAoEFloat { get; set; } = 12;

            public bool PositionalRoleBased
            {
                get;
                set
                {
                    field = value;
                    if (value)
                        SchedulerHelper.ScheduleAction("PositionalRoleBasedBMRoleChecks", BMRoleChecks, () => PlayerHelper.IsReady);
                }
            } = true;

            public float MaxDistanceToTargetRoleMelee  { get; set; } = 2.6f;
            public float MaxDistanceToTargetRoleRanged { get; set; } = 10f;
        }

        internal bool       PositionalAvarice { get; set; } = true;
        public   Positional PositionalEnum    { get; set; } = Positional.Any;

        public  bool       AutoManageVnavAlignCamera      { get; set; } = true;
        public  bool       LootTreasure                   { get; set; } = true;
        public  LootMethod LootMethodEnum                 { get; set; } = LootMethod.AutoDuty;
        public  bool       LootBossTreasureOnly           { get; set; } = false;
        public  int        TreasureCofferScanDistance     { get; set; } = 25;

        public StuckConfig Stuck { get; set; }

        [JsonObject(MemberSerialization.OptOut)]
        public class StuckConfig(DutyConfigConfig config)
        {
            [JsonIgnore]
            private readonly DutyConfigConfig config = config;

            public bool RebuildNavmeshOnStuck          { get; set; } = true;
            public byte RebuildNavmeshAfterStuckXTimes { get; set; } = 5;
            public int  MinStuckTime                   { get; set; } = 500;
            public bool StuckOnStep                    { get; set; } = true;
            public int  StuckReturnX                   { get; set; } = 10;

            public bool StuckReturn
            {
                get => field && !MultiboxUtility.Config.MultiBox;
                set;
            } = true;
        }

        public bool PathDrawEnabled   { get; set; } = false;
        public int  PathDrawStepCount { get; set; } = 5;

        public bool DisableRenderWhileActive
        {
            get;
            set
            {
                field = value;
                if (!value)
                    RenderDisableManager.RemoveRequest();
                else if (Plugin.States != PluginState.None)
                    RenderDisableManager.PlaceRequest();
            }
        } = false;

        public bool OverridePartyValidation { get; set; } = false;
        public bool UsingAlternativeRotationPlugin { get; set; } = false;
        public bool UsingAlternativeMovementPlugin { get; set; } = false;
        public bool UsingAlternativeBossPlugin { get; set; } = false;

        public bool TreatUnsyncAsW2W { get; set; } = true;
        public JobWithRole W2WJobs { get; set; } = JobWithRole.Tanks;

        public bool IsW2W(Job? job = null, bool? unsync = null)
        {
            job ??= PlayerHelper.GetJob();

            if (this.W2WJobs.HasJob(job.Value))
                return true;
            
            unsync ??= this.config.Meta.Unsynced && this.config.Meta.DutyModeEnum.EqualsAny(DutyMode.Raid, DutyMode.Regular, DutyMode.Trial);

            return MultiboxUtility.Config.MultiBox || unsync.Value && this.TreatUnsyncAsW2W;
        }

        public bool LevelingListExperimentalEntries
        {
            get;
            set
            {
                if (field != value)
                    LevelingHelper.ResetLevelingDuties();
                field = value;
            }
        } = false;
    }

    public LoopConfig Loop { get; set; }

    [JsonObject(MemberSerialization.OptOut)]
    public class LoopConfig
    {
        [JsonIgnore]
        private readonly ConfigurationProfileV2 config;

        public LoopConfig(ConfigurationProfileV2 config)
        {
            this.config      = config;
            this.Pre     = new PreLoopConfig(this);
            this.Between = new BetweenLoopConfig(this);
            this.Termination = new TerminationConfig(this);
        }

        public PreLoopConfig Pre { get; set; }

        [JsonObject(MemberSerialization.OptOut)]
        public class PreLoopConfig(LoopConfig config)
        {
            [JsonIgnore]
            private readonly LoopConfig config = config;

            public bool                                                 Enabled                             { get; set; } = true;
            public bool                                                 ExecuteCommands                           { get; set; } = false;
            public List<string>                                         CustomCommands                            { get; set; } = [];
            public bool                                                 RetireMode                                       { get; set; } = false;
            public RetireLocation                                       RetireLocationEnum                               { get; set; } = RetireLocation.Inn;
            public List<Vector3>                                        PersonalHomeEntrancePath                         { get; set; } = [];
            public List<Vector3>                                        FCEstateEntrancePath                             { get; set; } = [];
            public bool                                                 AutoEquipRecommendedGear                         { get; set; }
            public GearsetUpdateSource                                  AutoEquipRecommendedGearSource                   { get; set; } = GearsetUpdateSource.Vanilla;
            public bool                                                 AutoEquipRecommendedGearGearsetterOldToInventory { get; set; }
            public bool                                                 AutoRepair                                       { get; set; } = false;
            public uint                                                 AutoRepairPct                                    { get; set; } = 50;
            public bool                                                 AutoRepairSelf                                   { get; set; } = false;
            public RepairNPCHelper.RepairNpcData?                       PreferredRepairNPC                               { get; set; } = null;
            public bool                                                 AutoConsume                                      { get; set; } = false;
            public bool                                                 AutoConsumeIgnoreStatus                          { get; set; } = false;
            public int                                                  AutoConsumeTime                                  { get; set; } = 29;
            public List<KeyValuePair<ushort, ConfigTab.ConsumableItem>> AutoConsumeItemsList                             { get; set; } = [];
        }

        
        public BetweenLoopConfig Between { get; set; }

        [JsonObject(MemberSerialization.OptOut)]
        public class BetweenLoopConfig(LoopConfig config)
        {
            [JsonIgnore]
            private readonly LoopConfig config = config;

            public bool         Enabled         { get; set; } = true;
            public bool         ExecuteLastLoop { get; set; } = false;
            public int          WaitTimeBeforeAfterLoopActions   { get; set; } = 0;
            public bool         ExecuteCommands       { get; set; } = false;
            public List<string> CustomCommands        { get; set; } = [];
            public bool         AutoExtract                      { get; set; } = false;

            public bool                     AutoOpenCoffers             { get; set; }
            public byte?                    AutoOpenCoffersGearset      { get; set; }
            public bool                     AutoOpenCoffersBlacklistUse { get; set; }
            public Dictionary<uint, string> AutoOpenCoffersBlacklist    { get; set; } = [];

            public bool AutoExtractAll { get; set; }

            public bool AutoDesynth
            {
                get;
                set
                {
                    field = value;
                    if (value && !this.AutoDesynthSkillUp)
                        this.AutoGCTurnin = false;
                }
            }

            public bool AutoDesynthSkillUp
            {
                get;
                set
                {
                    field = value;
                    if (!value && this.AutoGCTurnin)
                        this.AutoDesynth = false;
                }
            }

            public int   AutoDesynthSkillUpLimit { get; set; } = 50;
            public bool  AutoDesynthNQOnly       { get; set; } = false;
            public bool  AutoDesynthNoGearset    { get; set; } = true;
            public ulong AutoDesynthCategories   { get; set; } = 0x1;

            public bool AutoGCTurnin
            {
                get;
                set
                {
                    field = value;
                    if (value && !this.AutoDesynthSkillUp)
                        this.AutoDesynth = false;
                }
            }

            public int  AutoGCTurninSlotsLeft     { get; set; } = 5;
            public bool AutoGCTurninSlotsLeftBool { get; set; } = false;
            public bool AutoGCTurninUseTicket     { get; set; } = false;

            public bool ArmoireEntrust      { get; set; } = false;
            public bool GlamourChestEntrust { get; set; } = false;

            public bool TripleTriadRegister         { get; set; }
            public bool TripleTriadSell             { get; set; }
            public int  TripleTriadSellMinItemCount { get; set; } = 1;
            public int  TripleTriadSellMinSlotCount { get; set; } = 1;

            public bool DiscardItems { get; set; }

            public bool                   EnableAutoRetainer         { get; set; } = false;
            public SummoningBellLocations PreferredSummoningBellEnum { get; set; } = 0;
            public long                   AutoRetainerRemainingTime  { get; set; } = 0L;

            public bool          EnableAutoRetainerMultiMode { get; set; } = false;
            public MultiModeType AutoRetainerMultiModeType   { get; set; } = MultiModeType.Everything;
        }

        public TerminationConfig Termination { get; set; }

        [JsonObject(MemberSerialization.OptOut)]
        public class TerminationConfig(LoopConfig config)
        {
            private readonly LoopConfig config = config;

            public bool                                        Enabled      { get; set; } = true;
            public bool                                        StopLevel                     { get; set; }
            public int                                         StopLevelInt                  { get; set; } = 1;
            public bool                                        StopNoRestedXP                { get; set; }
            public bool                                        StopItemQty                   { get; set; }
            public bool                                        StopItemAll                   { get; set; }
            public Dictionary<uint, KeyValuePair<string, int>> StopItemQtyItemDictionary     { get; set; } = [];
            public int                                         StopItemQtyInt                { get; set; } = 1;
            public bool                                        StopWhenDutyGathered          { get; set; }
            public bool                                        TerminationBLUSpellsEnabled   { get; set; }
            public List<uint>                                  TerminationBLUSpells          { get; set; } = [];
            public bool                                        TerminationBLUSpellsAll       { get; set; }
            public bool                                        TerminationInventoryFree      { get; set; }
            public int                                         TerminationInventoryFreeSlots { get; set; }
            public bool                                        TerminationiLvl               { get; set; }
            public int                                         TerminationiLvlInt            { get; set; }

            public bool            ExecuteCommands { get; set; }
            public List<string>    CustomCommands  { get; set; } = [];
            public bool            PlayEndSound               { get; set; }
            public bool            CustomSound                { get; set; }
            public float           CustomSoundVolume          { get; set; } = 0.5f;
            public Sounds          SoundEnum                  { get; set; } = Sounds.None;
            public string          SoundPath                  { get; set; } = "";
            public TerminationMode TerminationMethodEnum      { get; set; } = TerminationMode.Do_Nothing;
            public bool            TerminationKeepActive      { get; set; } = true;
        }
    }

    public TrustMemberName?[] SelectedTrustMembers { get; set; } = new TrustMemberName?[3];


    public static void Save()
    {
        ConfigurationMain.Save();
    }
}
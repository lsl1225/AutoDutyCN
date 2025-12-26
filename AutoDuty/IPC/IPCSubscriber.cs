using ECommons.DalamudServices;
using ECommons.EzIpcManager;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System.Globalization;
using System.Numerics;
using static ECommons.IPC.ECommonsIPC;

// ReSharper disable InconsistentNaming
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS0169 // Field is never used
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#nullable disable

namespace AutoDuty.IPC
{
    using System;
    using System.Collections.Generic;
    using ECommons.GameFunctions;
    using Helpers;
    using System.ComponentModel;
    using Dalamud.Plugin;
    using ECommons.IPC.Subscribers.RotationSolverReborn;

    internal static class AutoRetainer_IPCSubscriber
    {
        internal static bool IsEnabled => IPCSubscriber_Common.IsReady("AutoRetainer");

        internal static bool IsBusy() => 
            AutoRetainer.IsBusy();
        internal static bool AreAnyRetainersAvailableForCurrentChara() => 
            AutoRetainer.AreAnyRetainersAvailableForCurrentChara();

        internal static void AbortAllTasks() =>
            AutoRetainer.AbortAllTasks();

        internal static void EnableMultiMode() =>
            AutoRetainer.EnableMultiMode();

        internal static void EnqueueGCInitiation() =>
            AutoRetainer.EnqueueInitiation();

        public static bool RetainersAvailable()
        {
            if (Configuration.EnableAutoRetainer && IsEnabled)
            {
                long? remaining = AutoRetainer.GetClosestRetainerVentureSecondsRemaining(Player.CID);
                Svc.Log.Debug($"AutoRetainer IPC - Closest Retainer Venture Remaining Time: {remaining}");
                return remaining.HasValue && remaining < Configuration.AutoRetainer_RemainingTime;
            }

            return false;
        }
    }

    internal static class BossMod_IPCSubscriber
    {
        internal static bool IsEnabled => IPCSubscriber_Common.IsReady("BossMod") || IPCSubscriber_Common.IsReady("BossModReborn");

        public static bool HasModuleByDataId(uint id) => BossMod.HasModuleByDataId(id);

        public static void AddPreset(string name, string preset)
        {
            if (BossMod.Presets_Get(name) == null)
                Svc.Log.Debug($"BossMod Adding Preset: {name} {BossMod.Presets_Create(preset, true)}");
        }

        public static void RefreshPreset(string name, string preset)
        {
            if (BossMod.Presets_Get(name) != null)
                BossMod.Presets_Delete(name);
            AddPreset(name, preset);
        }

        public static void SetPreset(string name, string preset)
        {
            if (Configuration.AutoManageBossModAISettings)
                if (BossMod.Presets_GetActive() != name)
                {
                    Svc.Log.Debug($"BossMod Setting Preset: {name}");
                    AddPreset(name, preset);
                    BossMod.Presets_SetActive(name);
                }
        }

        public static void DisablePresets()
        {
            if (Configuration.AutoManageBossModAISettings)
                if (BossMod.Presets_GetActive() != null)
                {
                    Svc.Log.Debug($"BossMod Disabling Presets");
                    BossMod.Presets_ClearActive();
                }
        }

        public static void SetRange(float range)
        {
            if (Configuration.AutoManageBossModAISettings)
            {
                Svc.Log.Debug($"BossMod Setting Range to: {range}");

                BossMod.Presets_AddTransientStrategy("AutoDuty",         "BossMod.Autorotation.MiscAI.StayCloseToTarget", "range", MathF.Round(range, 1).ToString(CultureInfo.InvariantCulture));
                BossMod.Presets_AddTransientStrategy("AutoDuty Passive", "BossMod.Autorotation.MiscAI.StayCloseToTarget", "range", MathF.Round(range, 1).ToString(CultureInfo.InvariantCulture));
            }
        }

        public enum DestinationStrategy { None, Pathfind, Explicit }

        public static void SetMovement(bool on)
        {
            if (Configuration.AutoManageBossModAISettings)
            {
                Svc.Log.Debug($"BossMod Setting Movement: {on}");

                string destinationStrategy = (on ? DestinationStrategy.Pathfind : DestinationStrategy.None).ToString();

                BossMod.Presets_AddTransientStrategy("AutoDuty",         "BossMod.Autorotation.MiscAI.NormalMovement", "Destination", destinationStrategy);
                BossMod.Presets_AddTransientStrategy("AutoDuty Passive", "BossMod.Autorotation.MiscAI.NormalMovement", "Destination", destinationStrategy);
            }
        }

        public static void SetPositional(Positional positional)
        {
            if (Configuration.AutoManageBossModAISettings)
            {
                Svc.Log.Debug($"BossMod Setting Positional: {positional}");

                BossMod.Presets_AddTransientStrategy("AutoDuty Passive", "BossMod.Autorotation.MiscAI.GoToPositional", "Positional", positional.ToString());
            }
        }

        public static void InBoss(bool boss)
        {
            if (Configuration.AutoManageBossModAISettings)
            {
                string role = boss ? "None" : nameof(Role.Tank);

                BossMod.Presets_AddTransientStrategy("AutoDuty",         "BossMod.Autorotation.MiscAI.StayCloseToPartyRole", "Role", role);
                BossMod.Presets_AddTransientStrategy("AutoDuty Passive", "BossMod.Autorotation.MiscAI.StayCloseToPartyRole", "Role", role);
            }
        }
    }

    
    internal static class YesAlready_IPCSubscriber
    {
        public static bool IsEnabled => YesAlready.IsPluginEnabled();

        public static void SetState(bool on) => 
            YesAlready.SetPluginEnabled(on);
    }

    internal static class Gearsetter_IPCSubscriber
    {
        internal static bool IsEnabled => IPCSubscriber_Common.IsReady("Gearsetter");

        internal static List<(uint ItemId, InventoryType? SourceInventory, byte? SourceInventorySlot, RaptureGearsetModule.GearsetItemIndex TargetSlot)> GetRecommendationsForGearset(byte gearset) =>
            Gearsetter.GetRecommendationsForGearset(gearset);
    }

    internal static class Stylist_IPCSubscriber
    {
        internal static bool IsEnabled => IPCSubscriber_Common.IsReady("Stylist");
        internal static void UpdateCurrentGearsetEx(bool? moveItemsFromInventory, bool? shouldEquip) =>
            Stylist.UpdateCurrentGearsetEx(moveItemsFromInventory, shouldEquip);

        internal static bool IsBusy    => Stylist.IsBusy();
    }


    internal static class VNavmesh_IPCSubscriber
    {
        internal static bool IsEnabled => IPCSubscriber_Common.IsReady("vnavmesh");

        internal static void  Path_Stop()                                 => Vnavmesh.Stop();
        internal static bool  Nav_IsReady                                 => Vnavmesh.IsReady();
        internal static bool  SimpleMove_PathfindInProgress               => Vnavmesh.PathfindInProgress();
        internal static bool  Path_IsRunning                              => Vnavmesh.IsRunning();
        internal static void  Path_MoveTo(List<Vector3> points, bool fly) => Vnavmesh.MoveTo(points, fly);
        internal static bool  GetNav_Rebuild()  => Vnavmesh.Rebuild();
        internal static float Nav_BuildProgress => Vnavmesh.BuildProgress();
        internal static bool SimpleMove_PathfindAndMoveTo(Vector3 position, bool canFly) =>
            Vnavmesh.PathfindAndMoveTo(position, canFly);
        internal static int      Path_NumWaypoints                                   => Vnavmesh.NumWaypoints();
        internal static float    Path_GetTolerance                                   => Vnavmesh.GetTolerance();
        internal static void     Path_SetTolerance(float tolerance)                  => Vnavmesh.SetTolerance(tolerance);
        internal static bool     Path_GetAlignCamera                                 => Vnavmesh.GetAlignCamera();
        internal static void     Path_SetAlignCamera(bool        align)              => Vnavmesh.SetAlignCamera(align);
        internal static Vector3? Query_Mesh_PointOnFloor(Vector3 p, bool a, float b) => Vnavmesh.PointOnFloor(p, a, b);

        internal static void SetMovementAllowed(bool move)
        {
            if (Vnavmesh.GetMovementAllowed() != move)
                Vnavmesh.SetMovementAllowed(move);
        }
    }

    internal static class PandorasBox_IPCSubscriber
    {
        internal static bool IsEnabled => IPCSubscriber_Common.IsReady("PandorasBox");

        internal static void SetFeatureEnabled(string feature, bool enabled) => PandorasBox.SetFeatureEnabled(feature, enabled);
        internal static bool? GetFeatureEnabled(string feature) => PandorasBox.GetFeatureEnabled(feature);
    }

    public static class Wrath_IPCSubscriber
    {
        /// <summary>
        ///     Why a lease was cancelled.
        /// </summary>
        public enum CancellationReason
        {
            [Description("The Wrath user manually elected to revoke your lease.")]
            WrathUserManuallyCancelled,

            [Description("Your plugin was detected as having been disabled, " +
                         "not that you're likely to see this.")]
            LeaseePluginDisabled,

            [Description("The Wrath plugin is being disabled.")]
            WrathPluginDisabled,

            [Description("Your lease was released by IPC call, " +
                         "theoretically this was done by you.")]
            LeaseeReleased,

            [Description("IPC Services have been disabled remotely. "                 +
                         "Please see the commit history for /res/ipc_status.txt. \n " +
                         "https://github.com/PunishXIV/WrathCombo/commits/main/res/ipc_status.txt")]
            AllServicesSuspended,
        }

        /// <summary>
        ///     The subset of <see cref="AutoRotationConfig" /> options that can be set
        ///     via IPC.
        /// </summary>
        public enum AutoRotationConfigOption
        {
            InCombatOnly         = 0, //bool
            DPSRotationMode      = 1,
            HealerRotationMode   = 2,
            FATEPriority         = 3,  //bool
            QuestPriority        = 4,  //bool
            SingleTargetHPP      = 5,  //int
            AoETargetHPP         = 6,  //int
            SingleTargetRegenHPP = 7,  //int
            ManageKardia         = 8,  //bool
            AutoRez              = 9,  //bool
            AutoRezDPSJobs       = 10, //bool
            AutoCleanse          = 11, //bool
            IncludeNPCs          = 12, //bool
            OnlyAttackInCombat   = 13, //bool
        }

        public enum DPSRotationMode
        {
            Manual          = 0,
            Highest_Max     = 1,
            Lowest_Max      = 2,
            Highest_Current = 3,
            Lowest_Current  = 4,
            Tank_Target     = 5,
            Nearest         = 6,
            Furthest        = 7,
        }

        /// <summary>
        ///     The subset of <see cref="HealerRotationMode" /> options
        ///     that can be set via IPC.
        /// </summary>
        public enum HealerRotationMode
        {
            Manual          = 0,
            Highest_Current = 1,
            Lowest_Current  = 2
            //Self_Priority,
            //Tank_Priority,
            //Healer_Priority,
            //DPS_Priority,
        }

        public enum SetResult
        {
            [Description("A default value that shouldn't ever be seen.")]
            IGNORED = -1,

            // Success Statuses

            [Description("The configuration was set successfully.")]
            Okay = 0,

            [Description("The configuration will be set, it is working asynchronously.")]
            OkayWorking = 1,

            // Error Statuses
            [Description("IPC services are currently disabled.")]
            IPCDisabled = 10,

            [Description("Invalid lease.")]
            InvalidLease = 11,

            [Description("Blacklisted lease.")]
            BlacklistedLease = 12,

            [Description("Configuration you are trying to set is already set.")]
            Duplicate = 13,

            [Description("Player object is not available.")]
            PlayerNotAvailable = 14,

            [Description("The configuration you are trying to set is not available.")]
            InvalidConfiguration = 15,

            [Description("The value you are trying to set is invalid.")]
            InvalidValue = 16,
        }

        private static Guid? _curLease;


        internal static bool IsEnabled => IPCSubscriber_Common.IsReady("WrathCombo");

        private static readonly EzIPCDisposalToken[] _disposalTokens = EzIPC.Init(typeof(Wrath_IPCSubscriber), "WrathCombo", SafeWrapper.IPCException);

        /// <summary>
        ///     Register your plugin for control of Wrath Combo.
        /// </summary>
        /// <param name="internalPluginName">
        ///     The internal name of your plugin.<br />
        ///     Needs to be the actual internal name of your plugin, as it will be used
        ///     to check if your plugin is still loaded.
        /// </param>
        /// <param name="pluginName">
        ///     The name you want shown to Wrath users for options your plugin controls.
        /// </param>
        /// <param name="leaseCancelledCallback">
        ///     Your method to be called when your lease is cancelled, usually
        ///     by the user.<br />
        ///     The <see cref="CancellationReason" /> and a string with any additional
        ///     info will be passed to your method.
        /// </param>
        /// <returns>
        ///     Your lease ID to be used in <c>set</c> calls.<br />
        ///     Or <c>null</c> if your lease was not registered, which can happen for
        ///     multiple reasons:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 A lease exists with the <c>pluginName</c>.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 Your lease was revoked by the user recently.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 The IPC service is currently disabled.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </returns>
        /// <remarks>
        ///     Each lease is limited to controlling <c>60</c> configurations.
        /// </remarks>
        [EzIPC] private static readonly Func<string, string, string?, Guid?> RegisterForLeaseWithCallback;

        /// <summary>
        ///     Get the current state of the Auto-Rotation setting in Wrath Combo.
        /// </summary>
        /// <returns>Whether Auto-Rotation is enabled or disabled</returns>
        /// <remarks>
        ///     This is only the state of Auto-Rotation, not whether any combos are
        ///     enabled in Auto-Mode.
        /// </remarks>
        [EzIPC] internal static readonly Func<bool> GetAutoRotationState;

        /// <summary>
        ///     Set the state of Auto-Rotation in Wrath Combo.
        /// </summary>
        /// <param name="lease">Your lease ID from <see cref="RegisterForLease" /></param>
        /// <param name="enabled">
        ///     Optionally whether to enable Auto-Rotation.<br />
        ///     Only used to disable Auto-Rotation, as enabling it is the default.
        /// </param>
        /// <seealso cref="GetAutoRotationState" />
        /// <remarks>
        ///     This is only the state of Auto-Rotation, not whether any combos are
        ///     enabled in Auto-Mode.
        /// </remarks>
        /// <value>+1 <c>set</c></value>
        [EzIPC] private static readonly Func<Guid, bool, SetResult> SetAutoRotationState;

        /// <summary>
        ///     Checks if the current job has a Single and Multi-Target combo configured
        ///     that are enabled in Auto-Mode.
        /// </summary>
        /// <returns>
        ///     If the user's current job is fully ready for Auto-Rotation.
        /// </returns>
        [EzIPC] internal static readonly Func<bool> IsCurrentJobAutoRotationReady;

        /// <summary>
        ///     Sets up the user's current job for Auto-Rotation.<br />
        ///     This will enable the Single and Multi-Target combos, and enable them in
        ///     Auto-Mode.<br />
        ///     This will try to use the user's existing settings, only enabling default
        ///     states for jobs that are not configured.
        /// </summary>
        /// <value>
        ///     +2 <c>set</c><br />
        ///     (can be up to 38 for non-simple jobs, the highest being healers)
        /// </value>
        /// <param name="lease">Your lease ID from <see cref="RegisterForLease" /></param>
        /// <remarks>This can take a little bit to finish.</remarks>
        [EzIPC] private static readonly Func<Guid, SetResult> SetCurrentJobAutoRotationReady;

        /// <summary>
        ///     This cancels your lease, removing your control of Wrath Combo.
        /// </summary>
        /// <param name="lease">Your lease ID from <see cref="RegisterForLease" /></param>
        /// <remarks>
        ///     Will call your <c>leaseCancelledCallback</c> method if you provided one,
        ///     with the reason <see cref="CancellationReason.LeaseeReleased" />.
        /// </remarks>
        [EzIPC] private static readonly Action<Guid> ReleaseControl;

        /// <summary>
        ///     Get the state of Auto-Rotation Configuration in Wrath Combo.
        /// </summary>
        /// <param name="option">The option to check the value of.</param>
        /// <returns>The correctly-typed value of the configuration.</returns>
        [EzIPC] private static readonly Func<AutoRotationConfigOption, object?> GetAutoRotationConfigState;

        /// <summary>
        ///     Set the state of Auto-Rotation Configuration in Wrath Combo.
        /// </summary>
        /// <param name="lease">Your lease ID from <see cref="RegisterForLease" /></param>
        /// <param name="option">
        ///     The Auto-Rotation Configuration option you want to set.<br />
        ///     This is a subset of the Auto-Rotation options, flattened into a single
        ///     enum.
        /// </param>
        /// <param name="value">
        ///     The value you want to set the option to.<br />
        ///     All valid options can be parsed from an int, or the exact expected types.
        /// </param>
        /// <value>+1 <c>set</c></value>
        /// <seealso cref="AutoRotationConfigOption"/>
        /// <seealso cref="DPSRotationMode"/>
        /// <seealso cref="HealerRotationMode"/>
        [EzIPC] private static readonly Func<Guid, AutoRotationConfigOption, object, SetResult> SetAutoRotationConfigState;

        public static bool DoThing(Func<SetResult> action)
        {
            SetResult result = action();
            bool      check  = result.CheckResult();
            if (!check && result == SetResult.InvalidLease)
                check = action().CheckResult();
            return check;
        }

        private static bool CheckResult(this SetResult result)
        {
            switch (result)
            {
                case SetResult.Okay:
                case SetResult.OkayWorking:
                    return true;
                case SetResult.InvalidLease:
                    _curLease = null;
                    Register();
                    return false;
                case SetResult.BlacklistedLease:
                    Configuration.AutoManageRotationPluginState = false;
                    Windows.Configuration.Save();
                    return false;
                case SetResult.IPCDisabled:
                case SetResult.Duplicate:
                case SetResult.PlayerNotAvailable:
                case SetResult.InvalidConfiguration:
                case SetResult.InvalidValue:
                case SetResult.IGNORED:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        }

        internal static bool SetJobAutoReady() => 
            Register() && DoThing(() => SetCurrentJobAutoRotationReady(_curLease!.Value));

        internal static void SetAutoMode(bool on)
        {
            if (Register())
            {
                bool autoRotationState = DoThing(() => SetAutoRotationState(_curLease!.Value, on));
                if (autoRotationState && on)
                {
                    SetAutoRotationConfigState(_curLease.Value, AutoRotationConfigOption.InCombatOnly,       false);
                    SetAutoRotationConfigState(_curLease.Value, AutoRotationConfigOption.AutoRez,            true);
                    SetAutoRotationConfigState(_curLease.Value, AutoRotationConfigOption.AutoRezDPSJobs,     true);
                    SetAutoRotationConfigState(_curLease.Value, AutoRotationConfigOption.IncludeNPCs,        true);
                    SetAutoRotationConfigState(_curLease.Value, AutoRotationConfigOption.OnlyAttackInCombat, false);

                    DPSRotationMode dpsConfig = Plugin.currentPlayerItemLevelAndClassJob.Value.GetCombatRole() == CombatRole.Tank ?
                                                    Configuration.Wrath_TargetingTank :
                                                    Configuration.Wrath_TargetingNonTank;
                    SetAutoRotationConfigState(_curLease.Value, AutoRotationConfigOption.DPSRotationMode, dpsConfig);

                    SetAutoRotationConfigState(_curLease.Value, AutoRotationConfigOption.HealerRotationMode, HealerRotationMode.Lowest_Current);

                }
            }
        }

        internal static bool Register()
        {
            if (_curLease == null)
            {
                _curLease = RegisterForLeaseWithCallback("AutoDuty", "AutoDuty", null);

                if (_curLease == null && IsEnabled)
                {
                    Configuration.AutoManageRotationPluginState = false;
                    Windows.Configuration.Save();
                }
            }
            return _curLease != null;
        }

        internal static void CancelActions(int reason, string s)
        {
            switch ((CancellationReason) reason)
            {
                case CancellationReason.WrathUserManuallyCancelled:
                    Configuration.AutoManageRotationPluginState = false;
                    Windows.Configuration.Save();
                    break;
                case CancellationReason.LeaseePluginDisabled:
                case CancellationReason.WrathPluginDisabled:
                case CancellationReason.LeaseeReleased:
                case CancellationReason.AllServicesSuspended:
                default:
                    break;
            }

            _curLease = null;
            Svc.Log.Info($"Wrath lease cancelled via {(CancellationReason) reason} for: {s}");
        }

        internal static void Release()
        {
            if (_curLease.HasValue)
            {
                ReleaseControl(_curLease.Value);
                _curLease = null;
            }
        }

        internal static void Dispose()
        {
            Release();
            IPCSubscriber_Common.DisposeAll(_disposalTokens);
        }
    }

    public static class RSR_IPCSubscriber
    {
        public static string GetHostileTypeDescription(RotationSolverRebornIPC.TargetHostileType type) =>
            type switch
            {
                RotationSolverRebornIPC.TargetHostileType.AllTargetsCanAttack => "All Targets Can Attack aka Tank/Autoduty Mode",
                RotationSolverRebornIPC.TargetHostileType.TargetsHaveTarget => "Targets Have A Target",
                RotationSolverRebornIPC.TargetHostileType.AllTargetsWhenSoloInDuty => "All Targets When Solo In Duty",
                RotationSolverRebornIPC.TargetHostileType.AllTargetsWhenSolo => "All Targets When Solo",
                _ => "Unknown Target Type"
            };

        internal static         bool                 IsEnabled => IPCSubscriber_Common.IsReady("RotationSolver");

        public static void RotationAuto()
        {
            RotationSolverReborn.OtherCommand(RotationSolverRebornIPC.OtherCommandType.Settings, $"HostileType {Configuration.RSR_TargetHostileType}");
            RotationSolverReborn.OtherCommand(RotationSolverRebornIPC.OtherCommandType.Settings, "FriendlyPartyNpcHealRaise3 true");
            RotationSolverReborn.OtherCommand(RotationSolverRebornIPC.OtherCommandType.Settings, "AutoOffAfterCombat false");
            RotationSolverReborn.AutodutyChangeOperatingMode(RotationSolverRebornIPC.StateCommandType.AutoDuty, Plugin.currentPlayerItemLevelAndClassJob.Value.GetCombatRole() == CombatRole.Tank ?
                                                                                                                    Configuration.RSR_TargetingTypeTank :
                                                                                                                    Configuration.RSR_TargetingTypeNonTank);
        }

        public static void RotationStop() => RotationSolverReborn.ChangeOperatingMode(RotationSolverRebornIPC.StateCommandType.Off);
    }

    internal class IPCSubscriber_Common
    {
        internal static bool IsReady(string pluginName) => DalamudReflector.TryGetDalamudPlugin(pluginName, out _, false, true);

        internal static Version Version(string pluginName) => DalamudReflector.TryGetDalamudPlugin(pluginName, out IDalamudPlugin dalamudPlugin, false, true) ? dalamudPlugin.GetType().Assembly.GetName().Version : new Version(0, 0, 0, 0);

        internal static void DisposeAll(EzIPCDisposalToken[] _disposalTokens)
        {
            foreach (EzIPCDisposalToken token in _disposalTokens)
                try
                {
                    token.Dispose();
                }
                catch (Exception ex)
                {
                    Svc.Log.Error($"Error while unregistering IPC: {ex}");
                }
        }
    }
}

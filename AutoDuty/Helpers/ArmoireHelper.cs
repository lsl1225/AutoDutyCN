namespace AutoDuty.Helpers;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Cabinet = Lumina.Excel.Sheets.Cabinet;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;

internal class ArmoireHelper : ActiveHelperBase<ArmoireHelper>
{
    protected override string    Name        { get; } = nameof(ArmoireHelper);
    protected override string    DisplayName { get; } = "Armoire";
    public override    string[]? Commands    { get; init; } = ["armoire"];
    public override string? CommandDescription { get; init; } = "Stores items in your inventory to the Armoire.";

    protected override string[] AddonsToClose { get; } = {"SelectYesno", "Cabinet", "SelectString"};

    private int  skippedEntries = 0;

    internal override void Start()
    {
        IEnumerable<InventoryItem> items = InventoryHelper.GetInventorySelection(InventoryHelper.Bag).Where(item => this.ItemToCabinetIds.ContainsKey(item.ItemId)).ToList();

        if (!items.Any())
            return;

        if (GlamourLog_IPCSubscriber.IsEnabled)
            if (items.All(item => GlamourLog_IPCSubscriber.IsStored(item.ItemId)))
                return;

        base.Start();
        this.skippedEntries = 0;
    }

    protected override unsafe void HelperUpdate(IFramework framework)
    {
        if (!this.UpdateBase() || !PlayerHelper.IsValid)
            return;

        if (!EzThrottler.Throttle("Armoire", 125))
            return;

        if (GotoInnHelper.State == ActionState.Running)
            return;

        Plugin.action = "Armoire";

        if(Svc.Targets.Target == null || Svc.Targets.Target.Struct()->EventHandler->Info.EventId != 720978)
        {
            this.DebugLog("Target is not the armoire.");
            IGameObject? armoire = Svc.Objects.OrderBy(ObjectHelper.GetDistanceToPlayer).FirstOrDefault(o =>
                                                                                                        {
                                                                                                            EventHandler* eventHandler = o.Struct()->EventHandler;
                                                                                                            return eventHandler != null && eventHandler->Info.EventId == 720978;
                                                                                                        });

            if (armoire != null)
            {
                Svc.Targets.Target = armoire;
            }
            else if (!GotoInnHelper.InGCInn())
            {
                this.DebugLog("Not in the correct territory for the inn.");
                GotoInnHelper.Invoke();
            }
            return;
        }

        if (!ObjectHelper.BelowDistanceToPlayer(Svc.Targets.Target.Position, 3f, 2f))
        {
            this.DebugLog("Armoire is too far away.");
            if (!VNavmesh_IPCSubscriber.Path_IsRunning)
                VNavmesh_IPCSubscriber.SimpleMove_PathfindAndMoveTo(Svc.Targets.Target.Position, false);
            return;
        }
        
        if (VNavmesh_IPCSubscriber.Path_IsRunning)
            VNavmesh_IPCSubscriber.Path_Stop();

        if (GenericHelpers.TryGetAddonByName("SelectYesno", out AtkUnitBase* addonSelectYesno) && GenericHelpers.IsAddonReady(addonSelectYesno))
        {
            this.DebugLog("Selecting SelectYesno");
            AddonHelper.ClickSelectYesno();
            return;
        }

        AgentCabinet* agentCabinet = AgentCabinet.Instance();
        if (agentCabinet->IsAddonReady() && GenericHelpers.TryGetAddonByName("Cabinet", out AtkUnitBase* addonCabinet))
        {
            this.DebugLog("Cabinet addon is ready.");


            NumberArrayData* data = RaptureAtkModule.Instance()->GetNumberArrayData(48);

            if (data->IntArray[0] <= this.skippedEntries) // Number of items in the current category
            {
                this.Stop();
                return;
            }

            int baseIndex = 8 + this.skippedEntries * 7;

            RaptureAtkModule.ItemCache itemCache = agentCabinet->ItemCaches[this.skippedEntries];
            uint                       cabinetId = this.ItemToCabinetIds[itemCache.Id];

            this.DebugLog(UIState.Instance()->Cabinet.IsItemInCabinet(cabinetId).ToString());

            if (data->IntArray[baseIndex + 1] < 30_000 || UIState.Instance()->Cabinet.IsItemInCabinet(cabinetId))
            {
                this.skippedEntries++;
                this.DebugLog($"Skipping item. Skipped entries: {this.skippedEntries}");
                return;
            }

            AddonHelper.FireCallBack(addonCabinet, true, 0, data->IntArray[baseIndex + 4]);
            return;
        }

        if (GenericHelpers.TryGetAddonByName("SelectString", out AtkUnitBase* addonSelectString) && GenericHelpers.IsAddonReady(addonSelectString))
        {
            this.DebugLog("Selecting SelectString");
            AddonHelper.ClickSelectString(0);
            return;
        }

        if (!agentCabinet->IsAddonShown())
        {
            this.DebugLog("Interact with Cabinet");
            ObjectHelper.InteractWithObject(Svc.Targets.Target);
            return;
        }
    }

    public Dictionary<uint, uint> ItemToCabinetIds
    {
        get
        {
            if (field == null)
            {
                field = [];
                foreach (Cabinet cabinet in Svc.Data.GetExcelSheet<Cabinet>().ToList())
                    field[cabinet.Item.RowId] = cabinet.RowId;
            }
            return field;
        }
    }
}
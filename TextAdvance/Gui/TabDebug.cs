﻿using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Network.Structures;
using Dalamud.Memory;
using ECommons.Automation;
using ECommons.Automation.LegacyTaskManager;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel.GeneratedSheets2;
using System.Diagnostics;
using TextAdvance.Executors;
using Level = Lumina.Excel.GeneratedSheets.Level;
using QuestLinkMarker = FFXIVClientStructs.FFXIV.Client.UI.Agent.QuestLinkMarker;

namespace TextAdvance.Gui;

internal static unsafe class TabDebug
{
    static TaskManager TestTaskManager;
    internal static void Draw()
    {
        if (ImGui.CollapsingHeader("Cutscene"))
        {
            
        }
        if (ImGui.CollapsingHeader("Request"))
        {
            if (TryGetAddonByName<AddonRequest>("Request", out var request) && IsAddonReady((AtkUnitBase*)request))
            {
                ImGuiEx.Text($"{request->EntryCount}");
            }
        }
        if (ImGui.Button("Install hook")) Callback.InstallHook();
        if (ImGui.Button("UnInstall hook")) Callback.UninstallHook();
        if (ImGui.CollapsingHeader("Antistuck"))
        {
            ImGuiEx.Text($"""
                Last position: {S.MoveManager.LastPosition}
                Last update: {S.MoveManager.LastPositionUpdate} ({Environment.TickCount64 - S.MoveManager.LastPositionUpdate} ms ago)
                IsRunning: {P.NavmeshManager.IsRunning()}
                Animation locked: {Player.IsAnimationLocked} / {Player.AnimationLock}
                """);
        }
        if (ImGui.CollapsingHeader("Quest markers"))
        {
            var markers = AgentHUD.Instance()->MapMarkers.AsSpan();
            for (int i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                if (ThreadLoadImageHandler.TryGetIconTextureWrap(marker.IconId, false, out var tex))
                {
                    ImGui.Image(tex.ImGuiHandle, tex.Size);
                }
                ImGuiEx.Text($"{marker.IconId} / {marker.X} / {marker.Y} / {marker.Z} / {Vector3.Distance(Player.Position, new(marker.X, marker.Y, marker.Z))}");
                ImGui.Separator();
            }
        }
        if (ImGui.Button("copy target descriptor"))
        {
            if (Svc.Targets.Target != null) Copy(new ObjectDescriptor(Svc.Targets.Target, true).AsCtorString());
        }
        if (ImGui.CollapsingHeader("Auto interact"))
        {
            ImGuiEx.Text($"Target: {ExecAutoInteract.WasInteracted(Svc.Targets.Target)}");
            ImGuiEx.Text($"Auto interacted objects: {ExecAutoInteract.InteractedObjects.Print("\n")}");
        }
        if (ImGui.CollapsingHeader("Quests"))
        {
            ImGuiEx.Text($"{Utils.GetQuestArray().Print("\n")}");
        }
        if (ImGui.CollapsingHeader("Map"))
        {
            //ImGuiEx.Text($"Flight addr: {P.Memory.FlightAddr:X16} / {(P.Memory.FlightAddr - Process.GetCurrentProcess().MainModule.BaseAddress):X}");
            //ImGuiEx.Text($"CanFly: {P.Memory.IsFlightProhibited(P.Memory.FlightAddr)}");
            var questLinkSpan = AgentMap.Instance()->MiniMapQuestLinkContainer.Markers;

            foreach (var q in questLinkSpan)
            {
                ImGuiEx.Text($"{q.TooltipText.ToString()}");
                if (Svc.Data.GetExcelSheet<Level>().GetRow(q.LevelId) is not { X: var x, Y: var y, Z: var z }) continue;
                ImGuiEx.Text($"   {x}, {y} {z}");
            }
        }
        if (ImGui.CollapsingHeader("Reward pick"))
        {
            if (TryGetAddonByName<AtkUnitBase>("JournalResult", out var addon) && IsAddonReady(addon))
            {
                var canvas = addon->UldManager.NodeList[7];
                var r = new ReaderJournalResult(addon);
                ImGuiEx.Text($"Rewards: \n{r.OptionalRewards.Select(x => $"ID:{x.ItemID} / Icon:{x.IconID} / Amount:{x.Amount} / Name:{x.Name} ").Print("\n")}");
                for (int i = 0; i < 5; i++)
                {
                    if (ImGui.Button($"{i}"))
                    {
                        P.Memory.PickRewardItemUnsafe((nint)canvas->GetComponent(), i);
                    }
                }
                if(ImGui.Button("Stress test"))
                {
                    TestTaskManager ??= new();
                    for (int i = 0; i < 1000; i++)
                    {
                        var x = i % 5;
                        TestTaskManager.Enqueue(() => P.Memory.PickRewardItemUnsafe((nint)canvas->GetComponent(), x));
                    }
                }
                if(TestTaskManager != null)
                {
                    ImGuiEx.Text($"Task {TestTaskManager.MaxTasks - TestTaskManager.NumQueuedTasks}/{TestTaskManager.MaxTasks}");
                }
                if(ImGui.Button("Stop stress test"))
                {
                    TestTaskManager.Abort();
                }
            }
        }
    }
}

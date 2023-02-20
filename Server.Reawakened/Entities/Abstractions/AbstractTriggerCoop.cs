﻿using Microsoft.Extensions.Logging;
using Server.Base.Core.Extensions;
using Server.Base.Network;
using Server.Reawakened.Levels;
using Server.Reawakened.Levels.Enums;
using Server.Reawakened.Levels.Models.Entities;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Extensions;

namespace Server.Reawakened.Entities.Abstractions;

public abstract class AbstractTriggerCoop<T> : SyncedEntity<T>, ITriggerable where T : TriggerCoopController
{
    public bool DisabledAfterActivation => EntityData.DisabledAfterActivation;

    public int NbInteractionsNeeded => EntityData.NbInteractionsNeeded;
    public bool NbInteractionsMatchesNbPlayers => EntityData.NbInteractionsMatchesNbPlayers;

    public int TargetLevelEditorId => EntityData.TargetLevelEditorID;
    public int Target02LevelEditorId => EntityData.Target02LevelEditorID;
    public int Target03LevelEditorId => EntityData.Target03LevelEditorID;
    public int Target04LevelEditorId => EntityData.Target04LevelEditorID;
    public int Target05LevelEditorId => EntityData.Target05LevelEditorID;
    public int Target06LevelEditorId => EntityData.Target06LevelEditorID;
    public int Target07LevelEditorId => EntityData.Target07LevelEditorID;
    public int Target08LevelEditorId => EntityData.Target08LevelEditorID;

    public int TargetToDeactivateLevelEditorId => EntityData.TargetToDeactivateLevelEditorID;
    public int Target02ToDeactivateLevelEditorId => EntityData.Target02ToDeactivateLevelEditorID;
    public int Target03ToDeactivateLevelEditorId => EntityData.Target03ToDeactivateLevelEditorID;
    public int Target04ToDeactivateLevelEditorId => EntityData.Target04ToDeactivateLevelEditorID;

    public bool IsEnable => EntityData.isEnable;

    public int Target01ToEnableLevelEditorId => EntityData.Target01ToEnableLevelEditorID;
    public int Target02ToEnableLevelEditorId => EntityData.Target02ToEnableLevelEditorID;
    public int Target03ToEnableLevelEditorId => EntityData.Target03ToEnableLevelEditorID;
    public int Target04ToEnableLevelEditorId => EntityData.Target04ToEnableLevelEditorID;
    public int Target05ToEnableLevelEditorId => EntityData.Target05ToEnableLevelEditorID;

    public int Target01ToDisableLevelEditorId => EntityData.Target01ToDisableLevelEditorID;
    public int Target02ToDisableLevelEditorId => EntityData.Target02ToDisableLevelEditorID;
    public int Target03ToDisableLevelEditorId => EntityData.Target03ToDisableLevelEditorID;
    public int Target04ToDisableLevelEditorId => EntityData.Target04ToDisableLevelEditorID;
    public int Target05ToDisableLevelEditorId => EntityData.Target05ToDisableLevelEditorID;

    public float ActiveDuration => EntityData.ActiveDuration;

    public bool TriggerOnPressed => EntityData.TriggerOnPressed;
    public bool TriggerOnFireDamage => EntityData.TriggerOnFireDamage;
    public bool TriggerOnEarthDamage => EntityData.TriggerOnEarthDamage;
    public bool TriggerOnAirDamage => EntityData.TriggerOnAirDamage;
    public bool TriggerOnIceDamage => EntityData.TriggerOnIceDamage;
    public bool TriggerOnLightningDamage => EntityData.TriggerOnLightningDamage;
    public bool TriggerOnNormalDamage => EntityData.TriggerOnNormalDamage;

    public bool StayTriggeredOnUnpressed => EntityData.StayTriggeredOnUnpressed;
    public bool StayTriggeredOnReceiverActivated => EntityData.StayTriggeredOnReceiverActivated;

    public string TriggeredByItemInInventory => EntityData.TriggeredByItemInInventory;
    public bool TriggerOnGrapplingHook => EntityData.TriggerOnGrapplingHook;

    public bool Flip => EntityData.Flip;

    public string ActiveMessage => EntityData.ActiveMessage;
    public int SendActiveMessageToObjectId => EntityData.SendActiveMessageToObjectID;
    public string DeactiveMessage => EntityData.DeactiveMessage;

    public string TimerSound => EntityData.TimerSound;
    public string TimerEndSound => EntityData.TimerEndSound;

    public string QuestCompletedRequired => EntityData.QuestCompletedRequired;
    public string QuestInProgressRequired => EntityData.QuestInProgressRequired;

    public float TriggerRepeatDelay => EntityData.TriggerRepeatDelay;
    public TriggerCoopController.InteractionType InteractType => EntityData.InteractType;

    public float ActivationTimeAfterFirstInteraction => EntityData.ActivationTimeAfterFirstInteraction;

    public ILogger<TriggerCoopController> Logger { get; set; }

    public Dictionary<int, TriggerType> Triggers;

    public List<int> CurrentInteractors;

    public bool IsEnabled = true;
    public bool IsActive = true;

    public override void InitializeEntity()
    {
        IsEnabled = IsEnable;
        IsActive = false;

        CurrentInteractors = new List<int>();

        Triggers = new Dictionary<int, TriggerType>();

        AddToTriggers(new List<int>
        {
            TargetLevelEditorId,
            Target02LevelEditorId,
            Target03LevelEditorId,
            Target04LevelEditorId,
            Target05LevelEditorId,
            Target06LevelEditorId,
            Target07LevelEditorId,
            Target08LevelEditorId
        }, TriggerType.Activate);

        AddToTriggers(new List<int>
        {
            TargetToDeactivateLevelEditorId,
            Target02ToDeactivateLevelEditorId,
            Target03ToDeactivateLevelEditorId,
            Target04ToDeactivateLevelEditorId
        }, TriggerType.Deactivate);

        AddToTriggers(new List<int>
        {
            Target01ToEnableLevelEditorId,
            Target02ToEnableLevelEditorId,
            Target03ToEnableLevelEditorId,
            Target04ToEnableLevelEditorId,
            Target05ToEnableLevelEditorId
        }, TriggerType.Enable);

        AddToTriggers(new List<int>
        {
            Target01ToDisableLevelEditorId,
            Target02ToDisableLevelEditorId,
            Target03ToDisableLevelEditorId,
            Target04ToDisableLevelEditorId,
            Target05ToDisableLevelEditorId
        }, TriggerType.Disable);

        CheckTriggered();
    }

    public void AddToTriggers(List<int> triggers, TriggerType triggerType)
    {
        foreach (var trigger in triggers.Where(trigger => trigger > 0))
            Triggers.Add(trigger, triggerType);
    }

    public override void SendDelayedData(NetState netState) =>
        netState.SentEntityTriggered(Id, true, IsActive);

    public override void RunSyncedEvent(SyncEvent syncEvent, NetState netState)
    {
        if (!IsEnabled)
            return;

        var tEvent = new Trigger_SyncEvent(syncEvent);
        var player = netState.Get<Player>();

        var hasUpdated = false;

        if (tEvent.Activate)
        {
            if (!CurrentInteractors.Contains(player.PlayerId))
            {
                CurrentInteractors.Add(player.PlayerId);
                hasUpdated = true;
            }
        }
        else
        {
            if (CurrentInteractors.Contains(player.PlayerId))
            {
                CurrentInteractors.Remove(player.PlayerId);
                hasUpdated = true;
            }
        }

        if (!hasUpdated)
            return;

        // ReSharper disable once InvertIf
        if (
            (!IsActive || !StayTriggeredOnUnpressed) && !StayTriggeredOnReceiverActivated ||
            StayTriggeredOnReceiverActivated && !TriggerReceiverActivated() ||
            IsActive && StayTriggeredOnUnpressed && !StayTriggeredOnReceiverActivated
        )
        {
            Level.SentEntityTriggered(Id, player, true, tEvent.Activate);
            CheckTriggered();
        }
    }

    private void CheckTriggered()
    {
        switch (IsActive)
        {
            case false when CurrentInteractors.Count >= NbInteractionsNeeded:
                IsActive = true;
                Trigger();
                break;
            case true when !StayTriggeredOnReceiverActivated || !TriggerReceiverActivated():
                IsActive = false;
                Trigger();
                break;
        }
    }

    private void Trigger()
    {
        Logger.LogTrace("Trigger for {Id} has been set to {IsActive}, affecting {EntityCount}.", Id, IsActive, Triggers.Count);

        foreach (var trigger in Triggers)
        {
            if (Level.LevelEntities.Entities.ContainsKey(trigger.Key))
                if (Level.LevelEntities.Entities[trigger.Key].Count > 0)
                {
                    var triggers = Level.LevelEntities.Entities[trigger.Key];

                    var canTriggerEntities = triggers.OfType<ITriggerable>().ToList();

                    if (canTriggerEntities.Any())
                        foreach (var triggerEntity in canTriggerEntities)
                            triggerEntity.TriggerStateChange(trigger.Value, Level, IsActive);
                    else
                        Logger.LogError("Cannot trigger entity {Id} to state {State}, no class implements 'TriggerableEntity'.", trigger.Key, trigger.Value);

                    continue;
                }

            var entityType = "Unknown";

            if (Level.LevelEntities.UnknownEntities.TryGetValue(trigger.Key, out var value))
                entityType = string.Join(", ", value);

            Logger.LogError("Cannot trigger entity {Id} to state {State}, no entity for {EntityType}.",
                trigger.Key, trigger.Value, entityType);
        }
    }

    private bool TriggerReceiverActivated()
    {
        var receivers = Level.LevelEntities.GetEntities<TriggerReceiverEntity>();

        return Triggers
            .Where(r => r.Value == TriggerType.Activate)
            .Where(trigger => receivers.ContainsKey(trigger.Key))
            .Select(trigger => receivers[trigger.Key]).All(receiver => receiver.Activated);
    }

    public void TriggerStateChange(TriggerType triggerType, Level level, bool triggered) =>
        Logger.LogError("Trigger not implemented for {TypeName} (tried to change to {TriggerType}).",
            GetType().Name, triggerType);
}

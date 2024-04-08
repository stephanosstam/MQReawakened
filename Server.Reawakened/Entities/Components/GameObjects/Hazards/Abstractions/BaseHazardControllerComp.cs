﻿using A2m.Server;
using Microsoft.Extensions.Logging;
using Server.Base.Timers.Extensions;
using Server.Base.Timers.Services;
using Server.Reawakened.Core.Configs;
using Server.Reawakened.Entities.Colliders;
using Server.Reawakened.Entities.Colliders.Abstractions;
using Server.Reawakened.Entities.Components.Characters.Controllers;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.Rooms.Extensions;
using Server.Reawakened.Rooms.Models.Entities;
using Server.Reawakened.XMLs.Bundles.Base;
using UnityEngine;

namespace Server.Reawakened.Entities.Components.GameObjects.Hazards.Abstractions;

public abstract class BaseHazardControllerComp<T> : Component<T> where T : HazardController
{
    public string HurtEffect => ComponentData.HurtEffect;
    public float HurtLength => ComponentData.HurtLenght;
    public float InitialDamageDelay => ComponentData.InitialDamageDelay;
    public float DamageDelay => ComponentData.DamageDelay;
    public bool DeathPlane => ComponentData.DeathPlane;
    public string NullifyingEffect => ComponentData.NullifyingEffect;
    public bool HitOnlyVisible => ComponentData.HitOnlyVisible;
    public float InitialProgressRatio => ComponentData.InitialProgressRatio;
    public float ActiveDuration => ComponentData.ActiveDuration;
    public float DeactivationDuration => ComponentData.DeactivationDuration;
    public float HealthRatioDamage => ComponentData.HealthRatioDamage;
    public int HurtSelfOnDamage => ComponentData.HurtSelfOnDamage;

    public ItemEffectType EffectType = ItemEffectType.Unknown;
    public bool IsActive = true;
    public bool TimedHazard = false;

    public int Damage;
    private EnemyControllerComp _enemyController;
    private string _id;

    public TimerThread TimerThread { get; set; }
    public ItemRConfig ItemRConfig { get; set; }
    public WorldStatistics WorldStatistics { get; set; }
    public ItemCatalog ItemCatalog { get; set; }
    public ILogger<BaseHazardControllerComp<HazardController>> Logger { get; set; }

    public override object[] GetInitData(Player player) => [0];

    public override void InitializeComponent()
    {
        SetId(Id);

        Enum.TryParse(HurtEffect, true, out EffectType);

        //Activate timed hazards.
        if (ActiveDuration > 0 && DeactivationDuration > 0)
        {
            TimedHazard = true;
            TimerThread.DelayCall(ActivateHazard, null, TimeSpan.Zero, TimeSpan.Zero, 1);
        }

        //Prevents enemies and hazards sharing same collider Ids.
        if (_enemyController == null)
            //Hazards which also contain the LinearPlatform component already have colliders and do not need a new one created. They have NoEffect.
            //Many Toxic Clouds seem to have no components, so we find the object with PrefabName to create its colliders. (Seek Moss Temple for example)
            if (HurtEffect != ItemRConfig.NoEffect || PrefabName.Contains(ItemRConfig.ToxicCloud))
                TimerThread.DelayCall(ColliderCreationDelay, null, TimeSpan.FromSeconds(3), TimeSpan.Zero, 1);
    }

    public void SetId(string id)
    {
        _id = id;
        _enemyController = Room.GetEntityFromId<EnemyControllerComp>(id);
    }

    //Creates hazard colliders after enemy colliders are created to prevent duplicated collider ID bugs.
    public void ColliderCreationDelay(object _)
    {
        //Prevents spider webs from inaccurately adjusting collider positioning.
        if (EffectType == ItemEffectType.SlowStatusEffect)
            Rectangle.X = 0;

        var size = new Vector2(Rectangle.X, Rectangle.Y);
        var pos = BaseCollider.AdjustPosition(new Vector3(Position.X, Position.Y, Position.Z), size);


        Room.AddCollider(new HazardEffectCollider(_id, pos, size, ParentPlane, Room, Logger));
    }

    public void DeactivateHazard(object _)
    {
        IsActive = false;

        TimerThread.DelayCall(ActivateHazard, null,
                TimeSpan.FromSeconds(DeactivationDuration), TimeSpan.Zero, 1);
    }

    public void ActivateHazard(object _)
    {
        IsActive = true;

        TimerThread.DelayCall(DeactivateHazard, null,
                TimeSpan.FromSeconds(ActiveDuration), TimeSpan.Zero, 1);
    }

    //Standard Hazards
    public override void NotifyCollision(NotifyCollision_SyncEvent notifyCollisionEvent, Player player)
    {
        if (!notifyCollisionEvent.Colliding || player.TempData.Invincible)
            return;

        Room.SendSyncEvent(new StatusEffect_SyncEvent(player.GameObjectId, Room.Time,
            (int)ItemEffectType.BluntDamage, 0, 1, true, _id, false));

        var enemy = Room.GetEnemy(_id);

        if (enemy != null)
        {
            var damage = WorldStatistics.GetValue(ItemEffectType.AbilityPower, WorldStatisticsGroup.Enemy, enemy.Level) - player.Character.Data.CalculateDefense(EffectType, ItemCatalog);

            player.ApplyCharacterDamage(damage > 0 ? damage : 1, 1, TimerThread);

            return;
        }

        player.ApplyDamageByPercent(HealthRatioDamage, TimerThread);
    }

    public void ApplyHazardEffect(Player player)
    {
        if (player == null || player.TempData.Invincible ||
            TimedHazard && !IsActive || HitOnlyVisible && player.TempData.Invisible)
            return;

        Enum.TryParse(HurtEffect, true, out ItemEffectType effectType);

        Damage = (int)Math.Ceiling(player.Character.Data.MaxLife * HealthRatioDamage);

        //For toxic purple cloud hazards with no components
        if (PrefabName.Contains(ItemRConfig.ToxicCloud))
            effectType = ItemEffectType.PoisonDamage;

        switch (effectType)
        {
            case ItemEffectType.SlowStatusEffect:
                ApplySlowEffect(player);
                break;

            case ItemEffectType.BluntDamage:
                Room.SendSyncEvent(new StatusEffect_SyncEvent(player.GameObjectId, Room.Time,
                (int)ItemEffectType.BluntDamage, 1, 1, true, _id, false));

                player.ApplyCharacterDamage(Damage, DamageDelay, TimerThread);
                break;

            case ItemEffectType.PoisonDamage:
                TimerThread.DelayCall(ApplyPoisonEffect, player,
                    TimeSpan.FromSeconds(InitialDamageDelay), TimeSpan.FromSeconds(DamageDelay), 1);
                break;

            default:
                //Waterbreathing.
                if (HurtLength < 0)
                {
                    if (IsActive)
                    {
                        ApplyWaterBreathing(player);
                        IsActive = false;
                    }
                    return;
                }

                if (!player.TempData.Invincible)
                    Logger.LogInformation("Applied {statusEffect} to {characterName}", EffectType, player.CharacterName);

                //Used by Flamer and Dragon Statues which emit fire damage.
                Room.SendSyncEvent(new StatusEffect_SyncEvent(player.GameObjectId, Room.Time,
                (int)ItemEffectType.FireDamage, 1, 1, true, _id, false));

                player.ApplyCharacterDamage(Damage, DamageDelay, TimerThread);

                player.TemporaryInvincibility(TimerThread, 1);

                break;
        }
    }

    public void ApplyPoisonEffect(object playerData)
    {
        if (playerData == null)
            return;

        if (playerData is not Player player)
            return;

        var collider = Room.GetColliderById(_id);

        if (collider != null)
            if (!collider.CheckCollision(new PlayerCollider(player)))
                return;

        player.StartPoisonDamage(_id, Damage, (int)HurtLength, TimerThread);
    }

    public void ApplyWaterBreathing(object playerData)
    {
        if (playerData == null || playerData is not Player player)
            return;

        Room.SendSyncEvent(new StatusEffect_SyncEvent(player.GameObjectId, Room.Time,
                    (int)ItemEffectType.WaterBreathing, 1, 1, true, _id, false));

        player.StartUnderwaterTimer(player.Character.Data.MaxLife / 10, TimerThread, ItemRConfig);

        TimerThread.DelayCall(RestartTimerDelay, null, TimeSpan.FromSeconds(1), TimeSpan.Zero, 1);
        Logger.LogInformation("Reset underwater timer for {characterName}", player.CharacterName);
    }

    public void RestartTimerDelay(object data) => IsActive = true;

    public void ApplySlowEffect(Player player) =>
        player.ApplySlowEffect(_id, Damage);
}

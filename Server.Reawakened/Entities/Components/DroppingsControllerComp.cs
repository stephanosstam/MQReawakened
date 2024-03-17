﻿using Server.Base.Timers.Extensions;
using Server.Base.Timers.Services;
using Server.Reawakened.Entities.Entity.Utils;
using Server.Reawakened.Entities.Entity;
using Server.Reawakened.Rooms.Extensions;
using Server.Reawakened.Rooms.Models.Entities;
using Server.Reawakened.Rooms.Models.Planes;
using Server.Reawakened.Players;
using A2m.Server;

namespace Server.Reawakened.Entities.Components;
public class DroppingsControllerComp : Component<DroppingsController>
{
    public float DropRate => ComponentData.DropRate;

    public Vector3Model StartPosition { get; } = new Vector3Model();
    public TimerThread TimerThread { get; set; }

    public override void InitializeComponent()
    {
        SetStartPosition(Position);
        WaitDrop();
    }

    public void WaitDrop() =>
        TimerThread.DelayCall(SendDrop, null, TimeSpan.FromSeconds(DropRate), TimeSpan.FromSeconds(1), 1);

    public void SendDrop(object _)
    {
        if (Room.KilledObjects.Contains(Id)) return;

        ResetPosition(Room.GetEntityFromId<DroppingsControllerComp>(Id).Position);

        var projectileId = Room.SetProjectileId();

        var aiProjectile = new AIProjectileEntity(Room, Id, projectileId, Position, 0, -5, 3, false, TimerThread);
        Room.Projectiles.Add(projectileId, aiProjectile);

        Room.SendSyncEvent(AISyncEventHelper.AILaunchItem(Room.GetEntityFromId<DroppingsControllerComp>(Id),
            Position.X, Position.Y, Position.Z, 0, 0, 3, int.Parse(projectileId), 0));

        WaitDrop();
    }

    public void FreezePlayer(Player player)
    {
        Room.SendSyncEvent(new StatusEffect_SyncEvent(player.GameObjectId, Room.Time,
            (int)ItemEffectType.IceDamage, 1, 5, true, Id, false));

        Room.SendSyncEvent(new StatusEffect_SyncEvent(player.GameObjectId, Room.Time,
            (int)ItemEffectType.Freezing, 1, 5, true, Id, false));

        Room.SendSyncEvent(new StatusEffect_SyncEvent(player.GameObjectId, Room.Time,
           (int)ItemEffectType.FreezingStatusEffect, 1, 5, true, Id, false));
    }

    public void SetStartPosition(Vector3Model position)
    {
        StartPosition.X = position.X;
        StartPosition.Y = position.Y;
        StartPosition.Z = position.Z;
    }

    public void ResetPosition(Vector3Model position)
    {
        position.X = StartPosition.X;
        position.Y = StartPosition.Y;
        position.Z = StartPosition.Z;
    }
}

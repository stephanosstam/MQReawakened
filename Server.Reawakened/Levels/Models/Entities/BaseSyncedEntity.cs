﻿using Microsoft.Extensions.Logging;
using Server.Base.Network;
using Server.Reawakened.Levels.Models.Planes;

namespace Server.Reawakened.Levels.Models.Entities;

public abstract class BaseSyncedEntity
{
    public StoredEntityModel StoredEntity { get; private set; }

    public Level Level => StoredEntity.Level;
    public int Id => StoredEntity.GameObject.ObjectInfo.ObjectId;
    public string PrefabName => StoredEntity.GameObject.ObjectInfo.PrefabName;
    public Vector3Model Position => StoredEntity.GameObject.ObjectInfo.Position;
    public Vector3Model Rotation => StoredEntity.GameObject.ObjectInfo.Rotation;
    public Vector3Model Scale => StoredEntity.GameObject.ObjectInfo.Scale;

    public abstract string Name { get; }
    
    public virtual void InitializeEntity() { }

    public virtual string[] GetInitData(NetState netState) => Array.Empty<string>();

    public virtual void SendDelayedData(NetState netState) { }

    public virtual void RunSyncedEvent(SyncEvent syncEvent, NetState netState) =>
        StoredEntity.Logger.LogError("The synchronized entity {Id} has no override for events! Skipping...", Id);

    protected void SetEntityData(StoredEntityModel storedEntity)
    {
        StoredEntity = storedEntity;
        InitializeEntity();
    }
}
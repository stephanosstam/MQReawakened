﻿using Server.Reawakened.Entities.Components;
using Server.Reawakened.Entities.Enemies.BehaviorEnemies.BehaviourTypes;
using Server.Reawakened.Entities.Enemies.BehaviorEnemies.Extensions;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Helpers;
using Server.Reawakened.Rooms;
using Server.Reawakened.Rooms.Extensions;
using Server.Reawakened.XMLs.Models.Enemy.Enums;
using Server.Reawakened.XMLs.Models.Enemy.States;

namespace Server.Reawakened.Entities.Enemies.BehaviorEnemies.EnemyTypes;
public class EnemyBathog(Room room, string entityId, string prefabName, EnemyControllerComp enemyController, IServiceProvider services) : BehaviorEnemy(room, entityId, prefabName, enemyController, services)
{
    public override void Initialize()
    {
        base.Initialize();

        // Address magic numbers when we get to adding enemy effect mods
        Room.SendSyncEvent(AIInit(1, 1, 1));
        Room.SendSyncEvent(AISyncEventHelper.AIDo(Id, Room.Time, Position, 1.0f, BehaviorModel.IndexOf(StateTypes.Patrol), string.Empty, Position.x, Position.y, -1, false));

        AiBehavior = ChangeBehavior(StateTypes.Patrol);
    }

    public override void Damage(int damage, Player player)
    {
        base.Damage(damage, player);

        if (AiBehavior is not AIBehaviorShooting)
        {
            Room.SendSyncEvent(AISyncEventHelper.AIDo(Id, Room.Time, Position, 1.0f, BehaviorModel.IndexOf(OffensiveBehavior), string.Empty, player.TempData.Position.X,
                    player.TempData.Position.Y, Generic.Patrol_ForceDirectionX, false));

            // For some reason, the SyncEvent doesn't initialize these properly, so I just do them here
            AiData.Sync_TargetPosX = player.TempData.Position.X;
            AiData.Sync_TargetPosY = player.TempData.Position.Y;

            AiBehavior = ChangeBehavior(OffensiveBehavior);

            BehaviorEndTime = ResetBehaviorTime(MinBehaviorTime);
        }
    }

    public override void HandlePatrol()
    {
        base.HandlePatrol();

        DetectPlayers(OffensiveBehavior);
    }

    public override void HandleAggro()
    {
        base.HandleAggro();

        if (!AiBehavior.Update(ref AiData, Room.Time))
        {
            Room.SendSyncEvent(AISyncEventHelper.AIDo(Id, Room.Time, Position, 1.0f, BehaviorModel.IndexOf(StateTypes.LookAround), string.Empty, Position.x, Position.y,
            AiData.Intern_Dir, false));

            AiBehavior = ChangeBehavior(StateTypes.LookAround);

            var behavior = BehaviorModel.BehaviorData[StateTypes.LookAround] as LookAroundState;
            BehaviorEndTime = ResetBehaviorTime(behavior.LookTime);
        }
    }

    public override void HandleLookAround()
    {
        base.HandleLookAround();

        DetectPlayers(OffensiveBehavior);

        if (Room.Time >= BehaviorEndTime)
        {
            var argBuilder = new SeparatedStringBuilder('`');
            argBuilder.Append(Position.x);
            argBuilder.Append(AiData.Intern_SpawnPosY);

            Room.SendSyncEvent(AISyncEventHelper.AIDo(Id, Room.Time, Position, 1.0f, BehaviorModel.IndexOf(StateTypes.ComeBack), argBuilder.ToString(), Position.x, AiData.Intern_SpawnPosY, AiData.Intern_Dir, false));

            AiBehavior = ChangeBehavior(StateTypes.ComeBack);
            AiBehavior.MustDoComeback(AiData);
        }
    }

    public override void HandleShooting()
    {
        base.HandleShooting();

        if (!AiBehavior.Update(ref AiData, Room.Time))
        {
            Room.SendSyncEvent(AISyncEventHelper.AIDo(Id, Room.Time, Position, 1.0f, BehaviorModel.IndexOf(StateTypes.LookAround), string.Empty, AiData.Sync_TargetPosX, AiData.Sync_TargetPosY,
            AiData.Intern_Dir, false));

            AiBehavior = ChangeBehavior(StateTypes.LookAround);

            var behavior = BehaviorModel.BehaviorData[StateTypes.LookAround] as LookAroundState;
            BehaviorEndTime = ResetBehaviorTime(behavior.LookTime);
        }
    }

    public override void HandleComeBack()
    {
        base.HandleComeBack();

        if (!AiBehavior.Update(ref AiData, Room.Time))
        {
            AiBehavior = ChangeBehavior(StateTypes.Patrol);
            Room.SendSyncEvent(AISyncEventHelper.AIDo(Id, Room.Time, Position, 1.0f, BehaviorModel.IndexOf(StateTypes.Patrol), string.Empty, Position.x, Position.y, Generic.Patrol_ForceDirectionX, false));
        }
    }

    public override void DetectPlayers(StateTypes behaviorToRun)
    {
        foreach (var player in Room.Players.Values)
            if (PlayerInRange(player.TempData.Position, GlobalProperties.Global_DetectionLimitedByPatrolLine))
            {
                Room.SendSyncEvent(AISyncEventHelper.AIDo(Id, Room.Time, Position, 1.0f, BehaviorModel.IndexOf(behaviorToRun), string.Empty, player.TempData.Position.X,
                    player.TempData.Position.Y, Generic.Patrol_ForceDirectionX, false));

                // For some reason, the SyncEvent doesn't initialize these properly, so I just do them here
                AiData.Sync_TargetPosX = player.TempData.Position.X;
                AiData.Sync_TargetPosY = player.TempData.Position.Y;

                AiBehavior = ChangeBehavior(behaviorToRun);

                BehaviorEndTime = ResetBehaviorTime(MinBehaviorTime);
            }
    }
}

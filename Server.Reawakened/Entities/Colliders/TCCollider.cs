﻿using Server.Reawakened.Rooms.Models.Entities.Colliders.Abstractions;
using Server.Reawakened.Rooms.Models.Planes;
using UnityEngine;

namespace Server.Reawakened.Rooms.Models.Entities.Colliders;
public class TCCollider(string id, Vector3Model position, Vector2 size, string plane, Room room) :
    BaseCollider(id, position, size, plane, room, ColliderType.TerrainCube)
{
}
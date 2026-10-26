using System;
using System.Collections.Generic;
using UnityEngine;

namespace TomorrowsCrime.Procedural.Hotel
{
    public enum RoomType
    {
        Lobby,
        Reception,
        DiningRoom,
        Kitchen,
        StaffOffice,
        UtilityRoom,
        GroundCorridor,
        UpperLanding,
        GuestRoom101,
        GuestRoom102,
        GuestRoom103,
        HousekeepingStorage
    }

    public enum AnchorCategory
    {
        Tabletop,
        Desk,
        Counter,
        Nightstand,
        Shelf,
        Wall,
        FloorCorner,
        KeyStation,
        ServiceRack
    }

    [Serializable]
    public class RoomAnchor
    {
        public string AnchorId;
        public AnchorCategory Category;
        public Vector3 LocalPosition;
        public Vector3 LocalRotation;
        public bool IsOccupied;
        public string OccupyingObjectId;

        public RoomAnchor(string id, AnchorCategory cat, Vector3 pos, Vector3 rot = default)
        {
            AnchorId = id;
            Category = cat;
            LocalPosition = pos;
            LocalRotation = rot;
            IsOccupied = false;
        }
    }

    [Serializable]
    public class DoorwayConnection
    {
        public string ConnectionId;
        public string FromRoomId;
        public string ToRoomId;
        public Vector3 WorldPosition;
        public Vector3 DoorOrientationEuler;
        public bool RequiresKey;
        public string RequiredKeyId;
        public string DoorDisplayName;

        public DoorwayConnection(string id, string fromRoom, string toRoom, Vector3 pos, Vector3 rot, bool reqKey = false, string keyId = null, string name = null)
        {
            ConnectionId = id;
            FromRoomId = fromRoom;
            ToRoomId = toRoom;
            WorldPosition = pos;
            DoorOrientationEuler = rot;
            RequiresKey = reqKey;
            RequiredKeyId = keyId;
            DoorDisplayName = name ?? $"Door {id}";
        }
    }

    /// <summary>
    /// Immutable architectural definition of a hotel room.
    /// Specifies spatial bounds, floor index, entrance connections, and object placement anchors.
    /// </summary>
    public class RoomDefinition
    {
        public string RoomId { get; }
        public string DisplayName { get; }
        public RoomType Type { get; }
        public int FloorIndex { get; } // 0 = Ground, 1 = Upper
        public Bounds Bounds { get; }
        public List<DoorwayConnection> Connections { get; } = new();
        public List<RoomAnchor> Anchors { get; } = new();

        public RoomDefinition(string roomId, string displayName, RoomType type, int floor, Vector3 center, Vector3 size)
        {
            RoomId = roomId;
            DisplayName = displayName;
            Type = type;
            FloorIndex = floor;
            Bounds = new Bounds(center, size);
        }

        public void AddConnection(DoorwayConnection connection)
        {
            Connections.Add(connection);
        }

        public void AddAnchor(string anchorId, AnchorCategory cat, Vector3 localPos, Vector3 localRot = default)
        {
            Anchors.Add(new RoomAnchor(anchorId, cat, localPos, localRot));
        }
    }
}

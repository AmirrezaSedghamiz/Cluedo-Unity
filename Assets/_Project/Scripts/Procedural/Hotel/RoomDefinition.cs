using System;
using System.Collections.Generic;
using UnityEngine;

namespace TomorrowsCrime.Procedural.Hotel
{
    public enum RoomType
    {
        // Floor 0: Ground Floor (Lobby & Public Halls)
        Lobby,
        Reception,
        DiningRoom,
        Kitchen,
        StaffOffice,
        UtilityRoom,
        GroundCorridor,
        BarLounge,
        Conservatory,
        LuggageStorage,

        // Floor 1: First Floor (Guest Suites & Library)
        UpperLanding,
        Floor1Gallery,
        Library,
        CardRoom,
        LinenStorage,
        GuestRoom101,
        GuestRoom102,
        GuestRoom103,
        GuestRoom104,
        GuestRoom105,
        ReadingParlor,
        HousekeepingStorage,

        // Floor 2: Second Floor (Grand Salon & Deluxe Suites)
        Floor2Promenade,
        MusicSalon,
        BilliardsRoom,
        CigarLounge,
        ServiceAnnexFloor2,
        GuestRoom201,
        GuestRoom202,
        GuestRoom203,
        GuestRoom204,
        GuestRoom205,

        // Floor 3: Third Floor (Penthouses, Observatory & Solarium)
        Floor3SkyFoyer,
        CrownPenthouse,
        PresidentialBedroom,
        PresidentialStudy,
        Observatory,
        Solarium,
        VIPSuite301,
        VIPSuite302,
        MotorRoom,
        RoofGarden
    }

    [Serializable]
    public class RoomStyling
    {
        public string WallpaperMatKey = "WallpaperEmerald";
        public string WainscotMatKey = "WoodMahogany";
        public string FloorMatKey = "CarpetBurgundy";
        public string TrimMatKey = "BrassGold";
        public string CeilingMatKey = "WoodMahogany";
        public Color LightColor = new Color(1.0f, 0.88f, 0.65f); // 2700K warm tungsten
        public float LightIntensity = 2.4f;
        public float LightRange = 16.0f;
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
        public int FloorIndex { get; } // 0 = Ground, 1 = Upper, 2 = Grand, 3 = Penthouse
        public Bounds Bounds { get; }
        public RoomStyling Styling { get; set; } = new RoomStyling();
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

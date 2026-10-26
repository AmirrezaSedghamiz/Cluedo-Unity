using System;
using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Procedural.Hotel;

namespace TomorrowsCrime.Procedural.Objects
{
    public enum ObjectCategory
    {
        Clutter,
        Key,
        Utensil,
        Document,
        Container,
        Tool,
        Valuable
    }

    public enum ObjectRole
    {
        Decorative,
        Functional,
        InvestigativeEvidence
    }

    /// <summary>
    /// Definition template for physical spawnable objects.
    /// Specifies eligible rooms, valid anchor types, physical collision dimensions,
    /// interaction behavior, and evidence roles.
    /// </summary>
    [Serializable]
    public class ObjectDefinition
    {
        public string ObjectId;
        public string DisplayName;
        public ObjectCategory Category;
        public ObjectRole Role;
        public List<RoomType> EligibleRooms = new();
        public List<AnchorCategory> EligibleAnchors = new();
        public Vector3 ColliderSize = new Vector3(0.5f, 0.3f, 0.5f);
        public Vector3 LocalOffset = Vector3.zero;
        public string ModelPrefabPath;
        public string MaterialKey;
        public string ShortDescription;
        public string DetailedDescription;
        public string LinkedEvidenceId;

        public ObjectDefinition(string id, string name, ObjectCategory cat, ObjectRole role,
            string prefabPath = null, string matKey = null, string shortDesc = null, string detailDesc = null, string evidenceId = null)
        {
            ObjectId = id;
            DisplayName = name;
            Category = cat;
            Role = role;
            ModelPrefabPath = prefabPath;
            MaterialKey = matKey;
            ShortDescription = shortDesc;
            DetailedDescription = detailDesc;
            LinkedEvidenceId = evidenceId;
        }

        public ObjectDefinition WithRooms(params RoomType[] rooms)
        {
            EligibleRooms.AddRange(rooms);
            return this;
        }

        public ObjectDefinition WithAnchors(params AnchorCategory[] anchors)
        {
            EligibleAnchors.AddRange(anchors);
            return this;
        }

        public ObjectDefinition WithCollider(Vector3 size, Vector3 offset = default)
        {
            ColliderSize = size;
            LocalOffset = offset;
            return this;
        }
    }
}

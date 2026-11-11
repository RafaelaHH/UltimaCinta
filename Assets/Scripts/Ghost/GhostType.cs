using System.Collections.Generic;
using UltimaCinta.Data;
using UnityEngine;

namespace UltimaCinta.Ghost
{
    [CreateAssetMenu(fileName = "GhostType", menuName = "UltimaCinta/Ghost Type")]
    public sealed class GhostType : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField, Min(0f)] private float movementSpeed;
        [SerializeField] private GhostEvidence[] evidences = new GhostEvidence[0];
        [SerializeField] private GhostBehavior behavior;
        [SerializeField] private InventoryItem[] ritualItems = new InventoryItem[0];
        [SerializeField] private GameObject ghostPrefab;

        public string DisplayName => displayName;
        public float MovementSpeed => movementSpeed;
        public IReadOnlyList<GhostEvidence> Evidences => evidences;
        public GhostBehavior Behavior => behavior;
        public IReadOnlyList<InventoryItem> RitualItems => ritualItems;
        public GameObject GhostPrefab => ghostPrefab;
    }
}

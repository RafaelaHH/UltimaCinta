using UnityEngine;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostRoomMarker : MonoBehaviour
    {
        [SerializeField] private string roomName;
        public string RoomName => string.IsNullOrWhiteSpace(roomName) ? transform.parent?.name ?? name : roomName;
    }
}

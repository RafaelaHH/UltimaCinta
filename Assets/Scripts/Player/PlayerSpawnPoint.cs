using Fusion;
using UnityEngine;

namespace UltimaCinta.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private Transform[] points;

        public Vector3 GetPosition(PlayerRef player)
        {
            var point = GetPoint(player);
            return point != null ? point.position : transform.position;
        }

        public Quaternion GetRotation(PlayerRef player)
        {
            var point = GetPoint(player);
            return point != null ? point.rotation : transform.rotation;
        }

        private Transform GetPoint(PlayerRef player)
        {
            if (points == null || points.Length == 0) return transform;
            var index = Mathf.Abs(player.PlayerId) % points.Length;
            return points[index] != null ? points[index] : transform;
        }
    }
}

using UnityEngine;

namespace UltimaCinta.Player
{
    public interface IPlayerInputSource
    {
        Vector2 Move { get; }
        Vector2 LookDelta { get; }
        bool SprintHeld { get; }
        bool CrouchHeld { get; }
        bool InteractPressed { get; }
        bool NightVisionPressed { get; }
        float ZoomDelta { get; }
        bool FlashlightPressed { get; }
        int InventorySlotPressed { get; }
    }
}

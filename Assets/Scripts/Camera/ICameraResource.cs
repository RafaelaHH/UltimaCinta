using System;
using UnityEngine;
namespace UltimaCinta.CameraSystem
{
    public interface ICameraResource
    {
        float BatteryNormalized { get; }
        float TapeNormalized { get; }
        bool IsPowered { get; }
        bool NightVisionEnabled { get; }
        event Action ResourcesChanged;
        void ToggleNightVision();
        void SetZoom(float inputDelta);
        void Tick(float deltaTime);
    }
}

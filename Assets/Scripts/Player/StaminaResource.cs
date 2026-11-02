using System;
using UltimaCinta.Data;
using UnityEngine;

namespace UltimaCinta.Player
{
    public interface IStaminaResource
    {
        float NormalizedValue { get; }
        bool TryDrain(float deltaTime);
        void Recover(float deltaTime);
        event Action<float> Changed;
    }

    [DisallowMultipleComponent]
    public sealed class StaminaResource : MonoBehaviour, IStaminaResource
    {
        [SerializeField] private GameConfig gameConfig;
        private float _currentSeconds;

        public event Action<float> Changed;
        public float CurrentSeconds => _currentSeconds;
        public float NormalizedValue => gameConfig == null || gameConfig.StaminaCapacitySeconds <= 0f
            ? 0f
            : _currentSeconds / gameConfig.StaminaCapacitySeconds;

        private void Awake()
        {
            if (gameConfig != null)
            {
                _currentSeconds = gameConfig.StaminaCapacitySeconds;
            }
        }

        public bool TryDrain(float deltaTime)
        {
            if (gameConfig == null || _currentSeconds <= 0f)
            {
                return false;
            }

            SetCurrent(_currentSeconds - gameConfig.StaminaDrainPerSecond * deltaTime);
            return _currentSeconds > 0f;
        }

        public void Recover(float deltaTime)
        {
            if (gameConfig == null)
            {
                return;
            }

            SetCurrent(_currentSeconds + gameConfig.StaminaRecoveryPerSecond * deltaTime);
        }

        private void SetCurrent(float value)
        {
            var next = Mathf.Clamp(value, 0f, gameConfig.StaminaCapacitySeconds);
            if (Mathf.Approximately(next, _currentSeconds))
            {
                return;
            }

            _currentSeconds = next;
            Changed?.Invoke(NormalizedValue);
        }
    }
}

using UnityEngine;
using UltimaCinta.Interaction;
using UltimaCinta.Data;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class EvidenceTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private GhostEvidence evidence;
        [SerializeField] private GameConfig gameConfig;
        public string Prompt => $"E — buscar {EvidenceLabel}";
        private string EvidenceLabel => evidence switch
        {
            GhostEvidence.Apparition => "aparición",
            GhostEvidence.EmfLevelFive => "EMF nivel 5",
            GhostEvidence.FreezingTemperature => "temperatura bajo cero",
            GhostEvidence.Writing => "escritura",
            GhostEvidence.Orbs => "orbes",
            _ => evidence.ToString()
        };

        public bool Interact(GameObject interactor)
        {
            var world = FindFirstObjectByType<GhostWorldController>();
            if (world == null || world.Agent == null || interactor == null || gameConfig == null) return false;
            if (Vector3.Distance(interactor.transform.position, world.Agent.transform.position) > gameConfig.EvidenceConfirmationRange)
            {
                interactor.GetComponent<UltimaCinta.Player.PlayerInventory>()?.ShowMessage("No se detecta señal.");
                return false;
            }
            var journal = interactor.GetComponent<GhostJournal>();
            if (!world.Agent.HasEvidence(evidence))
            {
                Debug.Log($"[Fase 3] Evidencia consultada: {evidence}; no corresponde a {world.CurrentType.DisplayName}.", this);
                interactor.GetComponent<UltimaCinta.Player.PlayerInventory>()?.ShowMessage("Sin evidencia concluyente.");
                return false;
            }
            journal?.MarkEvidence(evidence);
            Debug.Log($"[Fase 3] Evidencia confirmada: {evidence}; fantasma={world.CurrentType.DisplayName}.", this);
            interactor.GetComponent<UltimaCinta.Player.PlayerInventory>()?.ShowMessage($"Evidencia: {evidence}");
            return true;
        }
    }
}

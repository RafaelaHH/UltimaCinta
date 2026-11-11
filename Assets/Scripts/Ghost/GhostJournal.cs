using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostJournal : MonoBehaviour
    {
        private enum JournalSection { Evidencias, Fantasmas, Ritual, Notas }
        private static readonly string[] SectionLabels = { "EVIDENCIAS", "FANTASMAS", "RITUAL", "NOTAS" };
        private static readonly GhostEvidence[] AllEvidence = (GhostEvidence[])System.Enum.GetValues(typeof(GhostEvidence));

        [SerializeField] private GhostType[] catalog = new GhostType[0];
        private readonly HashSet<GhostEvidence> _marked = new HashSet<GhostEvidence>();
        private readonly List<GhostType> _candidateBuffer = new List<GhostType>();
        private JournalSection _section;
        private bool _open;
        private bool _movementWasEnabled;
        private bool _lookWasEnabled;
        private bool _interactionWasEnabled;
        private UltimaCinta.Player.FirstPersonMovementController _movement;
        private UltimaCinta.Player.FirstPersonLookController _look;
        private UltimaCinta.Interaction.PlayerInteractionController _interaction;
        private UltimaCinta.Player.PlayerInventory _inventory;
        public GhostType SelectedGhost { get; private set; }
        public IReadOnlyCollection<GhostEvidence> MarkedEvidence => _marked;

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current?.jKey.wasPressedThisFrame != true) return;
            if (_open) SetOpen(false);
            else if (HasJournal()) SetOpen(true);
            else _inventory?.ShowMessage("Recoge el diario en la camioneta.");
        }

        private void Awake()
        {
            _movement = GetComponent<UltimaCinta.Player.FirstPersonMovementController>();
            _look = GetComponent<UltimaCinta.Player.FirstPersonLookController>();
            _interaction = GetComponent<UltimaCinta.Interaction.PlayerInteractionController>();
            _inventory = GetComponent<UltimaCinta.Player.PlayerInventory>();
        }

        public void MarkEvidence(GhostEvidence evidence) => _marked.Add(evidence);

        private void OnGUI()
        {
            if (!_open)
            {
                DrawJournalPrompt();
                return;
            }

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            var panelWidth = Mathf.Min(900f, Screen.width - 32f);
            var panelHeight = Mathf.Min(540f, Screen.height - 32f);
            var panel = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);
            GUI.color = new Color(0.32f, 0.22f, 0.14f);
            GUI.Box(panel, GUIContent.none);
            var page = new Rect(panel.x + 10f, panel.y + 10f, panel.width - 20f, panel.height - 20f);
            GUI.color = new Color(0.91f, 0.84f, 0.68f);
            GUI.Box(page, GUIContent.none);
            GUI.color = previousColor;

            var ink = new Color(0.18f, 0.14f, 0.10f);
            var title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 24, fontStyle = FontStyle.Bold };
            title.normal.textColor = ink;
            GUI.Label(new Rect(page.x + 22f, page.y + 10f, page.width - 44f, 36f), "DIARIO DE CAMPO", title);
            GUI.DrawTexture(new Rect(page.x + 22f, page.y + 49f, page.width - 44f, 2f), Texture2D.whiteTexture);

            var nav = new Rect(page.x + 18f, page.y + 66f, 168f, page.height - 112f);
            var content = new Rect(nav.xMax + 16f, nav.y, page.width - nav.width - 52f, nav.height);
            DrawNavigation(nav, ink);
            GUI.color = new Color(0.42f, 0.31f, 0.20f, 0.38f);
            GUI.Box(new Rect(content.x - 8f, content.y, 1f, content.height), GUIContent.none);
            GUI.color = previousColor;

            switch (_section)
            {
                case JournalSection.Evidencias: DrawEvidenceSection(content, ink); break;
                case JournalSection.Fantasmas: DrawGhostSection(content, ink); break;
                case JournalSection.Ritual: DrawRitualSection(content, ink); break;
                case JournalSection.Notas: DrawNotesSection(content, ink); break;
            }

            var footer = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 13 };
            footer.normal.textColor = ink;
            GUI.Label(new Rect(page.x + 20f, page.yMax - 34f, page.width - 180f, 22f), "ÚLTIMA CINTA  ·  Registro de campo", footer);
            if (GUI.Button(new Rect(page.xMax - 126f, page.yMax - 40f, 106f, 30f), "Cerrar [J]")) SetOpen(false);
            GUI.color = previousColor;
        }

        private void DrawJournalPrompt()
        {
            if (HasJournal())
            {
                if (GUI.Button(new Rect(Screen.width - 132f, 18f, 112f, 34f), "Diario [J]")) SetOpen(true);
                return;
            }

            var prompt = new Rect(Screen.width * 0.5f - 190f, Screen.height - 120f, 380f, 30f);
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.Box(prompt, GUIContent.none);
            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
            GUI.Label(prompt, "DIARIO · Recógelo en la camioneta [E]", style);
        }

        private void DrawNavigation(Rect area, Color ink)
        {
            for (var index = 0; index < SectionLabels.Length; index++)
            {
                var section = (JournalSection)index;
                var button = new Rect(area.x, area.y + index * 52f, area.width, 42f);
                var style = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = _section == section ? FontStyle.Bold : FontStyle.Normal };
                var tabColor = GUI.color;
                GUI.color = _section == section ? new Color(0.65f, 0.51f, 0.32f) : new Color(0.78f, 0.70f, 0.53f);
                if (GUI.Button(button, SectionLabels[index], style)) _section = section;
                GUI.color = tabColor;
            }
        }

        private void DrawEvidenceSection(Rect area, Color ink)
        {
            DrawSectionTitle(area, "Evidencias", "Marca las señales que hayas confirmado con tus herramientas.", ink);
            var lineStyle = BodyStyle(ink);
            var y = area.y + 80f;
            foreach (var evidence in AllEvidence)
            {
                var wasMarked = _marked.Contains(evidence);
                var nowMarked = GUI.Toggle(new Rect(area.x + 14f, y, area.width - 28f, 34f), wasMarked, EvidenceLabel(evidence), lineStyle);
                if (nowMarked) _marked.Add(evidence); else _marked.Remove(evidence);
                DrawRule(area.x + 14f, y + 36f, area.width - 28f);
                y += 48f;
            }
            DrawCallout(new Rect(area.x + 14f, area.yMax - 92f, area.width - 28f, 64f),
                $"Evidencias registradas: {_marked.Count} de 5.\nAbre FANTASMAS para reducir la lista de sospechosos.", ink);
        }

        private void DrawGhostSection(Rect area, Color ink)
        {
            DrawSectionTitle(area, "Identificación de fantasmas", "La lista se actualiza según las evidencias marcadas.", ink);
            var candidates = GetCandidates();
            var y = area.y + 82f;
            var lineStyle = BodyStyle(ink);
            if (candidates.Count == 0)
            {
                DrawCallout(new Rect(area.x + 14f, y, area.width - 28f, 62f), "Ningún fantasma coincide. Revisa las evidencias marcadas.", ink);
                return;
            }
            foreach (var ghost in candidates)
            {
                var chosen = SelectedGhost == ghost;
                if (GUI.Toggle(new Rect(area.x + 14f, y, area.width - 28f, 34f), chosen, ghost.DisplayName, lineStyle)) SelectedGhost = ghost;
                y += 42f;
            }
            if (SelectedGhost != null && Matches(SelectedGhost))
            {
                y += 8f;
                DrawRule(area.x + 14f, y, area.width - 28f);
                y += 12f;
                GUI.Label(new Rect(area.x + 14f, y, area.width - 28f, 26f), "EVIDENCIAS CARACTERÍSTICAS", SectionStyle(ink));
                y += 30f;
                foreach (var evidence in SelectedGhost.Evidences)
                {
                    if (y > area.yMax - 38f) break;
                    GUI.Label(new Rect(area.x + 18f, y, area.width - 36f, 26f), "• " + EvidenceLabel(evidence), lineStyle);
                    y += 28f;
                }
            }
        }

        private void DrawRitualSection(Rect area, Color ink)
        {
            DrawSectionTitle(area, "Objetos de ritual", "Consulta los objetos asociados al sospechoso actual.", ink);
            var y = area.y + 82f;
            var lineStyle = BodyStyle(ink);
            if (SelectedGhost == null || !Matches(SelectedGhost))
            {
                DrawCallout(new Rect(area.x + 14f, y, area.width - 28f, 62f), "Primero selecciona un sospechoso en la sección FANTASMAS.", ink);
                return;
            }
            GUI.Label(new Rect(area.x + 14f, y, area.width - 28f, 30f), SelectedGhost.DisplayName.ToUpperInvariant(), SectionStyle(ink));
            y += 40f;
            var items = SelectedGhost.RitualItems;
            if (items == null || items.Count == 0)
            {
                GUI.Label(new Rect(area.x + 14f, y, area.width - 28f, 32f), "Sin objetos de ritual registrados.", lineStyle);
                return;
            }
            foreach (var item in items)
            {
                if (item == null) continue;
                GUI.Label(new Rect(area.x + 18f, y, area.width - 36f, 32f), "• " + item.DisplayName, lineStyle);
                y += 38f;
            }
        }

        private void DrawNotesSection(Rect area, Color ink)
        {
            DrawSectionTitle(area, "Notas de investigación", "Resumen de lo que el equipo ha registrado en esta sesión.", ink);
            var y = area.y + 82f;
            var lineStyle = BodyStyle(ink);
            GUI.Label(new Rect(area.x + 14f, y, area.width - 28f, 30f), $"Evidencias confirmadas: {_marked.Count} / 5", SectionStyle(ink));
            y += 42f;
            foreach (var evidence in _marked)
            {
                GUI.Label(new Rect(area.x + 18f, y, area.width - 36f, 26f), "• " + EvidenceLabel(evidence), lineStyle);
                y += 30f;
            }
            y += 10f;
            DrawRule(area.x + 14f, y, area.width - 28f);
            y += 16f;
            GUI.Label(new Rect(area.x + 14f, y, area.width - 28f, 28f),
                SelectedGhost != null && Matches(SelectedGhost) ? "Sospechoso señalado: " + SelectedGhost.DisplayName : "Sospechoso señalado: ninguno",
                SectionStyle(ink));
            y += 40f;
            GUI.Label(new Rect(area.x + 14f, y, area.width - 28f, 28f), "Coincidencias posibles", SectionStyle(ink));
            y += 32f;
            var candidates = GetCandidates();
            foreach (var ghost in candidates)
            {
                if (y > area.yMax - 8f) break;
                GUI.Label(new Rect(area.x + 18f, y, area.width - 36f, 25f), "• " + ghost.DisplayName, lineStyle);
                y += 28f;
            }
        }

        private List<GhostType> GetCandidates()
        {
            _candidateBuffer.Clear();
            if (catalog == null) return _candidateBuffer;
            foreach (var ghost in catalog)
                if (ghost != null && Matches(ghost)) _candidateBuffer.Add(ghost);
            return _candidateBuffer;
        }

        private static void DrawSectionTitle(Rect area, string heading, string description, Color ink)
        {
            GUI.Label(new Rect(area.x + 14f, area.y + 2f, area.width - 28f, 32f), heading, SectionStyle(ink));
            GUI.Label(new Rect(area.x + 14f, area.y + 34f, area.width - 28f, 30f), description, BodyStyle(ink));
        }

        private static void DrawCallout(Rect rect, string message, Color ink)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.52f, 0.42f, 0.29f, 0.24f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = previousColor;
            var style = BodyStyle(ink);
            style.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(rect.x + 10f, rect.y + 6f, rect.width - 20f, rect.height - 12f), message, style);
        }

        private static void DrawRule(float x, float y, float width)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.42f, 0.31f, 0.20f, 0.34f);
            GUI.DrawTexture(new Rect(x, y, width, 1f), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static GUIStyle BodyStyle(Color ink)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            style.normal.textColor = ink;
            return style;
        }

        private static GUIStyle SectionStyle(Color ink)
        {
            var style = BodyStyle(ink);
            style.fontSize = 18;
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        private void SetOpen(bool open)
        {
            if (_open == open) return;
            _open = open;
            if (open)
            {
                _movementWasEnabled = _movement != null && _movement.enabled;
                _lookWasEnabled = _look != null && _look.enabled;
                _interactionWasEnabled = _interaction != null && _interaction.enabled;
                if (_movement != null) _movement.enabled = false;
                if (_look != null) _look.enabled = false;
                if (_interaction != null) _interaction.enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                var isDead = TryGetComponent<SanityController>(out var sanity) && sanity.IsDead;
                if (_movement != null) _movement.enabled = _movementWasEnabled && !isDead;
                if (_look != null) _look.enabled = _lookWasEnabled && !isDead;
                if (_interaction != null) _interaction.enabled = _interactionWasEnabled && !isDead;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private bool Matches(GhostType ghost)
        {
            foreach (var evidence in _marked)
                if (!ghost.Evidences.Contains(evidence)) return false;
            return true;
        }

        private bool HasJournal() => _inventory != null && _inventory.HasJournal;

        private static string EvidenceLabel(GhostEvidence evidence) => evidence switch
        {
            GhostEvidence.Apparition => "Aparición",
            GhostEvidence.EmfLevelFive => "EMF nivel 5",
            GhostEvidence.FreezingTemperature => "Temperatura bajo cero",
            GhostEvidence.Writing => "Libro de escritura",
            GhostEvidence.Orbs => "Orbes",
            _ => evidence.ToString()
        };
    }
}

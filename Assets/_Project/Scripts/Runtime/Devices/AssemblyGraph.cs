using System;
using System.Collections.Generic;
using System.Text;

namespace PhoneRepair.Devices
{
    /// <summary>Answer from the assembly rules: allowed, or a player-facing reason why not.</summary>
    public readonly struct RuleCheck
    {
        private RuleCheck(bool allowed, string reason)
        {
            Allowed = allowed;
            Reason = reason ?? string.Empty;
        }

        public bool Allowed { get; }
        public string Reason { get; }

        public static RuleCheck Ok() => new(true, string.Empty);
        public static RuleCheck No(string reason) => new(false, reason);
    }

    /// <summary>
    /// The assembly rules of one phone as plain data (no scene objects), so they can be unit tested.
    /// Every slot is a node with a state. "BlockedBy" lists what has to come off first;
    /// "MountedOn" lists what has to be in place before the part can go back on.
    /// The same two lists give the disassembly AND the reassembly order.
    /// </summary>
    public sealed class AssemblyGraph
    {
        private readonly Dictionary<string, Node> nodes = new();
        private readonly List<Node> ordered = new();

        public AssemblyGraph(IEnumerable<Node> source)
        {
            foreach (Node node in source)
            {
                if (node == null || nodes.ContainsKey(node.Id))
                {
                    continue;
                }

                nodes.Add(node.Id, node);
                ordered.Add(node);
            }
        }

        public IReadOnlyList<Node> Nodes => ordered;

        /// <summary>Builds the graph from a phone's slots and part definitions (all parts start Secured).</summary>
        public static AssemblyGraph FromDefinition(PhoneModelDefinition model)
        {
            var list = new List<Node>();
            if (model != null)
            {
                foreach (PartSlot slot in model.Slots)
                {
                    if (string.IsNullOrWhiteSpace(slot.slotId) || slot.part == null)
                    {
                        continue;
                    }

                    PartDefinition part = slot.part;
                    list.Add(new Node(slot.slotId, part.DisplayName, part.DetachStyle, part.DetachVerb, part.AttachVerb,
                        slot.blockedBy, slot.mountedOn));
                }
            }

            return new AssemblyGraph(list);
        }

        public bool TryGet(string id, out Node node) => nodes.TryGetValue(id ?? string.Empty, out node);

        public PartState GetState(string id) => nodes[id].State;

        public void SetState(string id, PartState state) => nodes[id].State = state;

        /// <summary>Rule entries that point at slots that don't exist ("Battery -> Batery").</summary>
        public IEnumerable<string> FindMissingReferences()
        {
            foreach (Node node in ordered)
            {
                foreach (string other in node.BlockedBy)
                {
                    if (!nodes.ContainsKey(other))
                    {
                        yield return $"{node.Id} blockedBy '{other}'";
                    }
                }

                foreach (string other in node.MountedOn)
                {
                    if (!nodes.ContainsKey(other))
                    {
                        yield return $"{node.Id} mountedOn '{other}'";
                    }
                }
            }
        }

        /// <summary>Can this part come off (or unplug) right now?</summary>
        public RuleCheck CanDetach(string id)
        {
            Node node = nodes[id];
            switch (node.Style)
            {
                case DetachStyle.Fixed:
                    return RuleCheck.No($"The {node.DisplayName} holds everything together. It stays.");
                case DetachStyle.ReleaseInPlace when node.State != PartState.Secured:
                    return RuleCheck.No($"{node.DisplayName} is already unplugged.");
                case DetachStyle.Remove when node.State == PartState.Removed:
                    return RuleCheck.No($"{node.DisplayName} is already out.");
            }

            string blocked = Describe(node.BlockedBy, mounts: false);
            return blocked == null ? RuleCheck.Ok() : RuleCheck.No(blocked);
        }

        /// <summary>Can this part go back on (from the mat) or be fastened (a loose screw, an unplugged connector)?</summary>
        public RuleCheck CanAttach(string id)
        {
            Node node = nodes[id];
            if (node.Style == DetachStyle.Fixed || node.State == PartState.Secured)
            {
                return RuleCheck.No($"{node.DisplayName} is already in place.");
            }

            string blocked = Describe(node.BlockedBy, mounts: false);
            if (blocked != null)
            {
                return RuleCheck.No(blocked);
            }

            string missing = Describe(node.MountedOn, mounts: true);
            return missing == null ? RuleCheck.Ok() : RuleCheck.No(missing);
        }

        /// <summary>"First: unscrew P2 Mochi Screw (×4), unplug Battery Connector" — or null if nothing is in the way.</summary>
        private string Describe(IReadOnlyList<string> ids, bool mounts)
        {
            List<(string text, int count)> groups = null;
            foreach (string id in ids)
            {
                if (!nodes.TryGetValue(id, out Node other))
                {
                    continue; // reported by FindMissingReferences / content tests
                }

                bool inTheWay = mounts ? other.State != PartState.Secured : other.IsBlocking;
                if (!inTheWay)
                {
                    continue;
                }

                string text = $"{LowerFirst(mounts ? other.AttachVerb : other.DetachVerb)} {other.DisplayName}";
                groups ??= new List<(string, int)>();
                int index = groups.FindIndex(g => g.text == text);
                if (index >= 0)
                {
                    groups[index] = (text, groups[index].count + 1);
                }
                else
                {
                    groups.Add((text, 1));
                }
            }

            if (groups == null)
            {
                return null;
            }

            var sb = new StringBuilder("First: ");
            for (int i = 0; i < groups.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(groups[i].text);
                if (groups[i].count > 1)
                {
                    sb.Append(" (×").Append(groups[i].count).Append(')');
                }
            }

            return sb.ToString();
        }

        private static string LowerFirst(string text) =>
            string.IsNullOrEmpty(text) ? text : char.ToLowerInvariant(text[0]) + text.Substring(1);

        /// <summary>One slot of the phone in the rules graph.</summary>
        public sealed class Node
        {
            public Node(string id, string displayName, DetachStyle style, string detachVerb, string attachVerb,
                IReadOnlyList<string> blockedBy, IReadOnlyList<string> mountedOn)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
                Style = style;
                DetachVerb = string.IsNullOrWhiteSpace(detachVerb) ? "Remove" : detachVerb;
                AttachVerb = string.IsNullOrWhiteSpace(attachVerb) ? "Put back" : attachVerb;
                BlockedBy = blockedBy ?? Array.Empty<string>();
                MountedOn = mountedOn ?? Array.Empty<string>();
            }

            public string Id { get; }
            public string DisplayName { get; }
            public DetachStyle Style { get; }
            public string DetachVerb { get; }
            public string AttachVerb { get; }
            public IReadOnlyList<string> BlockedBy { get; }
            public IReadOnlyList<string> MountedOn { get; }
            public PartState State { get; internal set; } = PartState.Secured;

            /// <summary>True while this part physically stops the parts it holds or covers from moving.</summary>
            public bool IsBlocking => Style == DetachStyle.ReleaseInPlace
                ? State == PartState.Secured
                : State != PartState.Removed;

            /// <summary>Taken off (or unplugged) as far as it goes.</summary>
            public bool IsDetached => Style == DetachStyle.ReleaseInPlace
                ? State != PartState.Secured
                : State == PartState.Removed;
        }
    }
}

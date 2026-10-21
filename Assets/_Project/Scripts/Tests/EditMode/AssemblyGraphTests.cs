using System.Collections.Generic;
using NUnit.Framework;
using PhoneRepair.Devices;
using UnityEditor;

namespace PhoneRepair.Tests
{
    /// <summary>The Mochi One's real assembly rules (from its PhoneModelDefinition asset).</summary>
    public class AssemblyGraphTests
    {
        private const string MochiOnePath = "Assets/_Project/Data/Phones/MochiOne/PHN_MochiOne_Model.asset";
        private static readonly string[] Screws = { "Screw_01", "Screw_02", "Screw_03", "Screw_04" };

        private static AssemblyGraph MochiOne()
        {
            var definition = AssetDatabase.LoadAssetAtPath<PhoneModelDefinition>(MochiOnePath);
            if (definition == null)
            {
                Assert.Ignore("Run Tools > Phone Repair > Run Setup first.");
            }

            return AssemblyGraph.FromDefinition(definition);
        }

        private static void RemoveScrewsAndCover(AssemblyGraph g)
        {
            foreach (string screw in Screws)
            {
                g.SetState(screw, PartState.Removed);
            }

            g.SetState("BackCover", PartState.Removed);
        }

        [Test]
        public void Rules_PointOnlyAtExistingSlots()
        {
            Assert.That(MochiOne().FindMissingReferences(), Is.Empty);
        }

        [Test]
        public void BackCover_IsHeldOnByItsFourScrews()
        {
            AssemblyGraph g = MochiOne();
            RuleCheck check = g.CanDetach("BackCover");
            Assert.That(check.Allowed, Is.False);
            Assert.That(check.Reason, Does.Contain("unscrew").And.Contain("×4"));

            for (int i = 0; i < Screws.Length; i++)
            {
                Assert.That(g.CanDetach(Screws[i]).Allowed, Is.True, Screws[i]);
                g.SetState(Screws[i], PartState.Removed);
                Assert.That(g.CanDetach("BackCover").Allowed, Is.EqualTo(i == Screws.Length - 1), $"after {i + 1} screws");
            }
        }

        [Test]
        public void HalfUnscrewedScrew_StillHoldsTheCover()
        {
            AssemblyGraph g = MochiOne();
            foreach (string screw in Screws)
            {
                g.SetState(screw, PartState.Removed);
            }

            g.SetState("Screw_03", PartState.Loose);
            Assert.That(g.CanDetach("BackCover").Allowed, Is.False);
        }

        [Test]
        public void Battery_NeedsTheCoverOffAndItsConnectorUnplugged()
        {
            AssemblyGraph g = MochiOne();
            Assert.That(g.CanDetach("Battery").Allowed, Is.False);
            RemoveScrewsAndCover(g);

            RuleCheck check = g.CanDetach("Battery");
            Assert.That(check.Allowed, Is.False);
            Assert.That(check.Reason, Does.Contain("unplug").And.Contain("Battery Connector"));

            Assert.That(g.CanDetach("BatteryConnector").Allowed, Is.True);
            g.SetState("BatteryConnector", PartState.Loose);
            Assert.That(g.CanDetach("Battery").Allowed, Is.True);
        }

        [Test]
        public void Frame_NeverComesOff()
        {
            AssemblyGraph g = MochiOne();
            Assert.That(g.CanDetach("Frame").Allowed, Is.False);
        }

        [Test]
        public void Screws_CanOnlyBeTightenedIntoAnAttachedCover()
        {
            AssemblyGraph g = MochiOne();
            RemoveScrewsAndCover(g);

            RuleCheck check = g.CanAttach("Screw_01");
            Assert.That(check.Allowed, Is.False);
            Assert.That(check.Reason, Does.Contain("snap on"));

            Assert.That(g.CanAttach("BackCover").Allowed, Is.True);
            g.SetState("BackCover", PartState.Secured);
            Assert.That(g.CanAttach("Screw_01").Allowed, Is.True);
            g.SetState("Screw_01", PartState.Loose);
            Assert.That(g.CanAttach("Screw_01").Allowed, Is.True, "a loose screw can be tightened");
        }

        [Test]
        public void Connector_CannotBePluggedIntoAMissingBattery()
        {
            AssemblyGraph g = MochiOne();
            RemoveScrewsAndCover(g);
            g.SetState("BatteryConnector", PartState.Loose);
            g.SetState("Battery", PartState.Removed);

            RuleCheck check = g.CanAttach("BatteryConnector");
            Assert.That(check.Allowed, Is.False);
            Assert.That(check.Reason, Does.Contain("Battery Pack"));
        }

        [Test]
        public void CoverCanCloseOverAMissingBattery_ButThenBlocksIt()
        {
            AssemblyGraph g = MochiOne();
            RemoveScrewsAndCover(g);
            g.SetState("BatteryConnector", PartState.Loose);
            g.SetState("Battery", PartState.Removed);

            // Physically fine (nothing blocks the cover); the power-on test in M3 will complain instead.
            Assert.That(g.CanAttach("BackCover").Allowed, Is.True);
            g.SetState("BackCover", PartState.Secured);

            RuleCheck check = g.CanAttach("Battery");
            Assert.That(check.Allowed, Is.False, "the cover is in the way now");
            Assert.That(check.Reason, Does.Contain("pry off"));
        }

        [Test]
        public void PartsSittingOnAPart_AlsoKeepItFromComingOut()
        {
            // "A is mounted on B" must come with "B is blocked by A", or B could be pulled out from
            // under A and the reverse order would no longer rebuild the phone.
            AssemblyGraph g = MochiOne();
            foreach (AssemblyGraph.Node node in g.Nodes)
            {
                foreach (string baseId in node.MountedOn)
                {
                    Assert.That(g.TryGet(baseId, out AssemblyGraph.Node basePart), Is.True, baseId);
                    Assert.That(basePart.BlockedBy, Does.Contain(node.Id), $"{node.Id} sits on {baseId}");
                }
            }
        }

        [Test]
        public void EveryPart_ComesOff_AndTheReverseOrderRebuildsThePhone()
        {
            AssemblyGraph g = MochiOne();
            var order = new List<AssemblyGraph.Node>();

            for (bool progress = true; progress;)
            {
                progress = false;
                foreach (AssemblyGraph.Node node in g.Nodes)
                {
                    if (node.Style == DetachStyle.Fixed || node.IsDetached || !g.CanDetach(node.Id).Allowed)
                    {
                        continue;
                    }

                    g.SetState(node.Id, node.Style == DetachStyle.ReleaseInPlace ? PartState.Loose : PartState.Removed);
                    order.Add(node);
                    progress = true;
                }
            }

            foreach (AssemblyGraph.Node node in g.Nodes)
            {
                Assert.That(node.Style == DetachStyle.Fixed || node.IsDetached, Is.True, $"{node.Id} could never be taken off");
            }

            for (int i = order.Count - 1; i >= 0; i--)
            {
                RuleCheck check = g.CanAttach(order[i].Id);
                Assert.That(check.Allowed, Is.True, $"{order[i].Id}: {check.Reason}");
                g.SetState(order[i].Id, PartState.Secured);
            }

            foreach (AssemblyGraph.Node node in g.Nodes)
            {
                Assert.That(node.State, Is.EqualTo(PartState.Secured), node.Id);
            }
        }
    }
}

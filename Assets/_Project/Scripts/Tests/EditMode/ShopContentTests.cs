using System.Linq;
using NUnit.Framework;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.Repair;
using PhoneRepair.Shop;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.Tests
{
    /// <summary>Guard rails for content: customers, the calendar, the tools and the 2D phone fit together.</summary>
    public class ShopContentTests
    {
        private const string CalendarPath = "Assets/_Project/Data/Shop/CAL_Default.asset";
        private static readonly string[] InteractiveSlots =
            { "Screen", "Battery", "BatteryConnector", "DisplayConnector", "BackCover", "Screw_01", "Screw_02", "Screw_03", "Screw_04" };

        private static ShopCalendar LoadCalendar()
        {
            var calendar = AssetDatabase.LoadAssetAtPath<ShopCalendar>(CalendarPath);
            if (calendar == null)
            {
                Assert.Ignore("Run Tools > Phone Repair > Run Setup first.");
            }

            return calendar;
        }

        private static CustomerDefinition[] AllCustomers() =>
            AssetDatabase.FindAssets("t:CustomerDefinition")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CustomerDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();

        [Test]
        public void EveryDay_HasCustomers()
        {
            ShopCalendar calendar = LoadCalendar();
            Assert.That(calendar.DayCount, Is.GreaterThan(0));
            for (int day = 1; day <= calendar.DayCount; day++)
            {
                Assert.That(calendar.CustomersFor(day), Is.Not.Empty, $"day {day}");
                Assert.That(calendar.CustomersFor(day), Has.None.Null, $"day {day}");
            }
        }

        [Test]
        public void EveryCustomer_IsComplete()
        {
            LoadCalendar();
            CustomerDefinition[] customers = AllCustomers();
            Assert.That(customers, Is.Not.Empty);
            foreach (CustomerDefinition c in customers)
            {
                string who = c.name;
                Assert.That(c.Idle.Length, Is.GreaterThanOrEqualTo(2), $"{who}: idle frames");
                Assert.That(c.Talk.Length, Is.GreaterThanOrEqualTo(2), $"{who}: talk frames");
                Assert.That(c.Idle.Concat(c.Talk), Has.None.Null, who);
                Assert.That(c.Happy, Is.Not.Null, who);
                Assert.That(c.Sad, Is.Not.Null, who);
                Assert.That(new[] { c.FaceNeutral, c.FaceHappy, c.FaceWorried }, Has.None.Null, $"{who}: faces");
                Assert.That(c.Payment, Is.GreaterThan(0), who);
                Assert.That(new[] { c.Greeting, c.Problem, c.Accepted, c.Declined, c.FixedLine, c.StillBrokenLine }, Has.None.Empty, $"{who}: lines");
                Assert.That(c.Phone, Is.Not.Null, $"{who}: phone");
                Assert.That(c.Phone.Prefab, Is.Not.Null, $"{who}: phone has no 2D prefab");

                PhonePart broken = c.Phone.Prefab.GetComponentsInChildren<PhonePart>(true).FirstOrDefault(p => p.SlotId == c.BrokenSlot);
                Assert.That(broken, Is.Not.Null, $"{who}: no part '{c.BrokenSlot}' to break");
                var view = broken.GetComponent<PartView2D>();
                Assert.That(view, Is.Not.Null, who);
                var data = new SerializedObject(view);
                Assert.That(data.FindProperty("broken").objectReferenceValue, Is.Not.Null, $"{who}: '{c.BrokenSlot}' has no broken look");
            }
        }

        [Test]
        public void PhonePrefab_HasEveryPartTheJobsNeed()
        {
            CustomerDefinition first = AllCustomers().FirstOrDefault();
            if (first == null || first.Phone == null || first.Phone.Prefab == null)
            {
                Assert.Ignore("Run Tools > Phone Repair > Run Setup first.");
            }

            GameObject prefab = first.Phone.Prefab;
            Assert.That(prefab.GetComponent<PhoneDevice>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Phone2D>(), Is.Not.Null);
            foreach (string slot in InteractiveSlots)
            {
                PhonePart part = prefab.GetComponentsInChildren<PhonePart>(true).FirstOrDefault(p => p.SlotId == slot);
                Assert.That(part, Is.Not.Null, slot);
                Assert.That(part.Definition, Is.Not.Null, slot);
                Assert.That(part.GetComponent<Interactable>(), Is.Not.Null, slot);
                Assert.That(part.GetComponent<Collider2D>(), Is.Not.Null, $"{slot}: can't be clicked");
                Assert.That(part.GetComponent<PartView2D>(), Is.Not.Null, slot);
            }

            PhonePart connector = prefab.GetComponentsInChildren<PhonePart>(true).First(p => p.SlotId == "BatteryConnector");
            Assert.That(connector.transform.parent.name, Is.EqualTo("Battery"), "the connector rides on the battery");
        }

        [Test]
        public void Tools_HaveIcons_AndCursors()
        {
            ToolDefinition[] tools = AssetDatabase.FindAssets("t:ToolDefinition")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ToolDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();
            Assert.That(tools.Select(t => t.Kind), Is.SupersetOf(new[] { ToolKind.Hands, ToolKind.Screwdriver, ToolKind.Spudger, ToolKind.SuctionCup }));
            foreach (ToolDefinition tool in tools)
            {
                Assert.That(tool.Icon, Is.Not.Null, tool.name);
                if (tool.Kind != ToolKind.Hands)
                {
                    Assert.That(tool.Cursor, Is.Not.Null, tool.name);
                }
            }
        }
    }
}

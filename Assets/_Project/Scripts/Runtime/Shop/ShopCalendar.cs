using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>Who walks in on which day. After the last listed day the days repeat.</summary>
    [CreateAssetMenu(fileName = "CAL_New", menuName = "Phone Repair/Shop Calendar", order = 31)]
    public sealed class ShopCalendar : ScriptableObject
    {
        [Serializable]
        public struct Day
        {
            public List<CustomerDefinition> customers;
        }

        [SerializeField] private List<Day> days = new();

        public int DayCount => days.Count;

        /// <summary>Customers for day 1, 2, 3... (wraps around the listed days).</summary>
        public IReadOnlyList<CustomerDefinition> CustomersFor(int dayNumber)
        {
            if (days.Count == 0)
            {
                return Array.Empty<CustomerDefinition>();
            }

            int index = (Mathf.Max(1, dayNumber) - 1) % days.Count;
            return (IReadOnlyList<CustomerDefinition>)days[index].customers ?? Array.Empty<CustomerDefinition>();
        }
    }
}

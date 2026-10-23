using System;
using System.Globalization;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>The shop's money, in yen. Milestone 5 saves it and spends it on spares and upgrades.</summary>
    [DisallowMultipleComponent]
    public sealed class Wallet : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingYen;

        public int Yen { get; private set; }

        public event Action<int> Changed;

        public void Add(int yen)
        {
            if (yen == 0)
            {
                return;
            }

            Yen = Mathf.Max(0, Yen + yen);
            Changed?.Invoke(Yen);
        }

        public static string Format(int yen) => "¥" + yen.ToString("N0", CultureInfo.InvariantCulture);

        private void Awake() => Yen = startingYen;
    }
}

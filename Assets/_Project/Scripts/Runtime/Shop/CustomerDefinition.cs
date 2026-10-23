using System;
using PhoneRepair.Devices;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>
    /// One customer: how they look and sound, the phone they bring, what's wrong with it, what they
    /// pay, and what they say. Sprites come from the art contract (Art/Pixel/Customers/...).
    /// Adding a customer = one of these assets plus their sprites; no code.
    /// </summary>
    [CreateAssetMenu(fileName = "CUS_New", menuName = "Phone Repair/Customer", order = 30)]
    public sealed class CustomerDefinition : ScriptableObject
    {
        [Tooltip("Stable id for save data. Also the sprite prefix: cust_<id>_idle_0.png ...")]
        [SerializeField] private string customerId;
        [SerializeField] private string displayName;
        [Tooltip("Pitch of the talk blips (1 = middle).")]
        [SerializeField, Range(0.5f, 2f)] private float voicePitch = 1f;

        [Header("Sprites (96×160, pivot at the feet)")]
        [SerializeField] private Sprite[] idle = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] talk = Array.Empty<Sprite>();
        [SerializeField] private Sprite happy;
        [SerializeField] private Sprite sad;

        [Header("Faces for the bench (48×48)")]
        [SerializeField] private Sprite faceNeutral;
        [SerializeField] private Sprite faceHappy;
        [SerializeField] private Sprite faceWorried;

        [Header("Job")]
        [SerializeField] private PhoneModelDefinition phone;
        [Tooltip("Slot id of the broken part, e.g. Screen or Battery.")]
        [SerializeField] private string brokenSlot = "Screen";
        [Tooltip("Short problem for the ticket, e.g. \"Cracked screen\".")]
        [SerializeField] private string problemLabel = "Cracked screen";
        [SerializeField, Min(0)] private int payment = 3000;
        [Tooltip("Something goofy they leave on top (\"a mikan\"). Empty = no tip.")]
        [SerializeField] private string tip;

        [Header("Lines")]
        [SerializeField, TextArea(2, 4)] private string greeting = "Hello!";
        [SerializeField, TextArea(2, 4)] private string problem = "My phone is broken.";
        [SerializeField, TextArea(2, 4)] private string accepted = "Thank you!";
        [SerializeField, TextArea(2, 4)] private string declined = "Oh... okay.";
        [SerializeField, TextArea(2, 4)] private string fixedLine = "It works!";
        [SerializeField, TextArea(2, 4)] private string stillBrokenLine = "It's still broken...";

        public string CustomerId => string.IsNullOrWhiteSpace(customerId) ? name : customerId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public float VoicePitch => voicePitch;
        public Sprite[] Idle => idle;
        public Sprite[] Talk => talk;
        public Sprite Happy => happy;
        public Sprite Sad => sad;
        public Sprite FaceNeutral => faceNeutral;
        public Sprite FaceHappy => faceHappy;
        public Sprite FaceWorried => faceWorried;
        public PhoneModelDefinition Phone => phone;
        public string BrokenSlot => brokenSlot ?? string.Empty;
        public string ProblemLabel => problemLabel ?? string.Empty;
        public int Payment => payment;
        public string Tip => tip ?? string.Empty;
        public string Greeting => greeting ?? string.Empty;
        public string Problem => problem ?? string.Empty;
        public string Accepted => accepted ?? string.Empty;
        public string Declined => declined ?? string.Empty;
        public string FixedLine => fixedLine ?? string.Empty;
        public string StillBrokenLine => stillBrokenLine ?? string.Empty;
    }
}

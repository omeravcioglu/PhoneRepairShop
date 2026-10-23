using System;
using System.Collections.Generic;
using PhoneRepair.Devices;
using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// Small tween engine for parts on the bench: arcing flights between the phone, the mat and the
    /// parts box, the "nope" wiggle and the landing hop. Everything lands on whole pixels.
    /// Flights run in the destination's local space, so a part flying home lands right even if the
    /// phone moved meanwhile.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(20)]
    public sealed class PartFlights2D : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float wiggleSeconds = 0.3f;
        [SerializeField, Min(0.05f)] private float hopSeconds = 0.14f;

        private sealed class Flight
        {
            public PhonePart Part;
            public Transform Target;
            public Vector3 From;
            public Vector3 To;
            public float Arc;
            public float Duration;
            public float Elapsed;
            public Action OnArrived;
        }

        private sealed class Shake
        {
            public Transform Target;
            public Vector3 Rest;
            public float Duration;
            public float Elapsed;
            public bool Hop;
        }

        private readonly List<Flight> flights = new();
        private readonly List<Shake> shakes = new();
        private readonly List<Flight> landed = new();

        public bool IsFlying(Transform target) => flights.Exists(f => f.Target == target);

        public bool AnyFlying => flights.Count > 0;

        /// <summary>
        /// Reparents the part to <paramref name="destination"/> (keeping where it is on screen) and flies
        /// it to <paramref name="localTarget"/> in a hop of <paramref name="arcHeight"/> pixels. The part is
        /// busy until it lands; the callback runs after landing.
        /// </summary>
        public void Fly(PhonePart part, Transform destination, Vector3 localTarget, float arcHeight, float seconds, Action onArrived)
        {
            Transform target = part.transform;
            StopAll(target);
            part.IsBusy = true;
            target.SetParent(destination, true);
            flights.Add(new Flight
            {
                Part = part,
                Target = target,
                From = target.localPosition,
                To = localTarget,
                Arc = arcHeight,
                Duration = Mathf.Max(0.05f, seconds),
                OnArrived = onArrived,
            });
        }

        /// <summary>Side-to-side one-pixel shake: "that doesn't come off yet".</summary>
        public void Wiggle(Transform target)
        {
            if (target == null || IsFlying(target) || shakes.Exists(s => s.Target == target))
            {
                return;
            }

            shakes.Add(new Shake { Target = target, Rest = target.localPosition, Duration = wiggleSeconds });
        }

        /// <summary>Little two-pixel hop (landing, seating, petting the cat).</summary>
        public void Hop(Transform target)
        {
            if (target == null || IsFlying(target) || shakes.Exists(s => s.Target == target))
            {
                return;
            }

            shakes.Add(new Shake { Target = target, Rest = target.localPosition, Duration = hopSeconds, Hop = true });
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = flights.Count - 1; i >= 0; i--)
            {
                Flight f = flights[i];
                if (f.Target == null)
                {
                    flights.RemoveAt(i);
                    continue;
                }

                f.Elapsed += dt;
                float u = Mathf.Clamp01(f.Elapsed / f.Duration);
                float e = Ease.InOutCubic(u);
                Vector3 local = Vector3.LerpUnclamped(f.From, f.To, e);
                Transform parent = f.Target.parent;
                Vector3 world = parent != null ? parent.TransformPoint(local) : local;
                world.y += Ease.Arc(u) * f.Arc;
                f.Target.position = PixelGrid.Snap(world);
                if (u >= 1f)
                {
                    f.Target.localPosition = f.To;
                    flights.RemoveAt(i);
                    landed.Add(f);
                }
            }

            for (int i = shakes.Count - 1; i >= 0; i--)
            {
                Shake s = shakes[i];
                if (s.Target == null)
                {
                    shakes.RemoveAt(i);
                    continue;
                }

                s.Elapsed += dt;
                if (s.Elapsed >= s.Duration)
                {
                    s.Target.localPosition = s.Rest;
                    shakes.RemoveAt(i);
                    continue;
                }

                Vector3 offset = s.Hop
                    ? new Vector3(0f, Mathf.Round(Ease.Arc(s.Elapsed / s.Duration) * 2f), 0f)
                    : new Vector3(Mathf.FloorToInt(s.Elapsed * 30f) % 2 == 0 ? 1f : -1f, 0f, 0f);
                s.Target.localPosition = s.Rest + offset;
            }

            // callbacks last: they may start new flights
            foreach (Flight f in landed)
            {
                if (f.Part != null)
                {
                    f.Part.IsBusy = false;
                }

                f.OnArrived?.Invoke();
            }

            landed.Clear();
        }

        private void StopAll(Transform target)
        {
            flights.RemoveAll(f => f.Target == target);
            for (int i = shakes.Count - 1; i >= 0; i--)
            {
                if (shakes[i].Target == target)
                {
                    target.localPosition = shakes[i].Rest;
                    shakes.RemoveAt(i);
                }
            }
        }
    }
}

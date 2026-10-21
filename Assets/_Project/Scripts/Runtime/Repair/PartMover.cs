using System;
using System.Collections.Generic;
using PhoneRepair.Devices;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// Small tween engine for parts: arcing flights between the phone and the mat/tray, hinge
    /// flips, "nope" wiggles and landing squashes. Flights run in the destination's local space,
    /// so a part flying back into the phone still lands right if the phone is turned meanwhile.
    /// </summary>
    [DefaultExecutionOrder(20)]
    [DisallowMultipleComponent]
    public sealed class PartMover : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float landingSquash = 0.08f;
        [SerializeField, Min(0.01f)] private float squashSeconds = 0.18f;
        [SerializeField, Min(0.01f)] private float wiggleSeconds = 0.35f;
        [SerializeField, Range(0f, 30f)] private float wiggleDegrees = 7f;

        private readonly List<Flight> flights = new();
        private readonly List<RotationTween> rotations = new();
        private readonly List<WiggleTween> wiggles = new();
        private readonly List<SquashTween> squashes = new();
        private readonly List<Flight> landed = new();

        private sealed class Flight
        {
            public PhonePart Part;
            public Transform Target;
            public Vector3 P0, C1, C2, P3;
            public Quaternion R0, R1;
            public int SpinTurns;
            public float Duration, Elapsed;
            public Action OnArrived;
        }

        private sealed class RotationTween
        {
            public Transform Target;
            public Quaternion From, To;
            public float Duration, Elapsed;
        }

        private sealed class WiggleTween
        {
            public Transform Target;
            public Quaternion Base;
            public float Elapsed;
        }

        private sealed class SquashTween
        {
            public Transform Target;
            public float Amount, Elapsed;
        }

        public bool IsFlying(Transform target) => flights.Exists(f => f.Target == target);

        /// <summary>
        /// Reparents the part to <paramref name="destination"/> and flies it to <paramref name="localTarget"/>:
        /// it leaves along <paramref name="exitDirectionWorld"/> and arrives coming from
        /// <paramref name="arriveFromDirectionWorld"/>. The part is busy until it lands.
        /// </summary>
        public void Fly(PhonePart part, Transform destination, Pose localTarget, Vector3 exitDirectionWorld,
            Vector3 arriveFromDirectionWorld, float arcHeight, int spinTurns, float duration, Action onArrived)
        {
            Transform target = part.transform;
            StopAll(target);
            part.IsBusy = true;
            target.SetParent(destination, true);

            var flight = new Flight
            {
                Part = part,
                Target = target,
                P0 = target.localPosition,
                R0 = target.localRotation,
                P3 = localTarget.position,
                R1 = localTarget.rotation,
                SpinTurns = spinTurns,
                Duration = Mathf.Max(0.05f, duration),
                OnArrived = onArrived,
            };

            float height = Mathf.Max(arcHeight, Vector3.Distance(flight.P0, flight.P3) * 0.3f);
            Vector3 exit = SafeDirection(destination.InverseTransformDirection(exitDirectionWorld));
            Vector3 arrive = SafeDirection(destination.InverseTransformDirection(arriveFromDirectionWorld));
            flight.C1 = flight.P0 + exit * height;
            flight.C2 = flight.P3 + arrive * (height * 0.6f);
            flights.Add(flight);
        }

        /// <summary>Eases a local rotation (hinge flips of connectors).</summary>
        public void RotateLocal(Transform target, Quaternion localRotation, float duration)
        {
            rotations.RemoveAll(r => r.Target == target);
            rotations.Add(new RotationTween
            {
                Target = target,
                From = target.localRotation,
                To = localRotation,
                Duration = Mathf.Max(0.01f, duration),
            });
        }

        /// <summary>Quick side-to-side shake: "that doesn't come off yet".</summary>
        public void Wiggle(Transform target)
        {
            if (target == null || IsFlying(target) || rotations.Exists(r => r.Target == target))
            {
                return;
            }

            WiggleTween existing = wiggles.Find(w => w.Target == target);
            if (existing != null)
            {
                existing.Elapsed = 0f;
                return;
            }

            wiggles.Add(new WiggleTween { Target = target, Base = target.localRotation });
        }

        /// <summary>Little squash-and-stretch pop (landing, tightening).</summary>
        public void Squash(Transform target, float amount = -1f)
        {
            squashes.RemoveAll(s => s.Target == target);
            squashes.Add(new SquashTween { Target = target, Amount = amount < 0f ? landingSquash : amount });
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int i = flights.Count - 1; i >= 0; i--)
            {
                Flight f = flights[i];
                if (f.Target == null)
                {
                    flights.RemoveAt(i);
                    continue;
                }

                f.Elapsed += deltaTime;
                float u = Mathf.Clamp01(f.Elapsed / f.Duration);
                float e = EaseInOutCubic(u);
                f.Target.localPosition = Bezier(f.P0, f.C1, f.C2, f.P3, e);
                Quaternion rotation = Quaternion.Slerp(f.R0, f.R1, e);
                if (f.SpinTurns != 0)
                {
                    rotation *= Quaternion.AngleAxis(360f * f.SpinTurns * e, Vector3.up);
                }

                f.Target.localRotation = rotation;
                if (u >= 1f)
                {
                    f.Target.SetLocalPositionAndRotation(f.P3, f.R1);
                    flights.RemoveAt(i);
                    landed.Add(f);
                }
            }

            for (int i = rotations.Count - 1; i >= 0; i--)
            {
                RotationTween r = rotations[i];
                if (r.Target == null)
                {
                    rotations.RemoveAt(i);
                    continue;
                }

                r.Elapsed += deltaTime;
                float u = Mathf.Clamp01(r.Elapsed / r.Duration);
                r.Target.localRotation = Quaternion.Slerp(r.From, r.To, 1f - (1f - u) * (1f - u) * (1f - u));
                if (u >= 1f)
                {
                    rotations.RemoveAt(i);
                }
            }

            for (int i = wiggles.Count - 1; i >= 0; i--)
            {
                WiggleTween w = wiggles[i];
                if (w.Target == null)
                {
                    wiggles.RemoveAt(i);
                    continue;
                }

                w.Elapsed += deltaTime;
                float u = Mathf.Clamp01(w.Elapsed / wiggleSeconds);
                float angle = Mathf.Sin(w.Elapsed * Mathf.PI * 2f * 9f) * wiggleDegrees * (1f - u);
                w.Target.localRotation = w.Base * Quaternion.AngleAxis(angle, Vector3.forward);
                if (u >= 1f)
                {
                    w.Target.localRotation = w.Base;
                    wiggles.RemoveAt(i);
                }
            }

            for (int i = squashes.Count - 1; i >= 0; i--)
            {
                SquashTween s = squashes[i];
                if (s.Target == null)
                {
                    squashes.RemoveAt(i);
                    continue;
                }

                s.Elapsed += deltaTime;
                float u = Mathf.Clamp01(s.Elapsed / squashSeconds);
                float k = Mathf.Sin(u * Mathf.PI) * s.Amount;
                s.Target.localScale = new Vector3(1f + k, 1f - k, 1f + k);
                if (u >= 1f)
                {
                    s.Target.localScale = Vector3.one;
                    squashes.RemoveAt(i);
                }
            }

            // Callbacks last: they may start new flights.
            foreach (Flight f in landed)
            {
                if (f.Part != null)
                {
                    f.Part.IsBusy = false;
                }

                Squash(f.Target);
                f.OnArrived?.Invoke();
            }

            landed.Clear();
        }

        private void StopAll(Transform target)
        {
            flights.RemoveAll(f => f.Target == target);
            rotations.RemoveAll(r => r.Target == target);
            for (int i = wiggles.Count - 1; i >= 0; i--)
            {
                if (wiggles[i].Target == target)
                {
                    target.localRotation = wiggles[i].Base;
                    wiggles.RemoveAt(i);
                }
            }

            if (squashes.RemoveAll(s => s.Target == target) > 0)
            {
                target.localScale = Vector3.one;
            }
        }

        private static Vector3 SafeDirection(Vector3 direction) =>
            direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.up;

        private static Vector3 Bezier(Vector3 p0, Vector3 c1, Vector3 c2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * p3;
        }

        private static float EaseInOutCubic(float t) =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }
}

using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// The screwdriver minigame's input model, kept free of scene objects so it can be unit tested.
    /// Holding the button turns the screw slowly; circling the cursor around the screw turns it
    /// faster (about one turn per circle): counter-clockwise loosens, clockwise tightens, the way a
    /// real screwdriver goes ("lefty-loosey"). Turning the wrong way simply doesn't help.
    /// </summary>
    public sealed class ScrewTurnTracker
    {
        private readonly float holdTurnsPerSecond;
        private readonly float turnsPerCircle;
        private readonly float minRadiusPixels;
        private float lastAngle;
        private bool hasAngle;

        public ScrewTurnTracker(float holdTurnsPerSecond = 0.6f, float turnsPerCircle = 1f, float minRadiusPixels = 10f)
        {
            this.holdTurnsPerSecond = Mathf.Max(0f, holdTurnsPerSecond);
            this.turnsPerCircle = Mathf.Max(0f, turnsPerCircle);
            this.minRadiusPixels = Mathf.Max(0f, minRadiusPixels);
        }

        public void Reset() => hasAngle = false;

        /// <summary>
        /// Turns made this frame in the wanted direction (never negative).
        /// </summary>
        /// <param name="pointer">Cursor in screen pixels (origin bottom-left).</param>
        /// <param name="center">Screw on screen.</param>
        /// <param name="deltaTime">Frame time.</param>
        /// <param name="direction">+1 to loosen (counter-clockwise), -1 to tighten (clockwise).</param>
        public float Step(Vector2 pointer, Vector2 center, float deltaTime, int direction)
        {
            float turns = holdTurnsPerSecond * Mathf.Max(0f, deltaTime);
            Vector2 offset = pointer - center;
            if (offset.sqrMagnitude < minRadiusPixels * minRadiusPixels)
            {
                hasAngle = false; // too close to the centre to read a direction reliably
                return turns;
            }

            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            if (hasAngle)
            {
                float delta = Mathf.DeltaAngle(lastAngle, angle); // + = counter-clockwise on screen
                float wanted = direction >= 0 ? delta : -delta;
                if (wanted > 0f)
                {
                    turns += wanted / 360f * turnsPerCircle;
                }
            }

            lastAngle = angle;
            hasAngle = true;
            return turns;
        }
    }
}

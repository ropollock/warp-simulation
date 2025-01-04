namespace WarpTesting.AnimatinoCurve;

using System;
using System.Collections.Generic;
using System.Linq;

public class AnimationCurve
{
    public class Keyframe
    {
        public float Time { get; set; }
        public float Value { get; set; }

        public Keyframe(float time, float value)
        {
            Time = time;
            Value = value;
        }
    }

    private List<Keyframe> keyframes;

    public AnimationCurve()
    {
        keyframes = new List<Keyframe>();
    }

    public AnimationCurve(IEnumerable<Keyframe> keyframes)
    {
        this.keyframes = keyframes.OrderBy(k => k.Time).ToList();
    }

    public void AddKeyframe(float time, float value)
    {
        var keyframe = new Keyframe(time, value);
        keyframes.Add(keyframe);
        keyframes = keyframes.OrderBy(k => k.Time).ToList();
    }

    public float Evaluate(float time)
    {
        if (keyframes.Count == 0)
        {
            throw new InvalidOperationException("The curve contains no keyframes.");
        }

        if (time <= keyframes.First().Time)
        {
            return keyframes.First().Value;
        }

        if (time >= keyframes.Last().Time)
        {
            return keyframes.Last().Value;
        }

        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            var kf1 = keyframes[i];
            var kf2 = keyframes[i + 1];

            if (time >= kf1.Time && time <= kf2.Time)
            {
                float t = (time - kf1.Time) / (kf2.Time - kf1.Time);
                return MathUtil.Lerp(kf1.Value, kf2.Value, t);
            }
        }

        return 0; // Should never reach here
    }

    public IEnumerable<Keyframe> GetKeyframes()
    {
        return keyframes;
    }
}
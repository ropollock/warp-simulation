namespace WarpTesting.DoublePrecisionCurve;

using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class DoublePrecisionCurve : ICloneable
{
    private List<Keyframe> keyframes;

    public DoublePrecisionCurve()
    {
        keyframes = new List<Keyframe>();
    }

    public DoublePrecisionCurve(IEnumerable<Keyframe> keyframes)
    {
        this.keyframes = keyframes.OrderBy(k => k.Time).ToList();
    }

    public void AddKeyframe(double time, double value, double inTangent = 0.0, double outTangent = 0.0)
    {
        var keyframe = new Keyframe(time, value, inTangent, outTangent);
        keyframes.Add(keyframe);
        keyframes = keyframes.OrderBy(k => k.Time).ToList();
    }

    public double Evaluate(double time)
    {
        if (keyframes.Count == 0)
        {
            return 0.0;
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
            var k1 = keyframes[i];
            var k2 = keyframes[i + 1];

            if (time >= k1.Time && time <= k2.Time)
            {
                double t1 = k1.Time;
                double t2 = k2.Time;
                double v1 = k1.Value;
                double v2 = k2.Value;
                double out1 = k1.OutTangent;
                double in2 = k2.InTangent;

                double localTime = (time - t1) / (t2 - t1);
                double timeSquared = localTime * localTime;
                double timeCubed = timeSquared * localTime;

                double h1 = 2.0 * timeCubed - 3.0 * timeSquared + 1.0;
                double h2 = -2.0 * timeCubed + 3.0 * timeSquared;
                double h3 = timeCubed - 2.0 * timeSquared + localTime;
                double h4 = timeCubed - timeSquared;

                double dt = t2 - t1;
                return h1 * v1 +
                       h2 * v2 +
                       h3 * out1 * dt +
                       h4 * in2 * dt;
            }
        }

        return 0.0; // Should never reach here
    }

    public object Clone()
    {
        return new DoublePrecisionCurve(keyframes.Select(k => k.Clone() as Keyframe));
    }

    [Serializable]
    public class Keyframe : ICloneable
    {
        public double Time { get; set; }
        public double Value { get; set; }
        public double InTangent { get; set; }
        public double OutTangent { get; set; }

        public Keyframe(double time, double value, double inTangent = 0.0, double outTangent = 0.0)
        {
            Time = time;
            Value = value;
            InTangent = inTangent;
            OutTangent = outTangent;
        }

        public object Clone()
        {
            return new Keyframe(Time, Value, InTangent, OutTangent);
        }
    }

    public IEnumerable<Keyframe> GetKeyframes()
    {
        return keyframes;
    }
}
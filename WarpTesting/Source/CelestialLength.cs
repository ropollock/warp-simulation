namespace WarpTesting;

[Serializable]
public struct CelestialLength
{
    public static double AU = 149597900.0;
    public static double LIGHTYEAR = 9460730000000.0;
        
    public enum ScaleType
    {
        Meter,
        Kilometer,
        AU,
        Lightyear
    }

    public double Value;
    public ScaleType Scale;

    public double SqrValue
    {
        get { return Value * Value; }
    }

    public CelestialLength(double newValue, ScaleType newScale)
    {
        Value = newValue;
        Scale = newScale;
    }

    public static implicit operator double(CelestialLength length)
    {
        switch (length.Scale)
        {
            case ScaleType.Meter: return length.Value / 1000.0f;
            case ScaleType.Kilometer: return length.Value;
            case ScaleType.AU: return length.Value * 149597900.0;
            case ScaleType.Lightyear: return length.Value * 9460730000000.0;
        }

        return default(double);
    }

    public static implicit operator CelestialLength(double length)
    {
        return new CelestialLength(length, ScaleType.Meter);
    }
}
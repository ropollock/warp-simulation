namespace WarpTesting;

public static class MathUtil
{
    public static double Divide(double a, double b)
    {
        return b != 0.0 ? a / b : 0.0;
    }

    public static float FDivide(float a, float b)
    {
        return b != 0.0f ? a / b : 0.0f;
    }
    
    public static float Lerp(float start, float end, float t)
    {
        return start + (end - start) * t;
    }

    public static double DoubleLerp(double start, double end, double t)
    {
        return start + (end - start) * t;
    }

    public static float LerpInAndOutWithRange(
        float progress,
        float rangeStart, // When effect starts (e.g. 0.2)
        float rangeEnd, // When effect ends (e.g. 0.8) 
        float fadeInPercent = 0.2f, // How much of the range is fade-in
        float fadeOutPercent = 0.8f) // How much of the range is fade-out
    {
        if (progress < rangeStart || progress > rangeEnd)
        {
            return 0.0f;
        }

        // Normalize progress to 0-1 within our range
        var normalizedProgress = (progress - rangeStart) / (rangeEnd - rangeStart);

        return LerpInAndOut(normalizedProgress, fadeInPercent, fadeOutPercent);
    }

    public static double LerpInAndOutWithRangePrecision(
        double progress,
        double rangeStart, // When effect starts (e.g. 0.2)
        double rangeEnd, // When effect ends (e.g. 0.8) 
        double fadeInPercent = 0.2, // How much of the range is fade-in
        double fadeOutPercent = 0.8) // How much of the range is fade-out
    {
        if (progress < rangeStart || progress > rangeEnd)
        {
            return 0.0;
        }

        // Normalize progress to 0-1 within our range
        var normalizedProgress = (progress - rangeStart) / (rangeEnd - rangeStart);

        return LerpInAndOutPrecision(normalizedProgress, fadeInPercent, fadeOutPercent);
    }

    public static double LerpInAndOutPrecision(double progress,
        double fadeInEndPercent = 0.1,
        double fadeOutStartPercent = 0.9)
    {
        if (fadeInEndPercent > fadeOutStartPercent)
        {
            // Use default safe values
            fadeInEndPercent = 0.1f;
            fadeOutStartPercent = 0.9f;
        }

        double val;
        if (progress <= fadeInEndPercent)
        {
            // Fade in - normalize progress to 0-1 range within fade in period
            val = progress / fadeInEndPercent;
        }
        else if (progress >= fadeOutStartPercent)
        {
            // Fade out - normalize progress to 1-0 range within fade out period
            val = (1.0f - progress) / (1.0f - fadeOutStartPercent);
        }
        else
        {
            // Full value between fade in and fade out
            val = 1.0f;
        }

        return val;
    }

    public static float LerpInAndOut(float progress,
        float fadeInEndPercent = 0.1f,
        float fadeOutStartPercent = 0.9f)
    {
        if (fadeInEndPercent > fadeOutStartPercent)
        {
            // Use default safe values
            fadeInEndPercent = 0.1f;
            fadeOutStartPercent = 0.9f;
        }

        float val;
        if (progress <= fadeInEndPercent)
        {
            // Fade in - normalize progress to 0-1 range within fade in period
            val = progress / fadeInEndPercent;
        }
        else if (progress >= fadeOutStartPercent)
        {
            // Fade out - normalize progress to 1-0 range within fade out period
            val = (1.0f - progress) / (1.0f - fadeOutStartPercent);
        }
        else
        {
            // Full value between fade in and fade out
            val = 1.0f;
        }

        return val;
    }
}
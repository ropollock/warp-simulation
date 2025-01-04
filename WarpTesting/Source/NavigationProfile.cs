namespace WarpTesting;
using Keyframe = WarpTesting.DoublePrecisionCurve;

[Serializable]
public class NavigationProfile
{
    public float maxVelocity;

    public bool warpEnabled = true;
    public float warpMaxVelocity;
    public float warpAcceleration;
    public float warpMinAccelScale;
    public float warpMinVelocityScale;
    public float warpReferenceDistance =  1f;
    public float warpTransitionReferenceDistance = 1f;
    public float warpTransitionVelocityMultiplier = 50f;
    public float warpTransitionMinSpeedScale = 0.1f;
    public float warpTransitionMinAccelScale = 0.1f;
    
    public float warpChargeModifier;
    public float baseWarpChargeTime;

    public NavigationProfile Clone()
    {
        return new NavigationProfile()
        {
            maxVelocity = maxVelocity,
            warpEnabled = warpEnabled,
            warpMaxVelocity = warpMaxVelocity,
            warpAcceleration = warpAcceleration,
            warpMinAccelScale = warpMinAccelScale,
            warpMinVelocityScale = warpMinVelocityScale,
            warpReferenceDistance = warpReferenceDistance,
            baseWarpChargeTime = baseWarpChargeTime,
            warpChargeModifier = warpChargeModifier,
            warpTransitionReferenceDistance = warpTransitionReferenceDistance,
            warpTransitionVelocityMultiplier = warpTransitionVelocityMultiplier,
            warpTransitionMinAccelScale = warpTransitionMinAccelScale,
            warpTransitionMinSpeedScale = warpTransitionMinSpeedScale
        };
    }
}
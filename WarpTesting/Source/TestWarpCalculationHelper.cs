namespace WarpTesting;

public static class TestWarpCalculationHelper
{
    // Define constants for unit conversions
    static double KM_PER_AU = CelestialLength.AU; // km/AU
    static double M_PER_KM = 1000.0; // m/km
    static double M_PER_AU = KM_PER_AU * M_PER_KM; // m/AU

    static double MIN_TRANSITION_KM = 150;
    static double MAX_TRANSITION_KM = 1000000.0;

    public static void TestWarpCalculation()
    {
        var testDistancesAU = new[]
        {
            0.000001f,
            0.00001f,
            0.0001f,
            0.001f,
            0.01f,
            0.05f,
            0.1f,
            0.5f,
            1.0f,
            5.0f,
            8.0f,
            10.0f,
            15.0f,
            20.0f,
            30.0f,
            50.0f,
            100.0f,
            150.0f,
            250.0f
        };

        // Core physics parameters
        var profile = new NavigationProfile
        {
            warpMaxVelocity = 3.0f, // AU/s - Max warp speed
            warpAcceleration = 0.5f, // AU/s² - Ship's warp acceleration
            maxVelocity = 3.5f, // km/s - Ship's normal max velocity
            warpReferenceDistance = 2.0f,
            warpMinAccelScale = 0.0001f,
            warpMinVelocityScale = 0.0001f,
            warpTransitionReferenceDistance = 1f,
            warpTransitionVelocityMultiplier = 50f,
            warpTransitionMinAccelScale = 0.01f,
            warpTransitionMinSpeedScale = 0.01f
        };

        Console.WriteLine("=== Warp Test Results ===");
        Console.WriteLine($"Ship Parameters:");
        Console.WriteLine($"  Normal Max Velocity: {profile.maxVelocity:N2} km/s");
        Console.WriteLine($"  Max Warp Velocity: {profile.warpMaxVelocity:N2} AU/s");
        Console.WriteLine($"  Warp Acceleration: {profile.warpAcceleration:N2} AU/s²");
        Console.WriteLine("");

        foreach (var distanceAU in testDistancesAU)
        {
            Console.WriteLine($"=== Testing Warp Distance: {distanceAU} AU ===");
            // Calculate local transitions based on main phase velocities
            var localPhase = CalculateLocalTransitions(
                distanceAU,
                profile);

            // Calculate main warp physics first
            var mainPhase =
                CalculateMainWarpPhase(distanceAU - (localPhase.totalTransitionDistM / 1000 / CelestialLength.AU),
                    profile, localPhase.mainPhaseEntryVelocityMS, localPhase.mainPhaseExitVelocityMS);
            CalculateLegacyWarpTime(profile, distanceAU);


            Console.WriteLine($"Total Warp Time: {mainPhase.totalTime + localPhase.totalTime:F1}s");

            // Calculate final phase points as ratios of total time
            var phases = CalculatePhasePoints(mainPhase, localPhase);
            
            // Log detailed results
            LogWarpTest(distanceAU, mainPhase, localPhase, phases);
            
            // Validate our invariants
            ValidateWarpPhases(phases, mainPhase, localPhase);
            Console.WriteLine("");
        }
    }

    private class MainWarpPhase
    {
        // All distances in meters, all velocities in m/s, all times in seconds
        public double maxVelocityMS; // Maximum achieved velocity (m/s)
        public double initialVelocityMS; // Velocity at start of acceleration (m/s)
        public double finalVelocityMS; // Velocity at end of deceleration (m/s)
        public double accelTime; // Time spent accelerating (s)
        public double cruiseTime; // Time at max velocity (s)
        public double decelTime; // Time spent decelerating (s)
        public double accelDistM; // Distance covered in acceleration (m)
        public double cruiseDistM; // Distance covered at max velocity (m)
        public double decelDistM; // Distance covered in deceleration (m)
        public double totalTime; // Total time for main phase (s)
    }

    private class LocalTransitionPhase
    {
        // All distances in meters, all velocities in m/s, all times in seconds
        public double departureTime; // Time for departure transition (s)
        public double arrivalTime; // Time for arrival transition (s)
        public double departureDistM; // Distance covered in departure (m)
        public double arrivalDistM; // Distance covered in arrival (m)
        public double totalTime; // Total time for local transitions (s)
        public double maxTransitionDistM; // Maximum allowed transition distance (m)
        public double totalTransitionDistM;
        public double mainPhaseEntryVelocityMS;
        public double mainPhaseExitVelocityMS;
    }

    private class PhasePoints
    {
        public double departureEnd; // End of local departure (ratio)
        public double accelEnd; // End of main acceleration (ratio)
        public double decelStart; // Start of main deceleration (ratio)
        public double decelEnd; // Start of local arrival (ratio)
    }

    // Warp in three phases, acceleration, cruise (linear velocity), deceleration.
    // This will handle velocity scaling for short distances.
    // Target a minimum of 20% of the travel time to be in the cruise phase.
    // To achieve this we scale the max warp velocity and warp acceleration down until our acceleration and deceleration phases fit
    // a 20% cruise phase in-between them.
    private static MainWarpPhase CalculateMainWarpPhase(
        double distanceAU,
        NavigationProfile profile,
        double entryVelocityMS = 0.0, // Velocity at start of main phase (m/s)
        double exitVelocityMS = 0.0) // Velocity at end of main phase (m/s)
    {
        var result = new MainWarpPhase();

        // Convert all units to kilometers for consistency first
        double entryVelocityKMS = entryVelocityMS / 1000;
        double exitVelocityKMS = exitVelocityMS / 1000;
        double normalSpeedKMS = profile.maxVelocity; // km/s
        double baseWarpSpeedKMS = profile.warpMaxVelocity * KM_PER_AU; // Full warp speed without scaling
        double distanceKM = distanceAU * KM_PER_AU;

        // Calculate base accelerations (unscaled)
        double baseAccelKMS2 = profile.warpAcceleration * KM_PER_AU;
        double baseDecelKMS2 = baseAccelKMS2 / 3.0; // Gentler deceleration

        // Calculate distances needed at max warp speed, accounting for entry/exit velocities
        // Using: d = (v² - u²)/(2a) where v is final velocity, u is initial velocity
        double baseAccelDistKM = (baseWarpSpeedKMS * baseWarpSpeedKMS - entryVelocityKMS * entryVelocityKMS) /
                                 (2 * baseAccelKMS2);
        double baseDecelDistKM = (baseWarpSpeedKMS * baseWarpSpeedKMS - exitVelocityKMS * exitVelocityKMS) /
                                 (2 * baseDecelKMS2);
        double minCruiseDistKM = distanceKM * 0.2; // Required 20% cruise distance
        double totalRequiredDistKM = baseAccelDistKM + baseDecelDistKM + minCruiseDistKM;

        // Store initial/final velocities
        result.initialVelocityMS = entryVelocityMS;
        result.finalVelocityMS = exitVelocityMS;

        if (distanceKM >= totalRequiredDistKM)
        {
            // We can reach full speed and maintain 20% cruise
            result.maxVelocityMS = baseWarpSpeedKMS * 1000;

            // Calculate acceleration time: v = u + at
            result.accelTime = (baseWarpSpeedKMS - entryVelocityKMS) / baseAccelKMS2;
            result.decelTime = (baseWarpSpeedKMS - exitVelocityKMS) / baseDecelKMS2;

            // Calculate distances
            result.accelDistM = (baseAccelDistKM) * 1000;
            result.decelDistM = (baseDecelDistKM) * 1000;
            result.cruiseDistM = (distanceKM - (baseAccelDistKM + baseDecelDistKM)) * 1000;

            result.cruiseTime = result.cruiseDistM / result.maxVelocityMS;
        }
        else
        {
            // Short warp - calculate and apply scaling
            double accelScale = Math.Max(profile.warpMinAccelScale,
                Math.Min(1.0, Math.Pow(distanceAU / profile.warpReferenceDistance, 0.5)));
            double velocityScale = Math.Max(profile.warpMinVelocityScale,
                Math.Min(1.0, Math.Pow(distanceAU / profile.warpReferenceDistance, 0.75)));

            // Apply scaling
            double maxWarpSpeedKMS = baseWarpSpeedKMS * velocityScale;
            double accelKMS2 = baseAccelKMS2 * accelScale;
            double decelKMS2 = baseDecelKMS2 * accelScale;

            // Calculate reduced speed to ensure 20% cruise phase
            double targetCruiseDistKM = distanceKM * 0.2;
            double remainingDistKM = distanceKM - targetCruiseDistKM;

            // Calculate speed that allows for acceleration, cruise, and deceleration
            double effectiveAccel = (accelKMS2 * decelKMS2) / (accelKMS2 + decelKMS2);
            double achievableSpeedKMS = Math.Sqrt(2 * effectiveAccel * remainingDistKM +
                                                  (entryVelocityKMS * entryVelocityKMS +
                                                   exitVelocityKMS * exitVelocityKMS) / 2);

            // Cap speed between 2x normal speed and scaled max warp speed
            result.maxVelocityMS = Math.Min(maxWarpSpeedKMS,
                Math.Max(normalSpeedKMS * 2, achievableSpeedKMS)) * 1000;

            // Calculate times
            result.accelTime = (result.maxVelocityMS / 1000 - entryVelocityKMS) / accelKMS2;
            result.decelTime = (result.maxVelocityMS / 1000 - exitVelocityKMS) / decelKMS2;

            // Calculate distances
            double maxSpeedKMS = result.maxVelocityMS / 1000;
            result.accelDistM = (maxSpeedKMS * maxSpeedKMS - entryVelocityKMS * entryVelocityKMS) / (2 * accelKMS2) *
                                1000;
            result.decelDistM = (maxSpeedKMS * maxSpeedKMS - exitVelocityKMS * exitVelocityKMS) / (2 * decelKMS2) *
                                1000;
            result.cruiseDistM = targetCruiseDistKM * 1000;

            result.cruiseTime = result.cruiseDistM / result.maxVelocityMS;
        }

        result.totalTime = result.accelTime + result.cruiseTime + result.decelTime;

        // Debug output
        Console.WriteLine($"Main Phase Calculation Details (Revised):");
        Console.WriteLine($"  Distance: {distanceAU} AU = {distanceKM:N1} km");
        Console.WriteLine($"  Acceleration Distance: {baseAccelDistKM:N1} km");
        Console.WriteLine($"  Deceleration Distance: {baseDecelDistKM:N1} km");
        Console.WriteLine($"  Required Cruise Distance: {minCruiseDistKM:N1} km");
        Console.WriteLine($"  Total Required Distance: {totalRequiredDistKM:N1} km");
        Console.WriteLine($"  Is Short Warp: {distanceKM < totalRequiredDistKM}");
        Console.WriteLine($"  Entry Velocity: {entryVelocityKMS:F2} km/s");
        Console.WriteLine($"  Exit Velocity: {exitVelocityKMS:F2} km/s");
        Console.WriteLine(
            $"  Max Speed: {result.maxVelocityMS / (KM_PER_AU * 1000):F5} AU/s, {result.maxVelocityMS / 1000:F2} km/s");
        Console.WriteLine($"  Cruise Ratio: {(result.cruiseDistM / 1000) / distanceKM:P1}");
        Console.WriteLine(
            $"  Phase Times: accel={result.accelTime:F2}s, cruise={result.cruiseTime:F2}s, decel={result.decelTime:F2}s");
        Console.WriteLine($"  Total Time: {result.totalTime:F2}s");
        Console.WriteLine(
            $"  Phase Distances: accel={result.accelDistM / M_PER_AU:F6} AU, cruise={result.cruiseDistM / M_PER_AU:F6} AU, decel={result.decelDistM / M_PER_AU:F6} AU");

        return result;
    }

    private static LocalTransitionPhase CalculateLocalTransitions(
        double distanceAU,
        NavigationProfile profile)
    {
        var result = new LocalTransitionPhase();

        // Convert distance to meters
        double distanceM = distanceAU * M_PER_AU;

        // Define transition thresholds in meters
        double minTransitionM = MIN_TRANSITION_KM * M_PER_KM;
        double maxTransitionM = MAX_TRANSITION_KM * M_PER_KM;

        // Only apply transitions if distance is large enough for both phases
        if (distanceM < minTransitionM * 2) // Need room for two transitions
        {
            return result; // Return zero values for all properties
        }

        // Calculate transition distance - scales with total distance up to max
        double transitionDist = Math.Min(distanceM * 0.05, maxTransitionM);
        transitionDist = Math.Max(transitionDist, minTransitionM);
        result.maxTransitionDistM = transitionDist;

        // Store total transition distance (both phases)
        result.totalTransitionDistM = transitionDist * 2;

        // Calculate base velocities in m/s
        double normalSpeedMS = profile.maxVelocity * M_PER_KM;

        // Calculate scaling factors based on distance
        // Use 1 AU as reference distance for local transitions
        double localTransitionReferenceAU = profile.warpTransitionReferenceDistance;
        double speedScale = Math.Max(profile.warpTransitionMinSpeedScale, Math.Min(1.0, Math.Sqrt(distanceAU / localTransitionReferenceAU)));
        double accelScale = Math.Max(profile.warpTransitionMinAccelScale, Math.Min(1.0, Math.Pow(distanceAU / localTransitionReferenceAU, 0.5)));

        // Scale entry/exit velocities with distance
        double velocityMultiplier = profile.warpTransitionVelocityMultiplier * speedScale;
        double mainPhaseEntrySpeed = normalSpeedMS * velocityMultiplier;
        double mainPhaseExitSpeed = mainPhaseEntrySpeed; // Keep symmetrical

        // Scale accelerations with distance
        double baseAccelMS2 = (profile.warpAcceleration * M_PER_AU) * 0.005 * accelScale;
        double departureAccelMS2 = baseAccelMS2;
        double arrivalDecelMS2 = baseAccelMS2 * 0.5; // Keep arrival gentler

        // Calculate departure transition
        double departureV2 = mainPhaseEntrySpeed * mainPhaseEntrySpeed;
        double departureU2 = normalSpeedMS * normalSpeedMS;
        result.departureTime = Math.Sqrt((departureV2 - departureU2) / (2 * departureAccelMS2));
        result.departureDistM = transitionDist;

        // Calculate arrival transition
        double arrivalV2 = mainPhaseExitSpeed * mainPhaseExitSpeed;
        double arrivalU2 = normalSpeedMS * normalSpeedMS;
        result.arrivalTime = Math.Sqrt((arrivalV2 - arrivalU2) / (2 * arrivalDecelMS2));
        result.arrivalDistM = transitionDist;

        result.totalTime = result.departureTime + result.arrivalTime;

        // Store entry/exit velocities for main phase
        result.mainPhaseEntryVelocityMS = mainPhaseEntrySpeed;
        result.mainPhaseExitVelocityMS = mainPhaseExitSpeed;

        // Debug output
        Console.WriteLine($"Local Transition Details:");
        Console.WriteLine($"  Min Transition: {minTransitionM / M_PER_KM:N1} km");
        Console.WriteLine($"  Max Transition: {maxTransitionM / M_PER_KM:N1} km");
        Console.WriteLine($"  Used Transition (each): {transitionDist / M_PER_KM:N1} km");
        Console.WriteLine($"  Total Transition Distance: {result.totalTransitionDistM / M_PER_KM:N1} km");
        Console.WriteLine($"  Distance Scale Factors:");
        Console.WriteLine($"    Speed Scale: {speedScale:F3}");
        Console.WriteLine($"    Acceleration Scale: {accelScale:F3}");
        Console.WriteLine($"  Velocities:");
        Console.WriteLine($"    Normal Speed: {normalSpeedMS / 1000:N2} km/s");
        Console.WriteLine($"    Entry Speed: {mainPhaseEntrySpeed / 1000:N2} km/s ({velocityMultiplier:F1}x normal)");
        Console.WriteLine($"    Exit Speed: {mainPhaseExitSpeed / 1000:N2} km/s");
        Console.WriteLine($"  Accelerations:");
        Console.WriteLine($"    Base: {baseAccelMS2:N2} m/s²");
        Console.WriteLine($"    Departure: {departureAccelMS2:N2} m/s²");
        Console.WriteLine($"    Arrival: {arrivalDecelMS2:N2} m/s²");
        Console.WriteLine($"  Times:");
        Console.WriteLine($"    Departure: {result.departureTime:F5}s");
        Console.WriteLine($"    Arrival: {result.arrivalTime:F5}s");
        Console.WriteLine($"    Total: {result.totalTime:F5}s");

        return result;
    }

    private static PhasePoints CalculatePhasePoints(MainWarpPhase main, LocalTransitionPhase local)
    {
        var points = new PhasePoints();

        double totalTime = local.totalTime + main.totalTime;
        if (totalTime <= 0)
        {
            throw new InvalidOperationException("Total time must be positive");
        }

        // Calculate phase points as ratios of total time
        points.departureEnd = local.departureTime / totalTime;
        points.accelEnd = (local.departureTime + main.accelTime) / totalTime;
        points.decelStart = (local.departureTime + main.accelTime + main.cruiseTime) / totalTime;
        points.decelEnd = (local.departureTime + main.totalTime) / totalTime;
        // Note: points.decelEnd + local.arrivalTime/totalTime should equal 1.0

        // Debug output
        Console.WriteLine($"Phase Points:");
        Console.WriteLine($"  Departure End: {points.departureEnd:F3}");
        Console.WriteLine($"  Acceleration End: {points.accelEnd:F3}");
        Console.WriteLine($"  Deceleration Start: {points.decelStart:F3}");
        Console.WriteLine($"  Deceleration End: {points.decelEnd:F3}");

        return points;
    }

    private static void ValidateWarpPhases(PhasePoints phases, MainWarpPhase main, LocalTransitionPhase local)
    {
        // Track any validation failures
        var validationFailures = new List<string>();

        // Positive times
        if (main.totalTime <= 0)
            validationFailures.Add("Main phase time must be positive");
        if (local.totalTime < 0) // Can be zero if no transitions
            validationFailures.Add("Local transition time cannot be negative");

        // Phase point ordering
        if (phases.departureEnd >= phases.accelEnd)
            validationFailures.Add("Departure must end before acceleration ends");
        if (phases.accelEnd >= phases.decelStart)
            validationFailures.Add("Acceleration must end before deceleration starts");
        if (phases.decelStart >= phases.decelEnd)
            validationFailures.Add("Deceleration must start before deceleration ends");

        // Phase points must be between 0 and 1
        if (phases.departureEnd < 0 || phases.departureEnd > 1)
            validationFailures.Add("Departure end ratio must be between 0 and 1");
        if (phases.accelEnd < 0 || phases.accelEnd > 1)
            validationFailures.Add("Acceleration end ratio must be between 0 and 1");
        if (phases.decelStart < 0 || phases.decelStart > 1)
            validationFailures.Add("Deceleration start ratio must be between 0 and 1");
        if (phases.decelEnd < 0 || phases.decelEnd > 1)
            validationFailures.Add("Deceleration end ratio must be between 0 and 1");

        // Velocity continuity
        double velocityMatchTolerance = 0.1; // 0.1 m/s tolerance
        if (Math.Abs(local.mainPhaseEntryVelocityMS - main.initialVelocityMS) > velocityMatchTolerance)
            validationFailures.Add(
                $"Velocity mismatch at departure transition: {local.mainPhaseEntryVelocityMS} != {main.initialVelocityMS}");
        if (Math.Abs(local.mainPhaseExitVelocityMS - main.finalVelocityMS) > velocityMatchTolerance)
            validationFailures.Add(
                $"Velocity mismatch at arrival transition: {local.mainPhaseExitVelocityMS} != {main.finalVelocityMS}");

        // Final arrival should complete at ratio 1.0
        double finalRatio = phases.decelEnd + (local.arrivalTime / (local.totalTime + main.totalTime));
        if (Math.Abs(finalRatio - 1.0) > 0.0001)
            validationFailures.Add($"Final phase ratio should be 1.0, got {finalRatio:F4}");

        // Output any validation failures
        if (validationFailures.Count > 0)
        {
            Console.WriteLine("Phase Validation Failures:");
            foreach (var failure in validationFailures)
            {
                Console.WriteLine($"  - {failure}");
            }
        }
        else
        {
            Console.WriteLine("All phase validations passed");
        }
    }

    private static void LogWarpTest(
        float distanceAU,
        MainWarpPhase main,
        LocalTransitionPhase local,
        PhasePoints phases)
    {
        Console.WriteLine($"Test Warp: {distanceAU} AU");
        Console.WriteLine("Main Phase:");
        Console.WriteLine($"  Max Velocity: {main.maxVelocityMS / M_PER_KM:N4} km/s");
        Console.WriteLine($"  Accel Time: {main.accelTime:N4}s");
        Console.WriteLine($"  Cruise Time: {main.cruiseTime:N4}s");
        Console.WriteLine($"  Decel Time: {main.decelTime:N4}s");
        Console.WriteLine($"  Total Time: {main.totalTime:N4}s");

        Console.WriteLine("Local Transitions:");
        Console.WriteLine($"  Departure Time: {local.departureTime:N4}s");
        Console.WriteLine($"  Arrival Time: {local.arrivalTime:N4}s");

        Console.WriteLine("Phase Points (as ratio of total time):");
        Console.WriteLine($"  Departure End: {phases.departureEnd:N3}");
        Console.WriteLine($"  Accel End: {phases.accelEnd:N3}");
        Console.WriteLine($"  Decel Start: {phases.decelStart:N3}");
        Console.WriteLine($"  Decel End: {phases.decelEnd:N3}");
        Console.WriteLine("");
    }

    public static double CalculateLegacyWarpTime(NavigationProfile profile, double warpDistance)
    {
        // Constants and derived values
        var kAccel = profile.warpAcceleration * CelestialLength.AU * 1000;
        var kDecel = kAccel / 3;

        var warpDropoutSpeedMS = Math.Min((profile.maxVelocity * 1000) / 2, 100f);
        var maxWarpSpeedMS = profile.warpMaxVelocity * CelestialLength.AU * 1000;

        // Corrected formulas for distances during acceleration and deceleration
        var accelDistance = (maxWarpSpeedMS * maxWarpSpeedMS) / (2 * kAccel);

        // Updated decelDistance using logarithmic model
        var decelTime = Math.Log(maxWarpSpeedMS / warpDropoutSpeedMS) / kDecel;
        var decelDistance = (maxWarpSpeedMS - warpDropoutSpeedMS) / kDecel;

        // Calculate the minimum distance required to reach max speed
        var minDistance = accelDistance + decelDistance;

        // Warp distance in meters
        var warpDistanceM = warpDistance * CelestialLength.AU * 1000;

        // Initialize cruise time
        var cruiseTime = 0.0;

        // Check if the warp distance allows for reaching max warp speed
        if (minDistance > warpDistanceM)
        {
            // Recalculate maxWarpSpeedMS if the distance is too short to reach full speed
            maxWarpSpeedMS = Math.Sqrt((warpDistanceM * 2 * kAccel * kDecel) / (kAccel + kDecel));
        }
        else
        {
            // Calculate cruise time for the remaining distance after acceleration and deceleration
            cruiseTime = (warpDistanceM - minDistance) / maxWarpSpeedMS;
        }

        // Calculate time for acceleration phase
        var accelTime = maxWarpSpeedMS / kAccel;

        // Total time is the sum of acceleration, cruise, and deceleration times
        var totalTime = accelTime + cruiseTime + decelTime;

        // Log phase times for debugging
        Console.WriteLine(
            $"Legacy Phases: accel {accelTime:F5}, cruise {cruiseTime:F5}, decel {decelTime:F5} = {totalTime:F5}");

        return totalTime;
    }
}
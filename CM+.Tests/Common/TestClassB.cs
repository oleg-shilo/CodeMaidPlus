class TestClassB
{
    // /// <summary>
    // /// The test1
    // /// </summary>
    // int test1;

    // /// <summary>
    // /// The test2
    // /// </summary>
    // int test2;

    protected static AppConfiguration CreateAppConfig(
            bool tracingEnabled = false,
            int timeAccelerationFactor = 1,
            string cameraModesDirectory = "")
    {
        var appSettings = new NameValueCollection
            {
                { "TracingEnabled", tracingEnabled.ToString() },
                { "TimeAccelerationFactor", timeAccelerationFactor.ToString() },
                { "CameraModesDirectory", cameraModesDirectory }
            };
        return new AppConfiguration(appSettings);
    }

    void Test11()
    {
        _instrumentAvailableMessageFactory = instrumentPoweredOn =>
            new InstrumentAvailable
            {
                CurrentInstrumentVersion = currentInstrumentVersion,
                InstrumentPoweredOn = instrumentPoweredOn
            };
    }

    void Test()
    {
        var linesWithCode = root.DescendantTokens()
                                .Where(y => y.HasLeadingTrivia &&
                                         y.LeadingTrivia
                                           .Select(x => x)
                                         .Any(y => y.IsKind(SyntaxKind.WhitespaceTrivia)));

        {
            int test = 1;
        }

        Console.WriteLine("",
                   1,
          2,
                   3,
             "a".ToCharArray().Length,
             "b");

        //         var str = @"
        //         System.Console.WriteLine("",
        //             1,

        //           2,

        //            3,
        //                ""a"".ToCharArray().Length,
        //             "b");
        // ";
    }

    var reagentsIdentified = new ReagentsIdentified
    {
        Lanes = racks.Select(
                   rack =>
                   {
                       Console.WriteLine(
                           ds
                           );
                       var reagentLaneScan = new ReagentLaneScan
                       {
                           LaneNumber = rack.LaneNumber,
                           TimeLoaded = rack.TimeLoaded,
                           Reagents = rack.Positions
                                       .Where(p => p.Container != null &&
                                                   p.State == HvReagentPositionState.IdKnown)
                                         .Select(reagent => new ReagentScan
                                         {
                                             Position = reagent.Position,
                                             Upi = reagent.Container.UPI
                                         }).ToList()
                       };
                       return reagentLaneScan;
                   }).ToList()
    }
}
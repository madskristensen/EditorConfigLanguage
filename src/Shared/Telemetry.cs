using Microsoft.VisualStudio.Telemetry;
using System;

namespace EditorConfig
{
    public static class Telemetry
    {
        private const string _namespace = "WebTools/EdiorConfig/";

        public static void TrackUserTask(string name, TelemetryResult result = TelemetryResult.Success)
        {
            string actualName = name.Replace(" ", "_");
            TelemetryService.DefaultSession.PostUserTask(_namespace + actualName, result);
        }

        public static void TrackOperation(string name, TelemetryResult result = TelemetryResult.Success)
        {
            string actualName = name.Replace(" ", "_");
            TelemetryService.DefaultSession.PostOperation(_namespace + actualName, result);
        }

        public static void TrackException(string name, Exception exception)
        {
            string actualName = name.Replace(" ", "_");
            TelemetryService.DefaultSession.PostFault(actualName, exception.Message, exception);
        }
    }
}

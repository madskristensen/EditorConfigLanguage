using Microsoft.VisualStudio.Telemetry;
using System;

namespace EditorConfig
{
    public static class Telemetry
    {
        private const string _namespace = "WebTools/EdiorConfig/";

        public static void TrackUserTask(string name, TelemetryResult result = TelemetryResult.Success)
        {
            TelemetryService.DefaultSession.PostUserTask(_namespace + name, result);
        }

        public static void TrackOperation(string name, TelemetryResult result = TelemetryResult.Success)
        {
            TelemetryService.DefaultSession.PostOperation(_namespace + name, result);
        }

        public static void TrackException(string name, Exception exception)
        {
            TelemetryService.DefaultSession.PostFault(name, exception.Message, exception);
        }

        private static TelemetryEvent CreateTelemetryEvent(string name)
        {
            return new TelemetryEvent(_namespace + name);
        }
    }
}

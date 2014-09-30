using CommandPattern;

namespace ArcGisServerPermissionsProxy.Api.Commands {

    public class ConvertToNetUtcCommand : Command<ConvertToNetUtcCommand.Container> {
        private readonly long _jsTicks;

        public class Container {
            public Container(long ticks)
            {
                Ticks = ticks;
            }

            public long Ticks { get; set; }
        }

        public ConvertToNetUtcCommand(long jsTicks)
        {
            _jsTicks = jsTicks;
        }

        public override void Execute()
        {
            Result = new Container((_jsTicks * 10000) + 621355968000000000);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}, JsTicks: {1}", "ConvertToNetUtcCommand", _jsTicks);
        }
    }

}
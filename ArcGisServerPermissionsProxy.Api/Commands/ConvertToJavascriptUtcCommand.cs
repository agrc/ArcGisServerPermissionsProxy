using System;
using CommandPattern;

namespace ArcGisServerPermissionsProxy.Api.Commands {

    /// <summary>
    /// http://chuchuva.com/pavel/2010/10/converting-net-datetime-to-javascript-date/
    /// </summary>
    public class ConvertToJavascriptUtcCommand : Command<ConvertToJavascriptUtcCommand.Container> {
        private readonly long _datetimeMinTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;
        private readonly DateTime _dt;

        public ConvertToJavascriptUtcCommand(DateTime dt)
        {
            _dt = dt;
        }

        public override void Execute()
        {
            var utc = ((_dt.ToUniversalTime().Ticks - _datetimeMinTimeTicks)/10000);

            Result = new Container(utc);
        }

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}, Date: {1}", "ConvertToJavascriptUtcCommand", _dt.ToShortDateString() + _dt.ToShortTimeString());
        }

        public class Container {
            /// <summary>
            ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
            /// </summary>
            public Container(long ticks)
            {
                Ticks = ticks;
            }

            public long Ticks { get; set; }
        }
    }

}
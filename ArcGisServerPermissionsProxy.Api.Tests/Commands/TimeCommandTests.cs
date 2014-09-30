using System;
using ArcGisServerPermissionsProxy.Api.Commands;
using CommandPattern;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests.Commands {

    [TestFixture]
    public class TimeCommandTests {
         [Test]
         public void CanConvertToJsDate()
         {
             const long jsTicks = 1391238000000; // 2/1/2014 0:00:00
             var feb2 = new DateTime(2014, 2, 1, 7, 0, 0, 0, DateTimeKind.Utc);
             var command = new ConvertToJavascriptUtcCommand(feb2);

             var actual = CommandExecutor.ExecuteCommand(command);

             Assert.That(actual.Ticks, Is.EqualTo(jsTicks));
         }

        [Test]
        public void CanGetDateFromJsTicks()
        {
            const long jsTicks = 1391238000000; // 2/1/2014 0:00:00

            var command = new ConvertToNetUtcCommand(jsTicks);
            var actual = CommandExecutor.ExecuteCommand(command);
            var feb2 = new DateTime(2014, 2, 1, 7, 0, 0, 0, DateTimeKind.Utc).Ticks;

            Assert.That(actual.Ticks, Is.EqualTo(feb2));
        }
    }

}
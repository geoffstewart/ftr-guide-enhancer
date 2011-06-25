namespace GuideEnricher.Tests
{
    using System.Collections.Generic;
    using GuideEnricher.Config;
    using GuideEnricher.EpisodeMatchMethods;
    using GuideEnricher.tvdb;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class SeriesMatchingTests
    {
        [Test]
        public void SeriesWithPunctuationsAreMatchedCorrectly()
        {
            // Now we can put in your code fix
            // it works!
            // Now the next step is refactor
            // it still works, i didnt break it! :)

            //Arrange
            var config = new Mock<IConfiguration>();
            var matchMethods = new List<IEpisodeMatchMethod>();
            var tvdbLib = new TvdbLibAccess(config.Object, matchMethods);

            //Act
            var seriesID = tvdbLib.getSeriesId("American Dad");

            //Assert
            Assert.AreEqual(73141, seriesID);
        }
    }
}
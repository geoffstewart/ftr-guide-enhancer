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
            var config = new Mock<IConfiguration>();
            var matchMethods = new List<IEpisodeMatchMethod>();
            var tvdbLib = new TvdbLibAccess(config.Object, matchMethods);

            var seriesID = tvdbLib.getSeriesId("American Dad");

            Assert.AreEqual(73141, seriesID);
        }
    }
}
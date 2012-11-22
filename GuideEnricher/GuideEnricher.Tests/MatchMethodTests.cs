namespace GuideEnricher.Tests
{
    using System;
    using ArgusTV.DataContracts;
    using ArgusTV.ServiceContracts;

    using GuideEnricher.Config;
    using GuideEnricher.EpisodeMatchMethods;
    using GuideEnricher.Model;
    using GuideEnricher.tvdb;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class MatchMethodTests
    {
        [Test]
        public void ShouldUpdateSimilarPrograms()
        {
            var programA = new GuideProgram();
            programA.Title = "Intervention";
            programA.SubTitle = "Sarah; Mikeal";
            programA.PreviouslyAiredTime = new DateTime(2011, 6, 27);
            programA.GuideProgramId = Guid.NewGuid();

            var programB = new GuideProgram();
            programB.Title = "Intervention";
            programB.SubTitle = "Sarah; Mikeal";
            programB.GuideProgramId = Guid.NewGuid();

            var series = new GuideEnricherSeries("Intervention", false, false, false);
            series.AddProgram(new GuideEnricherEntities(programA));
            series.AddProgram(new GuideEnricherEntities(programB));

            var config = Config.GetInstance();
            var matchMethods = EpisodeMatchMethodLoader.GetMatchMethods();
            var tvdbLibAccess = new TvdbLibAccess(config, matchMethods);
            var enricher = new Enricher(config, new Mock<ILogService>().Object, new Mock<IGuideService>().Object, new Mock<ISchedulerService>().Object, tvdbLibAccess, matchMethods);
            enricher.EnrichSeries(series);

            Assert.AreEqual(11, programA.SeriesNumber);
            Assert.AreEqual(2, programA.EpisodeNumber);
            Assert.AreEqual(11, programB.SeriesNumber);
            Assert.AreEqual(2, programB.EpisodeNumber);
        }
    }
}
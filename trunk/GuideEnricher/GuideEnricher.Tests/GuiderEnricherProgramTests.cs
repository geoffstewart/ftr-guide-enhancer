namespace GuideEnricher.Tests
{
    using ForTheRecord.Entities;
    using GuideEnricher.Model;
    using NUnit.Framework;

    [TestFixture]
    public class GuiderEnricherProgramTests
    {
        [Test]
        public void EpisodeIsEnricherReturnsTrueWhenEpisodeNumberAndSeasonEpisode()
        {
            var guideProgram = new GuideProgram();
            guideProgram.SeriesNumber = 3;
            guideProgram.EpisodeNumber = 10;
            guideProgram.EpisodeNumberDisplay = "S03E10";
            var program = new GuideEnricherEntities(guideProgram);
            Assert.IsTrue(program.EpisodeIsEnriched());
        }

        [Test]
        public void EpisodeIsEnricherReturnsFalseWhenNoEpisodeNumber()
        {
            var guideProgram = new GuideProgram();
            var program = new GuideEnricherEntities(guideProgram);
            Assert.IsFalse(program.EpisodeIsEnriched());
        }

        [Test]
        public void EpisodeIsEnricherReturnsFalseWhenNoSeasonEpisode()
        {
            var guideProgram = new GuideProgram();
            guideProgram.SeriesNumber = 3;
            guideProgram.EpisodeNumber = 10;
            guideProgram.EpisodeNumberDisplay = "Test";
            var program = new GuideEnricherEntities(guideProgram);
            Assert.IsFalse(program.EpisodeIsEnriched());
        }
    }
}
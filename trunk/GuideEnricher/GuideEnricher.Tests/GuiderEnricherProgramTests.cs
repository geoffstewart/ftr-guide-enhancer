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
            var program = new GuideEnricherProgram(guideProgram);
            Assert.IsTrue(program.EpisodeIsEnriched());
        }

        [Test]
        public void EpisodeIsEnricherReturnsFalseWhenNoEpisodeNumber()
        {
            var guideProgram = new GuideProgram();
            var program = new GuideEnricherProgram(guideProgram);
            Assert.IsFalse(program.EpisodeIsEnriched());
        }

        [Test]
        public void EpisodeIsEnricherReturnsFalseWhenNoSeasonEpisode()
        {
            var guideProgram = new GuideProgram();
            guideProgram.SeriesNumber = 3;
            guideProgram.EpisodeNumber = 10;
            guideProgram.EpisodeNumberDisplay = "Test";
            var program = new GuideEnricherProgram(guideProgram);
            Assert.IsFalse(program.EpisodeIsEnriched());
        }

        [Test]
        public void GuideEnricherProgramWithSameTitleAndSubtitleIsEqual()
        {
            var programA = new GuideEnricherProgram(new GuideProgram());
            var programB = new GuideEnricherProgram(new GuideProgram());
            programA.Title = "Show";
            programA.SubTitle = "Episde";
            programA.EpisodeNumberDisplay = "S01E01";
            programB.Title = "Show";
            programB.SubTitle = "Episde";
            programB.EpisodeNumberDisplay = "S01E01";

            Assert.IsTrue(programA.Equals(programB));
        }
    }
}
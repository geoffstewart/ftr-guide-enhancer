namespace GuideEnricher.Tests
{
    using ForTheRecord.Entities;
    using GuideEnricher.tvdb;
    using NUnit.Framework;

    [TestFixture]
    public class TVDBLibTest
    {
        [Test]
        public void TestEnricherWithProgramNameAsSubTitle()
        {
            var program = new GuideProgram();
            program.Title = "The Big Bang Theory";
            program.SubTitle = "The Zazzy Substitution";
            var enricher = new TvDbDataEnricher();
            enricher.enrichProgram(program);
            Assert.AreEqual("S04E03", program.EpisodeNumberDisplay);
        }

        [Test]
        public void TestEnricherWithAbsoluteEpisodeNumber()
        {
            var program = new GuideProgram();
            program.Title = "The Big Bang Theory";
            program.SubTitle = "66";
            var enricher = new TvDbDataEnricher();
            enricher.enrichProgram(program);
            Assert.AreEqual("S04E03", program.EpisodeNumberDisplay);
        }
    }
}

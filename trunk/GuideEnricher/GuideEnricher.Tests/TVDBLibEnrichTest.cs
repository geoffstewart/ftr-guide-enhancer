namespace GuideEnricher.Tests
{
    using ForTheRecord.Entities;
    using GuideEnricher.tvdb;
    using NUnit.Framework;

    [TestFixture]
    public class TVDBLibEnrichTest
    {
        private TvDbDataEnricher enricher;

        [SetUp]
        public void Setup()
        {
            this.enricher = new TvDbDataEnricher();
        }

        [Test]
        public void TestEnricherWithProgramNameAsSubTitle()
        {
            var program = new GuideProgram();
            program.Title = "The Big Bang Theory";
            program.SubTitle = "The Zazzy Substitution";
            this.enricher.enrichProgram(program);
            Assert.AreEqual("S04E03", program.EpisodeNumberDisplay);
        }

        [Test]
        public void TestEnricherWithAbsoluteEpisodeNumber()
        {
            // I know this one fails, need some examples of actual guide entries...
            var program = new GuideProgram();
            program.Title = "The Big Bang Theory";
            program.SubTitle = "66";
            this.enricher.enrichProgram(program);
            Assert.AreEqual("S04E03", program.EpisodeNumberDisplay);
        }
    }
}

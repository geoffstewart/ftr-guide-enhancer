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
            // I know this one fails, need to add series map for this one "Castle (2009)"
            var program = new GuideProgram();
            program.Title = "Castle";
            program.SubTitle = "Pretty Dead (59)";
            this.enricher.enrichProgram(program);
            Assert.AreEqual("S03E23", program.EpisodeNumberDisplay);
        }
    }
}

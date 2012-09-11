namespace GuideEnricher.Tests
{
    using System;
    using System.Collections.Generic;
    using GuideEnricher.Config;
    using GuideEnricher.tvdb;
    using log4net.Config;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class TVDBLibEnrichTest
    {
        private List<TestProgram> testPrograms;
        private Mock<IConfiguration> mockConfig;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            BasicConfigurator.Configure();
        }

        [SetUp]
        public void Setup()
        {
            this.mockConfig = new Mock<IConfiguration>();
            this.mockConfig.Setup(x => x.getProperty(It.IsAny<string>())).Returns(string.Empty);
            this.mockConfig.Setup(x => x.getProperty("TvDbLibCache")).Returns(@"c:\tvdblibcache\");
        }

        private void CreateTestData()
        {
            this.testPrograms = new List<TestProgram>();
            this.testPrograms.Add(new TestProgram("Being Human", "30", 30, "S01E07"));
            this.testPrograms.Add(new TestProgram("House", "the fix (153)", 153, "S07E21"));
            this.testPrograms.Add(new TestProgram("Chuck", "Chuck versus the last details (83)", 83, "S05E05"));
            this.testPrograms.Add(new TestProgram("Family Guy", "Brian sings and swings (70)", 70, "S04E19"));
            this.testPrograms.Add(new TestProgram("Family Guy", "Deep Throats", 74, "S04E23"));
            this.testPrograms.Add(new TestProgram("The Big Bang Theory", "The Zazzy Substitution", 0, "S04E03"));
            this.testPrograms.Add(new TestProgram("Castle", "Pretty Dead (59)", 59, "S03E23"));
            this.testPrograms.Add(new TestProgram("Shark Tank", "Episode 2", 202, "S02E02"));
        }

        [Test]
        public void TestEnricherMethods()
        {
            var enricher = new TvdbLibAccess(mockConfig.Object, EpisodeMatchMethodLoader.GetMatchMethods());
            this.CreateTestData();

            bool pass = true;
            foreach (var testProgram in this.testPrograms)
            {
                try
                {
                    var series = enricher.GetTvdbSeries(enricher.getSeriesId(testProgram.Title),false);
                    enricher.EnrichProgram(testProgram, series);
                    if (testProgram.EpisodeNumberDisplay == testProgram.ExpectedEpisodeNumberDisplay)
                    {
                        Console.WriteLine(string.Format("Correctly matched {0} - {1}", testProgram.Title, testProgram.EpisodeNumberDisplay));   
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Unable to match {0} - {1}", testProgram.Title, testProgram.SubTitle));
                        pass = false;
                    }
                    
                }
                catch (Exception exception)
                {
                    pass = false;
                    Console.WriteLine(string.Format("Couldn't match {0} - {1}", testProgram.Title, testProgram.SubTitle));   
                    Console.WriteLine(exception.Message);
                }
            }

            if (pass)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }

        [Test]
        public void TestMappingNameWithID()
        {
            var lawOrderProgram = new TestProgram("Law & Order: Special Victims Unit", "Identity", 0, "S06E12");
            var seriesNameMap = new Dictionary<string, string>(1);
            seriesNameMap.Add("Law & Order: Special Victims Unit", "id=75692");
            mockConfig.Setup(x => x.getSeriesNameMap()).Returns(seriesNameMap);
            var enricher = new TvdbLibAccess(mockConfig.Object, EpisodeMatchMethodLoader.GetMatchMethods());
            var series = enricher.GetTvdbSeries(enricher.getSeriesId(lawOrderProgram.Title), false);
            enricher.EnrichProgram(lawOrderProgram, series);
            Assert.IsTrue(lawOrderProgram.EpisodeIsEnriched());
        }

        [Test]
        public void TestMappingRegexWithRegexReplace()
        {
            var lawOrderProgram = new TestProgram("Law & Order: New York", "Identity", 0, "S06E12");
            var seriesNameMap = new Dictionary<string, string>(1);
            seriesNameMap.Add("regex=Law & Order", "replace=Law and Order");
            mockConfig.Setup(x => x.getSeriesNameMap()).Returns(seriesNameMap);
            var enricher = new TvdbLibAccess(mockConfig.Object, EpisodeMatchMethodLoader.GetMatchMethods());
            var series = enricher.GetTvdbSeries(enricher.getSeriesId(lawOrderProgram.Title), false);
            enricher.EnrichProgram(lawOrderProgram, series);
            Assert.IsTrue(lawOrderProgram.EpisodeIsEnriched());
        }

        [Test]
        public void TestMappingRegex()
        {
            var lawOrderProgram = new TestProgram("Stargate Atlantis123", "Common Ground", 0, "S03E07");
            var seriesNameMap = new Dictionary<string, string>(1);
            seriesNameMap.Add("regex=Stargate Atl.*", "Stargate Atlantis");
            mockConfig.Setup(x => x.getSeriesNameMap()).Returns(seriesNameMap);
            var enricher = new TvdbLibAccess(mockConfig.Object, EpisodeMatchMethodLoader.GetMatchMethods());
            var series = enricher.GetTvdbSeries(enricher.getSeriesId(lawOrderProgram.Title), false);
            enricher.EnrichProgram(lawOrderProgram, series);
            Assert.IsTrue(lawOrderProgram.EpisodeIsEnriched());
        }

        [Test]
        public void TestRegularMapping()
        {
            var lawOrderProgram = new TestProgram("Stargate Atlantis123", "Common Ground", 0, "S03E07");
            var seriesNameMap = new Dictionary<string, string>(1);
            seriesNameMap.Add("Stargate Atlantis123", "Stargate Atlantis");
            mockConfig.Setup(x => x.getSeriesNameMap()).Returns(seriesNameMap);
            var enricher = new TvdbLibAccess(mockConfig.Object, EpisodeMatchMethodLoader.GetMatchMethods());
            var series = enricher.GetTvdbSeries(enricher.getSeriesId(lawOrderProgram.Title), false);
            enricher.EnrichProgram(lawOrderProgram, series);
            Assert.IsTrue(lawOrderProgram.EpisodeIsEnriched());
        }
    }
}

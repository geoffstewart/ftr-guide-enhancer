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
        private TvdbLibAccess enricher;
        private List<TestProgram> testPrograms;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            BasicConfigurator.Configure();
        }

        [SetUp]
        public void Setup()
        {
            var mockConfig = new Mock<IConfiguration>();
            this.enricher = new TvdbLibAccess(mockConfig.Object, EpisodeMatchMethodLoader.GetMatchMethods());
            this.CreateTestData();
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
        }

        [Test]
        public void TestEnricherWithAbsoluteEpisodeNumber()
        {
            bool pass = true;
            foreach (var testProgram in this.testPrograms)
            {
                try
                {
                    this.enricher.EnrichProgram(testProgram, false);
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
    }
}

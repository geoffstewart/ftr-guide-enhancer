namespace GuideEnricher.Tests
{
    using System;
    using System.Collections.Generic;
    using ForTheRecord.Entities;
    using GuideEnricher.tvdb;
    using NUnit.Framework;

    [TestFixture]
    public class TVDBLibEnrichTest
    {
        private TvDbDataEnricher enricher;
        private List<TestProgram> testPrograms;

        [SetUp]
        public void Setup()
        {
            this.enricher = new TvDbDataEnricher();
            CreateTestData();
        }

        private void CreateTestData()
        {
            this.testPrograms = new List<TestProgram>();
            this.testPrograms.Add(new TestProgram("Being Human", "30", "S01E07"));
            this.testPrograms.Add(new TestProgram("House", "the fix (243)", "S07E21"));
            this.testPrograms.Add(new TestProgram("Chuck", "Chuck versus the last details (83)", "S04E23"));
            this.testPrograms.Add(new TestProgram("Family Guy", "Brian sings and swings (70)", "S04E19"));
            this.testPrograms.Add(new TestProgram("The Big Bang Theory", "The Zazzy Substitution", "S04E03"));
            this.testPrograms.Add(new TestProgram("Castle", "Pretty Dead (59)", "S03E23"));
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
            foreach (var testProgram in this.testPrograms)
            {
                try
                {
                    this.enricher.enrichProgram(testProgram);
                    if (testProgram.EpisodeNumberDisplay == testProgram.ExpectedEpisodeNumberDisplay)
                    {
                        Console.WriteLine(string.Format("Correctly matched {0} - {1}", testProgram.Title, testProgram.EpisodeNumberDisplay));   
                    }

                    Console.WriteLine();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(string.Format("Couldn't match {0} - {1}", testProgram.Title, testProgram.SubTitle));   
                    Console.WriteLine(exception.Message);
                }
            }
        }
    }
}

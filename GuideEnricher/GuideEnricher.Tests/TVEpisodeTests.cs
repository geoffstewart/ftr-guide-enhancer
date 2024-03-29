namespace GuideEnricher.Tests
{
    using System;
    using System.Collections.Generic;

    using ArgusTV.DataContracts;

    using GuideEnricher.Model;

    using NUnit.Framework;

    using Should;

    using TvdbLib.Data;

    [TestFixture]
    public class TVEpisodeTests
    {
        [Test]
        public void SortTest()
        {
            var episodes = new List<TvdbEpisode>();
            episodes.Add(new TvdbEpisode { SeasonNumber = 2, EpisodeNumber = 1 });
            episodes.Add(new TvdbEpisode { SeasonNumber = 1, EpisodeNumber = 1});
            episodes.Add(new TvdbEpisode { SeasonNumber = 1, EpisodeNumber = 5 });
            episodes.Add(new TvdbEpisode { SeasonNumber = 3, EpisodeNumber = 12 });
            episodes.Add(new TvdbEpisode { SeasonNumber = 2, EpisodeNumber = 12 });

            episodes.Sort(new TvEpisodeComparer());

            Assert.AreEqual(1, episodes[0].SeasonNumber);
            Assert.AreEqual(1, episodes[0].EpisodeNumber);

            Assert.AreEqual(1, episodes[1].SeasonNumber);
            Assert.AreEqual(5, episodes[1].EpisodeNumber);

            Assert.AreEqual(2, episodes[2].SeasonNumber);
            Assert.AreEqual(1, episodes[2].EpisodeNumber);

            Assert.AreEqual(2, episodes[3].SeasonNumber);
            Assert.AreEqual(12, episodes[3].EpisodeNumber);

            Assert.AreEqual(3, episodes[4].SeasonNumber);
            Assert.AreEqual(12, episodes[4].EpisodeNumber);
        }

        [Test]
        public void ShouldUpdateSimilarPrograms()
        {
            var programA =
                new GuideEnricherProgram(
                    new GuideProgram
                    {
                        Title = "Intervention",
                        SubTitle = "Sarah; Mikeal",
                        PreviouslyAiredTime = new DateTime(2011, 6, 27),
                        GuideProgramId = Guid.NewGuid()
                    });

            var programB =
                new GuideEnricherProgram(
                    new GuideProgram
                    {
                        Title = "Intervention",
                        SubTitle = "Sarah; Mikeal",
                        GuideProgramId = Guid.NewGuid()
                    });

            var matchedSeries = new GuideEnricherSeries("Intervention", false, false, false);
            var pendingSeries = new List<GuideEnricherProgram>(1) { programB };

            matchedSeries.AddProgram(programA);



            var similarPrograms = matchedSeries.FindSimilarPrograms(pendingSeries, programA);

            similarPrograms.Count.ShouldEqual(1);
        }

        [Test]
        public void GetValidEpisodeNumberInSubtitle()
        {
            var program = new GuideEnricherProgram(new GuideProgram { Title = "Some Show", SubTitle = "13"});
            program.GetValidEpisodeNumber().ShouldEqual(13);
        }

        [Test]
        public void GetValidEpisodeNumberInEpisodeNumber()
        {
            var program = new GuideEnricherProgram(new GuideProgram { Title = "Some Show", EpisodeNumber = 13});
            program.GetValidEpisodeNumber().ShouldEqual(13);
        }
    }
}
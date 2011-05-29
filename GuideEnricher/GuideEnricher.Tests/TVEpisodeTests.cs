namespace GuideEnricher.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;
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
    }
}
namespace GuideEnricher.Tests
{
    using ForTheRecord.Entities;

    public class TestProgram : GuideProgram
    {
        public TestProgram(string title, string subTitle, string expectedEpisodeNumberDisplay)
        {
            this.Title = title;
            this.SubTitle = subTitle;
            this.ExpectedEpisodeNumberDisplay = expectedEpisodeNumberDisplay;
        }

        public string ExpectedEpisodeNumberDisplay { get; set; }
    }
}
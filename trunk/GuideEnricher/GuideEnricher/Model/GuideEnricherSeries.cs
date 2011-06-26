namespace GuideEnricher.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class GuideEnricherSeries
    {
        private readonly bool updateAll;
        private readonly bool updateSubtitles;
        private static readonly GuideProgramEqualityComparer guideProgramEqualityComparer = new GuideProgramEqualityComparer();

        public GuideEnricherSeries(String title, bool updateAllParameter, bool updateSubtitlesParameter)
        {
            this.Title = title;
            this.updateAll = updateAllParameter;
            this.updateSubtitles = updateSubtitlesParameter;

            this.PendingPrograms = new List<GuideEnricherEntities>();
            this.SuccessfulPrograms = new List<GuideEnricherEntities>();
            this.FailedPrograms = new List<GuideEnricherEntities>();
            this.IgnoredPrograms = new List<GuideEnricherEntities>();
        }

        public List<GuideEnricherEntities> PendingPrograms { get; set; }

        public List<GuideEnricherEntities> SuccessfulPrograms { get; set; }

        public List<GuideEnricherEntities> FailedPrograms { get; set; }

        public List<GuideEnricherEntities> IgnoredPrograms { get; set; }

        public string Title { get; set; }
        
        public int TvDbSeriesID { get; set; }
        
        public bool isRefreshed { get; set; }
        
        public bool isIgnored { get; set; }
        

        public void AddProgram(GuideEnricherEntities program)
        {
            if (!this.updateAll && program.Matched)
            {
                if (!this.IgnoredPrograms.Contains(program, guideProgramEqualityComparer))
                {
                    this.IgnoredPrograms.Add(program);  
                }
            }
            else
            {
                if (!this.PendingPrograms.Contains(program, guideProgramEqualityComparer))
                {
                    this.PendingPrograms.Add(program);
                }
            }
        }

        public void AddAllToEnrichedPrograms(GuideEnricherEntities program)
        {
            SuccessfulPrograms.Add(program);
            PendingPrograms.Remove(program);
            List<GuideEnricherEntities> similarPrograms = PendingPrograms.FindAll(x => x.SubTitle == program.OriginalSubTitle);
            if (similarPrograms.Count > 0)
            {
                PendingPrograms = new List<GuideEnricherEntities>(PendingPrograms.Except(similarPrograms));
                foreach (GuideEnricherEntities similarProgram in similarPrograms)
                {
                    similarProgram.SeriesNumber = program.SeriesNumber;
                    similarProgram.EpisodeNumber = program.EpisodeNumber;
                    similarProgram.EpisodeNumberDisplay = program.EpisodeNumberDisplay;
                    if (this.updateSubtitles)
                    {
                        similarProgram.SubTitle = program.SubTitle;
                    }
                }

                SuccessfulPrograms.AddRange(similarPrograms);
            }
        }

        public void AddAllToFailedPrograms(GuideEnricherEntities program)
        {
            FailedPrograms.Add(program);
            PendingPrograms.Remove(program);
            List<GuideEnricherEntities> similarPrograms = PendingPrograms.FindAll(x => x.SubTitle == program.OriginalSubTitle);
            if (similarPrograms.Count > 0)
            {
                PendingPrograms = new List<GuideEnricherEntities>(PendingPrograms.Except(similarPrograms));                
                FailedPrograms.AddRange(similarPrograms);
            }
        }

        public void TvDbInformationRefreshed()
        {
            this.isRefreshed = true;
            this.PendingPrograms.AddRange(this.FailedPrograms);
            this.FailedPrograms.Clear();
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
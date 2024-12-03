using System.Collections.Generic;

namespace PipeWorkshopApp.Models
{
    public class SectionQueue
    {
        public string SectionName { get; set; }
        public Queue<PipeData> Pipes { get; set; }

        public SectionQueue(string sectionName)
        {
            SectionName = sectionName;
            Pipes = new Queue<PipeData>();
        }
    }
}

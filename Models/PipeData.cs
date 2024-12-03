namespace PipeWorkshopApp.Models
{
    public class PipeData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool IsRejected { get; set; } = false;
        public string RejectionStage { get; set; }
        public string MarkingInfo { get; set; } // Информация от маркировщика
    }
}

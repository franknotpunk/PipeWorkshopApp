using System;

namespace PipeWorkshopApp.Models
{
    public class MarkingData
    {
        public Guid PipeId { get; set; }  // Уникальный идентификатор трубы
        public string Info { get; set; }  // Информация от маркировщика

        public string Test {  get; set; }

        // Здесь можно добавить дополнительные поля, если потребуется
    }
}

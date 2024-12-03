using System;

namespace PipeWorkshopApp.Models
{
    public class MarkingData
    {
        public Guid PipeId { get; set; }  // Уникальный идентификатор трубы
        public string Info { get; set; }  // Информация от маркировщика

        // Здесь можно добавить дополнительные поля, если потребуется
    }
}

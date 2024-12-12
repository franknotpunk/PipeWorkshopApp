namespace PipeWorkshopApp.Models
{
    public class PipeData
    {
        public int Id { get; set; }
        public string PipeNumber { get; set; }    // Номер трубы (из textBoxPipeNumber)
        public string Diameter { get; set; }       // Диаметр (из comboBoxDiameter)
        public string Material { get; set; }       // Материал ("CR" или "", в зависимости от comboBoxMaterial)
        public string Group { get; set; }          // Группа (из comboBoxGroup)
        public string PipeLength { get; set; }     // Длина трубы, считанная по Modbus (из getLength())
        public string Thickness { get; set; }      // Толщина стенки (из comboBoxThickness)

        // Новое поле для партии
        public int BatchNumber { get; set; }
    }
}

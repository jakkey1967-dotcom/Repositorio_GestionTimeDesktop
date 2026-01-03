using System;

namespace GestionTime.Desktop.WinForms.Models
{
    public class CatalogItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public override string ToString()
        {
            return Nombre;
        }
    }
}

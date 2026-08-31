using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos
{
    public record class ConsultaCatalogo : ConsultaPaginada
    {
        public bool SoloActivos { get; set; } = true;
    }
}

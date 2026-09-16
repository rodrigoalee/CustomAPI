
namespace CustomCore.API.Configuracion
{
    /// <summary>
    /// Opciones de configuración para Stripe, utilizadas para procesar pagos en la aplicación.
    /// </summary>
    public sealed class OpcionesStripe
    {
        /// <summary>
        /// Llave privada del API de Stripe
        /// </summary>
        public const string Seccion = "Stripe";

        /// <summary>
        /// Clave secreta de Stripe, utilizada para autenticar las solicitudes a la API de Stripe.
        /// </summary>
        public string ClaveSecreta { get; set; } = string.Empty;

        /// <summary>
        /// Clave secreta del webhook de Stripe, utilizada para verificar la autenticidad de los eventos recibidos desde Stripe.
        /// </summary>
        public string SecretoWebhook { get; set; } = string.Empty;

        /// <summary>
        /// Código de moneda en minúscula según ISO 4217, utilizado para las transacciones con Stripe. Por defecto es "gtq" (quetzal guatemalteco).
        /// </summary>
        public string Moneda { get; set; } = "gtq";

        /// <summary>
        /// URL a la que se redirige al cliente después de un pago exitoso, utilizada para mostrar una página de confirmación o agradecimiento.
        /// </summary>
        public string UrlExito { get; set; } = string.Empty;

        /// <summary>
        /// URL a la que se redirige al cliente después de cancelar un pago, utilizada para mostrar una página de cancelación.
        /// </summary>
        public string UrlCancelacion { get; set; } = string.Empty;
    }
}

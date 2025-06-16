namespace secwin.core
{
    public class SecwinAppSettings
    {
        /// <summary>
        /// Path to the certificate file used for secure communication.
        /// </summary>
        public string CertificatePath { get; set; } = string.Empty;

        /// <summary>
        /// Password for the certificate used for secure communication.
        /// </summary>
        public string CertificatePassword { get; set; } = string.Empty;

        /// <summary>
        /// The port of the service host.
        /// </summary>
        public int ServicePort { get; set; }

        /// <summary>
        /// The name of the event source for logging.
        /// </summary>
        public string EventSource { get; set; } = string.Empty;

        /// <summary>
        /// The name of the event log where the service logs will be written.
        /// </summary>
        public string EventLog { get; set; } = string.Empty;
    }
}

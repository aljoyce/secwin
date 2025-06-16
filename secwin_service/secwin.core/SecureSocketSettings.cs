namespace secwin.core
{
    public class SecureSocketSettings : ConnectionSettings
    {
        public required int MaxMessageSize { get; set; }

        public required string Host { get; set; }

        public required int Port { get; set; }

        required public string CertificatePath { get; set; }

        required public string CertificatePassword { get; set; }
    }
}
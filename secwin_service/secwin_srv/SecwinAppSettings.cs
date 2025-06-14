namespace secwin_srv
{
    public class SecwinAppSettings
    {
        public string CertificatePath { get; set; } = string.Empty;
        public string CertificatePassword { get; set; } = string.Empty;
        public int ServicePort { get; set; }
    }
}

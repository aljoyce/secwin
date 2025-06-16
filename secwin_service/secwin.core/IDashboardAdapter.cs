namespace secwin.core
{
    public interface IDashboardAdapter
    {
        public Task Connect();

        public void Disconnect();
    }
}
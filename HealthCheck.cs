namespace WebApiService
{
    public struct HealthCheck 
    {
        public string DependencyName { get; }
        public bool IsDown { get; }
        public int ResponseTimeMilliseconds { get; }
        public bool IsCritical { get; }
        public string MachineName { get; set; }

        public HealthCheck(string dependencyName, bool isDown, int responseTimeMilliseconds, bool isCritical = true, string machineName = null)
        {
            DependencyName = dependencyName;
            IsDown = isDown;
            ResponseTimeMilliseconds = responseTimeMilliseconds;
            IsCritical = isCritical;
            MachineName = machineName ?? System.Environment.MachineName;
        }
    }
}

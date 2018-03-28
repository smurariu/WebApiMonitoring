namespace WebApiService
{
    public struct HealthCheck 
    {
        public string DependencyName { get; }
        public bool IsDown { get; }
        public int ResponseTimeMilliseconds { get; }
        public bool IsCritical { get; }

        public HealthCheck(string dependencyName, bool isDown, int responseTimeMilliseconds, bool isCritical = true)
        {
            DependencyName = dependencyName;
            IsDown = isDown;
            ResponseTimeMilliseconds = responseTimeMilliseconds;
            IsCritical = isCritical;
        }
    }
}

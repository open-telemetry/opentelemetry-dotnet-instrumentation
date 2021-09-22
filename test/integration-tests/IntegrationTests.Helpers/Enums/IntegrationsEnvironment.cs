namespace IntegrationTests.Helpers.Enums
{
    public enum IntegrationsEnvironment
    {
        /// <summary>
        /// Integration tests are running locally
        /// </summary>
        Local = 0,

        /// <summary>
        /// Integration tests are running in CI
        /// </summary>
        CI
    }
}

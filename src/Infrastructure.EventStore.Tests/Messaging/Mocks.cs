namespace Infrastructure.EventStore.Tests.Messaging
{
    public class TestAppCategoryV1
    {
        private const string prefix = "testApp.";

        public const string Users = prefix + "users";
        public const string Orgs = prefix + "orgs";

        public static class Logs
        {
            private const string prefix = TestAppCategoryV1.prefix + "logs.";

            public const string Logins = prefix + "logins";
        }

        public class Sagas
        {
            private const string prefix = TestAppCategoryV1.prefix + "sagas.";

            public const string userRegistration = prefix + "userRegistration";
            public const string usernameMods = prefix + "usernameMods";
        }
    }

    public class TestAppCategoryV2
    {
        private const string prefix = "testApp.";

        public const string Users = prefix + "users";
        public const string Orgs = prefix + "orgs";
        public const string OrgsV2 = prefix + "orgsV2";

        public static class Logs
        {
            private const string prefix = TestAppCategoryV2.prefix + "logs.";

            public const string Logins = prefix + "logins";
        }

        public class Sagas
        {
            private const string prefix = TestAppCategoryV2.prefix + "sagas.";

            public const string userRegistration = prefix + "userRegistration";
            public const string usernameMods = prefix + "usernameMods";
        }
    }
}

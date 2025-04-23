namespace HomeCentre.PIM.Common
{
    public static class Constants
    {
        public const string AllCacheKeys = "All";

        public static int BYTE_VALUE { get; set; }

        public static class AdminGroup
        {
            public const long Id = 0;
            public const string Name = "ScriptAssist";
        }

        public static class ChatMessageType
        {
            public static class Attachment
            {
                public const long Id = 1;
                public const string Name = "attachment";
            }

            public static class Text
            {
                public const long Id = 0;
                public const string Name = "text";
            }
        }

        public static class PaymentBy
        {
            public static readonly byte CreditCard = 1;
            public static readonly string FreeTransaction = "FREE";
        }

        public static class Roles
        {
            public static class Admin
            {
                public const long Id = 1;
                public const string Name = "admin";
            }

            public static class SuperAdmin
            {
                public const long Id = 2;
                public const string Name = "superadmin";
            }

            public static class User
            {
                public const long Id = 0;
                public const string Name = "user";
            }
        }

        public class Policies
        {
            public const string Admin = "admin";
            public const string AdminAndSuperAdmin = "admin,superadmin";
            public const string All = "user,admin,superadmin";
            public const string SuperAdmin = "superadmin";
            public const string User = "user";
        }
    }
}
using System.IO;
using FluentMigrator;

namespace HomeCentre.PIM.Migrator.Migration
{
    [Migration(1)]
    public class BaseLine : FluentMigrator.Migration
    {
        public override void Up()
        {
            this.Execute.Sql(File.ReadAllText("BaselineUp.sql"));

            this.CreateTableAndAddStandardColumns("AppUser", false);

            this.CreateMasterTable("Role");
            this.CreateMasterTable("Gender");
            this.CreateMasterTable("AppUserRole");
            this.CreateMasterTable("Language");
            this.CreateMasterTable("Label");
            this.CreateMasterTable("Country");

            this.CreateColIfNotExists("AppUser", "Email", column => column.AsString().NotNullable());
            this.CreateColIfNotExists("AppUser", "Name", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "Password", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "VerificationKey", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "EncryptionKey", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "IsEmailVerified", column => column.AsBoolean().Nullable());
            this.CreateColIfNotExists("AppUser", "OauthKey", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "OauthToken", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "OauthProvider", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "HasAcceptedTerms", column => column.AsBoolean().Nullable());
            this.CreateColIfNotExists("AppUser", "HasAcceptedPrivacyPolicy", column => column.AsBoolean().Nullable());
            this.CreateColIfNotExists("AppUser", "IsDeleted", column => column.AsBoolean().Nullable());
            this.CreateForeignKeyColIfNotExists("AppUser", "GenderId", "Gender", isnullable: true);
            this.CreateColIfNotExists("AppUser", "Country", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "State", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "City", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "Zip", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "Address", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "FirstName", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "LastName", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "ResetPasswordKey", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "Mobile", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "ResetPasswordExpiryTime",
                column => column.AsDateTimeOffset().Nullable());
            this.CreateColIfNotExists("AppUser", "VerificationKeyExpiryTime",
                column => column.AsDateTimeOffset().Nullable());
            this.CreateColIfNotExists("AppUser", "ProfilePicture", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "LastLoginDate", column => column.AsDateTime().Nullable());
            this.CreateColIfNotExists("AppUser", "LastLoginLocation", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "Message", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "CompanyName", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "Website", column => column.AsString().Nullable());
            this.CreateColIfNotExists("AppUser", "Domain", column => column.AsString().Nullable());

            this.CreateForeignKeyColIfNotExists("AppUser", "CreatedBy", "AppUser");
            this.CreateForeignKeyColIfNotExists("AppUser", "UpdatedBy", "AppUser");

            this.CreateForeignKeyColIfNotExists("AppUserRole", "AppUserId", "AppUser", isnullable: false);
            this.CreateForeignKeyColIfNotExists("AppUserRole", "RoleId", "Role", isnullable: false);

            this.CreateTableAndAddStandardColumns("LoginHistory");
            this.CreateForeignKeyColIfNotExists("LoginHistory", "AppUserId", "AppUser", isnullable: false);
            this.CreateColIfNotExists("LoginHistory", "Latitude", column => column.AsDecimal().Nullable());
            this.CreateColIfNotExists("LoginHistory", "Longitude", column => column.AsDecimal().Nullable());
            this.CreateColIfNotExists("LoginHistory", "IpAddress", column => column.AsString().Nullable());
            this.CreateColIfNotExists("LoginHistory", "Location", column => column.AsString().Nullable());
            this.CreateJsonBColIfNotExists("LoginHistory", "geojson");
            this.CreateJsonBColIfNotExists("LoginHistory", "geoipjson");

            this.CreateForeignKeyColIfNotExists("Label", "LanguageId", "Language");
            this.CreateColIfNotExists("Country", "CountryCode", column => column.AsString().Nullable());

            this.Execute.Sql(
                "INSERT INTO APPUSER(ID, CREATEDBY, UPDATEDBY, EMAIL, FIRSTNAME, LASTNAME, ISACTIVE, ISDELETED) VALUES (1, 1, 1, 'hcpimtester@yopmail.com', 'Admin', 'Admin', true, false)");
        }

        public override void Down()
        {
        }
    }
}
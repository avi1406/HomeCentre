using System.IO;
using FluentMigrator;

namespace HomeCentre.PIM.Migrator.Migration
{
    [Maintenance(MigrationStage.BeforeAll)]
    public class PreMigrationScript : FluentMigrator.Migration
    {
        public override void Up()
        {
            var sql = File.ReadAllText("PreMigrationScript.sql");
            if (string.IsNullOrWhiteSpace(sql))
            {
                return;
            }

            this.Execute.Sql(File.ReadAllText("PreMigrationScript.sql"));
        }

        public override void Down()
        {
        }
    }
}
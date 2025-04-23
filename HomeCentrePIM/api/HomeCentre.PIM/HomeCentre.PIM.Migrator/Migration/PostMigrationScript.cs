using System.IO;
using FluentMigrator;

namespace HomeCentre.PIM.Migrator.Migration
{
    [Maintenance(MigrationStage.AfterAll)]
    public class PostMigrationScript : FluentMigrator.Migration
    {
        public override void Up()
        {
            var sql = File.ReadAllText("PostMigrationScript.sql");
            if (string.IsNullOrWhiteSpace(sql))
            {
                return;
            }

            this.Execute.Sql(File.ReadAllText("PostMigrationScript.sql"));
        }

        public override void Down()
        {
        }
    }
}
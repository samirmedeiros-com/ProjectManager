using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagerWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEventsSequenceAndTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_EVENTS_ID START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN NULL; END;");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE TRIGGER TRG_EVENTS_ID
                BEFORE INSERT ON Events
                FOR EACH ROW
                WHEN (NEW.Id IS NULL OR NEW.Id = 0)
                BEGIN
                    SELECT SEQ_EVENTS_ID.NEXTVAL INTO :NEW.Id FROM DUAL;
                END;
                /
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER TRG_EVENTS_ID");
            migrationBuilder.Sql("DROP SEQUENCE SEQ_EVENTS_ID");
        }
    }
}

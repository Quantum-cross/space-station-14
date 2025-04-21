using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class BorgProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_antag_profile_profile_id",
                table: "antag");

            migrationBuilder.DropForeignKey(
                name: "FK_job_profile_profile_id",
                table: "job");

            migrationBuilder.DropForeignKey(
                name: "FK_profile_role_loadout_profile_profile_id",
                table: "profile_role_loadout");

            migrationBuilder.DropForeignKey(
                name: "FK_trait_profile_profile_id",
                table: "trait");

            migrationBuilder.AddColumn<int>(
                name: "humanoid_profile_id",
                table: "trait",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "species",
                table: "profile",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "spawn_priority",
                table: "profile",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "skin_color",
                table: "profile",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "sex",
                table: "profile",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "hair_name",
                table: "profile",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "hair_color",
                table: "profile",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "profile",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "facial_hair_name",
                table: "profile",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "facial_hair_color",
                table: "profile",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "eye_color",
                table: "profile",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "age",
                table: "profile",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "BorgProfile_spawn_priority",
                table: "profile",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_type",
                table: "profile",
                type: "TEXT",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "humanoid_profile_id",
                table: "antag",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_trait_humanoid_profile_id",
                table: "trait",
                column: "humanoid_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_antag_humanoid_profile_id",
                table: "antag",
                column: "humanoid_profile_id");

            migrationBuilder.AddForeignKey(
                name: "FK_antag_profile_humanoid_profile_id",
                table: "antag",
                column: "humanoid_profile_id",
                principalTable: "profile",
                principalColumn: "profile_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_job_profile_humanoid_profile_id",
                table: "job",
                column: "profile_id",
                principalTable: "profile",
                principalColumn: "profile_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_profile_role_loadout_profile_humanoid_profile_id",
                table: "profile_role_loadout",
                column: "profile_id",
                principalTable: "profile",
                principalColumn: "profile_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_trait_profile_humanoid_profile_id",
                table: "trait",
                column: "humanoid_profile_id",
                principalTable: "profile",
                principalColumn: "profile_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_antag_profile_humanoid_profile_id",
                table: "antag");

            migrationBuilder.DropForeignKey(
                name: "FK_job_profile_humanoid_profile_id",
                table: "job");

            migrationBuilder.DropForeignKey(
                name: "FK_profile_role_loadout_profile_humanoid_profile_id",
                table: "profile_role_loadout");

            migrationBuilder.DropForeignKey(
                name: "FK_trait_profile_humanoid_profile_id",
                table: "trait");

            migrationBuilder.DropIndex(
                name: "IX_trait_humanoid_profile_id",
                table: "trait");

            migrationBuilder.DropIndex(
                name: "IX_antag_humanoid_profile_id",
                table: "antag");

            migrationBuilder.DropColumn(
                name: "humanoid_profile_id",
                table: "trait");

            migrationBuilder.DropColumn(
                name: "BorgProfile_spawn_priority",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "profile_type",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "humanoid_profile_id",
                table: "antag");

            migrationBuilder.AlterColumn<string>(
                name: "species",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "spawn_priority",
                table: "profile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "skin_color",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sex",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "hair_name",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "hair_color",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "facial_hair_name",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "facial_hair_color",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "eye_color",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "age",
                table: "profile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_antag_profile_profile_id",
                table: "antag",
                column: "profile_id",
                principalTable: "profile",
                principalColumn: "profile_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_job_profile_profile_id",
                table: "job",
                column: "profile_id",
                principalTable: "profile",
                principalColumn: "profile_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_profile_role_loadout_profile_profile_id",
                table: "profile_role_loadout",
                column: "profile_id",
                principalTable: "profile",
                principalColumn: "profile_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_trait_profile_profile_id",
                table: "trait",
                column: "profile_id",
                principalTable: "profile",
                principalColumn: "profile_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

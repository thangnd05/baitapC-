using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Week1.Rbac.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolDomainAndStudentOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "student_id",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "course",
                columns: table => new
                {
                    course_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    course_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    course_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    credits = table.Column<short>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course", x => x.course_id);
                });

            migrationBuilder.CreateTable(
                name: "programme",
                columns: table => new
                {
                    programme_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    programme_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    programme_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    degree_level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    duration_years = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_programme", x => x.programme_id);
                });

            migrationBuilder.CreateTable(
                name: "student",
                columns: table => new
                {
                    student_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    programme_id = table.Column<long>(type: "bigint", nullable: false),
                    student_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    year_of_entry = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student", x => x.student_id);
                    table.ForeignKey(
                        name: "FK_student_programme_programme_id",
                        column: x => x.programme_id,
                        principalTable: "programme",
                        principalColumn: "programme_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_student_id",
                table: "users",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_course_code",
                table: "course",
                column: "course_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_programme_code",
                table: "programme",
                column: "programme_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_code",
                table: "student",
                column: "student_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_email",
                table: "student",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_programme_id",
                table: "student",
                column: "programme_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_student_student_id",
                table: "users",
                column: "student_id",
                principalTable: "student",
                principalColumn: "student_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_student_student_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "course");

            migrationBuilder.DropTable(
                name: "student");

            migrationBuilder.DropTable(
                name: "programme");

            migrationBuilder.DropIndex(
                name: "IX_users_student_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "student_id",
                table: "users");
        }
    }
}

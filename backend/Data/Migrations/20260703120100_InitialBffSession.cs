using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App1.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialBffSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bff_pending_logins",
                columns: table => new
                {
                    PendingId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    State = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CodeVerifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Prompt = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bff_pending_logins", x => x.PendingId);
                });

            migrationBuilder.CreateTable(
                name: "bff_sessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "text", nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "text", nullable: true),
                    EncryptedIdToken = table.Column<string>(type: "text", nullable: true),
                    AccessTokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PreferredUsername = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bff_sessions", x => x.SessionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bff_pending_logins_CreatedAt",
                table: "bff_pending_logins",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_bff_sessions_LastSeenAt",
                table: "bff_sessions",
                column: "LastSeenAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bff_pending_logins");

            migrationBuilder.DropTable(
                name: "bff_sessions");
        }
    }
}

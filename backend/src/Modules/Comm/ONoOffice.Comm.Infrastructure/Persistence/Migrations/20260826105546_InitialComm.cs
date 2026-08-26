using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ONoOffice.Comm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialComm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "comm");

            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "comm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    pair_key = table.Column<string>(type: "character varying(73)", maxLength: 73, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "comm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    sent_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    seq = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participants",
                schema: "comm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_read_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_participants_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "comm",
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_tenant_id_pair_key",
                schema: "comm",
                table: "conversations",
                columns: new[] { "tenant_id", "pair_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_id_seq",
                schema: "comm",
                table: "messages",
                columns: new[] { "conversation_id", "seq" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_messages_sender_user_id",
                schema: "comm",
                table: "messages",
                column: "sender_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_participants_conversation_id_user_id",
                schema: "comm",
                table: "participants",
                columns: new[] { "conversation_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participants_user_id",
                schema: "comm",
                table: "participants",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "messages",
                schema: "comm");

            migrationBuilder.DropTable(
                name: "participants",
                schema: "comm");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "comm");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karasu.ERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PosReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RefundMethod = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosReturns_Orders_OriginalOrderId",
                        column: x => x.OriginalOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosReturns_PosSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "PosSessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PosReturns_OriginalOrderId",
                table: "PosReturns",
                column: "OriginalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturns_SessionId",
                table: "PosReturns",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PosReturns_TenantId_OriginalOrderId",
                table: "PosReturns",
                columns: new[] { "TenantId", "OriginalOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_PosReturns_TenantId_SessionId_CreatedAt",
                table: "PosReturns",
                columns: new[] { "TenantId", "SessionId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PosReturns");
        }
    }
}

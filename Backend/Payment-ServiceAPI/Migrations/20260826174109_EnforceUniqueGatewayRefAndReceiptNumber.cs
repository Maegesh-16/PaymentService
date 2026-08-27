using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment_ServiceAPI.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueGatewayRefAndReceiptNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Receipts_ReceiptNumber",
                table: "Receipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_GatewayRef",
                table: "PaymentTransactions",
                column: "GatewayRef",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receipts_ReceiptNumber",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_GatewayRef",
                table: "PaymentTransactions");
        }
    }
}

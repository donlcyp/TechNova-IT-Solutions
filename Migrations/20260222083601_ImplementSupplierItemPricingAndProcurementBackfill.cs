using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class ImplementSupplierItemPricingAndProcurementBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "Supplier_Items",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "PHP");

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price",
                table: "Supplier_Items",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
UPDATE [Supplier_Items]
SET [currency_code] = 'PHP'
WHERE [currency_code] IS NULL OR LTRIM(RTRIM([currency_code])) = '';
");

            migrationBuilder.Sql(@"
;WITH item_match AS (
    SELECT
        p.[procurementID],
        si.[unit_price],
        si.[currency_code],
        ROW_NUMBER() OVER (PARTITION BY p.[procurementID] ORDER BY si.[supplier_itemID] DESC) AS rn
    FROM [Procurement] p
    LEFT JOIN [Supplier_Items] si
        ON si.[supplierID] = p.[supplierID]
       AND si.[item_name] = p.[item_name]
)
UPDATE p
SET
    p.[currency_code] = COALESCE(NULLIF(LTRIM(RTRIM(p.[currency_code])), ''), NULLIF(LTRIM(RTRIM(m.[currency_code])), ''), 'PHP'),
    p.[original_amount] = COALESCE(p.[original_amount], CASE WHEN m.[unit_price] > 0 AND p.[quantity] IS NOT NULL THEN ROUND(m.[unit_price] * p.[quantity], 2) END, 0)
FROM [Procurement] p
INNER JOIN item_match m ON p.[procurementID] = m.[procurementID] AND m.rn = 1;
");

            migrationBuilder.Sql(@"
UPDATE [Procurement]
SET [exchange_rate] = 1
WHERE [exchange_rate] IS NULL OR [exchange_rate] <= 0;
");

            migrationBuilder.Sql(@"
UPDATE [Procurement]
SET [converted_amount] = ROUND(COALESCE([original_amount], 0) * COALESCE([exchange_rate], 1), 2)
WHERE [converted_amount] IS NULL;
");

            migrationBuilder.AlterColumn<decimal>(
                name: "original_amount",
                table: "Procurement",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "exchange_rate",
                table: "Procurement",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 1m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "currency_code",
                table: "Procurement",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "PHP",
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "converted_amount",
                table: "Procurement",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "Supplier_Items");

            migrationBuilder.DropColumn(
                name: "unit_price",
                table: "Supplier_Items");

            migrationBuilder.AlterColumn<decimal>(
                name: "original_amount",
                table: "Procurement",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "exchange_rate",
                table: "Procurement",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)");

            migrationBuilder.AlterColumn<string>(
                name: "currency_code",
                table: "Procurement",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "converted_amount",
                table: "Procurement",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class HashAPItokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "token",
                table: "api_tokens",
                newName: "token_hash");

            migrationBuilder.RenameIndex(
                name: "IX_api_tokens_token",
                table: "api_tokens",
                newName: "IX_api_tokens_token_hash");
            
            migrationBuilder.Sql(
                $"""
                 UPDATE api_tokens SET token_hash = encode(digest(token_hash, 'sha256'), 'hex')
                 """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new InvalidOperationException("This migration cannot be reverted because token hashing is irreversible.");
        }
    }
}

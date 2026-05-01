using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISDOX.DMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDeletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
         name: "DeletedAt",
         table: "Users",
         type: "timestamp without time zone", 
         nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
         name: "DeletedAt",
         table: "Users");
        }
    }
}

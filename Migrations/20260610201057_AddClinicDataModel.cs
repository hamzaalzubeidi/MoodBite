using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoodBite.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicDataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clinics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DietitianId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    VisitType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.CheckConstraint("CK_Appointments_DurationMinutes", "[DurationMinutes] BETWEEN 5 AND 480");
                    table.CheckConstraint("CK_Appointments_Status", "[Status] IN ('scheduled', 'completed', 'cancelled', 'noShow')");
                    table.ForeignKey(
                        name: "FK_Appointments_AspNetUsers_DietitianId",
                        column: x => x.DietitianId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AuthorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NoteType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsSharedWithPatient = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    InvitationType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    InvitedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AcceptedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicInvitations", x => x.Id);
                    table.CheckConstraint("CK_ClinicInvitations_Status", "[Status] IN ('pending', 'accepted', 'revoked', 'expired')");
                    table.CheckConstraint("CK_ClinicInvitations_TargetRole", "[TargetRole] IS NULL OR [TargetRole] IN ('ClinicOwner', 'Dietitian', 'ClinicStaff')");
                    table.CheckConstraint("CK_ClinicInvitations_Type", "[InvitationType] IN ('patient', 'staff')");
                    table.ForeignKey(
                        name: "FK_ClinicInvitations_AspNetUsers_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicInvitations_AspNetUsers_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicInvitations_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvitedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicMembers", x => x.Id);
                    table.CheckConstraint("CK_ClinicMembers_Role", "[Role] IN ('ClinicOwner', 'Dietitian', 'ClinicStaff')");
                    table.ForeignKey(
                        name: "FK_ClinicMembers_AspNetUsers_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicMembers_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicPatients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PrimaryDietitianId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ConsentGranted = table.Column<bool>(type: "bit", nullable: false),
                    ConsentGrantedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicPatients", x => x.Id);
                    table.CheckConstraint("CK_ClinicPatients_Status", "[Status] IN ('pending', 'active', 'archived', 'discharged')");
                    table.ForeignKey(
                        name: "FK_ClinicPatients_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicPatients_AspNetUsers_PrimaryDietitianId",
                        column: x => x.PrimaryDietitianId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicPatients_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClinicId_StartsAt",
                table: "Appointments",
                columns: new[] { "ClinicId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClinicId_Status",
                table: "Appointments",
                columns: new[] { "ClinicId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DietitianId_StartsAt",
                table: "Appointments",
                columns: new[] { "DietitianId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId_StartsAt",
                table: "Appointments",
                columns: new[] { "PatientId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_AuthorId",
                table: "ClinicalNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_ClinicId_NoteType_IsArchived",
                table: "ClinicalNotes",
                columns: new[] { "ClinicId", "NoteType", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_ClinicId_PatientId_CreatedAt",
                table: "ClinicalNotes",
                columns: new[] { "ClinicId", "PatientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_PatientId",
                table: "ClinicalNotes",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvitations_AcceptedByUserId",
                table: "ClinicInvitations",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvitations_ClinicId_Email_Status",
                table: "ClinicInvitations",
                columns: new[] { "ClinicId", "Email", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvitations_ExpiresAt",
                table: "ClinicInvitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvitations_InvitedByUserId",
                table: "ClinicInvitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvitations_TokenHash",
                table: "ClinicInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMembers_ClinicId_Role_IsActive",
                table: "ClinicMembers",
                columns: new[] { "ClinicId", "Role", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMembers_ClinicId_UserId",
                table: "ClinicMembers",
                columns: new[] { "ClinicId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMembers_InvitedByUserId",
                table: "ClinicMembers",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMembers_UserId",
                table: "ClinicMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicPatients_ClinicId_PatientId",
                table: "ClinicPatients",
                columns: new[] { "ClinicId", "PatientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicPatients_ClinicId_Status",
                table: "ClinicPatients",
                columns: new[] { "ClinicId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicPatients_PatientId",
                table: "ClinicPatients",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicPatients_PrimaryDietitianId",
                table: "ClinicPatients",
                column: "PrimaryDietitianId");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_IsActive",
                table: "Clinics",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Slug",
                table: "Clinics",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "ClinicalNotes");

            migrationBuilder.DropTable(
                name: "ClinicInvitations");

            migrationBuilder.DropTable(
                name: "ClinicMembers");

            migrationBuilder.DropTable(
                name: "ClinicPatients");

            migrationBuilder.DropTable(
                name: "Clinics");
        }
    }
}

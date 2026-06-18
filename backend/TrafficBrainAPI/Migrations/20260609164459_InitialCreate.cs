using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TrafficBrainAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmsAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JunctionId = table.Column<int>(type: "integer", nullable: true),
                    RecipientPhone = table.Column<string>(type: "text", nullable: false),
                    RecipientName = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    AlertType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    District = table.Column<string>(type: "text", nullable: false),
                    BadgeNumber = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    Condition = table.Column<string>(type: "text", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    Humidity = table.Column<double>(type: "double precision", nullable: false),
                    WindSpeed = table.Column<double>(type: "double precision", nullable: false),
                    Visibility = table.Column<double>(type: "double precision", nullable: false),
                    WeatherImpact = table.Column<string>(type: "text", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherConditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Districts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Districts_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    JunctionId = table.Column<int>(type: "integer", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    UserRole = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Junctions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    CityId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RoadNames = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    ControlMode = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    LastDataReceived = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Junctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Junctions_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Junctions_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JunctionId = table.Column<int>(type: "integer", nullable: false),
                    AlertType = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    AcknowledgedBy = table.Column<string>(type: "text", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_Junctions_JunctionId",
                        column: x => x.JunctionId,
                        principalTable: "Junctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JunctionId = table.Column<int>(type: "integer", nullable: false),
                    VehicleType = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    CorridorJunctions = table.Column<string>(type: "text", nullable: false),
                    CorridorCleared = table.Column<bool>(type: "boolean", nullable: false),
                    TimeSavedMinutes = table.Column<double>(type: "double precision", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyEvents_Junctions_JunctionId",
                        column: x => x.JunctionId,
                        principalTable: "Junctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JunctionId = table.Column<int>(type: "integer", nullable: false),
                    ReportedById = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedBy = table.Column<string>(type: "text", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Junctions_JunctionId",
                        column: x => x.JunctionId,
                        principalTable: "Junctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Incidents_Users_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "JunctionOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JunctionId = table.Column<int>(type: "integer", nullable: false),
                    OfficerId = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    ForcedDirection = table.Column<string>(type: "text", nullable: false),
                    CustomGreenDuration = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JunctionOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JunctionOverrides_Junctions_JunctionId",
                        column: x => x.JunctionId,
                        principalTable: "Junctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JunctionOverrides_Users_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lanes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JunctionId = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    LaneNumber = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lanes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lanes_Junctions_JunctionId",
                        column: x => x.JunctionId,
                        principalTable: "Junctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JunctionId = table.Column<int>(type: "integer", nullable: false),
                    ControlMode = table.Column<string>(type: "text", nullable: false),
                    AverageWaitTime = table.Column<double>(type: "double precision", nullable: false),
                    MaxWaitTime = table.Column<double>(type: "double precision", nullable: false),
                    TotalVehiclesProcessed = table.Column<int>(type: "integer", nullable: false),
                    ThroughputPerHour = table.Column<double>(type: "double precision", nullable: false),
                    FuelWasteEstimateLitres = table.Column<double>(type: "double precision", nullable: false),
                    CO2SavedKg = table.Column<double>(type: "double precision", nullable: false),
                    CongestionScore = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceMetrics_Junctions_JunctionId",
                        column: x => x.JunctionId,
                        principalTable: "Junctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OfficerId = table.Column<int>(type: "integer", nullable: false),
                    JunctionId = table.Column<int>(type: "integer", nullable: false),
                    ShiftStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShiftEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalOverrides = table.Column<int>(type: "integer", nullable: false),
                    TotalIncidentsHandled = table.Column<int>(type: "integer", nullable: false),
                    HandoverNotes = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftRecords_Junctions_JunctionId",
                        column: x => x.JunctionId,
                        principalTable: "Junctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftRecords_Users_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignalPhases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JunctionId = table.Column<int>(type: "integer", nullable: false),
                    PhaseNumber = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    GreenDuration = table.Column<int>(type: "integer", nullable: false),
                    RedDuration = table.Column<int>(type: "integer", nullable: false),
                    YellowDuration = table.Column<int>(type: "integer", nullable: false),
                    QueueLengthAtChange = table.Column<int>(type: "integer", nullable: false),
                    WaitTimeBefore = table.Column<double>(type: "double precision", nullable: false),
                    WaitTimeAfter = table.Column<double>(type: "double precision", nullable: false),
                    TriggeredBy = table.Column<string>(type: "text", nullable: false),
                    ControlMode = table.Column<string>(type: "text", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalPhases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignalPhases_Junctions_JunctionId",
                        column: x => x.JunctionId,
                        principalTable: "Junctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrafficPredictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JunctionId = table.Column<int>(type: "integer", nullable: false),
                    PredictedFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedVehicleCount = table.Column<int>(type: "integer", nullable: false),
                    CongestionScore = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    PredictionBasis = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficPredictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrafficPredictions_Junctions_JunctionId",
                        column: x => x.JunctionId,
                        principalTable: "Junctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViolationEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JunctionId = table.Column<int>(type: "integer", nullable: false),
                    ViolationType = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    Speed = table.Column<double>(type: "double precision", nullable: true),
                    ImagePath = table.Column<string>(type: "text", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViolationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViolationEvents_Junctions_JunctionId",
                        column: x => x.JunctionId,
                        principalTable: "Junctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleCounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JunctionId = table.Column<int>(type: "integer", nullable: false),
                    LaneId = table.Column<int>(type: "integer", nullable: true),
                    Cars = table.Column<int>(type: "integer", nullable: false),
                    Motorcycles = table.Column<int>(type: "integer", nullable: false),
                    Buses = table.Column<int>(type: "integer", nullable: false),
                    Trucks = table.Column<int>(type: "integer", nullable: false),
                    Pedestrians = table.Column<int>(type: "integer", nullable: false),
                    TotalVehicles = table.Column<int>(type: "integer", nullable: false),
                    AverageSpeed = table.Column<double>(type: "double precision", nullable: false),
                    CongestionScore = table.Column<int>(type: "integer", nullable: false),
                    Lane = table.Column<string>(type: "text", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleCounts_Junctions_JunctionId",
                        column: x => x.JunctionId,
                        principalTable: "Junctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VehicleCounts_Lanes_LaneId",
                        column: x => x.LaneId,
                        principalTable: "Lanes",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Regions",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, true, "Northern" },
                    { 2, true, "Central" },
                    { 3, true, "Southern" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "BadgeNumber", "CreatedAt", "District", "Email", "FullName", "IsActive", "LastLogin", "PasswordHash", "PhoneNumber", "RefreshToken", "RefreshTokenExpiry", "Role" },
                values: new object[] { 1, "TB-ADMIN-001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lilongwe", "admin@trafficbrain.mw", "System Administrator", true, null, "$2a$11$IHlZ4GMAExqpiNlIKAj2HukmMj.fuvtjXHYFjcjz9ZwHWfS1i/aaq", "+265999000001", null, null, "Admin" });

            migrationBuilder.InsertData(
                table: "Districts",
                columns: new[] { "Id", "IsActive", "Name", "RegionId" },
                values: new object[,]
                {
                    { 1, true, "Lilongwe", 2 },
                    { 2, true, "Blantyre", 3 },
                    { 3, true, "Mzimba", 1 },
                    { 4, true, "Zomba", 3 }
                });

            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "Id", "DistrictId", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, 1, true, "Lilongwe" },
                    { 2, 2, true, "Blantyre" },
                    { 3, 3, true, "Mzuzu" },
                    { 4, 4, true, "Zomba" }
                });

            migrationBuilder.InsertData(
                table: "Junctions",
                columns: new[] { "Id", "CityId", "ControlMode", "CreatedAt", "DistrictId", "IsActive", "IsOnline", "LastDataReceived", "Latitude", "Longitude", "Name", "RoadNames" },
                values: new object[,]
                {
                    { 1, 1, "Auto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, false, null, -13.9626, 33.774099999999997, "Kamuzu Procession Rd Junction", "Kamuzu Procession Rd / Kenyatta Rd" },
                    { 2, 1, "Auto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, false, null, -13.9689, 33.787799999999997, "Glyn Jones Rd Junction", "Glyn Jones Rd / Paul Kagame Rd" },
                    { 3, 1, "Auto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, false, null, -13.974500000000001, 33.781199999999998, "Area 18 Junction", "Presidential Way / Area 18 Rd" },
                    { 4, 2, "Auto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, false, null, -15.786099999999999, 35.005800000000001, "Chipembere Highway Junction", "Chipembere Highway / Victoria Ave" },
                    { 5, 2, "Auto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, false, null, -15.7925, 35.002099999999999, "Kamuzu Highway Junction", "Kamuzu Highway / Kidney Crescent" },
                    { 6, 3, "Auto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, false, null, -11.4657, 34.0199, "Mzuzu City Centre Junction", "M1 Road / Orton Chirwa Ave" },
                    { 7, 4, "Auto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, false, null, -15.3833, 35.316699999999997, "Zomba Main Junction", "M3 Road / College Rd" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_JunctionId",
                table: "Alerts",
                column: "JunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_DistrictId",
                table: "Cities",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Districts_RegionId",
                table: "Districts",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyEvents_JunctionId",
                table: "EmergencyEvents",
                column: "JunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_JunctionId",
                table: "Incidents",
                column: "JunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ReportedById",
                table: "Incidents",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_JunctionOverrides_JunctionId",
                table: "JunctionOverrides",
                column: "JunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_JunctionOverrides_OfficerId",
                table: "JunctionOverrides",
                column: "OfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_Junctions_CityId",
                table: "Junctions",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Junctions_DistrictId",
                table: "Junctions",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_JunctionId",
                table: "Lanes",
                column: "JunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_JunctionId",
                table: "PerformanceMetrics",
                column: "JunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRecords_JunctionId",
                table: "ShiftRecords",
                column: "JunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRecords_OfficerId",
                table: "ShiftRecords",
                column: "OfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalPhases_JunctionId",
                table: "SignalPhases",
                column: "JunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_UserId",
                table: "SystemLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficPredictions_JunctionId",
                table: "TrafficPredictions",
                column: "JunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCounts_JunctionId",
                table: "VehicleCounts",
                column: "JunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCounts_LaneId",
                table: "VehicleCounts",
                column: "LaneId");

            migrationBuilder.CreateIndex(
                name: "IX_ViolationEvents_JunctionId",
                table: "ViolationEvents",
                column: "JunctionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "EmergencyEvents");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "JunctionOverrides");

            migrationBuilder.DropTable(
                name: "PerformanceMetrics");

            migrationBuilder.DropTable(
                name: "ShiftRecords");

            migrationBuilder.DropTable(
                name: "SignalPhases");

            migrationBuilder.DropTable(
                name: "SmsAlerts");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropTable(
                name: "TrafficPredictions");

            migrationBuilder.DropTable(
                name: "VehicleCounts");

            migrationBuilder.DropTable(
                name: "ViolationEvents");

            migrationBuilder.DropTable(
                name: "WeatherConditions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Lanes");

            migrationBuilder.DropTable(
                name: "Junctions");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "Districts");

            migrationBuilder.DropTable(
                name: "Regions");
        }
    }
}

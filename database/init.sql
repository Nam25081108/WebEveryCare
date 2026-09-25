CREATE EXTENSION IF NOT EXISTS postgis;
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS postgis;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE service_groups (
        "Id" uuid NOT NULL,
        "Name" character varying(150) NOT NULL,
        "Slug" character varying(100) NOT NULL,
        "Description" text NOT NULL,
        "IconName" text,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "IsProfessional" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_service_groups" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE users (
        "Id" uuid NOT NULL,
        "FullName" character varying(150) NOT NULL,
        "Phone" character varying(20) NOT NULL,
        "Email" character varying(255),
        "PasswordHash" character varying(500) NOT NULL,
        "Role" character varying(30) NOT NULL,
        "Status" character varying(30) NOT NULL,
        "LastLoginAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE professional_pricing_rules (
        "Id" uuid NOT NULL,
        "ServiceGroupId" uuid NOT NULL,
        "BuildingType" character varying(30) NOT NULL,
        "BuildingCondition" character varying(30) NOT NULL,
        "AreaTier" character varying(30) NOT NULL,
        "MinimumAreaSquareMeters" numeric(8,2) NOT NULL,
        "MaximumAreaSquareMeters" numeric(8,2) NOT NULL,
        "FixedPrice" numeric(14,2),
        "PricePerSquareMeter" numeric(14,2),
        "FurnishedMultiplier" numeric(5,2) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_professional_pricing_rules" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_professional_pricing_rules_service_groups_ServiceGroupId" FOREIGN KEY ("ServiceGroupId") REFERENCES service_groups ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE service_packages (
        "Id" uuid NOT NULL,
        "ServiceGroupId" uuid NOT NULL,
        "Name" text NOT NULL,
        "Slug" text NOT NULL,
        "Description" text,
        "Price" numeric(14,2) NOT NULL,
        "DurationMinutes" integer,
        "RequiredWorkers" integer NOT NULL,
        "MaximumAreaSquareMeters" numeric(8,2),
        "MaximumRooms" integer,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_service_packages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_service_packages_service_groups_ServiceGroupId" FOREIGN KEY ("ServiceGroupId") REFERENCES service_groups ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE service_process_steps (
        "Id" uuid NOT NULL,
        "ServiceGroupId" uuid NOT NULL,
        "Title" text NOT NULL,
        "Description" text,
        "DisplayOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_service_process_steps" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_service_process_steps_service_groups_ServiceGroupId" FOREIGN KEY ("ServiceGroupId") REFERENCES service_groups ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE service_tools (
        "Id" uuid NOT NULL,
        "ServiceGroupId" uuid NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "ImageUrl" text,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_service_tools" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_service_tools_service_groups_ServiceGroupId" FOREIGN KEY ("ServiceGroupId") REFERENCES service_groups ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE addresses (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Label" character varying(80) NOT NULL,
        "FullAddress" character varying(500) NOT NULL,
        "WardCode" character varying(30),
        "WardName" character varying(150),
        "CityName" character varying(100) NOT NULL,
        "Note" text,
        "Location" geography (point),
        "IsDefault" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_addresses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_addresses_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE customer_profiles (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "AvatarUrl" text,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_customer_profiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_customer_profiles_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE partner_profiles (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "PartnerType" character varying(30) NOT NULL,
        "VerificationStatus" character varying(30) NOT NULL,
        "IdentityNumber" text,
        "IdentityFrontUrl" text,
        "IdentityBackUrl" text,
        "TeamName" text,
        "TeamSize" integer NOT NULL,
        "IsAvailable" boolean NOT NULL,
        "CurrentLocation" geography (point),
        "LocationUpdatedAt" timestamp with time zone,
        "AverageRating" numeric(3,2) NOT NULL,
        "CompletedBookings" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_partner_profiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_partner_profiles_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE service_work_items (
        "Id" uuid NOT NULL,
        "ServicePackageId" uuid NOT NULL,
        "Area" character varying(30) NOT NULL,
        "Description" text NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_service_work_items" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_service_work_items_service_packages_ServicePackageId" FOREIGN KEY ("ServicePackageId") REFERENCES service_packages ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE bookings (
        "Id" uuid NOT NULL,
        "Code" character varying(30) NOT NULL,
        "CustomerId" uuid NOT NULL,
        "AddressId" uuid NOT NULL,
        "AddressSnapshot" text NOT NULL,
        "LocationSnapshot" geography (point),
        "ServiceGroupId" uuid NOT NULL,
        "ServicePackageId" uuid,
        "Status" character varying(40) NOT NULL,
        "CleanerSelectionMode" character varying(40) NOT NULL,
        "ScheduledStartAt" timestamp with time zone NOT NULL,
        "ScheduledEndAt" timestamp with time zone,
        "RequiredWorkers" integer NOT NULL,
        "BuildingType" character varying(30),
        "BuildingCondition" character varying(30),
        "HasFurniture" boolean NOT NULL,
        "AreaSquareMeters" numeric(8,2),
        "BasePrice" numeric(14,2) NOT NULL,
        "SelectionFee" numeric(14,2) NOT NULL,
        "ExtraChargeTotal" numeric(14,2) NOT NULL,
        "CancellationFee" numeric(14,2) NOT NULL,
        "EstimatedTotal" numeric(14,2) NOT NULL,
        "AssignedPartnerId" uuid,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "CancelledAt" timestamp with time zone,
        "CancellationReason" text,
        "IsRecurring" boolean NOT NULL,
        "RecurrenceRule" text,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_bookings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_bookings_addresses_AddressId" FOREIGN KEY ("AddressId") REFERENCES addresses ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_bookings_partner_profiles_AssignedPartnerId" FOREIGN KEY ("AssignedPartnerId") REFERENCES partner_profiles ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_bookings_service_groups_ServiceGroupId" FOREIGN KEY ("ServiceGroupId") REFERENCES service_groups ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_bookings_service_packages_ServicePackageId" FOREIGN KEY ("ServicePackageId") REFERENCES service_packages ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_bookings_users_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE favorite_partners (
        "Id" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "PartnerProfileId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_favorite_partners" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_favorite_partners_partner_profiles_PartnerProfileId" FOREIGN KEY ("PartnerProfileId") REFERENCES partner_profiles ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_favorite_partners_users_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE partner_service_capabilities (
        "PartnerProfileId" uuid NOT NULL,
        "ServiceGroupId" uuid NOT NULL,
        CONSTRAINT "PK_partner_service_capabilities" PRIMARY KEY ("PartnerProfileId", "ServiceGroupId"),
        CONSTRAINT "FK_partner_service_capabilities_partner_profiles_PartnerProfil~" FOREIGN KEY ("PartnerProfileId") REFERENCES partner_profiles ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_partner_service_capabilities_service_groups_ServiceGroupId" FOREIGN KEY ("ServiceGroupId") REFERENCES service_groups ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE partner_team_members (
        "Id" uuid NOT NULL,
        "PartnerProfileId" uuid NOT NULL,
        "FullName" text NOT NULL,
        "Phone" text NOT NULL,
        "IdentityNumber" text,
        "IdentityFrontUrl" text,
        "IdentityBackUrl" text,
        "VerificationStatus" character varying(30) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_partner_team_members" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_partner_team_members_partner_profiles_PartnerProfileId" FOREIGN KEY ("PartnerProfileId") REFERENCES partner_profiles ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE booking_assignments (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "PartnerProfileId" uuid NOT NULL,
        "Status" character varying(30) NOT NULL,
        "InvitedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "RespondedAt" timestamp with time zone,
        "DistanceMetersAtInvitation" double precision,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_booking_assignments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_booking_assignments_bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES bookings ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_booking_assignments_partner_profiles_PartnerProfileId" FOREIGN KEY ("PartnerProfileId") REFERENCES partner_profiles ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE booking_extra_charges (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "Description" text NOT NULL,
        "Amount" numeric(14,2) NOT NULL,
        "Status" text NOT NULL,
        "CustomerRespondedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_booking_extra_charges" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_booking_extra_charges_bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES bookings ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE booking_photos (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "Type" text NOT NULL,
        "ImageUrl" text NOT NULL,
        "UploadedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_booking_photos" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_booking_photos_bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES bookings ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE payments (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "Method" text NOT NULL,
        "Status" text NOT NULL,
        "Amount" numeric(14,2) NOT NULL,
        "BankTransactionReference" text,
        "PaidAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_payments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_payments_bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES bookings ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE TABLE reviews (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "PartnerProfileId" uuid NOT NULL,
        "Rating" integer NOT NULL,
        "Comment" text,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_reviews" PRIMARY KEY ("Id"),
        CONSTRAINT ck_reviews_rating CHECK ("Rating" BETWEEN 1 AND 5),
        CONSTRAINT "FK_reviews_bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES bookings ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_addresses_Location" ON addresses USING gist ("Location");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_addresses_UserId" ON addresses ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_booking_assignments_BookingId_PartnerProfileId" ON booking_assignments ("BookingId", "PartnerProfileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_booking_assignments_PartnerProfileId" ON booking_assignments ("PartnerProfileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_booking_extra_charges_BookingId" ON booking_extra_charges ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_booking_photos_BookingId" ON booking_photos ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_bookings_AddressId" ON bookings ("AddressId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_bookings_AssignedPartnerId" ON bookings ("AssignedPartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_bookings_Code" ON bookings ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_bookings_CustomerId" ON bookings ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_bookings_ServiceGroupId" ON bookings ("ServiceGroupId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_bookings_ServicePackageId" ON bookings ("ServicePackageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_bookings_Status_ScheduledStartAt" ON bookings ("Status", "ScheduledStartAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_customer_profiles_UserId" ON customer_profiles ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_favorite_partners_CustomerId_PartnerProfileId" ON favorite_partners ("CustomerId", "PartnerProfileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_favorite_partners_PartnerProfileId" ON favorite_partners ("PartnerProfileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_partner_profiles_CurrentLocation" ON partner_profiles USING gist ("CurrentLocation");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_partner_profiles_IsAvailable_VerificationStatus" ON partner_profiles ("IsAvailable", "VerificationStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_partner_profiles_UserId" ON partner_profiles ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_partner_service_capabilities_ServiceGroupId" ON partner_service_capabilities ("ServiceGroupId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_partner_team_members_PartnerProfileId" ON partner_team_members ("PartnerProfileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_payments_BookingId" ON payments ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_professional_pricing_rules_ServiceGroupId_BuildingType_Buil~" ON professional_pricing_rules ("ServiceGroupId", "BuildingType", "BuildingCondition", "AreaTier");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_reviews_BookingId" ON reviews ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_service_groups_Slug" ON service_groups ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_service_packages_ServiceGroupId_Slug" ON service_packages ("ServiceGroupId", "Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_service_process_steps_ServiceGroupId" ON service_process_steps ("ServiceGroupId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_service_tools_ServiceGroupId" ON service_tools ("ServiceGroupId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE INDEX "IX_service_work_items_ServicePackageId" ON service_work_items ("ServicePackageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_Email" ON users ("Email") WHERE "Email" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_Phone" ON users ("Phone");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916073155_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260916073155_InitialCreate', '8.0.22');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916074320_AddBookingNotifications') THEN
    CREATE TABLE partner_notifications (
        "Id" uuid NOT NULL,
        "PartnerUserId" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "Type" character varying(50) NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Message" text NOT NULL,
        "IsRead" boolean NOT NULL,
        "ReadAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_partner_notifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_partner_notifications_bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES bookings ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_partner_notifications_users_PartnerUserId" FOREIGN KEY ("PartnerUserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916074320_AddBookingNotifications') THEN
    CREATE INDEX "IX_partner_notifications_BookingId" ON partner_notifications ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916074320_AddBookingNotifications') THEN
    CREATE INDEX "IX_partner_notifications_PartnerUserId_IsRead_CreatedAt" ON partner_notifications ("PartnerUserId", "IsRead", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260916074320_AddBookingNotifications') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260916074320_AddBookingNotifications', '8.0.22');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    ALTER TABLE partner_profiles ADD "ApprovalEmailError" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    ALTER TABLE partner_profiles ADD "ApprovalEmailSentAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    ALTER TABLE partner_profiles ADD "RejectionReason" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    ALTER TABLE partner_profiles ADD "ReviewedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    ALTER TABLE partner_profiles ADD "ServiceAddress" character varying(500) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    ALTER TABLE partner_profiles ADD "ServiceLocation" geography (point);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    ALTER TABLE partner_profiles ADD "ServiceRadiusKilometers" numeric(5,2) NOT NULL DEFAULT 10.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    UPDATE partner_profiles
    SET "ServiceLocation" = "CurrentLocation",
        "ServiceAddress" = CASE WHEN "ServiceAddress" = '' THEN 'Địa chỉ đã đăng ký' ELSE "ServiceAddress" END
    WHERE "ServiceLocation" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    CREATE TABLE partner_availability_overrides (
        "Id" uuid NOT NULL,
        "PartnerProfileId" uuid NOT NULL,
        "Date" date NOT NULL,
        "IsUnavailable" boolean NOT NULL,
        "StartTime" time without time zone,
        "EndTime" time without time zone,
        "Note" text,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_partner_availability_overrides" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_partner_availability_overrides_partner_profiles_PartnerProf~" FOREIGN KEY ("PartnerProfileId") REFERENCES partner_profiles ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    CREATE TABLE partner_availability_rules (
        "Id" uuid NOT NULL,
        "PartnerProfileId" uuid NOT NULL,
        "DayOfWeek" integer NOT NULL,
        "StartTime" time without time zone NOT NULL,
        "EndTime" time without time zone NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_partner_availability_rules" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_partner_availability_rules_partner_profiles_PartnerProfileId" FOREIGN KEY ("PartnerProfileId") REFERENCES partner_profiles ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    CREATE TABLE partner_sessions (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TokenHash" character varying(128) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "RevokedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_partner_sessions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_partner_sessions_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    INSERT INTO partner_availability_rules
        ("Id", "PartnerProfileId", "DayOfWeek", "StartTime", "EndTime", "IsActive", "CreatedAt", "IsDeleted")
    SELECT gen_random_uuid(), p."Id", d.day, TIME '08:00', TIME '17:00', TRUE, CURRENT_TIMESTAMP, FALSE
    FROM partner_profiles p
    CROSS JOIN (VALUES (1), (2), (3), (4), (5), (6)) AS d(day)
    WHERE p."VerificationStatus" = 'Approved';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    CREATE INDEX "IX_partner_profiles_ServiceLocation" ON partner_profiles USING gist ("ServiceLocation");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    CREATE UNIQUE INDEX "IX_partner_availability_overrides_PartnerProfileId_Date" ON partner_availability_overrides ("PartnerProfileId", "Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    CREATE INDEX "IX_partner_availability_rules_PartnerProfileId_DayOfWeek" ON partner_availability_rules ("PartnerProfileId", "DayOfWeek");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    CREATE UNIQUE INDEX "IX_partner_sessions_TokenHash" ON partner_sessions ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    CREATE INDEX "IX_partner_sessions_UserId_ExpiresAt" ON partner_sessions ("UserId", "ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917021401_CompletePartnerWorkflow') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260917021401_CompletePartnerWorkflow', '8.0.22');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917040950_ExpandEveryCareServiceCatalog') THEN
    ALTER TABLE service_groups ADD "CategorySlug" character varying(50) NOT NULL DEFAULT 'cleaning';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917040950_ExpandEveryCareServiceCatalog') THEN
    ALTER TABLE service_groups ADD "IsComingSoon" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917040950_ExpandEveryCareServiceCatalog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260917040950_ExpandEveryCareServiceCatalog', '8.0.22');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917073630_RenameRoomCleaningService') THEN
    UPDATE service_groups SET "Name" = 'Dọn dẹp nhà cửa' WHERE "Slug" = 've-sinh-phong-le';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917073630_RenameRoomCleaningService') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260917073630_RenameRoomCleaningService', '8.0.22');
    END IF;
END $EF$;
COMMIT;


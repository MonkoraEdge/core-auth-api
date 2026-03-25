----------------------------------------------------------------------------------------------------------------------------------
--#table 02 mt_users
CREATE TABLE public.mt_users (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    tenant_id UUID,

    display_name TEXT,
    locale TEXT,          -- OIDC standard claim: BCP47 language tag (e.g. en-US, th-TH)
    zoneinfo TEXT,        -- OIDC standard claim: IANA timezone (e.g. Asia/Bangkok)

    phone_number TEXT, -- UNIQUE
    phone_verified BOOLEAN NOT NULL DEFAULT FALSE,
    email TEXT NOT NULL, -- UNIQUE
	email_verified BOOLEAN NOT NULL DEFAULT FALSE,

	status TEXT NOT NULL DEFAULT 'INACTIVE', -- ENUM --> INACTIVE, ACTIVE, PENDING_VERIFICATION, SUSPENDED, LOCKED, DELETED, ARCHIVED

	registration_source TEXT NOT NULL DEFAULT 'LOCAL',
	-- ช่องทางที่สมัครครั้งแรก:
	-- 'LOCAL'           = สมัครด้วย email + password ธรรมดา
	-- 'PHONE'           = สมัครด้วยเบอร์โทรศัพท์
	-- 'ADMIN'           = Admin สร้างให้
	-- 'INVITATION'      = สมัครผ่าน invitation link
	-- 'API'             = สร้างผ่าน API (machine/service)
	-- หรือ provider_code จาก mt_providers (เช่น 'GOOGLE', 'FACEBOOK', 'LINE', 'GITHUB', 'APPLE')
	
    last_login_at TIMESTAMPTZ,
    last_activity_at TIMESTAMPTZ,
	last_password_changed_at TIMESTAMPTZ,

	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,

	-- Primary Key
    CONSTRAINT pk_mt_users PRIMARY KEY (id),

	-- Foreign Key
    CONSTRAINT fk_mt_users_mt_tenants FOREIGN KEY (tenant_id)
	REFERENCES public.mt_tenants(id) ON DELETE SET NULL,
	
	CONSTRAINT chk_mt_users_status
	CHECK (status IN (
	'INACTIVE',
	'ACTIVE',
	'PENDING_VERIFICATION',
	'SUSPENDED',
	'LOCKED',
	'DELETED',
	'ARCHIVED'
	))	
);

-- Unique
CREATE UNIQUE INDEX uq_mt_users_email ON public.mt_users (LOWER(email)) WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX uq_mt_users_phone_number ON public.mt_users (phone_number) WHERE deleted_at IS NULL AND phone_number IS NOT NULL;

-- Index
-- filter is_active
CREATE INDEX idx_mt_users_is_active ON public.mt_users (is_active) WHERE deleted_at IS NULL;

-- filter created_at
CREATE INDEX idx_mt_users_created_at ON public.mt_users (created_at DESC) WHERE deleted_at IS NULL;

-- filter tenant id
CREATE INDEX idx_mt_users_tenant_id ON public.mt_users (tenant_id) WHERE deleted_at IS NULL;

-- filter status
CREATE INDEX idx_mt_users_status ON public.mt_users (status) WHERE deleted_at IS NULL;

-- filter registration_source
CREATE INDEX idx_mt_users_registration_source ON public.mt_users (registration_source) WHERE deleted_at IS NULL;


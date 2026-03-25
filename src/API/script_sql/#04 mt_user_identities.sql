----------------------------------------------------------------------------------------------------------------------------------
--#table 04 mt_user_identities
CREATE TABLE public.mt_user_identities (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL, -- FK mt_users

    provider_type TEXT NOT NULL DEFAULT 'LOCAL', -- ENUM --> LOCAL, SSO, OTHER
    username TEXT NOT NULL, -- UNIQUE
    password_hash TEXT,
	password_algo TEXT,
	password_updated_at TIMESTAMPTZ,
	failed_attempts INTEGER NOT NULL DEFAULT 0,
	locked_until TIMESTAMPTZ,

	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,

	-- Primary Key
    CONSTRAINT pk_mt_user_identities PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_mt_user_identities_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE,

	CONSTRAINT chk_mt_user_identities_provider_type
	CHECK (provider_type IN (
	'LOCAL',
	'SSO',
	'OTHER'
	))		
);

-- Unique
CREATE UNIQUE INDEX uq_mt_user_identities_username ON public.mt_user_identities (LOWER(username)) WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX uq_mt_user_identities_user_id ON public.mt_user_identities (user_id) WHERE provider_type = 'LOCAL' AND deleted_at IS NULL;

-- Index
-- query user_id + provider_type
CREATE INDEX idx_mt_user_identities_user_provider_type ON public.mt_user_identities (user_id, provider_type) WHERE deleted_at IS NULL;

-- query user_id + is_active
CREATE INDEX idx_mt_user_identities_user_is_active ON public.mt_user_identities (user_id, is_active) WHERE deleted_at IS NULL;

-- query user_id + created_at
CREATE INDEX idx_mt_user_identities_user_created_at ON public.mt_user_identities (user_id, created_at DESC) WHERE deleted_at IS NULL;

-- filter user_id
CREATE INDEX idx_mt_user_identities_user_id ON public.mt_user_identities (user_id) WHERE deleted_at IS NULL;

-- filter is_active
CREATE INDEX idx_mt_user_identities_is_active ON public.mt_user_identities (is_active) WHERE deleted_at IS NULL;

-- filter created_at
CREATE INDEX idx_mt_user_identities_created_at ON public.mt_user_identities (created_at DESC) WHERE deleted_at IS NULL;

-- filter provider_type
CREATE INDEX idx_mt_user_identities_provider_type ON public.mt_user_identities (provider_type) WHERE deleted_at IS NULL;


----------------------------------------------------------------------------------------------------------------------------------
--#table 12
CREATE TABLE public.mt_scopes (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    scope_name TEXT NOT NULL, -- UNIQUE
    display_name TEXT,
    scope_type TEXT NOT NULL DEFAULT 'CUSTOM', -- ENUM --> OPENID, PROFILE, EMAIL, PHONE, ADDRESS, OFFLINE_ACCESS, CUSTOM
    claims TEXT[], -- OIDC claims included when this scope is granted (e.g. {sub, name, email})

    is_system_scope BOOLEAN NOT NULL DEFAULT FALSE,
	is_active BOOLEAN NOT NULL DEFAULT TRUE,
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,

    CONSTRAINT pk_mt_scopes PRIMARY KEY (id),

    CONSTRAINT chk_mt_scopes_scope_type
    CHECK (scope_type IN (
        'OPENID',
        'PROFILE',
        'EMAIL',
        'PHONE',
        'ADDRESS',
        'OFFLINE_ACCESS',
        'CUSTOM'
    ))
);


-- Unique
CREATE UNIQUE INDEX uq_mt_scopes_scope_name ON public.mt_scopes (scope_name) WHERE deleted_at IS NULL;

-- Index
-- filter is_active
CREATE INDEX idx_mt_scopes_is_active ON public.mt_scopes (is_active) WHERE deleted_at IS NULL;

-- filter created_at
CREATE INDEX idx_mt_scopes_created_at ON public.mt_scopes (created_at DESC) WHERE deleted_at IS NULL;

-- filter scope_type
CREATE INDEX idx_mt_scopes_scope_type ON public.mt_scopes (scope_type) WHERE deleted_at IS NULL;


----------------------------------------------------------------------------------------------------------------------------------
--#table 01 mt_tenants
CREATE TABLE public.mt_tenants (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    tenant_code TEXT NOT NULL, -- UNIQUE
    tenant_name JSONB NOT NULL,

    settings JSONB,

	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,
	
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,

	-- Primary Key
    CONSTRAINT pk_mt_tenants PRIMARY KEY (id)
);

-- Unique
CREATE UNIQUE INDEX uq_mt_tenants_tenant_code ON public.mt_tenants (tenant_code) WHERE deleted_at IS NULL;

-- Index
-- filter is_active
CREATE INDEX idx_mt_tenants_is_active ON public.mt_tenants (is_active) WHERE deleted_at IS NULL;

-- filter created_at
CREATE INDEX idx_mt_tenants_created_at ON public.mt_tenants (created_at DESC) WHERE deleted_at IS NULL;


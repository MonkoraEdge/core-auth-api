----------------------------------------------------------------------------------------------------------------------------------
--#table 18
CREATE TABLE public.mt_roles (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

	tenant_id UUID, -- FK mt_tenants
    role_code TEXT NOT NULL, -- UNIQUE
    role_name JSONB NOT NULL,
    parent_role_id UUID, -- FK mt_roles
	
	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,	

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,
	
	-- Primary Key
    CONSTRAINT pk_mt_roles PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_mt_roles_mt_roles FOREIGN KEY (parent_role_id)
	REFERENCES public.mt_roles(id) ON DELETE SET NULL,

    CONSTRAINT fk_mt_roles_mt_tenants FOREIGN KEY (tenant_id)
	REFERENCES public.mt_tenants(id) ON DELETE SET NULL			
);

-- Unique: platform-wide roles have globally unique role_code
CREATE UNIQUE INDEX uq_mt_roles_role_code_platform ON public.mt_roles (role_code) WHERE tenant_id IS NULL AND deleted_at IS NULL;

-- Unique: tenant-scoped roles have role_code unique per tenant
CREATE UNIQUE INDEX uq_mt_roles_role_code_tenant ON public.mt_roles (role_code, tenant_id) WHERE tenant_id IS NOT NULL AND deleted_at IS NULL;

-- Index
-- filter is_active
CREATE INDEX idx_mt_roles_is_active ON public.mt_roles (is_active) WHERE deleted_at IS NULL;

-- filter created_at
CREATE INDEX idx_mt_roles_created_at ON public.mt_roles (created_at DESC) WHERE deleted_at IS NULL;

-- filter parent_role_id
CREATE INDEX idx_mt_roles_parent_role_id ON public.mt_roles(parent_role_id);

-- filter tenant_id
CREATE INDEX idx_mt_roles_tenant_id ON public.mt_roles(tenant_id) WHERE deleted_at IS NULL;


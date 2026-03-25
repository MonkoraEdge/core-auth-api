----------------------------------------------------------------------------------------------------------------------------------
--#table 19
CREATE TABLE public.mt_permissions (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
	
	tenant_id UUID, -- FK mt_tenants
    permission_code TEXT NOT NULL, -- UNIQUE
    permission_name JSONB NOT NULL,
    resource TEXT,     -- the resource this permission applies to (e.g. users, tokens, clients)
    action TEXT,       -- the action on the resource (e.g. READ, WRITE, DELETE, ADMIN)

	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,	

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,

	-- Primary Key
    CONSTRAINT pk_mt_permissions PRIMARY KEY (id),

    CONSTRAINT fk_mt_permissions_mt_tenants FOREIGN KEY (tenant_id)
	REFERENCES public.mt_tenants(id) ON DELETE SET NULL	
);

-- Unique: platform-wide permissions have globally unique permission_code
CREATE UNIQUE INDEX uq_mt_permissions_permission_code_platform ON public.mt_permissions (permission_code) WHERE tenant_id IS NULL AND deleted_at IS NULL;

-- Unique: tenant-scoped permissions have permission_code unique per tenant
CREATE UNIQUE INDEX uq_mt_permissions_permission_code_tenant ON public.mt_permissions (permission_code, tenant_id) WHERE tenant_id IS NOT NULL AND deleted_at IS NULL;

-- Index
-- filter is_active
CREATE INDEX idx_mt_permissions_is_active ON public.mt_permissions (is_active) WHERE deleted_at IS NULL;

-- filter created_at
CREATE INDEX idx_mt_permissions_created_at ON public.mt_permissions (created_at DESC) WHERE deleted_at IS NULL;

-- filter tenant_id
CREATE INDEX idx_mt_permissions_tenant_id ON public.mt_permissions(tenant_id) WHERE deleted_at IS NULL;


----------------------------------------------------------------------------------------------------------------------------------
--#table 30
CREATE TABLE public.mt_api_keys (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    tenant_id UUID, -- FK mt_tenants
    client_id UUID NOT NULL, -- FK mt_authorization_clients
    user_id UUID, -- FK mt_users (NULL = machine/service key, NOT NULL = user-owned key)

    key_hash TEXT NOT NULL, -- UNIQUE
    key_prefix TEXT NOT NULL,

    key_name TEXT NOT NULL,
    scopes TEXT[],
    allowed_ips TEXT[], -- IP whitelist; NULL = unrestricted

    expires_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ,

    last_used_at TIMESTAMPTZ,

	is_active BOOLEAN NOT NULL DEFAULT TRUE,
	description JSONB,
	
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,

	-- Primary Key
    CONSTRAINT pk_mt_api_keys  PRIMARY KEY (id),

	-- Foreign Key
    CONSTRAINT fk_mt_api_keys_tenant FOREIGN KEY (tenant_id)
	REFERENCES public.mt_tenants(id) ON DELETE SET NULL,

    CONSTRAINT fk_mt_api_keys_client FOREIGN KEY (client_id)
	REFERENCES public.mt_authorization_clients(id) ON DELETE CASCADE,

    CONSTRAINT fk_mt_api_keys_user FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE SET NULL
);

-- Unique
CREATE UNIQUE INDEX uq_mt_api_keys_key_hash ON public.mt_api_keys(key_hash);

-- Index
-- filter is_active
CREATE INDEX idx_mt_api_keys_is_active ON public.mt_api_keys (is_active) WHERE deleted_at IS NULL;

-- filter created_at
CREATE INDEX idx_mt_api_keys_created_at ON public.mt_api_keys (created_at DESC) WHERE deleted_at IS NULL;

-- filter tenant_id
CREATE INDEX idx_mt_api_keys_tenant_id ON public.mt_api_keys(tenant_id) WHERE deleted_at IS NULL;

-- filter client_id
CREATE INDEX idx_mt_api_keys_client_id ON public.mt_api_keys (client_id) WHERE deleted_at IS NULL;

-- filter user_id
CREATE INDEX idx_mt_api_keys_user_id ON public.mt_api_keys (user_id) WHERE deleted_at IS NULL;


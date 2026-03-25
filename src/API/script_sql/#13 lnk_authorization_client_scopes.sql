----------------------------------------------------------------------------------------------------------------------------------
--#table 13
CREATE TABLE public.lnk_authorization_client_scopes (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
	
    client_id UUID NOT NULL, -- FK mt_authorization_clients
    scope_id UUID NOT NULL, -- FK mt_scopes
	is_default BOOLEAN NOT NULL DEFAULT FALSE,  -- automatically included if scope not specified in request
	is_required BOOLEAN NOT NULL DEFAULT FALSE, -- cannot be deselected by user at consent
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,

	-- Primary Key
    CONSTRAINT pk_lnk_authorization_client_scopes PRIMARY KEY (id),

    -- Unique
    CONSTRAINT uq_lnk_authorization_client_scopes_client_scope UNIQUE (client_id, scope_id),

    -- Foreign Key
    CONSTRAINT fk_lnk_authorization_client_scopes_mt_authorization_clients FOREIGN KEY (client_id)
	REFERENCES public.mt_authorization_clients(id) ON DELETE CASCADE,

    CONSTRAINT fk_lnk_authorization_client_scopes_mt_scopes FOREIGN KEY (scope_id)
	REFERENCES public.mt_scopes(id) ON DELETE CASCADE		
);

-- Index
-- filter created_at
CREATE INDEX idx_lnk_authorization_client_scopes_created_at ON public.lnk_authorization_client_scopes (created_at DESC);

-- query scope_id PK
CREATE INDEX idx_lnk_authorization_client_scopes_scope_id ON public.lnk_authorization_client_scopes (scope_id);

-- query client_id PK
CREATE INDEX idx_lnk_authorization_client_scopes_client_id ON public.lnk_authorization_client_scopes (client_id);


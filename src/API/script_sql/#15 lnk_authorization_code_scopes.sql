----------------------------------------------------------------------------------------------------------------------------------
--#table 14
-- NOTE: This table must be created AFTER #15 tx_authorization_codes due to FK dependency.
-- It provides a normalized view of scopes per authorization code.
-- tx_authorization_codes also stores scopes as TEXT[] for quick access.
CREATE TABLE public.lnk_authorization_code_scopes (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
	
    authorization_code_id UUID NOT NULL, -- FK tx_authorization_codes
    scope_id UUID NOT NULL, -- FK mt_scopes
	description JSONB,
	
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	
	-- Primary Key
    CONSTRAINT pk_lnk_authorization_code_scopes PRIMARY KEY (id),

    -- Unique
    CONSTRAINT uq_lnk_authorization_code_scopes_code_scope UNIQUE (authorization_code_id, scope_id),

    -- Foreign Key
    CONSTRAINT fk_lnk_authorization_code_scopes_tx_authorization_codes FOREIGN KEY (authorization_code_id)
	REFERENCES public.tx_authorization_codes(id) ON DELETE CASCADE,

    CONSTRAINT fk_lnk_authorization_code_scopes_mt_scopes FOREIGN KEY (scope_id)
	REFERENCES public.mt_scopes(id) ON DELETE CASCADE			
);

-- Index
-- filter authorization_code_id
CREATE INDEX idx_lnk_authorization_code_scopes_authorization_code_id ON public.lnk_authorization_code_scopes (authorization_code_id);

-- filter scope_id
CREATE INDEX idx_lnk_authorization_code_scopes_scope_id ON public.lnk_authorization_code_scopes (scope_id);

-- filter created_at
CREATE INDEX idx_lnk_authorization_code_scopes_created_at ON public.lnk_authorization_code_scopes (created_at DESC);


----------------------------------------------------------------------------------------------------------------------------------
--#table 23
CREATE TABLE public.tx_authorization_consents (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    client_id UUID NOT NULL, -- FK mt_authorization_clients
    user_id UUID NOT NULL, -- FK mt_users
	session_id UUID, -- FK tx_user_sessions	
	
    scopes TEXT[] NOT NULL, -- space-delimited (OAuth standard)

    granted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,   -- when consent expires; NULL = never (user must re-consent after this)
    revoked_at TIMESTAMPTZ NULL,

    ip_address INET,
    user_agent TEXT,
	
	description JSONB,
	
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	
	-- Primary Key
    CONSTRAINT pk_tx_authorization_consents  PRIMARY KEY (id),

    -- Foreign Key	
    CONSTRAINT fk_tx_authorization_consents_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE,

    CONSTRAINT fk_tx_authorization_consents_mt_authorization_client FOREIGN KEY (client_id)
	REFERENCES public.mt_authorization_clients(id) ON DELETE CASCADE,

    CONSTRAINT fk_tx_authorization_consents_tx_user_sessions FOREIGN KEY (session_id)
	REFERENCES public.tx_user_sessions(id) ON DELETE SET NULL
);

-- Unique
CREATE UNIQUE INDEX uq_tx_authorization_consents_user_client ON public.tx_authorization_consents(user_id, client_id) WHERE revoked_at IS NULL;

-- Index
-- query user_id + created_at
CREATE INDEX idx_tx_authorization_consents_user_created_at ON public.tx_authorization_consents (user_id, created_at DESC);

-- query user_id + client_id
CREATE INDEX idx_tx_authorization_consents_user_client_id ON public.tx_authorization_consents (user_id, client_id);

-- query user_id + session_id
CREATE INDEX idx_tx_authorization_consents_user_session_id ON public.tx_authorization_consents (user_id, session_id);

-- filter user_id
CREATE INDEX idx_tx_authorization_consents_user_id ON public.tx_authorization_consents (user_id);

-- filter session_id
CREATE INDEX idx_tx_authorization_consents_session_id ON public.tx_authorization_consents (session_id);

-- filter created_at
CREATE INDEX idx_tx_authorization_consents_created_at ON public.tx_authorization_consents (created_at DESC);

-- filter client_id
CREATE INDEX idx_tx_authorization_consents_client_id ON public.tx_authorization_consents (client_id);


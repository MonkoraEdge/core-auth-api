----------------------------------------------------------------------------------------------------------------------------------
--#table 16
CREATE TABLE public.tx_authorization_access_tokens (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    token_hash TEXT NOT NULL, -- UNIQUE
    token_type TEXT NOT NULL DEFAULT 'Bearer',

    client_id UUID NOT NULL, -- FK mt_authorization_clients
    user_id UUID, -- FK mt_users
	session_id UUID, -- FK tx_user_sessions
	
    scopes TEXT[] NOT NULL, -- space-delimited
    grant_type TEXT, -- ENUM --> AUTHORIZATION_CODE, CLIENT_CREDENTIALS, REFRESH_TOKEN, IMPLICIT

    ip_address INET,
    user_agent TEXT,

	issued_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ,
	
	description JSONB,	

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,	
	
	-- Primary Key
    CONSTRAINT pk_tx_authorization_access_tokens PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_tx_authorization_access_tokens_mt_authorization_client FOREIGN KEY (client_id)
	REFERENCES public.mt_authorization_clients(id) ON DELETE CASCADE,

    CONSTRAINT fk_tx_authorization_access_tokens_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE SET NULL,

    CONSTRAINT fk_tx_authorization_access_tokens_tx_user_sessions FOREIGN KEY (session_id)
	REFERENCES public.tx_user_sessions(id) ON DELETE SET NULL,

    CONSTRAINT chk_tx_authorization_access_tokens_grant_type
    CHECK (
        grant_type IS NULL OR
        grant_type IN (
            'AUTHORIZATION_CODE',   -- OAuth2.1 §4.1
            'CLIENT_CREDENTIALS',   -- OAuth2.1 §4.2
            'REFRESH_TOKEN',        -- OAuth2.1 §6
            'DEVICE_CODE'           -- RFC 8628
            -- IMPLICIT removed: OAuth2.1 §2.1 prohibits implicit grant
            -- PASSWORD removed: OAuth2.1 §2.1 prohibits ROPC grant
        )
    ),

    CONSTRAINT chk_tx_authorization_access_tokens_token_type
    CHECK (token_type IN ('Bearer', 'DPoP'))
);

-- Unique
CREATE UNIQUE INDEX uq_tx_authorization_access_tokens_token_hash ON public.tx_authorization_access_tokens(token_hash);

-- Index
-- query user_id + created_at
CREATE INDEX idx_tx_authorization_access_tokens_user_created_at ON public.tx_authorization_access_tokens (user_id, created_at DESC);

-- query user_id + client_id
CREATE INDEX idx_tx_authorization_access_tokens_user_client_id ON public.tx_authorization_access_tokens(user_id, client_id);

-- filter user_id
CREATE INDEX idx_tx_authorization_access_tokens_user_id ON public.tx_authorization_access_tokens (user_id);

-- filter client_id
CREATE INDEX idx_tx_authorization_access_tokens_client_id ON public.tx_authorization_access_tokens(client_id);

-- filter session_id
CREATE INDEX idx_tx_authorization_access_tokens_session_id ON public.tx_authorization_access_tokens(session_id);

-- filter created_at
CREATE INDEX idx_tx_authorization_access_tokens_created_at ON public.tx_authorization_access_tokens (created_at DESC);

-- filter expires_at
CREATE INDEX idx_tx_authorization_access_tokens_expires_at ON public.tx_authorization_access_tokens(expires_at);

-- filter revoked_at (bulk query revoked tokens for audit / admin; most tokens are NOT revoked so partial index is small)
CREATE INDEX idx_tx_authorization_access_tokens_revoked_at ON public.tx_authorization_access_tokens (revoked_at) WHERE revoked_at IS NOT NULL;


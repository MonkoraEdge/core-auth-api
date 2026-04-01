----------------------------------------------------------------------------------------------------------------------------------
--#table 15
CREATE TABLE public.tx_authorization_codes (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    code_hash TEXT NOT NULL, -- UNIQUE
    client_id UUID NOT NULL, -- FK mt_authorization_clients
    user_id UUID NOT NULL, -- FK mt_users
	session_id UUID, -- FK tx_user_sessions
	
	scopes TEXT[] NOT NULL,
    redirect_uri TEXT NOT NULL,

    code_challenge TEXT,
    code_challenge_method TEXT, -- ENUM PLAIN / S256
	
	nonce TEXT NULL,
	auth_time TIMESTAMPTZ, -- when the user actively authenticated (OIDC max_age / auth_time claim)
	
    expires_at TIMESTAMPTZ NOT NULL,
    consumed_at TIMESTAMPTZ NULL,

	description JSONB,	

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,	

	-- Primary Key
    CONSTRAINT pk_tx_authorization_codes PRIMARY KEY (id),	

    -- Foreign Key
    CONSTRAINT fk_tx_authorization_codes_mt_authorization_clients FOREIGN KEY (client_id)
	REFERENCES public.mt_authorization_clients(id) ON DELETE CASCADE,

    CONSTRAINT fk_tx_authorization_codes_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE,

    CONSTRAINT fk_tx_authorization_codes_tx_user_sessions FOREIGN KEY (session_id)
	REFERENCES public.tx_user_sessions(id) ON DELETE SET NULL,

	CONSTRAINT chk_auth_code_expiry
	CHECK (expires_at > created_at),

	CONSTRAINT chk_tx_authorization_codes_code_challenge_method
	CHECK (
	    code_challenge_method IS NULL OR
	    code_challenge_method = 'S256'  -- OAuth 2.1 §4.1.1: plain PKCE is prohibited
	),
	
	CONSTRAINT chk_tx_authorization_codes_pkce_pairing
	CHECK (
		(code_challenge IS NULL AND code_challenge_method IS NULL)
		OR
		(code_challenge IS NOT NULL AND code_challenge_method IS NOT NULL)
	),

	-- consumed_at must be at or after creation — prevents back-dated consumption records
	CONSTRAINT chk_tx_authorization_codes_consumed_at
	CHECK (consumed_at IS NULL OR consumed_at >= created_at)	
);

-- Unique
CREATE UNIQUE INDEX uq_tx_authorization_codes_code_hash ON public.tx_authorization_codes (code_hash) WHERE consumed_at IS NULL;

-- Index
-- query user_id + created_at
CREATE INDEX idx_tx_authorization_codes_user_created_at ON public.tx_authorization_codes (user_id, created_at DESC);

-- query user_id + client_id
CREATE INDEX idx_tx_authorization_codes_user_client_id ON public.tx_authorization_codes (user_id, client_id);

-- filter session_id
CREATE INDEX idx_tx_authorization_codes_session_id ON public.tx_authorization_codes (session_id);

-- filter client_id
CREATE INDEX idx_tx_authorization_codes_client_id ON public.tx_authorization_codes (client_id);

-- filter user_id
CREATE INDEX idx_tx_authorization_codes_user_id ON public.tx_authorization_codes (user_id);

-- filter created_at
CREATE INDEX idx_tx_authorization_codes_created_at ON public.tx_authorization_codes (created_at DESC);

-- filter expires_at
CREATE INDEX idx_tx_authorization_codes_expires_at ON public.tx_authorization_codes(expires_at);

-- cleanup: expired unused codes (TTL cleanup job scans only unconsumed rows, keeping this index tiny)
CREATE INDEX idx_tx_authorization_codes_expires_at_cleanup ON public.tx_authorization_codes (expires_at)
    WHERE consumed_at IS NULL;


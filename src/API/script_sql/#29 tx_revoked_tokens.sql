----------------------------------------------------------------------------------------------------------------------------------
--#table 29
CREATE TABLE public.tx_revoked_tokens (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    client_id UUID NOT NULL, -- FK mt_authorization_clients
    user_id UUID, -- FK mt_users (nullable: client_credentials tokens have no user)
    session_id UUID, -- FK tx_user_sessions
	
    token_hash TEXT NOT NULL, -- UNIQUE
    token_type TEXT NOT NULL, -- ENUM --> ACCESS_TOKEN, REFRESH_TOKEN

    reason TEXT, -- ENUM --> LOGOUT, ROTATION, ADMIN_REVOKE, SECURITY

    revoked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
	
	description JSONB,	

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,	

	-- Primary Key
    CONSTRAINT pk_tx_revoked_tokens PRIMARY KEY (id),

	-- Foreign Key
    CONSTRAINT fk_tx_revoked_tokens_clients FOREIGN KEY (client_id)
	REFERENCES public.mt_authorization_clients(id) ON DELETE CASCADE,
	
    CONSTRAINT fk_tx_revoked_tokens_user FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE SET NULL,

    CONSTRAINT fk_tx_revoked_tokens_user_sessions FOREIGN KEY (session_id)
	REFERENCES public.tx_user_sessions(id) ON DELETE SET NULL,

	CONSTRAINT chk_tx_revoked_tokens_token_type
	CHECK (
	    token_type IN (
	        'ACCESS_TOKEN',
			'REFRESH_TOKEN'
	    )
	),

	CONSTRAINT chk_tx_revoked_tokens_reason
	CHECK (
	    reason IN (
	        'LOGOUT',
	        'ROTATION',
	        'ADMIN_REVOKE',			
			'SECURITY'
	    )
	)
);

-- Unique
CREATE UNIQUE INDEX uq_tx_revoked_tokens_token_hash_active ON public.tx_revoked_tokens(token_hash);

-- Index
-- query user_id + created_at
CREATE INDEX idx_tx_revoked_tokens_user_created_at ON public.tx_revoked_tokens (user_id, created_at DESC);

-- filter client_id
CREATE INDEX idx_tx_revoked_tokens_client_id ON public.tx_revoked_tokens(client_id);

-- filter session_id
CREATE INDEX idx_tx_revoked_tokens_session_id ON public.tx_revoked_tokens(session_id);

-- filter user_id
CREATE INDEX idx_tx_revoked_tokens_user_id ON public.tx_revoked_tokens (user_id);

-- filter created_at
CREATE INDEX idx_tx_revoked_tokens_created_at ON public.tx_revoked_tokens (created_at DESC);

-- filter expires_at
CREATE INDEX idx_tx_revoked_tokens_expires_at ON public.tx_revoked_tokens (expires_at);
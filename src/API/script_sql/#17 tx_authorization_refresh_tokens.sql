----------------------------------------------------------------------------------------------------------------------------------
--#table 17
CREATE TABLE public.tx_authorization_refresh_tokens (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    refresh_token_hash TEXT NOT NULL, -- UNIQUE
	client_id UUID NOT NULL, -- FK mt_authorization_clients (denormalized for self-contained token)
	user_id UUID NOT NULL,   -- FK mt_users (denormalized for self-contained token)
	session_id UUID, -- FK tx_user_sessions (nullable: refresh tokens can outlive sessions)
    replaced_by_token_id UUID, -- FK tx_authorization_refresh_tokens (rotation chain)
    family_id UUID NOT NULL,   -- shared by all tokens in one rotation chain;
                               -- reuse of a rotated token triggers full-family revocation (theft detection)

	scopes TEXT[] NOT NULL,  -- scopes granted with this refresh token

    ip_address INET,
    user_agent TEXT,

	issued_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),	
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ NULL,

	description JSONB,	

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	
	-- Primary Key
    CONSTRAINT pk_tx_authorization_refresh_tokens PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_tx_authorization_refresh_tokens_mt_authorization_clients FOREIGN KEY (client_id)
    REFERENCES public.mt_authorization_clients(id) ON DELETE CASCADE,

    CONSTRAINT fk_tx_authorization_refresh_tokens_mt_users FOREIGN KEY (user_id)
    REFERENCES public.mt_users(id) ON DELETE CASCADE,

    CONSTRAINT fk_tx_authorization_refresh_tokens_tx_user_sessions FOREIGN KEY (session_id)
    REFERENCES public.tx_user_sessions(id) ON DELETE SET NULL,

    CONSTRAINT fk_tx_authorization_refresh_tokens_tx_authorization_refresh FOREIGN KEY (replaced_by_token_id)
	REFERENCES public.tx_authorization_refresh_tokens(id) ON DELETE SET NULL
);

-- Unique
CREATE UNIQUE INDEX uq_tx_authorization_refresh_tokens_refresh_token_hash ON public.tx_authorization_refresh_tokens(refresh_token_hash);

-- Index
-- filter created_at
CREATE INDEX idx_tx_authorization_refresh_tokens_created_at ON public.tx_authorization_refresh_tokens (created_at DESC);

-- filter session_id
CREATE INDEX idx_tx_authorization_refresh_tokens_session_id ON public.tx_authorization_refresh_tokens (session_id);

-- filter user_id
CREATE INDEX idx_tx_authorization_refresh_tokens_user_id ON public.tx_authorization_refresh_tokens (user_id);

-- filter client_id
CREATE INDEX idx_tx_authorization_refresh_tokens_client_id ON public.tx_authorization_refresh_tokens (client_id);

-- filter family_id (revoke entire token family on theft detection)
CREATE INDEX idx_tx_authorization_refresh_tokens_family_id ON public.tx_authorization_refresh_tokens (family_id);

-- composite: user_id + client_id (list active sessions per user per client)
CREATE INDEX idx_tx_authorization_refresh_tokens_user_client_id ON public.tx_authorization_refresh_tokens (user_id, client_id);

-- filter expires_at
CREATE INDEX idx_tx_authorization_refresh_tokens_expires_at ON public.tx_authorization_refresh_tokens(expires_at);

-- filter revoked_at (bulk query revoked refresh tokens for audit / rotation chain; most tokens are NOT revoked so partial index is small)
CREATE INDEX idx_tx_authorization_refresh_tokens_revoked_at ON public.tx_authorization_refresh_tokens (revoked_at) WHERE revoked_at IS NOT NULL;


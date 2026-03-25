----------------------------------------------------------------------------------------------------------------------------------
--#table 11
CREATE TABLE public.tx_user_sessions (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
	
	client_id UUID NOT NULL, -- FK mt_authorization_clients
    user_id UUID NOT NULL, -- FK mt_users
    device_id UUID, -- FK tx_user_sessions_devices
	
    session_token_hash TEXT NOT NULL, -- UNIQUE

    ip_address INET,
    user_agent TEXT,
	
    expires_at TIMESTAMPTZ,   -- when this session expires (NULL = never, policy-driven)
    last_activity_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ,	

	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,

	-- Primary Key
    CONSTRAINT pk_tx_user_sessions PRIMARY KEY (id),	

    -- Foreign Key
    CONSTRAINT fk_tx_user_sessions_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE,

	CONSTRAINT fk_tx_user_sessions_tx_user_sessions_devices FOREIGN KEY (device_id)
	REFERENCES public.tx_user_sessions_devices(id) ON DELETE SET NULL,

    CONSTRAINT fk_tx_user_sessions_mt_authorization_clients FOREIGN KEY (client_id)
	REFERENCES public.mt_authorization_clients(id) ON DELETE CASCADE	
);

-- Unique
CREATE UNIQUE INDEX uq_tx_user_sessions_session_token_hash ON public.tx_user_sessions(session_token_hash) WHERE revoked_at IS NULL;

-- Index
-- query user_id + is_active
CREATE INDEX idx_tx_user_sessions_user_is_active ON public.tx_user_sessions (user_id, is_active);

-- query user_id + created_at
CREATE INDEX idx_tx_user_sessions_user_created_at ON public.tx_user_sessions (user_id, created_at DESC);

-- filter user_id
CREATE INDEX idx_tx_user_sessions_user_id ON public.tx_user_sessions (user_id);

-- filter is_active
CREATE INDEX idx_tx_user_sessions_is_active ON public.tx_user_sessions (is_active);

-- filter created_at
CREATE INDEX idx_tx_user_sessions_created_at ON public.tx_user_sessions (created_at DESC);

-- filter device_id
CREATE INDEX idx_tx_user_sessions_device_id ON public.tx_user_sessions (device_id) WHERE revoked_at IS NULL;

-- filter expires_at
CREATE INDEX idx_tx_user_sessions_expires_at ON public.tx_user_sessions (expires_at) WHERE revoked_at IS NULL;

-- filter revoked_at
CREATE INDEX idx_tx_user_sessions_revoked_at ON public.tx_user_sessions (revoked_at);


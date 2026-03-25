----------------------------------------------------------------------------------------------------------------------------------
--#table 28
CREATE TABLE public.tx_password_resets (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    user_id UUID NOT NULL, --FK mt_users
    token_hash TEXT NOT NULL, -- UNIQUE
    ip_address INET,      -- IP address from which the reset was requested

    expires_at TIMESTAMPTZ NOT NULL,
    used_at TIMESTAMPTZ,
	revoked_at TIMESTAMPTZ,   -- explicit invalidation (e.g. new reset request supersedes old)

	description JSONB,
	
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,	

	-- Primary Key
    CONSTRAINT pk_tx_password_resets PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_tx_password_resets_user_id_mt_users_id FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX uq_tx_password_resets_token ON public.tx_password_resets(token_hash);

-- Index
-- query user_id + created_at
CREATE INDEX idx_tx_password_resets_user_created ON public.tx_password_resets(user_id, created_at DESC);

-- filter user_id
CREATE INDEX idx_tx_password_resets_user_id ON public.tx_password_resets (user_id);

-- filter created_at
CREATE INDEX idx_tx_password_resets_created_at ON public.tx_password_resets (created_at DESC);

-- filter expires_at
CREATE INDEX idx_tx_password_resets_expires_at ON public.tx_password_resets (expires_at) WHERE used_at IS NULL AND revoked_at IS NULL;


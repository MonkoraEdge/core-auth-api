----------------------------------------------------------------------------------------------------------------------------------
--#table 26
CREATE TABLE public.tx_password_history (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL, --FK mt_users
	
    password_hash TEXT NOT NULL,
	hash_algorithm TEXT,
	
	password_strength INT,
	is_temporary BOOLEAN NOT NULL DEFAULT FALSE,
	
	description JSONB,
	
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,	

	-- Primary Key
    CONSTRAINT pk_tx_password_history PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_tx_password_history_user_id_mt_users_id FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE
);

-- Index
-- query user_id + created_at
CREATE INDEX idx_tx_password_history_user_created ON public.tx_password_history(user_id, created_at DESC);

-- filter user_id
CREATE INDEX idx_tx_password_history_user_id ON public.tx_password_history(user_id);

-- filter created_at
CREATE INDEX idx_tx_password_history_created_at ON public.tx_password_history (created_at DESC);


----------------------------------------------------------------------------------------------------------------------------------
--#table 09
CREATE TABLE public.tx_user_two_factor_recovery_codes (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL, -- FK mt_users
    two_factor_setting_id UUID, -- FK tx_user_two_factor_settings (which 2FA method these codes belong to)

    code_hash TEXT NOT NULL, -- UNIQUE
    code_prefix TEXT NOT NULL,
	
	batch_id UUID NOT NULL,
	expires_at TIMESTAMPTZ NULL,
    used_at TIMESTAMPTZ NULL,
    revoked_at TIMESTAMPTZ NULL,

	is_active BOOLEAN NOT NULL DEFAULT TRUE,
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,

	-- Primary Key
    CONSTRAINT pk_tx_user_two_factor_recovery_codes PRIMARY KEY (id),	

    -- Foreign Key
    CONSTRAINT fk_tx_user_two_factor_recovery_codes_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE,

    CONSTRAINT fk_tx_user_two_factor_recovery_codes_tx_user_two_factor_settings FOREIGN KEY (two_factor_setting_id)
	REFERENCES public.tx_user_two_factor_settings(id) ON DELETE SET NULL
);

-- Unique
CREATE UNIQUE INDEX uq_tx_user_two_factor_recovery_codes_code_hash ON public.tx_user_two_factor_recovery_codes (code_hash);

-- Index
-- query user_id + is_active
CREATE INDEX idx_tx_user_two_factor_recovery_codes_user_is_active ON public.tx_user_two_factor_recovery_codes (user_id, is_active);

-- query user_id + created_at
CREATE INDEX idx_tx_user_two_factor_recovery_codes_user_created_at ON public.tx_user_two_factor_recovery_codes (user_id, created_at DESC);

-- filter user_id
CREATE INDEX idx_tx_user_two_factor_recovery_codes_user_id ON public.tx_user_two_factor_recovery_codes (user_id);

-- filter is_active
CREATE INDEX idx_tx_user_two_factor_recovery_codes_is_active ON public.tx_user_two_factor_recovery_codes (is_active);

-- filter created_at
CREATE INDEX idx_tx_user_two_factor_recovery_codes_created_at ON public.tx_user_two_factor_recovery_codes (created_at DESC);

-- filter batch_id (invalidate all codes from a specific generation)
CREATE INDEX idx_tx_user_two_factor_recovery_codes_batch_id ON public.tx_user_two_factor_recovery_codes (batch_id);


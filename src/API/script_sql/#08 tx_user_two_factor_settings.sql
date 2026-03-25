----------------------------------------------------------------------------------------------------------------------------------
--#table 08
CREATE TABLE public.tx_user_two_factor_settings (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL, -- FK mt_users UNIQUE(+1)

    device_type TEXT NOT NULL, -- ENUM --> TOTP, SMS, EMAIL UNIQUE(+1)
    secret_key TEXT,
    email TEXT,
	phone_number TEXT,
    verified_at TIMESTAMPTZ,

	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,

	-- Primary Key
    CONSTRAINT pk_tx_user_two_factor_settings PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_tx_user_two_factor_settings_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE,

	CONSTRAINT chk_tx_user_two_factor_settings_device_type
	CHECK (
	    (device_type = 'TOTP' AND secret_key IS NOT NULL)
	    OR
	    (device_type = 'SMS' AND phone_number IS NOT NULL)
	    OR
	    (device_type = 'EMAIL' AND email IS NOT NULL)
	)
);

-- Unique
CREATE UNIQUE INDEX uq_tx_user_two_factor_settings_user_device_type ON public.tx_user_two_factor_settings (user_id, device_type) WHERE deleted_at IS NULL;

-- Index
-- query user_id + is_active
CREATE INDEX idx_tx_user_two_factor_settings_user_is_active ON public.tx_user_two_factor_settings (user_id, is_active);

-- query user_id + created_at
CREATE INDEX idx_tx_user_two_factor_settings_user_created_at ON public.tx_user_two_factor_settings (user_id, created_at DESC);

-- filter user_id
CREATE INDEX idx_tx_user_two_factor_settings_user_id ON public.tx_user_two_factor_settings (user_id);

-- filter is_active
CREATE INDEX idx_tx_user_two_factor_settings_is_active ON public.tx_user_two_factor_settings(is_active);

-- filter created_at
CREATE INDEX idx_tx_user_two_factor_settings_created_at ON public.tx_user_two_factor_settings (created_at DESC);


----------------------------------------------------------------------------------------------------------------------------------
--#table 07
CREATE TABLE public.tx_user_sessions_devices (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL, -- FK mt_users

    device_name TEXT,
    device_type TEXT NOT NULL, -- ENUM --> MOBILE, TABLET, DESKTOP, LAPTOP, BOT, UNKNOWN
    device_fingerprint TEXT,
	
    os_name TEXT,
    os_version TEXT,
	
    browser_name TEXT,
    browser_version TEXT,
	
    ip_address INET,
    user_agent TEXT,
	
	login_method TEXT,
	city TEXT,
	country TEXT,

	last_seen_at TIMESTAMPTZ,
    last_login_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ,
	
    is_blocked BOOLEAN NOT NULL DEFAULT FALSE,	
	is_trusted BOOLEAN NOT NULL DEFAULT FALSE,
	
	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,

	-- Primary Key
    CONSTRAINT pk_tx_user_sessions_devices PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_tx_user_sessions_devices_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE,

	CONSTRAINT chk_tx_user_sessions_devices_device_type
	CHECK (
	    device_type IN (
	        'MOBILE',
	        'TABLET',
	        'DESKTOP',
	        'LAPTOP',
	        'BOT',
	        'UNKNOWN'
	    )
	),
	
	CONSTRAINT chk_tx_user_sessions_devices_expires
	CHECK (expires_at IS NULL OR expires_at > created_at),

	CONSTRAINT chk_tx_user_sessions_devices_revoked_active
	CHECK (
	    revoked_at IS NULL OR is_active = FALSE
	)	
);

-- Unique
CREATE UNIQUE INDEX uq_tx_user_sessions_devices_user_device_fingerprint ON public.tx_user_sessions_devices (user_id, device_fingerprint) WHERE device_fingerprint IS NOT NULL;

-- Index
-- query user_id + is_active
CREATE INDEX idx_tx_user_sessions_devices_user_is_active ON public.tx_user_sessions_devices (user_id, is_active);

-- query user_id + created_at
CREATE INDEX idx_tx_user_sessions_devices_user_created_at ON public.tx_user_sessions_devices (user_id, created_at DESC);

-- filter user_id
CREATE INDEX idx_tx_user_sessions_devices_user_id ON public.tx_user_sessions_devices (user_id);

-- filter is_active
CREATE INDEX idx_tx_user_sessions_devices_is_active ON public.tx_user_sessions_devices (is_active);

-- filter created_at
CREATE INDEX idx_tx_user_sessions_devices_created_at ON public.tx_user_sessions_devices (created_at DESC);

-- filter revoked_at
CREATE INDEX idx_tx_user_sessions_devices_revoked_at ON public.tx_user_sessions_devices (revoked_at);

-- filter expires_at
CREATE INDEX idx_tx_user_sessions_devices_expires_at ON public.tx_user_sessions_devices (expires_at);

-- filter last_seen_at
CREATE INDEX idx_tx_user_sessions_devices_last_seen_at ON public.tx_user_sessions_devices (last_seen_at);


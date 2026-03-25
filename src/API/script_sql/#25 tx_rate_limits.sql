----------------------------------------------------------------------------------------------------------------------------------
--#table 25
CREATE TABLE public.tx_rate_limits (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    identifier TEXT NOT NULL, -- Unique(+)
    -- maybe:
    -- IP
    -- user_id
    -- client_id
    -- combination

    endpoint TEXT NOT NULL, -- link --> /token /login /authorize  -- Unique(+)

    window_start TIMESTAMPTZ NOT NULL, -- Unique(+)
    window_end TIMESTAMPTZ NOT NULL,

    request_count INT NOT NULL DEFAULT 0,
    limit_count INT NOT NULL,

    blocked_until TIMESTAMPTZ NULL,

	-- scope: the rate-limit category (e.g. LOGIN, TOKEN, AUTHORIZE, GLOBAL)
    scope TEXT NOT NULL,

    expires_at TIMESTAMPTZ NOT NULL,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,	

	-- Primary Key
    CONSTRAINT pk_tx_rate_limits  PRIMARY KEY (id),
	
	CONSTRAINT chk_tx_rate_limits_window
	CHECK (window_end > window_start)	
);

-- Unique
CREATE UNIQUE INDEX uq_tx_rate_limits_identifier_endpoint_window ON public.tx_rate_limits(identifier, endpoint, window_start);

-- Index
-- filter created_at
CREATE INDEX idx_tx_rate_limits_created_at ON public.tx_rate_limits (created_at DESC);

-- filter blocked_until
CREATE INDEX idx_tx_rate_limits_blocked ON public.tx_rate_limits(blocked_until);

-- filter identifier
CREATE INDEX idx_tx_rate_limits_identifier ON public.tx_rate_limits(identifier);


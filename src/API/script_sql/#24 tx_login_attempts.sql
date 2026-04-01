----------------------------------------------------------------------------------------------------------------------------------
--#table 24
CREATE TABLE public.tx_login_attempts (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

	client_id UUID, -- FK mt_authorization_clients (nullable: SET NULL on client delete preserves audit trail)
    user_id UUID, -- FK mt_users (nullable: unknown user on failed attempts)
	session_id UUID, -- FK tx_user_sessions (nullable: no session exists before successful login)
	
    username TEXT, -- mt_user_identities

	provider_id UUID, -- mt_providers

	login_method TEXT, -- ENUM --> LOCAL, TOTP, SMS, EMAIL_OTP, SOCIAL, MAGIC_LINK, PASSKEY
	
    ip_address INET NOT NULL,
    user_agent TEXT,
	device_fingerprint TEXT,

	country_code TEXT,
	city TEXT,	

    success BOOLEAN NOT NULL,
    failure_reason TEXT, -- ENUM --> INVALID_PASSWORD, USER_NOT_FOUND, ACCOUNT_LOCKED, ACCOUNT_DISABLED, TWO_FACTOR_REQUIRED, INVALID_OTP, PROVIDER_ERROR, RATE_LIMIT

    risk_score INT,
	latency_ms INT,

	description JSONB,
	
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,	

	-- Primary Key
    CONSTRAINT pk_tx_login_attempts  PRIMARY KEY (id),

    -- Foreign Key	
    CONSTRAINT fk_tx_login_attempts_mt_authorization_clients FOREIGN KEY (client_id)
	REFERENCES public.mt_authorization_clients(id) ON DELETE SET NULL,
	-- SET NULL (not CASCADE): deleting a client must not erase the security audit trail
	
    CONSTRAINT fk_tx_login_attempts_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE SET NULL,

    CONSTRAINT fk_tx_login_attempts_tx_user_sessions FOREIGN KEY (session_id)
	REFERENCES public.tx_user_sessions(id) ON DELETE SET NULL,

	CONSTRAINT fk_tx_login_attempts_mt_providers FOREIGN KEY (provider_id)
	REFERENCES public.mt_providers(id) ON DELETE SET NULL,

	CONSTRAINT chk_tx_login_attempts_failure_reason
	CHECK (
	    failure_reason IS NULL OR
	    failure_reason IN (
	        'INVALID_PASSWORD',
	        'USER_NOT_FOUND',
	        'ACCOUNT_LOCKED',
	        'ACCOUNT_DISABLED',
	        'TWO_FACTOR_REQUIRED',
	        'INVALID_OTP',
	        'PROVIDER_ERROR',
	        'RATE_LIMIT'
	    )
	),

	CONSTRAINT chk_tx_login_attempts_login_method
	CHECK (
	    login_method IS NULL OR
	    login_method IN (
	        'LOCAL',
	        'TOTP',
	        'SMS',
	        'EMAIL_OTP',
	        'SOCIAL',
	        'MAGIC_LINK',
	        'PASSKEY'
	    )
	)	
);

-- Index
-- query user_id + created_at
CREATE INDEX idx_tx_login_attempts_user_created_at ON public.tx_login_attempts (user_id, created_at DESC);

-- query user_id + client_id
CREATE INDEX idx_tx_login_attempts_user_client_id ON public.tx_login_attempts (user_id, client_id);

-- query user_id + session_id
CREATE INDEX idx_tx_login_attempts_user_session_id ON public.tx_login_attempts (user_id, session_id);

-- filter user_id
CREATE INDEX idx_tx_login_attempts_user_id ON public.tx_login_attempts (user_id);

-- filter session_id
CREATE INDEX idx_tx_login_attempts_session_id ON public.tx_login_attempts (session_id);

-- filter created_at
CREATE INDEX idx_tx_login_attempts_created_at ON public.tx_login_attempts (created_at DESC);

-- filter client_id
CREATE INDEX idx_tx_login_attempts_client_id ON public.tx_login_attempts (client_id);

-- filter ip_address + created_at
CREATE INDEX idx_tx_login_attempts_ip_address_created_at ON public.tx_login_attempts (ip_address, created_at DESC);

-- filter ip_address
CREATE INDEX idx_tx_login_attempts_ip ON public.tx_login_attempts(ip_address);

-- filter username
CREATE INDEX idx_tx_login_attempts_username ON public.tx_login_attempts(username);

-- filter username + created_at
CREATE INDEX idx_tx_login_attempts_username_created ON public.tx_login_attempts(username, created_at DESC);

-- filter success
CREATE INDEX idx_tx_login_attempts_success ON public.tx_login_attempts(success);

-- filter provider_id
CREATE INDEX idx_tx_login_attempts_provider ON public.tx_login_attempts(provider_id);

-- composite: ip_address + success + created_at
-- CountFailedByIpAddressAsync uses all three predicates; covering index avoids table scan
CREATE INDEX idx_tx_login_attempts_ip_success_created_at ON public.tx_login_attempts (ip_address, success, created_at DESC);

-- composite: username + success + created_at
-- CountFailedByUsernameAsync uses all three predicates; covering index avoids table scan
CREATE INDEX idx_tx_login_attempts_username_success_created_at ON public.tx_login_attempts (username, success, created_at DESC);


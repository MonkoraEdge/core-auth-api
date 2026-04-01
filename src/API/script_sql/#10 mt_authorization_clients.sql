----------------------------------------------------------------------------------------------------------------------------------
--#table 10
CREATE TABLE public.mt_authorization_clients (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
	
	tenant_id UUID,
	
    client_id TEXT NOT NULL, -- UNIQUE
    client_secret_hash TEXT,
    client_name TEXT NOT NULL,
	client_type TEXT NOT NULL DEFAULT 'CONFIDENTIAL', -- ENUM --> CONFIDENTIAL, PUBLIC

	token_endpoint_auth_method TEXT NOT NULL DEFAULT 'CLIENT_SECRET_BASIC', -- ENUM --> NONE, CLIENT_SECRET_BASIC, CLIENT_SECRET_POST, CLIENT_SECRET_JWT, PRIVATE_KEY_JWT
	
    require_pkce BOOLEAN NOT NULL DEFAULT TRUE,
	pkce_code_challenge_method TEXT DEFAULT 'S256',	-- ENUM --> PLAIN, S256
	require_consent BOOLEAN NOT NULL DEFAULT TRUE,

	redirect_uris TEXT[] NOT NULL,
	post_logout_redirect_uris TEXT[],
	
	allowed_grant_types TEXT[],
	allowed_response_types TEXT[],
	
    access_token_lifetime INT NOT NULL DEFAULT 3600,
    refresh_token_lifetime INT NOT NULL DEFAULT 2592000,

	logo_uri TEXT,                 -- client logo URL (OIDC client registration)
	client_uri TEXT,               -- client homepage URL (OIDC client registration)
	jwks_uri TEXT,                 -- JWKS endpoint for PRIVATE_KEY_JWT auth method
	jwks TEXT,                     -- inline JWKS (alternative to jwks_uri)

	client_secret_expires_at TIMESTAMPTZ,
	
	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,
	
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,

	-- Primary Key
    CONSTRAINT pk_mt_authorization_clients PRIMARY KEY (id),

    -- Foreign Key
	CONSTRAINT fk_mt_authorization_clients_mt_tenants FOREIGN KEY (tenant_id)
	REFERENCES public.mt_tenants(id) ON DELETE SET NULL,
	
    CONSTRAINT chk_mt_authorization_clients_access_token_lifetime 
        CHECK (access_token_lifetime > 0 AND access_token_lifetime <= 86400),
	-- OAuth2.1 recommends short-lived access tokens (≤ 900 s). The application layer
	-- caps issuance at 900 s; the DB enforces an absolute ceiling of 86 400 s (24 h).

    CONSTRAINT chk_mt_authorization_clients_refresh_token_lifetime 
        CHECK (refresh_token_lifetime > 0),

	CONSTRAINT chk_mt_authorization_clients_client_type
	CHECK (
	    client_type IN (
	        'CONFIDENTIAL',
	        'PUBLIC'
	    )
	),	

	CONSTRAINT chk_mt_authorization_clients_token_endpoint_auth_method
	CHECK (
	    token_endpoint_auth_method IN (
		    'NONE',                -- PUBLIC clients (OAuth2.1 §2.1 — no client secret)
	        'CLIENT_SECRET_BASIC', -- HTTP Basic auth header
	        'CLIENT_SECRET_POST',  -- credentials in request body
			'CLIENT_SECRET_JWT',   -- JWT signed with shared secret
			'PRIVATE_KEY_JWT'      -- JWT signed with private key (RFC 7523)
	    )
	),	

	CONSTRAINT chk_mt_authorization_clients_pkce_code_challenge_method
	CHECK (
	    pkce_code_challenge_method IN (
	        'PLAIN',
	        'S256'
	    )
	),

	CONSTRAINT chk_mt_authorization_clients_allowed_grant_types
	CHECK (
	    allowed_grant_types IS NULL OR
	    allowed_grant_types <@ ARRAY[
	        'AUTHORIZATION_CODE',   -- OAuth2.1 §4.1
	        'CLIENT_CREDENTIALS',   -- OAuth2.1 §4.2
	        'REFRESH_TOKEN',        -- OAuth2.1 §6
	        'DEVICE_CODE',          -- RFC 8628
	        'JWT_BEARER'            -- RFC 7523
	        -- IMPLICIT removed: OAuth2.1 §2.1 prohibits implicit grant
	        -- PASSWORD removed: OAuth2.1 §2.1 prohibits ROPC grant
	    ]::TEXT[]
	),

	CONSTRAINT chk_mt_authorization_clients_allowed_response_types
	CHECK (
	    allowed_response_types IS NULL OR
	    allowed_response_types <@ ARRAY[
	        'code',
	        'token',
	        'id_token',
	        'code token',
	        'code id_token',
	        'token id_token',
	        'code token id_token'
	    ]::TEXT[]
	)
);

-- Unique
CREATE UNIQUE INDEX uq_mt_authorization_clients_client_id ON public.mt_authorization_clients(client_id) WHERE deleted_at IS NULL;

-- Index
-- query client_id + is_active
CREATE INDEX idx_mt_authorization_clients_client_is_active ON public.mt_authorization_clients (client_id, is_active) WHERE deleted_at IS NULL;

-- filter is_active
CREATE INDEX idx_mt_authorization_clients_is_active ON public.mt_authorization_clients (is_active) WHERE deleted_at IS NULL;

-- filter created_at
CREATE INDEX idx_mt_authorization_clients_created_at ON public.mt_authorization_clients (created_at DESC) WHERE deleted_at IS NULL;

-- filter tenant_id
CREATE INDEX idx_mt_authorization_clients_tenant_id ON public.mt_authorization_clients (tenant_id) WHERE deleted_at IS NULL;


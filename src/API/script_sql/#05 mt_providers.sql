----------------------------------------------------------------------------------------------------------------------------------
--#table 05
CREATE TABLE public.mt_providers (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    provider_code TEXT NOT NULL, -- UNIQUE
    provider_name JSONB NOT NULL,
    protocol TEXT, -- ENUM --> OAUTH2, OIDC, SAML, OTHER

    client_id TEXT,
    client_secret_encrypt TEXT,
    scopes TEXT[] NOT NULL DEFAULT '{}',

	issuer TEXT,
	authorization_url TEXT,
	jwks_uri TEXT,
	token_url TEXT,
	userinfo_url TEXT,
	discovery_url TEXT,
	end_session_endpoint TEXT,
	callback_url TEXT,         -- redirect/callback URL registered at this provider
	pkce_supported BOOLEAN NOT NULL DEFAULT FALSE, -- whether provider supports PKCE
	
	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,

	-- Primary Key
    CONSTRAINT pk_mt_providers PRIMARY KEY (id),
	
	CONSTRAINT chk_mt_providers_protocol
	CHECK (protocol IN (
	'OAUTH2',
	'OIDC',
	'SAML',
	'OTHER'
	))		
);

-- Unique
CREATE UNIQUE INDEX uq_mt_providers_provider_code ON public.mt_providers (LOWER(provider_code)) WHERE deleted_at IS NULL;

-- Index
-- filter is_active
CREATE INDEX idx_mt_providers_is_active ON public.mt_providers (is_active) WHERE deleted_at IS NULL;

-- filter created_at
CREATE INDEX idx_mt_providers_created_at ON public.mt_providers (created_at DESC) WHERE deleted_at IS NULL;


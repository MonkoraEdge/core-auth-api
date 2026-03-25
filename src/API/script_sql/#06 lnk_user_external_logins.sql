----------------------------------------------------------------------------------------------------------------------------------
--#table 06
CREATE TABLE public.lnk_user_external_logins (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL, -- FK mt_users UNIQUE(+2)

    provider_id UUID NOT NULL, --FK mt_providers -- UNIQUE(+1) UNIQUE(+2)
    provider_user_id TEXT NOT NULL, -- sub / facebook id / line userId  -- UNIQUE(+1)
    provider_display_name TEXT,
	provider_email TEXT,

    access_token_encrypt TEXT, -- optional, encrypted
	refresh_token_encrypt TEXT, -- optional, encrypted
	token_expires_at TIMESTAMPTZ,
	scopes TEXT[], -- scopes granted by the external provider	

	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,

	-- Primary Key
    CONSTRAINT pk_lnk_user_external_logins PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_lnk_user_external_logins_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE,

    CONSTRAINT fk_lnk_user_external_logins_mt_providers FOREIGN KEY (provider_id)
	REFERENCES public.mt_providers(id) ON DELETE RESTRICT
);

-- Unique
CREATE UNIQUE INDEX uq_lnk_user_external_logins_provider_provider_user_id ON public.lnk_user_external_logins (provider_id, provider_user_id) WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX uq_lnk_user_external_logins_provider_user_id ON public.lnk_user_external_logins (provider_id, user_id) WHERE deleted_at IS NULL;

-- Index
-- query user_id + is_active
CREATE INDEX idx_lnk_user_external_logins_user_is_active ON public.lnk_user_external_logins (user_id, is_active);

-- query user_id + created_at
CREATE INDEX idx_lnk_user_external_logins_user_created_at ON public.lnk_user_external_logins (user_id, created_at DESC);

-- filter user_id
CREATE INDEX idx_lnk_user_external_logins_user_id ON public.lnk_user_external_logins (user_id);

-- filter is_active
CREATE INDEX idx_lnk_user_external_logins_is_active ON public.lnk_user_external_logins (is_active);

-- filter created_at
CREATE INDEX idx_lnk_user_external_logins_created_at ON public.lnk_user_external_logins (created_at DESC);


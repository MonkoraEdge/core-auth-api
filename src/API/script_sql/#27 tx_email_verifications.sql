----------------------------------------------------------------------------------------------------------------------------------
--#table 27
CREATE TABLE public.tx_email_verifications (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
	
    user_id UUID NOT NULL, --FK mt_users
    email TEXT NOT NULL,   -- the email address being verified (snapshot at time of request)
    verification_type TEXT NOT NULL DEFAULT 'REGISTRATION', -- ENUM --> REGISTRATION, EMAIL_CHANGE

    token_hash TEXT NOT NULL, -- UNIQUE
    expires_at TIMESTAMPTZ NOT NULL,
    verified_at TIMESTAMPTZ,

	description JSONB,
	
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,	

	-- Primary Key
    CONSTRAINT pk_tx_email_verifications PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_tx_email_verifications_user_id_mt_users_id FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE,

    CONSTRAINT chk_tx_email_verifications_verification_type
    CHECK (verification_type IN ('REGISTRATION', 'EMAIL_CHANGE'))
);

-- Unique
CREATE UNIQUE INDEX uq_tx_email_verifications_token ON public.tx_email_verifications(token_hash);

-- Index
-- query user_id + created_at
CREATE INDEX idx_tx_email_verifications_user_created ON public.tx_email_verifications(user_id, created_at DESC);

-- filter user_id
CREATE INDEX idx_tx_email_verifications_user_id ON public.tx_email_verifications (user_id);

-- filter created_at
CREATE INDEX idx_tx_email_verifications_created_at ON public.tx_email_verifications (created_at DESC);

-- filter expires_at
CREATE INDEX idx_tx_email_verifications_expires_at ON public.tx_email_verifications (expires_at);



----------------------------------------------------------------------------------------------------------------------------------
--#table 22
CREATE TABLE public.audit_logs (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
	
	client_id UUID, -- FK mt_authorization_clients (nullable: system events may have no client)
    user_id UUID, -- FK mt_users (nullable: pre-auth or system events)
	session_id UUID, -- FK tx_user_sessions (nullable: API key or system events)
	
    actor_type TEXT NOT NULL, --ENUM --> USER, CLIENT, SYSTEM
	action TEXT NOT NULL,
    entity_name TEXT,
    entity_id UUID,

    ip_address INET,
    user_agent TEXT,
	result TEXT NOT NULL, --ENUM --> SUCCESS, FAILURE, DENIED
	
	metadata JSONB,
	description JSONB,	

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,	

	-- Primary Key
	CONSTRAINT pk_audit_logs PRIMARY KEY (id),

    CONSTRAINT fk_audit_logs_mt_authorization_clients FOREIGN KEY (client_id)
	REFERENCES public.mt_authorization_clients(id) ON DELETE SET NULL,

    CONSTRAINT fk_audit_logs_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE SET NULL,	

    CONSTRAINT fk_audit_logs_tx_user_sessions FOREIGN KEY (session_id)
	REFERENCES public.tx_user_sessions(id) ON DELETE SET NULL,

	CONSTRAINT chk_audit_logs_actor_type
	CHECK (
	    actor_type IN (
	        'USER',
	        'CLIENT',
			'SYSTEM'
	    )
	),

	CONSTRAINT chk_audit_logs_result
	CHECK (
	    result IN (
	        'SUCCESS',
	        'FAILURE',
			'DENIED'
	    )
	)	
);

-- Index
-- query user_id + created_at
CREATE INDEX idx_audit_logs_user_created_at ON public.audit_logs (user_id, created_at DESC);

-- query user_id + client_id
CREATE INDEX idx_audit_logs_user_client_id ON public.audit_logs (user_id, client_id);

-- query user_id + session_id
CREATE INDEX idx_audit_logs_user_session_id ON public.audit_logs (user_id, session_id);

-- filter user_id
CREATE INDEX idx_audit_logs_user_id ON public.audit_logs (user_id);

-- filter session_id
CREATE INDEX idx_audit_logs_session_id ON public.audit_logs (session_id);

-- filter created_at
CREATE INDEX idx_audit_logs_created_at ON public.audit_logs (created_at DESC);

-- filter client_id
CREATE INDEX idx_audit_logs_client_id ON public.audit_logs (client_id);

-- filter entity_name  + entity_id
CREATE INDEX idx_audit_logs_entity ON public.audit_logs(entity_name, entity_id);

-- filter result
CREATE INDEX idx_audit_logs_result ON public.audit_logs(result);


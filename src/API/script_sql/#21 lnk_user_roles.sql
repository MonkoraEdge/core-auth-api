----------------------------------------------------------------------------------------------------------------------------------
--#table 21
CREATE TABLE public.lnk_user_roles (
	id UUID NOT NULL DEFAULT gen_random_uuid(),
	
    user_id UUID NOT NULL,
    role_id UUID NOT NULL,

	expires_at TIMESTAMPTZ,      -- when this role assignment expires (NULL = permanent)
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,

	-- Primary Key
    CONSTRAINT pk_lnk_user_roles PRIMARY KEY (id),

    -- Unique
    CONSTRAINT uq_lnk_user_roles_mt_users_id_role_id UNIQUE (user_id, role_id),	

    CONSTRAINT fk_lnk_user_roles_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE,

    CONSTRAINT fk_lnk_user_roles_mt_roles FOREIGN KEY (role_id)
	REFERENCES public.mt_roles(id) ON DELETE CASCADE
);

-- Index
-- filter created_at
CREATE INDEX idx_lnk_user_roles_created_at ON public.lnk_user_roles (created_at DESC);

-- filter user_id
CREATE INDEX idx_lnk_user_roles_user_id ON public.lnk_user_roles (user_id);

CREATE INDEX idx_lnk_user_roles_role_id ON public.lnk_user_roles(role_id);

-- filter expires_at (find expired role assignments for cleanup)
CREATE INDEX idx_lnk_user_roles_expires_at ON public.lnk_user_roles (expires_at) WHERE expires_at IS NOT NULL;


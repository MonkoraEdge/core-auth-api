----------------------------------------------------------------------------------------------------------------------------------
--#table 20
CREATE TABLE public.lnk_role_permissions (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
	
    role_id UUID NOT NULL, -- FK mt_roles 
    permission_id UUID NOT NULL, --FK mt_permissions 
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,	

	-- Primary Key
    CONSTRAINT pk_lnk_role_permissions PRIMARY KEY (id),

    -- Unique
    CONSTRAINT uq_lnk_role_permissions_role_permission UNIQUE (role_id, permission_id),	

    CONSTRAINT fk_lnk_role_permissions_role FOREIGN KEY (role_id)
	REFERENCES public.mt_roles(id) ON DELETE CASCADE,

    CONSTRAINT fk_lnk_role_permissions_permission FOREIGN KEY (permission_id)
	REFERENCES public.mt_permissions(id) ON DELETE CASCADE
);

-- Index
-- filter created_at
CREATE INDEX idx_lnk_role_permissions_created_at ON public.lnk_role_permissions (created_at DESC);

-- filter role_id
CREATE INDEX idx_lnk_role_permissions_role_id ON public.lnk_role_permissions(role_id);

-- filter permission_id
CREATE INDEX idx_lnk_role_permissions_permission_id ON public.lnk_role_permissions(permission_id);


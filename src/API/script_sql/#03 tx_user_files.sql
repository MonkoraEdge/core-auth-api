----------------------------------------------------------------------------------------------------------------------------------
--#table 03 tx_user_files
CREATE TABLE public.tx_user_files (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL, -- FK mt_users

    file_mime_type TEXT, -- ENUM --> IMAGE/JPEG, IMAGE/PNG, IMAGE/WEBP, IMAGE/GIF, APPLICATION/PDF, 
	-- APPLICATION/VND.OPENXMLFORMATS-OFFICEDOCUMENT.WORDPROCESSINGML.DOCUMENT, 
	-- APPLICATION/VND.OPENXMLFORMATS-OFFICEDOCUMENT.SPREADSHEETML.SHEET, 
	-- APPLICATION/ZIP, TEXT/PLAIN, TEXT/CSV, VIDEO/MP4, AUDIO/MPEG, UNKNOWN
    file_name JSONB NOT NULL,
    file_url TEXT NOT NULL,
	file_type TEXT NOT NULL DEFAULT 'OTHER', -- ENUM --> PROFILE, KYC, DOCUMENT, CONTRACT, RECEIPT, PAYMENT_SLIP, CERTIFICATE, MEDIA, ATTACHMENT, OTHER
	file_size INTEGER,

	is_active BOOLEAN NOT NULL DEFAULT FALSE,
	description JSONB,

	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	created_by TEXT NOT NULL DEFAULT 'SYSTEM',
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	deleted_at TIMESTAMPTZ,
	deleted_by TEXT,

	-- Primary Key
    CONSTRAINT pk_tx_user_files PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_tx_user_files_mt_users FOREIGN KEY (user_id)
	REFERENCES public.mt_users(id) ON DELETE CASCADE,

	CONSTRAINT chk_tx_user_files_file_mime_type
	CHECK (file_mime_type IN (
	'IMAGE/JPEG',
	'IMAGE/PNG',
	'IMAGE/WEBP',
	'IMAGE/GIF',
	'APPLICATION/PDF',
	'APPLICATION/VND.OPENXMLFORMATS-OFFICEDOCUMENT.WORDPROCESSINGML.DOCUMENT',
	'APPLICATION/VND.OPENXMLFORMATS-OFFICEDOCUMENT.SPREADSHEETML.SHEET',
	'APPLICATION/ZIP',
	'TEXT/PLAIN',
	'TEXT/CSV',
	'VIDEO/MP4',
	'AUDIO/MPEG',
	'UNKNOWN'
	)),

	CONSTRAINT chk_tx_user_files_file_type
	CHECK (file_type IN (
	'PROFILE',
	'KYC',
	'DOCUMENT',
	'CONTRACT',
	'RECEIPT',
	'PAYMENT_SLIP',
	'CERTIFICATE',
	'MEDIA',
	'ATTACHMENT',
	'OTHER'
	))	
);

-- Unique
CREATE UNIQUE INDEX uq_tx_user_files_user_id ON public.tx_user_files (user_id) WHERE file_type = 'PROFILE' AND is_active = TRUE;

-- Index
-- query user_id + is_active
CREATE INDEX idx_tx_user_files_user_is_active ON public.tx_user_files (user_id, is_active);

-- query user_id + created_at
CREATE INDEX idx_tx_user_files_user_created_at ON public.tx_user_files (user_id, created_at DESC);

-- query user_id + file_mime_type
CREATE INDEX idx_tx_user_files_user_file_mime_type ON public.tx_user_files (user_id, file_mime_type);

-- query user_id + file_type
CREATE INDEX idx_tx_user_files_user_file_type ON public.tx_user_files (user_id, file_type);

-- filter user_id
CREATE INDEX idx_tx_user_files_user_id ON public.tx_user_files (user_id);

-- filter is_active
CREATE INDEX idx_tx_user_files_is_active ON public.tx_user_files (is_active);

-- filter created_at
CREATE INDEX idx_tx_user_files_created_at ON public.tx_user_files (created_at DESC);

-- filter file_mime_type
CREATE INDEX idx_tx_user_files_file_mime_type ON public.tx_user_files (file_mime_type);

-- filter file_type
CREATE INDEX idx_tx_user_files_file_type ON public.tx_user_files (file_type);


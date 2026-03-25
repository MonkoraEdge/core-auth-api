----------------------------------------------------------------------------------------------------------------------------------
--#table 32 tx_agreement_accepts
-- เก็บประวัติการยอมรับ หรือ ถอนความยินยอม ของผู้ใช้ต่อเอกสารแต่ละฉบับ
-- ใช้สำหรับ PDPA compliance: ต้องแสดงได้ว่า ใคร ยอมรับอะไร เมื่อไหร่ อย่างไร จาก IP ไหน
-- ON DELETE RESTRICT บน agreement_id ป้องกันการลบ agreement ที่มีประวัติการยอมรับแล้ว
CREATE TABLE public.tx_agreement_accepts (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    user_id UUID NOT NULL,       -- FK mt_users
    agreement_id UUID NOT NULL,  -- FK mt_agreements
    client_id UUID,              -- FK mt_authorization_clients (NULL = platform-level, ไม่ผ่าน OAuth client)
    session_id UUID,             -- FK tx_user_sessions (NULL = ตอน registration ยังไม่มี session)

    accepted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    withdrawn_at TIMESTAMPTZ,
    -- เมื่อผู้ใช้ถอนความยินยอม (PDPA มาตรา 19 สิทธิถอนความยินยอม)
    -- withdrawn_at IS NOT NULL → ถือว่าไม่มีผลแล้ว
    -- ห้ามลบแถว เพราะต้องเก็บ audit trail ของการถอน ด้วย

    withdrawn_reason TEXT,
    -- เหตุผลที่ผู้ใช้ถอนความยินยอม (optional, เก็บเพื่อ analytics / compliance)

    acceptance_method TEXT NOT NULL DEFAULT 'CHECKBOX',
    -- ENUM:
    -- CHECKBOX      = ผู้ใช้ tick checkbox เอง (PDPA-compliant สำหรับ opt-in)
    -- BUTTON_CLICK  = ผู้ใช้กดปุ่มยืนยัน (เช่น "ฉันยอมรับ")
    -- IMPLICIT      = implicit consent (เช่น cookie ที่จำเป็น — ไม่ต้องขอ explicit)
    -- API           = ยอมรับผ่าน API call (machine-to-machine หรือ import user)

    ip_address INET,   -- IP ที่ยอมรับ (บันทึกตาม PDPA requirement)
    user_agent TEXT,   -- browser/device ที่ใช้ตอนยอมรับ

    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    description JSONB,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by TEXT NOT NULL DEFAULT 'SYSTEM',
    updated_at TIMESTAMPTZ,
    updated_by TEXT,

    -- Primary Key
    CONSTRAINT pk_tx_agreement_accepts PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_tx_agreement_accepts_mt_users FOREIGN KEY (user_id)
    REFERENCES public.mt_users(id) ON DELETE CASCADE,

    CONSTRAINT fk_tx_agreement_accepts_mt_agreements FOREIGN KEY (agreement_id)
    REFERENCES public.mt_agreements(id) ON DELETE RESTRICT,
    -- RESTRICT: ห้ามลบ agreement ที่มีผู้ยอมรับแล้ว ต้อง soft-delete แทน

    CONSTRAINT fk_tx_agreement_accepts_mt_authorization_clients FOREIGN KEY (client_id)
    REFERENCES public.mt_authorization_clients(id) ON DELETE SET NULL,

    CONSTRAINT fk_tx_agreement_accepts_tx_user_sessions FOREIGN KEY (session_id)
    REFERENCES public.tx_user_sessions(id) ON DELETE SET NULL,

    CONSTRAINT chk_tx_agreement_accepts_acceptance_method
    CHECK (acceptance_method IN (
        'CHECKBOX',
        'BUTTON_CLICK',
        'IMPLICIT',
        'API'
    ))
);

-- Unique: user มี active acceptance ได้แค่ 1 รายการต่อ agreement version
-- (ถ้าถอนแล้ว withdrawn_at IS NOT NULL → slot ว่าง สามารถยอมรับใหม่ได้)
CREATE UNIQUE INDEX uq_tx_agreement_accepts_user_agreement ON public.tx_agreement_accepts (user_id, agreement_id) WHERE withdrawn_at IS NULL;

-- Index
-- query user_id + accepted_at (ดู history การยอมรับของ user)
CREATE INDEX idx_tx_agreement_accepts_user_accepted_at ON public.tx_agreement_accepts (user_id, accepted_at DESC);

-- query user_id + agreement_id (ตรวจว่า user ยอมรับ agreement นี้หรือยัง)
CREATE INDEX idx_tx_agreement_accepts_user_agreement_id ON public.tx_agreement_accepts (user_id, agreement_id);

-- filter user_id
CREATE INDEX idx_tx_agreement_accepts_user_id ON public.tx_agreement_accepts (user_id);

-- filter agreement_id (ดูว่ามีใครยอมรับ agreement นี้บ้าง)
CREATE INDEX idx_tx_agreement_accepts_agreement_id ON public.tx_agreement_accepts (agreement_id);

-- filter client_id
CREATE INDEX idx_tx_agreement_accepts_client_id ON public.tx_agreement_accepts (client_id);

-- filter session_id
CREATE INDEX idx_tx_agreement_accepts_session_id ON public.tx_agreement_accepts (session_id);

-- filter withdrawn_at (หา active consents: WHERE withdrawn_at IS NULL)
CREATE INDEX idx_tx_agreement_accepts_withdrawn_at ON public.tx_agreement_accepts (withdrawn_at);

-- filter created_at
CREATE INDEX idx_tx_agreement_accepts_created_at ON public.tx_agreement_accepts (created_at DESC);

----------------------------------------------------------------------------------------------------------------------------------
--#table 31 mt_agreements
-- เก็บเอกสาร นโยบาย / ข้อตกลง / ความยินยอม ทุกประเภทที่ผู้ใช้ต้องยอมรับ
-- รองรับหลาย version และหลาย tenant
-- ใช้คู่กับ #32 tx_agreement_accepts เพื่อเก็บประวัติการยอมรับหรือถอนความยินยอมตาม PDPA
CREATE TABLE public.mt_agreements (
    id UUID NOT NULL DEFAULT gen_random_uuid(),

    tenant_id UUID, -- FK mt_tenants (NULL = ระดับ platform / ใช้ทุก tenant)

    agreement_code TEXT NOT NULL,
    -- UNIQUE identifier เช่น: PRIVACY_POLICY_v1, PDPA_CONSENT_v2, TERMS_OF_SERVICE_v1
    -- ใช้ reference ใน code / API

    agreement_type TEXT NOT NULL,
    -- ENUM:
    -- TERMS_OF_SERVICE         = ข้อกำหนดการใช้งาน
    -- PRIVACY_POLICY           = นโยบายความเป็นส่วนตัว
    -- PDPA_CONSENT             = ความยินยอมเก็บ/ใช้/เปิดเผยข้อมูลส่วนบุคคล (PDPA มาตรา 19)
    -- COOKIE_POLICY            = นโยบาย Cookie
    -- MARKETING_CONSENT        = ความยินยอมรับการตลาด/โปรโมชัน
    -- DATA_PROCESSING_AGREEMENT = สัญญาประมวลผลข้อมูล (DPA สำหรับ B2B)
    -- OTHER                    = อื่นๆ

    title JSONB NOT NULL,    -- หัวเรื่อง multi-language {"th": "...", "en": "..."}
    content JSONB,           -- เนื้อหาเต็ม multi-language (เก็บ HTML/Markdown)
    summary JSONB,           -- สรุปสั้นๆ แสดงที่หน้า consent/registration

    version TEXT NOT NULL,              -- Semantic version เช่น 1.0.0, 2.1.0
    effective_at TIMESTAMPTZ NOT NULL,  -- วันที่มีผลบังคับใช้ version นี้
    expires_at TIMESTAMPTZ,             -- NULL = ไม่หมดอายุ (ถูกแทนที่ด้วย version ใหม่)

    is_required BOOLEAN NOT NULL DEFAULT TRUE,
    -- TRUE  = ต้องยอมรับเพื่อใช้บริการ (เช่น Privacy Policy, Terms of Service)
    -- FALSE = ไม่บังคับ สามารถปฏิเสธได้ (เช่น Marketing Consent)

    requires_explicit_action BOOLEAN NOT NULL DEFAULT TRUE,
    -- TRUE  = ผู้ใช้ต้องกดเอง ห้าม pre-tick (PDPA ห้าม bundled consent)
    -- FALSE = IMPLICIT (เช่น แค่ดำเนินการต่อถือว่ายอมรับ Cookie บางประเภท)

    is_active BOOLEAN NOT NULL DEFAULT FALSE,
    description JSONB,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by TEXT NOT NULL DEFAULT 'SYSTEM',
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    deleted_at TIMESTAMPTZ,
    deleted_by TEXT,

    -- Primary Key
    CONSTRAINT pk_mt_agreements PRIMARY KEY (id),

    -- Foreign Key
    CONSTRAINT fk_mt_agreements_mt_tenants FOREIGN KEY (tenant_id)
    REFERENCES public.mt_tenants(id) ON DELETE SET NULL,

    CONSTRAINT chk_mt_agreements_agreement_type
    CHECK (agreement_type IN (
        'TERMS_OF_SERVICE',
        'PRIVACY_POLICY',
        'PDPA_CONSENT',
        'COOKIE_POLICY',
        'MARKETING_CONSENT',
        'DATA_PROCESSING_AGREEMENT',
        'OTHER'
    ))
);

-- Unique: agreement_code ต้องไม่ซ้ำ (ระบุ version อยู่ใน code แล้ว เช่น PRIVACY_POLICY_v2)
CREATE UNIQUE INDEX uq_mt_agreements_code ON public.mt_agreements (agreement_code) WHERE deleted_at IS NULL;

-- Unique: tenant + agreement_type + version ไม่ซ้ำกัน
CREATE UNIQUE INDEX uq_mt_agreements_tenant_type_version ON public.mt_agreements (tenant_id, agreement_type, version) WHERE deleted_at IS NULL;

-- Index
-- filter is_active
CREATE INDEX idx_mt_agreements_is_active ON public.mt_agreements (is_active) WHERE deleted_at IS NULL;

-- filter created_at
CREATE INDEX idx_mt_agreements_created_at ON public.mt_agreements (created_at DESC) WHERE deleted_at IS NULL;

-- filter agreement_type
CREATE INDEX idx_mt_agreements_agreement_type ON public.mt_agreements (agreement_type) WHERE deleted_at IS NULL;

-- filter effective_at (หา version ที่มีผลใช้งาน)
CREATE INDEX idx_mt_agreements_effective_at ON public.mt_agreements (effective_at) WHERE deleted_at IS NULL;

-- filter tenant_id
CREATE INDEX idx_mt_agreements_tenant_id ON public.mt_agreements (tenant_id) WHERE deleted_at IS NULL;

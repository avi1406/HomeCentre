-- Table: public.uploadedfiledatastage

-- DROP TABLE IF EXISTS public.uploadedfiledatastage;

CREATE TABLE IF NOT EXISTS public.uploadedfiledatastage
(
    id bigint NOT NULL DEFAULT nextval('uploadedfiledatastage_id_seq'::regclass),
    createdby bigint,
    updatedby bigint,
    datecreated timestamp with time zone NOT NULL DEFAULT now(),
    datemodified timestamp with time zone NOT NULL DEFAULT now(),
    isactive boolean NOT NULL DEFAULT true,
    stageid bigint,
    uploadedfiledataid bigint,
    uploadedfiledatastagestatusid bigint,
    CONSTRAINT "PK_uploadedfiledatastage" PRIMARY KEY (id),
    CONSTRAINT "FK_uploadedfiledatastage_createdby_appuser_id" FOREIGN KEY (createdby)
        REFERENCES public.appuser (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE SET NULL,
    CONSTRAINT "FK_uploadedfiledatastage_stageid_stage_id" FOREIGN KEY (stageid)
        REFERENCES public.stage (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE SET NULL,
    CONSTRAINT "FK_uploadedfiledatastage_updatedby_appuser_id" FOREIGN KEY (updatedby)
        REFERENCES public.appuser (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE SET NULL,
    CONSTRAINT "FK_uploadedfiledatastage_uploadedfiledataid_uploadedfiledata_id" FOREIGN KEY (uploadedfiledataid)
        REFERENCES public.uploadedfiledata (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE SET NULL,
    CONSTRAINT "FK_uploadedfiledatastage_uploadedfiledatastagestatusid_uploaded" FOREIGN KEY (uploadedfiledatastagestatusid)
        REFERENCES public.uploadedfiledatastagestatus (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE SET NULL
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.uploadedfiledatastage
    OWNER to lmdev_admin;

-- Trigger: uploadedfiledatastage_updatedatemodifiedcolumn

-- DROP TRIGGER IF EXISTS uploadedfiledatastage_updatedatemodifiedcolumn ON public.uploadedfiledatastage;

CREATE TRIGGER uploadedfiledatastage_updatedatemodifiedcolumn
    BEFORE UPDATE 
    ON public.uploadedfiledatastage
    FOR EACH ROW
    EXECUTE FUNCTION public.update_modified_column();
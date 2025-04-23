-- Table: public.uploadedfiledatastagestatus

-- DROP TABLE IF EXISTS public.uploadedfiledatastagestatus;

CREATE TABLE IF NOT EXISTS public.uploadedfiledatastagestatus
(
    id bigint NOT NULL DEFAULT nextval('uploadedfiledatastagestatus_id_seq'::regclass),
    createdby bigint,
    updatedby bigint,
    datecreated timestamp with time zone NOT NULL DEFAULT now(),
    datemodified timestamp with time zone NOT NULL DEFAULT now(),
    isactive boolean NOT NULL DEFAULT true,
    name text COLLATE pg_catalog."default" NOT NULL,
    description text COLLATE pg_catalog."default",
    CONSTRAINT "PK_uploadedfiledatastagestatus" PRIMARY KEY (id),
    CONSTRAINT "FK_uploadedfiledatastagestatus_createdby_appuser_id" FOREIGN KEY (createdby)
        REFERENCES public.appuser (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE SET NULL,
    CONSTRAINT "FK_uploadedfiledatastagestatus_updatedby_appuser_id" FOREIGN KEY (updatedby)
        REFERENCES public.appuser (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE SET NULL
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.uploadedfiledatastagestatus
    OWNER to lmdev_admin;

-- Trigger: uploadedfiledatastagestatus_updatedatemodifiedcolumn

-- DROP TRIGGER IF EXISTS uploadedfiledatastagestatus_updatedatemodifiedcolumn ON public.uploadedfiledatastagestatus;

CREATE TRIGGER uploadedfiledatastagestatus_updatedatemodifiedcolumn
    BEFORE UPDATE 
    ON public.uploadedfiledatastagestatus
    FOR EACH ROW
    EXECUTE FUNCTION public.update_modified_column();
-- Table: public.templatestagetype

-- DROP TABLE IF EXISTS public.templatestagetype;

CREATE SEQUENCE templatestagetype_id_seq START 1;
CREATE TABLE IF NOT EXISTS public.templatestagetype
(
    id bigint NOT NULL DEFAULT nextval('templatestagetype_id_seq'::regclass),
    createdby bigint,
    updatedby bigint,
    datecreated timestamp with time zone NOT NULL DEFAULT now(),
    datemodified timestamp with time zone NOT NULL DEFAULT now(),
    isactive boolean NOT NULL DEFAULT true,
    name text COLLATE pg_catalog."default" NOT NULL,
    description text COLLATE pg_catalog."default",
    CONSTRAINT "PK_templatestagetype" PRIMARY KEY (id),
    CONSTRAINT "FK_templatestagetype_createdby_appuser_id" FOREIGN KEY (createdby)
        REFERENCES public.appuser (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE SET NULL,
    CONSTRAINT "FK_templatestagetype_updatedby_appuser_id" FOREIGN KEY (updatedby)
        REFERENCES public.appuser (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE SET NULL
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.templatestagetype
    OWNER to lmdev_admin;

-- Trigger: templatestagetype_updatedatemodifiedcolumn

-- DROP TRIGGER IF EXISTS templatestagetype_updatedatemodifiedcolumn ON public.templatestagetype;

CREATE TRIGGER templatestagetype_updatedatemodifiedcolumn
    BEFORE UPDATE 
    ON public.templatestagetype
    FOR EACH ROW
    EXECUTE FUNCTION public.update_modified_column();
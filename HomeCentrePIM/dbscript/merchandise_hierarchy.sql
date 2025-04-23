-- Table: public.merchandise_hierarchy

-- DROP TABLE IF EXISTS public.merchandise_hierarchy;

CREATE TABLE IF NOT EXISTS public.merchandise_hierarchy
(
    business bigint,
    business_name text COLLATE pg_catalog."default",
    division bigint,
    div_name text COLLATE pg_catalog."default",
    group_no bigint,
    group_name text COLLATE pg_catalog."default",
    dept bigint,
    dept_name text COLLATE pg_catalog."default",
    class bigint,
    class_name text COLLATE pg_catalog."default",
    subclass bigint,
    sub_name text COLLATE pg_catalog."default",
    active_ind boolean
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.merchandise_hierarchy
    OWNER to pim_prod_admin;
CREATE OR REPLACE FUNCTION public.update_modified_column()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$

BEGIN
    NEW.DateModified = now();
    RETURN NEW;
END;
$BODY$;

ALTER FUNCTION public.update_modified_column()
    OWNER TO lmdev_admin;

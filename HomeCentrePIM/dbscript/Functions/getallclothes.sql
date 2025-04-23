CREATE OR REPLACE FUNCTION public.getallclothes(
	)
    RETURNS tablemetadata
    LANGUAGE 'sql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$

SELECT * FROM tablemetadata;

$BODY$;

ALTER FUNCTION public.getallclothes()
    OWNER TO lmdev_admin;

CREATE OR REPLACE FUNCTION public.getalltablemetadata(
	)
    RETURNS tablemetadata
    LANGUAGE 'sql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$

SELECT * FROM tablemetadata;

$BODY$;

ALTER FUNCTION public.getalltablemetadata()
    OWNER TO lmdev_admin;

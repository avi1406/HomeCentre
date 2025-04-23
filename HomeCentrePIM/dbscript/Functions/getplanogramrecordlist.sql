CREATE OR REPLACE FUNCTION public.getplanogramrecordlist(
	)
    RETURNS uidinitiation
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
BEGIN
    select * from vwplangramrecordlist;
END;
$BODY$;

ALTER FUNCTION public.getplanogramrecordlist()
    OWNER TO lmdev_admin;
